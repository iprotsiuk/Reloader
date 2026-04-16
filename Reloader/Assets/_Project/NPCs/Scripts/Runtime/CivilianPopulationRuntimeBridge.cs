using System;
using System.Collections.Generic;
using Reloader.Contracts.Runtime;
using Reloader.Core.Runtime;
using Reloader.Core.Save;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.Combat;
using Reloader.NPCs.Generation;
using Reloader.NPCs.Runtime.Capabilities;
using Reloader.Weapons.World;
using UnityEngine;

namespace Reloader.NPCs.Runtime
{
    public sealed class CivilianPopulationRuntimeBridge : MonoBehaviour, ISaveRuntimeBridge, IContractTargetEliminationSink
    {
        private const float MondayRefreshTimeOfDay = 8f;
        private const float ProceduralContractTargetDistanceMeters = 85f;
        private const float ProceduralContractTargetHealth = 15f;
        private const int ProceduralContractPayout = 1500;

        [SerializeField] private CivilianAppearanceLibrary _appearanceLibrary;
        [SerializeField] private GameObject _npcActorPrefab;
        [SerializeField] private MainTownPopulationDefinition _populationDefinition;
        [SerializeField] private int _initialPopulationCount;
        [SerializeField] private string _civilianIdPrefix = "citizen.mainTown";
        [SerializeField] private MainTownPopulationHabitat _spawnHabitat = MainTownPopulationHabitat.Any;
        [SerializeField] private string[] _spawnAnchorIds = Array.Empty<string>();
        [SerializeField] private string[] _contractTargetAnchorIds = Array.Empty<string>();
        [SerializeField] private MonoBehaviour _contractRuntimeProviderBehaviour;
        [SerializeField] private MonoBehaviour _crimeReporterBehaviour;

        private readonly CivilianPopulationRuntimeState _runtime = new CivilianPopulationRuntimeState();
        private readonly CivilianAppearanceGenerator _generator = new CivilianAppearanceGenerator();
        private CoreWorldController _coreWorldController;
        private CoreWorldController _subscribedCoreWorldController;
        private IContractRuntimeProvider _contractRuntimeProvider;
        private IContractTargetEliminationSink _contractTargetEliminationSink;
        private ILawEnforcementCrimeReporter _crimeReporter;
        private AssassinationContractDefinition _proceduralAvailableContract;
        private int _lastObservedWorldDayCount = -1;
        private float _lastObservedWorldTimeOfDay = -1f;

        internal readonly struct PoliceResponderDispatchCandidate
        {
            public PoliceResponderDispatchCandidate(
                CivilianPopulationRuntimeBridge bridge,
                string civilianId,
                string populationSlotId,
                float distanceMeters)
            {
                Bridge = bridge;
                CivilianId = civilianId ?? string.Empty;
                PopulationSlotId = populationSlotId ?? string.Empty;
                DistanceMeters = distanceMeters;
            }

            public CivilianPopulationRuntimeBridge Bridge { get; }
            public string CivilianId { get; }
            public string PopulationSlotId { get; }
            public float DistanceMeters { get; }
        }

        public CivilianPopulationRuntimeState Runtime => _runtime;
        public MainTownPopulationDefinition PopulationDefinition => _populationDefinition;

        public void SetCoreWorldController(CoreWorldController controller)
        {
            _coreWorldController = controller;
            SubscribeToCoreWorldController(_coreWorldController);
        }

        public void ConfigureContractRuntimeProvider(IContractRuntimeProvider contractRuntimeProvider)
        {
            _contractRuntimeProvider = contractRuntimeProvider;
            _contractRuntimeProviderBehaviour = contractRuntimeProvider as MonoBehaviour;
            _contractTargetEliminationSink = contractRuntimeProvider as IContractTargetEliminationSink;

            if (contractRuntimeProvider is ILawEnforcementCrimeReporter crimeReporter)
            {
                _crimeReporter = crimeReporter;
                _crimeReporterBehaviour = crimeReporter as MonoBehaviour;
            }

            RefreshSpawnedCivilianWitnessReporters();
            RefreshContractTargetDamageables();
        }

        public void ConfigureCrimeReporter(ILawEnforcementCrimeReporter crimeReporter)
        {
            _crimeReporter = crimeReporter;
            _crimeReporterBehaviour = crimeReporter as MonoBehaviour;
            RefreshSpawnedCivilianWitnessReporters();
        }

        private void Start()
        {
            SubscribeToCoreWorldController(ResolveCoreWorldController());
            EnsureRuntimePopulationInitializedForScene();
            RebuildScenePopulation();
        }

        private void OnEnable()
        {
            SaveRuntimeBridgeRegistry.Register(this);
            SubscribeToCoreWorldController(ResolveCoreWorldController());
        }

        private void OnDisable()
        {
            UnsubscribeFromCoreWorldController();
            SaveRuntimeBridgeRegistry.Unregister(this);
        }

        public void PrepareForSave(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            var module = ResolveModule(moduleRegistrations);
            if (module == null)
            {
                return;
            }

            HydrateRuntimeFromModuleIfNeeded(module);
            SeedInitialRosterIfNeeded(module);
            CopyRuntimeToModule(module);
        }

        public void FinalizeAfterLoad(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            var module = ResolveModule(moduleRegistrations);
            if (module == null)
            {
                return;
            }

            CopyModuleToRuntime(module);

            var replacedCount = 0;
            var coreWorldModule = ResolveCoreWorldModule(moduleRegistrations);
            if (coreWorldModule != null)
            {
                replacedCount = ExecutePendingReplacements(coreWorldModule.DayCount, coreWorldModule.TimeOfDay);
            }

            if (replacedCount == 0)
            {
                RebuildScenePopulation();
            }
        }

        public bool TryRetireCivilian(string civilianId, int retiredAtDay)
        {
            if (string.IsNullOrWhiteSpace(civilianId))
            {
                return false;
            }

            for (var i = 0; i < _runtime.Civilians.Count; i++)
            {
                var record = _runtime.Civilians[i];
                if (record == null || !string.Equals(record.CivilianId, civilianId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!record.IsAlive)
                {
                    return false;
                }

                record.IsAlive = false;
                record.IsContractEligible = false;
                record.RetiredAtDay = Math.Max(0, retiredAtDay);

                if (!HasPendingReplacement(civilianId))
                {
                    _runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                    {
                        VacatedCivilianId = record.CivilianId,
                        QueuedAtDay = record.RetiredAtDay,
                        SpawnAnchorId = record.SpawnAnchorId ?? string.Empty
                    });
                }

                return true;
            }

            return false;
        }

        public void RebuildScenePopulation()
        {
            ClearSpawnedScenePopulation();
            RefreshProceduralContractOffer();

            var trackedTargetId = ResolveTrackedContractTargetId();
            var trackedTargetRecord = FindCivilianById(trackedTargetId);

            for (var i = 0; i < _runtime.Civilians.Count; i++)
            {
                var record = _runtime.Civilians[i];
                if (!ShouldSpawnAmbientCivilian(record, trackedTargetId))
                {
                    continue;
                }

                var anchor = ResolveSpawnAnchor(record.SpawnAnchorId);
                if (anchor == null)
                {
                    continue;
                }

                SpawnPlaceholderCivilian(record, anchor);
            }

            SpawnDedicatedContractTarget(trackedTargetRecord);
            RefreshContractTargetDamageables();
        }

        public bool TryResolveSpawnedCivilian(string civilianId, out MainTownPopulationSpawnedCivilian civilian)
        {
            civilian = null;
            if (string.IsNullOrWhiteSpace(civilianId))
            {
                return false;
            }

            var spawned = GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            for (var i = 0; i < spawned.Length; i++)
            {
                var candidate = spawned[i];
                if (candidate != null && string.Equals(candidate.CivilianId, civilianId, StringComparison.Ordinal))
                {
                    civilian = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetCivilianAreaTag(string civilianId, out string areaTag)
        {
            areaTag = string.Empty;
            if (string.IsNullOrWhiteSpace(civilianId))
            {
                return false;
            }

            var record = FindCivilianById(civilianId);
            if (record == null || string.IsNullOrWhiteSpace(record.AreaTag))
            {
                return false;
            }

            areaTag = record.AreaTag;
            return true;
        }

        public int TrySpawnPoliceRespondersForDispatch(Vector3 selectionPoint, int desiredSpawnCount)
        {
            if (desiredSpawnCount <= 0)
            {
                return 0;
            }

            var candidates = new List<PoliceResponderDispatchCandidate>();
            CollectPoliceResponderDispatchCandidates(selectionPoint, candidates);

            if (candidates.Count == 0)
            {
                return 0;
            }

            candidates.Sort(CompareDispatchCandidates);

            var spawnedCount = 0;
            var targetCount = Mathf.Min(desiredSpawnCount, candidates.Count);
            for (var i = 0; i < targetCount; i++)
            {
                var candidate = candidates[i];
                if (TrySpawnPoliceResponderForDispatch(candidate.CivilianId))
                {
                    spawnedCount++;
                }
            }

            return spawnedCount;
        }

        internal void CollectPoliceResponderDispatchCandidates(Vector3 selectionPoint, List<PoliceResponderDispatchCandidate> candidates)
        {
            if (candidates == null || !isActiveAndEnabled)
            {
                return;
            }

            EnsureRuntimePopulationInitializedForScene();

            for (var i = 0; i < _runtime.Civilians.Count; i++)
            {
                var record = _runtime.Civilians[i];
                if (!IsDispatchReservePoliceRecord(record) || TryResolveSpawnedCivilian(record.CivilianId, out _))
                {
                    continue;
                }

                var anchor = ResolveSpawnAnchor(record.SpawnAnchorId);
                if (anchor == null)
                {
                    continue;
                }

                candidates.Add(new PoliceResponderDispatchCandidate(
                    this,
                    record.CivilianId,
                    record.PopulationSlotId,
                    PlanarDistance(anchor.position, selectionPoint)));
            }
        }

        internal bool TrySpawnPoliceResponderForDispatch(string civilianId)
        {
            if (!isActiveAndEnabled || string.IsNullOrWhiteSpace(civilianId))
            {
                return false;
            }

            EnsureRuntimePopulationInitializedForScene();

            var record = FindCivilianById(civilianId);
            if (!IsDispatchReservePoliceRecord(record) || TryResolveSpawnedCivilian(civilianId, out _))
            {
                return false;
            }

            var anchor = ResolveSpawnAnchor(record.SpawnAnchorId);
            if (anchor == null)
            {
                return false;
            }

            SpawnPlaceholderCivilian(record, anchor);
            return true;
        }

        public bool TryDespawnDispatchReservePolice(string civilianId)
        {
            if (!isActiveAndEnabled || string.IsNullOrWhiteSpace(civilianId))
            {
                return false;
            }

            EnsureRuntimePopulationInitializedForScene();

            var record = FindCivilianById(civilianId);
            if (!IsDispatchReservePoliceRecord(record) || IsActiveContractTarget(record))
            {
                return false;
            }

            if (!TryResolveSpawnedCivilian(civilianId, out var spawnedCivilian) || spawnedCivilian == null)
            {
                return false;
            }

            if (Application.isPlaying)
            {
                spawnedCivilian.transform.SetParent(null, false);
                spawnedCivilian.gameObject.SetActive(false);
                Destroy(spawnedCivilian.gameObject);
            }
            else
            {
                DestroyImmediate(spawnedCivilian.gameObject);
            }

            return true;
        }

        public void ReportContractTargetEliminated(string targetId, bool wasExposed)
        {
            var snapshot = ResolveCoreWorldController()?.CaptureSnapshot();
            var retiredAtDay = snapshot?.DayCount ?? 0;
            TryRetireCivilian(targetId, retiredAtDay);

            var targetEliminationSink = ResolveContractTargetEliminationSink();
            if (targetEliminationSink != null && !ReferenceEquals(targetEliminationSink, this))
            {
                targetEliminationSink.ReportContractTargetEliminated(targetId, wasExposed);
            }
        }

        public int ExecutePendingReplacements(int currentDay, float currentTimeOfDay)
        {
            if (_appearanceLibrary == null || _runtime.PendingReplacements.Count == 0)
            {
                return 0;
            }

            var normalizedDay = Math.Max(0, currentDay);
            var normalizedTimeOfDay = NormalizeTimeOfDay(currentTimeOfDay);
            var processedVacatedCivilianIds = new HashSet<string>(StringComparer.Ordinal);
            var processedPopulationSlotIds = new HashSet<string>(StringComparer.Ordinal);
            var occupiedLivePopulationSlotIds = CollectOccupiedLivePopulationSlotIds();
            var replacedCount = 0;
            for (var i = _runtime.PendingReplacements.Count - 1; i >= 0; i--)
            {
                var replacement = _runtime.PendingReplacements[i];
                if (replacement == null || !HasReachedMondayRefreshWindow(replacement, normalizedDay, normalizedTimeOfDay))
                {
                    continue;
                }

                if (!processedVacatedCivilianIds.Add(replacement.VacatedCivilianId))
                {
                    _runtime.PendingReplacements.RemoveAt(i);
                    continue;
                }

                var vacated = FindCivilianById(replacement.VacatedCivilianId);
                if (vacated == null || vacated.IsAlive)
                {
                    _runtime.PendingReplacements.RemoveAt(i);
                    continue;
                }

                if (occupiedLivePopulationSlotIds.Contains(vacated.PopulationSlotId))
                {
                    _runtime.PendingReplacements.RemoveAt(i);
                    continue;
                }

                if (!processedPopulationSlotIds.Add(vacated.PopulationSlotId))
                {
                    _runtime.PendingReplacements.RemoveAt(i);
                    continue;
                }

                var civilianId = CreateNextCivilianId(vacated.PopulationSlotId);
                var seed = ExtractCivilianNumericSuffix(civilianId);
                var isDispatchOnlyReserve = IsDispatchReservePoliceSlotId(vacated.PopulationSlotId);
                var isProtectedFromContracts = vacated.IsProtectedFromContracts || isDispatchOnlyReserve;
                _runtime.Civilians.Add(_generator.GenerateRecord(
                    _appearanceLibrary,
                    civilianId,
                    createdAtDay: normalizedDay,
                    vacated.SpawnAnchorId,
                    seed,
                    isContractEligible: !isProtectedFromContracts,
                    populationSlotId: vacated.PopulationSlotId,
                    poolId: vacated.PoolId,
                    areaTag: vacated.AreaTag,
                    isProtectedFromContracts: isProtectedFromContracts,
                    reservedPublicDisplayNames: CollectReservedPublicDisplayNames()));
                occupiedLivePopulationSlotIds.Add(vacated.PopulationSlotId);

                _runtime.PendingReplacements.RemoveAt(i);
                replacedCount++;
            }

            if (replacedCount > 0)
            {
                RebuildScenePopulation();
            }

            return replacedCount;
        }

        private void SeedInitialRosterIfNeeded(CivilianPopulationModule module)
        {
            if (_runtime.Civilians.Count > 0 || module.Civilians.Count > 0)
            {
                return;
            }

            if (_appearanceLibrary == null)
            {
                return;
            }

            if (_populationDefinition != null)
            {
                SeedRosterFromPopulationDefinition();
                return;
            }

            if (_initialPopulationCount <= 0)
            {
                return;
            }

            var anchorIds = NormalizeSpawnAnchors();
            if (anchorIds.Count == 0)
            {
                return;
            }

            var idPrefix = string.IsNullOrWhiteSpace(_civilianIdPrefix) ? "citizen.mainTown" : _civilianIdPrefix.Trim();
            for (var i = 0; i < _initialPopulationCount; i++)
            {
                var civilianId = $"{idPrefix}.{i + 1:0000}";
                var spawnAnchorId = anchorIds[i % anchorIds.Count];
                var seed = i + 1;
                _runtime.Civilians.Add(_generator.GenerateRecord(
                    _appearanceLibrary,
                    civilianId,
                    createdAtDay: 0,
                    spawnAnchorId,
                    seed,
                    isContractEligible: true,
                    populationSlotId: CreateFallbackPopulationSlotId(i),
                    poolId: "townsfolk",
                    areaTag: "maintown",
                    isProtectedFromContracts: false,
                    reservedPublicDisplayNames: CollectReservedPublicDisplayNames()));
            }
        }

        private void EnsureRuntimePopulationInitializedForScene()
        {
            if (_runtime.Civilians.Count > 0)
            {
                return;
            }

            if (_appearanceLibrary == null)
            {
                return;
            }

            if (_populationDefinition != null)
            {
                SeedRosterFromPopulationDefinition();
                return;
            }

            if (_initialPopulationCount <= 0)
            {
                return;
            }

            var anchorIds = NormalizeSpawnAnchors();
            if (anchorIds.Count == 0)
            {
                return;
            }

            var idPrefix = string.IsNullOrWhiteSpace(_civilianIdPrefix) ? "citizen.mainTown" : _civilianIdPrefix.Trim();
            for (var i = 0; i < _initialPopulationCount; i++)
            {
                var civilianId = $"{idPrefix}.{i + 1:0000}";
                var spawnAnchorId = anchorIds[i % anchorIds.Count];
                var seed = i + 1;
                _runtime.Civilians.Add(_generator.GenerateRecord(
                    _appearanceLibrary,
                    civilianId,
                    createdAtDay: 0,
                    spawnAnchorId,
                    seed,
                    isContractEligible: true,
                    populationSlotId: CreateFallbackPopulationSlotId(i),
                    poolId: "townsfolk",
                    areaTag: "maintown",
                    isProtectedFromContracts: false,
                    reservedPublicDisplayNames: CollectReservedPublicDisplayNames()));
            }
        }

        private void SeedRosterFromPopulationDefinition()
        {
            _populationDefinition.Validate();

            var idPrefix = string.IsNullOrWhiteSpace(_civilianIdPrefix) ? "citizen.mainTown" : _civilianIdPrefix.Trim();
            var slotOrdinal = 0;
            var filterByHabitat = _spawnHabitat != MainTownPopulationHabitat.Any;
            foreach (var slot in _populationDefinition.GetSlotsForHabitat(MainTownPopulationHabitat.Any))
            {
                if (slot == null)
                {
                    continue;
                }

                slotOrdinal++;
                if (filterByHabitat && slot.Habitat != _spawnHabitat)
                {
                    continue;
                }

                var civilianId = $"{idPrefix}.{slotOrdinal:0000}";
                var isDispatchOnlyReserve = IsDispatchOnlyPoliceSlot(slot);
                var isProtectedFromContracts = slot.IsProtectedFromContracts || isDispatchOnlyReserve;
                _runtime.Civilians.Add(_generator.GenerateRecord(
                    _appearanceLibrary,
                    civilianId,
                    createdAtDay: 0,
                    slot.SpawnAnchorId,
                    seed: slotOrdinal,
                    isContractEligible: !isProtectedFromContracts,
                    populationSlotId: slot.PopulationSlotId,
                    poolId: slot.PoolId,
                    areaTag: slot.AreaTag,
                    isProtectedFromContracts: isProtectedFromContracts,
                    reservedPublicDisplayNames: CollectReservedPublicDisplayNames()));
            }
        }

        private void CopyRuntimeToModule(CivilianPopulationModule module)
        {
            module.Civilians.Clear();
            for (var i = 0; i < _runtime.Civilians.Count; i++)
            {
                module.Civilians.Add(CloneRecord(_runtime.Civilians[i]));
            }

            module.PendingReplacements.Clear();
            for (var i = 0; i < _runtime.PendingReplacements.Count; i++)
            {
                module.PendingReplacements.Add(CloneReplacement(_runtime.PendingReplacements[i]));
            }

            module.LastOfferedCivilianId = _runtime.LastOfferedCivilianId ?? string.Empty;
            module.OfferRotationSeed = _runtime.OfferRotationSeed;
        }

        private void CopyModuleToRuntime(CivilianPopulationModule module)
        {
            _runtime.Civilians.Clear();
            for (var i = 0; i < module.Civilians.Count; i++)
            {
                _runtime.Civilians.Add(CloneRecord(module.Civilians[i]));
            }

            _runtime.PendingReplacements.Clear();
            for (var i = 0; i < module.PendingReplacements.Count; i++)
            {
                _runtime.PendingReplacements.Add(CloneReplacement(module.PendingReplacements[i]));
            }

            _runtime.LastOfferedCivilianId = module.LastOfferedCivilianId ?? string.Empty;
            _runtime.OfferRotationSeed = module.OfferRotationSeed;
        }

        private void HydrateRuntimeFromModuleIfNeeded(CivilianPopulationModule module)
        {
            if (_runtime.Civilians.Count > 0 || _runtime.PendingReplacements.Count > 0)
            {
                return;
            }

            if (module.Civilians.Count == 0 && module.PendingReplacements.Count == 0)
            {
                return;
            }

            CopyModuleToRuntime(module);
        }

        private void ClearSpawnedScenePopulation()
        {
            var spawned = GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            for (var i = 0; i < spawned.Length; i++)
            {
                var target = spawned[i];
                if (target == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    target.transform.SetParent(null, false);
                    target.gameObject.SetActive(false);
                    Destroy(target.gameObject);
                }
                else
                {
                    DestroyImmediate(target.gameObject);
                }
            }
        }

        private Transform ResolveSpawnAnchor(string anchorId)
        {
            if (string.IsNullOrWhiteSpace(anchorId))
            {
                return null;
            }

            var anchors = GetComponentsInChildren<Transform>(includeInactive: true);
            for (var i = 0; i < anchors.Length; i++)
            {
                var candidate = anchors[i];
                if (candidate != null && string.Equals(candidate.name, anchorId, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private void SpawnPlaceholderCivilian(CivilianPopulationRecord record, Transform anchor)
        {
            var civilian = CreateCivilianActor(record.CivilianId);
            civilian.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            InitializeSpawnedCivilian(civilian, record);

            var agent = civilian.GetComponent<NpcAgent>();
            agent?.InitializeCapabilities();
        }

        private void SpawnDedicatedContractTarget(CivilianPopulationRecord record)
        {
            var allowActiveReserveTarget = IsActiveContractTarget(record);
            if (record == null
                || !record.IsAlive
                || (IsDispatchReservePoliceRecord(record) && !allowActiveReserveTarget))
            {
                return;
            }

            var anchor = ResolveContractTargetAnchor(record);
            if (anchor == null)
            {
                return;
            }

            var civilian = CreateCivilianActor(record.CivilianId);
            civilian.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            InitializeSpawnedCivilian(civilian, record);
            ConfigureContractTargetIfEligible(civilian, record);

            var agent = civilian.GetComponent<NpcAgent>();
            agent?.InitializeCapabilities();
        }

        public bool TrySpawnDebugContractCivilian(
            Vector3 position,
            Quaternion rotation,
            out GameObject civilian,
            out string resultMessage)
        {
            civilian = null;
            resultMessage = string.Empty;

            if (_npcActorPrefab == null)
            {
                resultMessage = "Civilian population runtime is missing the NPC actor prefab.";
                return false;
            }

            if (_appearanceLibrary == null)
            {
                resultMessage = "Civilian population runtime is missing the appearance library.";
                return false;
            }

            var civilianId = CreateDebugCivilianId();
            var seed = CreateOfferRotationSeed();
            var record = _generator.GenerateRecord(
                _appearanceLibrary,
                civilianId,
                createdAtDay: 0,
                spawnAnchorId: "debug.contract",
                seed: seed,
                isContractEligible: true,
                populationSlotId: "debug.contract",
                poolId: "debug.contract",
                areaTag: "debug",
                isProtectedFromContracts: false);

            civilian = CreateCivilianActor(record.CivilianId);
            civilian.transform.SetPositionAndRotation(position, rotation);
            InitializeSpawnedCivilian(civilian, record);
            ConfigureContractTargetIfEligible(civilian, record);

            var agent = civilian.GetComponent<NpcAgent>();
            agent?.InitializeCapabilities();

            resultMessage = $"Spawned contract-eligible npc '{record.CivilianId}'.";
            return true;
        }

        private GameObject CreateCivilianActor(string civilianId)
        {
            var civilian = _npcActorPrefab != null
                ? Instantiate(_npcActorPrefab, transform, false)
                : new GameObject();

            civilian.name = $"Civilian_{civilianId}";
            civilian.transform.SetParent(transform, false);
            civilian.transform.localScale = Vector3.one;
            civilian.SetActive(true);
            return civilian;
        }

        private void InitializeSpawnedCivilian(GameObject civilian, CivilianPopulationRecord record)
        {
            var metadata = EnsureCivilianActorComponents(civilian);
            metadata.Initialize(record);
            ConfigureCivilianWitnessReporter(civilian, record);
            ConfigurePoliceShooter(civilian, record, metadata);
            ConfigurePoliceResponderMover(civilian, record);
            EnsurePoliceDispatchCoordinator(record);
        }

        private void ConfigureCivilianWitnessReporter(GameObject civilian, CivilianPopulationRecord record)
        {
            if (civilian == null)
            {
                return;
            }

            var witnessReporter = civilian.GetComponent<CivilianWitnessReporter>();
            if (!ShouldConfigureCivilianWitnessReporter(record))
            {
                if (witnessReporter == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    Destroy(witnessReporter);
                }
                else
                {
                    DestroyImmediate(witnessReporter);
                }

                return;
            }

            if (witnessReporter == null)
            {
                witnessReporter = civilian.AddComponent<CivilianWitnessReporter>();
            }

            witnessReporter.Configure(ResolveCrimeReporter());
        }

        private void RefreshSpawnedCivilianWitnessReporters()
        {
            var spawned = GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            for (var i = 0; i < spawned.Length; i++)
            {
                var civilian = spawned[i];
                if (civilian == null)
                {
                    continue;
                }

                ConfigureCivilianWitnessReporter(civilian.gameObject, FindCivilianById(civilian.CivilianId));
            }
        }

        private bool ShouldConfigureCivilianWitnessReporter(CivilianPopulationRecord record)
        {
            return record != null
                   && record.IsAlive
                   && !string.Equals(record.PoolId, "cops", StringComparison.Ordinal)
                   && !IsDispatchReservePoliceRecord(record)
                   && !IsActiveContractTarget(record);
        }

        private ILawEnforcementCrimeReporter ResolveCrimeReporter()
        {
            if (!IsReferenceAlive(_crimeReporter))
            {
                _crimeReporter = null;
            }

            if (_crimeReporter != null)
            {
                return _crimeReporter;
            }

            _crimeReporter = _crimeReporterBehaviour as ILawEnforcementCrimeReporter;
            if (_crimeReporter != null)
            {
                return _crimeReporter;
            }

            _crimeReporter = _contractRuntimeProviderBehaviour as ILawEnforcementCrimeReporter;
            return _crimeReporter;
        }

        private void EnsurePoliceDispatchCoordinator(CivilianPopulationRecord record)
        {
            if (record == null
                || !record.IsAlive
                || !string.Equals(record.PoolId, "cops", StringComparison.Ordinal))
            {
                return;
            }

            var coordinator = GetComponent<PoliceDispatchCoordinator>();
            if (coordinator == null)
            {
                coordinator = FindFirstObjectByType<PoliceDispatchCoordinator>(FindObjectsInactive.Include);
            }

            if (coordinator == null)
            {
                coordinator = gameObject.AddComponent<PoliceDispatchCoordinator>();
            }

            coordinator.enabled = true;
        }

        private static MainTownPopulationSpawnedCivilian EnsureCivilianActorComponents(GameObject civilian)
        {
            if (civilian.GetComponent<CapsuleCollider>() == null && civilian.GetComponentInChildren<CapsuleCollider>(includeInactive: true) == null)
            {
                civilian.AddComponent<CapsuleCollider>();
            }

            if (civilian.GetComponent<NpcAgent>() == null)
            {
                civilian.AddComponent<NpcAgent>();
            }

            if (civilian.GetComponent<AmbientCitizenCapability>() == null)
            {
                civilian.AddComponent<AmbientCitizenCapability>();
            }

            if (civilian.GetComponent<DialogueCapability>() == null)
            {
                civilian.AddComponent<DialogueCapability>();
            }

            if (civilian.GetComponent<HumanoidHitboxRig>() == null)
            {
                civilian.AddComponent<HumanoidHitboxRig>();
            }

            if (civilian.GetComponent<HumanoidDamageReceiver>() == null)
            {
                civilian.AddComponent<HumanoidDamageReceiver>();
            }

            if (civilian.GetComponent<HumanoidRagdollController>() == null)
            {
                civilian.AddComponent<HumanoidRagdollController>();
            }

            if (civilian.GetComponent<HumanoidCorpseLootController>() == null)
            {
                civilian.AddComponent<HumanoidCorpseLootController>();
            }

            var metadata = civilian.GetComponent<MainTownPopulationSpawnedCivilian>();
            if (metadata == null)
            {
                metadata = civilian.AddComponent<MainTownPopulationSpawnedCivilian>();
            }

            return metadata;
        }

        private static void ConfigurePoliceShooter(
            GameObject civilian,
            CivilianPopulationRecord record,
            MainTownPopulationSpawnedCivilian metadata)
        {
            if (civilian == null)
            {
                return;
            }

            var shooter = civilian.GetComponent<PoliceHostileShooter>();
            var shouldArmPolice = record != null
                                  && string.Equals(record.PoolId, "cops", StringComparison.Ordinal)
                                  && record.IsAlive;
            if (!shouldArmPolice)
            {
                if (shooter == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    Destroy(shooter);
                }
                else
                {
                    DestroyImmediate(shooter);
                }

                return;
            }

            if (shooter == null)
            {
                shooter = civilian.AddComponent<PoliceHostileShooter>();
            }

            shooter.ConfigureRuntimeOrigin(metadata != null ? metadata.ResolveDialogueFocusTarget() : civilian.transform);
            shooter.enabled = false;
        }

        private static void ConfigurePoliceResponderMover(GameObject civilian, CivilianPopulationRecord record)
        {
            if (civilian == null)
            {
                return;
            }

            var responderMover = civilian.GetComponent<PoliceResponderMover>();
            var shouldArmPolice = record != null
                                  && string.Equals(record.PoolId, "cops", StringComparison.Ordinal)
                                  && record.IsAlive;
            if (!shouldArmPolice)
            {
                if (responderMover == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    Destroy(responderMover);
                }
                else
                {
                    DestroyImmediate(responderMover);
                }

                return;
            }

            if (responderMover == null)
            {
                responderMover = civilian.AddComponent<PoliceResponderMover>();
            }

            responderMover.enabled = false;
        }

        private void ConfigureContractTargetIfEligible(GameObject civilian, CivilianPopulationRecord record)
        {
            var allowActiveReserveTarget = IsActiveContractTarget(record);
            if (civilian == null
                || record == null
                || (!allowActiveReserveTarget && !record.IsContractEligible)
                || (!allowActiveReserveTarget && record.IsProtectedFromContracts)
                || (IsDispatchReservePoliceRecord(record) && !allowActiveReserveTarget))
            {
                return;
            }

            var damageable = civilian.GetComponent<ContractTargetDamageable>();
            if (damageable == null)
            {
                damageable = civilian.AddComponent<ContractTargetDamageable>();
            }

            damageable.Configure(
                ResolveContractTargetEliminationSink(civilian),
                targetId: record.CivilianId,
                displayName: BuildPublicDisplayName(record),
                authoritativeDistanceMeters: ProceduralContractTargetDistanceMeters,
                maxHealth: ProceduralContractTargetHealth);
        }

        private static IContractTargetEliminationSink ResolveContractTargetEliminationSink(GameObject civilian)
        {
            if (civilian == null)
            {
                return null;
            }

            var localBehaviours = civilian.GetComponents<MonoBehaviour>();
            for (var i = 0; i < localBehaviours.Length; i++)
            {
                if (localBehaviours[i] is IContractTargetEliminationSink localSink)
                {
                    return localSink;
                }
            }

            var bridge = civilian.GetComponentInParent<CivilianPopulationRuntimeBridge>();
            if (bridge != null)
            {
                return bridge;
            }

            return null;
        }

        private void RefreshContractTargetDamageables()
        {
            var trackedTargetId = ResolveTrackedContractTargetId();
            var spawned = GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            MainTownPopulationSpawnedCivilian trackedSpawn = null;
            if (!string.IsNullOrWhiteSpace(trackedTargetId))
            {
                for (var i = 0; i < spawned.Length; i++)
                {
                    var metadata = spawned[i];
                    if (metadata != null &&
                        string.Equals(metadata.CivilianId, trackedTargetId, StringComparison.Ordinal))
                    {
                        trackedSpawn = metadata;
                        break;
                    }
                }
            }

            for (var i = 0; i < spawned.Length; i++)
            {
                var metadata = spawned[i];
                if (metadata == null)
                {
                    continue;
                }

                var damageable = metadata.GetComponent<ContractTargetDamageable>();
                if (trackedSpawn == null || !ReferenceEquals(metadata, trackedSpawn))
                {
                    if (damageable != null)
                    {
                        DestroyImmediate(damageable);
                    }

                    continue;
                }

                var record = FindCivilianById(metadata.CivilianId);
                ConfigureContractTargetIfEligible(metadata.gameObject, record);
            }
        }

        private void RefreshProceduralContractOffer()
        {
            var provider = ResolveStaticContractRuntimeProvider();
            if (provider == null)
            {
                return;
            }

            if (!provider.CanPublishAvailableContract())
            {
                return;
            }

            var target = FindNextEligibleContractCivilian();
            if (target == null)
            {
                if (provider.TryGetContractSnapshot(out var emptySnapshot) && emptySnapshot.HasAvailableContract)
                {
                    provider.SetAvailableContract(null);
                }

                return;
            }

            if (provider.TryGetContractSnapshot(out var snapshot) &&
                snapshot.HasAvailableContract &&
                string.Equals(snapshot.TargetId, target.CivilianId, StringComparison.Ordinal))
            {
                return;
            }

            if (_proceduralAvailableContract == null)
            {
                _proceduralAvailableContract = ScriptableObject.CreateInstance<AssassinationContractDefinition>();
            }

            _proceduralAvailableContract.ConfigureRuntimeOffer(
                contractId: $"contract.maintown.procedural.{target.CivilianId}",
                targetId: target.CivilianId,
                title: "MainTown Contract",
                targetDisplayName: BuildPublicDisplayName(target),
                targetDescription: BuildProceduralTargetDescription(target),
                briefingText: BuildProceduralBriefingText(target),
                distanceBand: ProceduralContractTargetDistanceMeters,
                payout: ProceduralContractPayout);

            _runtime.LastOfferedCivilianId = target.CivilianId ?? string.Empty;
            provider.SetAvailableContract(_proceduralAvailableContract);
        }

        private string ResolveTrackedContractTargetId()
        {
            if (!TryGetTrackedContractSnapshot(out var snapshot))
            {
                return string.Empty;
            }

            if (snapshot.HasActiveContract || snapshot.HasAvailableContract)
            {
                return snapshot.TargetId ?? string.Empty;
            }

            return string.Empty;
        }

        private CivilianPopulationRecord FindNextEligibleContractCivilian()
        {
            var eligible = new List<CivilianPopulationRecord>();
            for (var i = 0; i < _runtime.Civilians.Count; i++)
            {
                var record = _runtime.Civilians[i];
                if (record == null
                    || !record.IsAlive
                    || !record.IsContractEligible
                    || record.IsProtectedFromContracts
                    || IsDispatchReservePoliceRecord(record))
                {
                    continue;
                }

                if (ResolveSpawnAnchor(record.SpawnAnchorId) == null)
                {
                    continue;
                }

                eligible.Add(record);
            }

            if (eligible.Count == 0)
            {
                return null;
            }

            var lastOfferedCivilianId = _runtime.LastOfferedCivilianId;
            if (string.IsNullOrWhiteSpace(lastOfferedCivilianId))
            {
                var offerRotationSeed = EnsureOfferRotationSeed();
                var startingIndex = GetNonNegativeModulo(offerRotationSeed, eligible.Count);
                return eligible[startingIndex];
            }

            for (var i = 0; i < eligible.Count; i++)
            {
                if (!string.Equals(eligible[i].CivilianId, lastOfferedCivilianId, StringComparison.Ordinal))
                {
                    continue;
                }

                return eligible[(i + 1) % eligible.Count];
            }

            return eligible[GetNonNegativeModulo(EnsureOfferRotationSeed(), eligible.Count)];
        }

        private bool ShouldSpawnAmbientCivilian(CivilianPopulationRecord record, string trackedTargetId)
        {
            if (record == null || !record.IsAlive)
            {
                return false;
            }

            if (IsDispatchReservePoliceRecord(record))
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(trackedTargetId)
                || !string.Equals(record.CivilianId, trackedTargetId, StringComparison.Ordinal);
        }

        private Transform ResolveContractTargetAnchor(CivilianPopulationRecord record)
        {
            var anchors = NormalizeContractTargetAnchors();
            for (var i = 0; i < anchors.Count; i++)
            {
                var anchor = ResolveSpawnAnchor(anchors[i]);
                if (anchor != null)
                {
                    return anchor;
                }
            }

            return record == null ? null : ResolveSpawnAnchor(record.SpawnAnchorId);
        }

        private int EnsureOfferRotationSeed()
        {
            if (_runtime.OfferRotationSeed != 0)
            {
                return _runtime.OfferRotationSeed;
            }

            _runtime.OfferRotationSeed = CreateOfferRotationSeed();
            return _runtime.OfferRotationSeed;
        }

        private static int CreateOfferRotationSeed()
        {
            var seed = Guid.NewGuid().GetHashCode() ^ Environment.TickCount ^ (int)DateTime.UtcNow.Ticks;
            return seed == 0 ? 1 : seed;
        }

        private static int GetNonNegativeModulo(int value, int divisor)
        {
            if (divisor <= 0)
            {
                return 0;
            }

            return (int)(unchecked((uint)value) % (uint)divisor);
        }

        private static string BuildProceduralTargetDescription(CivilianPopulationRecord record)
        {
            if (record == null)
            {
                return string.Empty;
            }

            var clues = new List<string>(4);
            AddUniqueClue(clues, TryDescribeSex(record));
            AddUniqueClue(clues, TryDescribeClothing(record));
            AddUniqueClue(clues, TryDescribeHair(record));
            AddUniqueClue(clues, TryDescribeBeard(record));
            if (clues.Count >= 2)
            {
                return string.Join(", ", clues);
            }

            if (record.GeneratedDescriptionTags != null && record.GeneratedDescriptionTags.Count > 0)
            {
                return string.Join(", ", record.GeneratedDescriptionTags);
            }

            if (!string.IsNullOrWhiteSpace(record.PoolId) && !string.IsNullOrWhiteSpace(record.AreaTag))
            {
                return $"{record.PoolId} in {record.AreaTag}";
            }

            return record.AreaTag ?? string.Empty;
        }

        private static string BuildProceduralBriefingText(CivilianPopulationRecord record)
        {
            if (record == null)
            {
                return "Locate and eliminate the live procedural target in MainTown.";
            }

            var roleLabel = DescribeContractRole(record.PoolId);
            var locationLabel = DescribeContractLocation(record.AreaTag);
            if (!string.IsNullOrWhiteSpace(roleLabel) && !string.IsNullOrWhiteSpace(locationLabel))
            {
                return $"Contractor notes: {roleLabel}, usually found around {locationLabel}. Confirm the visual match before taking the shot.";
            }

            if (!string.IsNullOrWhiteSpace(roleLabel))
            {
                return $"Contractor notes: {roleLabel}. Confirm the visual match before taking the shot.";
            }

            if (!string.IsNullOrWhiteSpace(locationLabel))
            {
                return $"Contractor notes: usually found around {locationLabel}. Confirm the visual match before taking the shot.";
            }

            return "Locate and eliminate the live procedural target in MainTown.";
        }

        private static void AddUniqueClue(List<string> clues, string clue)
        {
            if (clues == null || string.IsNullOrWhiteSpace(clue))
            {
                return;
            }

            if (!clues.Contains(clue))
            {
                clues.Add(clue);
            }
        }

        private static string TryDescribeSex(CivilianPopulationRecord record)
        {
            if (record == null)
            {
                return string.Empty;
            }

            return MainTownCuratedAppearanceRules.TryInferGender(record.BaseBodyId, record.PresentationType, out var gender)
                ? gender == MainTownAppearanceGender.Female ? "female" : "male"
                : string.Empty;
        }

        private static string TryDescribeClothing(CivilianPopulationRecord record)
        {
            if (record == null)
            {
                return string.Empty;
            }

            var outerwearId = MainTownCuratedAppearanceRules.NormalizeOuterwearId(record.OuterwearId);
            var garmentSource = !string.IsNullOrWhiteSpace(outerwearId) ? outerwearId : record.OutfitTopId;
            var garment = DescribeGarment(garmentSource);
            if (string.IsNullOrWhiteSpace(garment))
            {
                return string.Empty;
            }

            var color = FirstRecognizedColor(record.MaterialColorIds);
            if (string.IsNullOrWhiteSpace(color))
            {
                color = RecognizeColorFromToken(outerwearId);
            }

            if (string.IsNullOrWhiteSpace(color))
            {
                color = RecognizeColorFromToken(record.OutfitTopId);
            }

            return string.IsNullOrWhiteSpace(color)
                ? garment
                : string.Concat(color, " ", garment);
        }

        private static string TryDescribeHair(CivilianPopulationRecord record)
        {
            if (record == null)
            {
                return string.Empty;
            }

            var hairId = record.HairId?.Trim() ?? string.Empty;
            if (ContainsToken(hairId, "long"))
            {
                return "long hair";
            }

            if (ContainsToken(hairId, "short"))
            {
                return "short hair";
            }

            if (ContainsToken(hairId, "bob"))
            {
                return "bob haircut";
            }

            if (ContainsToken(hairId, "wavy"))
            {
                return "wavy hair";
            }

            if (ContainsToken(hairId, "parted"))
            {
                return "parted hair";
            }

            if (ContainsToken(hairId, "swept"))
            {
                return "swept hair";
            }

            return string.Empty;
        }

        private static string TryDescribeBeard(CivilianPopulationRecord record)
        {
            if (record == null || !MainTownCuratedAppearanceRules.IsMaleBeardId(record.BeardId))
            {
                return string.Empty;
            }

            var beardId = record.BeardId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(beardId))
            {
                return string.Empty;
            }

            var beardNumber = ExtractTrailingNumber(beardId);
            if (beardNumber <= 0)
            {
                return "beard";
            }

            if (beardNumber <= 3)
            {
                return "trim beard";
            }

            if (beardNumber <= 6)
            {
                return "short beard";
            }

            if (beardNumber <= 8)
            {
                return "thick beard";
            }

            return "full beard";
        }

        private static int ExtractTrailingNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            var end = value.Length - 1;
            while (end >= 0 && char.IsDigit(value[end]))
            {
                end--;
            }

            if (end == value.Length - 1)
            {
                return 0;
            }

            return int.TryParse(value.Substring(end + 1), out var parsed) ? parsed : 0;
        }

        private static string DescribeGarment(string garmentId)
        {
            if (string.IsNullOrWhiteSpace(garmentId))
            {
                return string.Empty;
            }

            if (ContainsToken(garmentId, "openjacket"))
            {
                return "open jacket";
            }

            if (ContainsToken(garmentId, "hoody") || ContainsToken(garmentId, "hoodie"))
            {
                return "hoodie";
            }

            if (ContainsToken(garmentId, "jacket"))
            {
                return "jacket";
            }

            if (ContainsToken(garmentId, "coat"))
            {
                return "coat";
            }

            if (ContainsToken(garmentId, "tshirt") || ContainsToken(garmentId, "shirt"))
            {
                return "t-shirt";
            }

            return string.Empty;
        }

        private static string FirstRecognizedColor(List<string> values)
        {
            if (values == null)
            {
                return string.Empty;
            }

            for (var i = 0; i < values.Count; i++)
            {
                var color = RecognizeColorFromToken(values[i]);
                if (!string.IsNullOrWhiteSpace(color))
                {
                    return color;
                }
            }

            return string.Empty;
        }

        private static string RecognizeColorFromToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (ContainsToken(value, "black"))
            {
                return "black";
            }

            if (ContainsToken(value, "brown"))
            {
                return "brown";
            }

            if (ContainsToken(value, "gray") || ContainsToken(value, "grey"))
            {
                return "gray";
            }

            if (ContainsToken(value, "red"))
            {
                return "red";
            }

            if (ContainsToken(value, "blue"))
            {
                return "blue";
            }

            if (ContainsToken(value, "green"))
            {
                return "green";
            }

            if (ContainsToken(value, "blonde") || ContainsToken(value, "blond"))
            {
                return "blonde";
            }

            if (ContainsToken(value, "white"))
            {
                return "white";
            }

            if (ContainsToken(value, "dark"))
            {
                return "dark";
            }

            return string.Empty;
        }

        private static bool ContainsToken(string value, string token)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            return value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string BuildPublicDisplayName(CivilianPopulationRecord record)
        {
            if (record == null)
            {
                return string.Empty;
            }

            var firstName = record.FirstName?.Trim() ?? string.Empty;
            var lastName = record.LastName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
            {
                return record.CivilianId ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(firstName))
            {
                return lastName;
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                return firstName;
            }

            return string.Concat(firstName, " ", lastName);
        }

        private static string DescribeContractRole(string poolId)
        {
            var normalized = NormalizeIdentifier(poolId);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            return normalized switch
            {
                "townsfolk" => "local resident",
                "quarry workers" => "quarry worker",
                "hobos" => "drifter",
                "cops" => "police officer",
                _ => normalized
            };
        }

        private static string DescribeContractLocation(string areaTag)
        {
            var normalized = NormalizeIdentifier(areaTag);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            return normalized switch
            {
                "maintown square" => "the town square",
                "maintown watch" => "the watch post",
                "maintown alley" => "the alleys",
                "quarry" => "the quarry",
                _ when normalized.StartsWith("maintown ", StringComparison.Ordinal) => string.Concat("the ", normalized.Substring("maintown ".Length)),
                _ => string.Concat("the ", normalized)
            };
        }

        private static string NormalizeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var parts = value.Split(new[] { '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return string.Empty;
            }

            for (var i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Trim().ToLowerInvariant();
            }

            return string.Join(" ", parts);
        }

        private HashSet<string> CollectReservedPublicDisplayNames()
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < _runtime.Civilians.Count; i++)
            {
                var displayName = BuildPublicDisplayName(_runtime.Civilians[i]);
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    names.Add(displayName);
                }
            }

            return names;
        }

        private List<string> NormalizeSpawnAnchors()
        {
            var anchors = new List<string>();
            if (_spawnAnchorIds == null)
            {
                return anchors;
            }

            for (var i = 0; i < _spawnAnchorIds.Length; i++)
            {
                var value = _spawnAnchorIds[i];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    anchors.Add(value.Trim());
                }
            }

            return anchors;
        }

        private List<string> NormalizeContractTargetAnchors()
        {
            var anchors = new List<string>();
            if (_contractTargetAnchorIds == null)
            {
                return anchors;
            }

            for (var i = 0; i < _contractTargetAnchorIds.Length; i++)
            {
                var value = _contractTargetAnchorIds[i];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    anchors.Add(value.Trim());
                }
            }

            return anchors;
        }

        private static CivilianPopulationModule ResolveModule(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            if (moduleRegistrations == null)
            {
                return null;
            }

            for (var i = 0; i < moduleRegistrations.Count; i++)
            {
                if (moduleRegistrations[i]?.Module is CivilianPopulationModule module)
                {
                    return module;
                }
            }

            return null;
        }

        private static CoreWorldModule ResolveCoreWorldModule(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            if (moduleRegistrations == null)
            {
                return null;
            }

            for (var i = 0; i < moduleRegistrations.Count; i++)
            {
                if (moduleRegistrations[i].Module is CoreWorldModule module)
                {
                    return module;
                }
            }

            return null;
        }

        private CoreWorldController ResolveCoreWorldController()
        {
            if (_coreWorldController == null)
            {
                _coreWorldController = FindFirstObjectByType<CoreWorldController>(FindObjectsInactive.Include);
            }

            return _coreWorldController;
        }

        private void SubscribeToCoreWorldController(CoreWorldController controller)
        {
            if (ReferenceEquals(_subscribedCoreWorldController, controller))
            {
                return;
            }

            UnsubscribeFromCoreWorldController();
            _subscribedCoreWorldController = controller;
            if (_subscribedCoreWorldController == null)
            {
                return;
            }

            _lastObservedWorldDayCount = _subscribedCoreWorldController.CaptureSnapshot().DayCount;
            _lastObservedWorldTimeOfDay = _subscribedCoreWorldController.CaptureSnapshot().TimeOfDay;
            _subscribedCoreWorldController.WorldStateChanged += HandleCoreWorldStateChanged;
        }

        private void UnsubscribeFromCoreWorldController()
        {
            if (_subscribedCoreWorldController == null)
            {
                return;
            }

            _subscribedCoreWorldController.WorldStateChanged -= HandleCoreWorldStateChanged;
            _subscribedCoreWorldController = null;
            _lastObservedWorldDayCount = -1;
            _lastObservedWorldTimeOfDay = -1f;
        }

        private void HandleCoreWorldStateChanged()
        {
            if (_subscribedCoreWorldController == null)
            {
                return;
            }

            var snapshot = _subscribedCoreWorldController.CaptureSnapshot();
            var lastObservedDay = _lastObservedWorldDayCount;
            var lastObservedTime = _lastObservedWorldTimeOfDay;
            _lastObservedWorldDayCount = snapshot.DayCount;
            _lastObservedWorldTimeOfDay = snapshot.TimeOfDay;

            if (!HasWorldStateAdvanced(lastObservedDay, lastObservedTime, snapshot))
            {
                return;
            }

            ExecutePendingReplacements(snapshot.DayCount, snapshot.TimeOfDay);
        }

        private static bool HasWorldStateAdvanced(int previousDayCount, float previousTimeOfDay, CoreWorldRuntime.Snapshot snapshot)
        {
            if (previousDayCount < 0)
            {
                return true;
            }

            if (snapshot.DayCount > previousDayCount)
            {
                return true;
            }

            if (snapshot.DayCount < previousDayCount)
            {
                return false;
            }

            return snapshot.TimeOfDay > previousTimeOfDay;
        }

        private static bool HasReachedMondayRefreshWindow(
            CivilianPopulationReplacementRecord replacement,
            int currentDay,
            float currentTimeOfDay)
        {
            var refreshDay = GetFirstMondayRefreshDayAfterQueue(replacement.QueuedAtDay);
            if (currentDay < refreshDay)
            {
                return false;
            }

            if (currentDay > refreshDay)
            {
                return true;
            }

            return currentTimeOfDay >= MondayRefreshTimeOfDay;
        }

        private static int GetFirstMondayRefreshDayAfterQueue(int queuedAtDay)
        {
            var normalizedQueuedDay = Math.Max(0, queuedAtDay);
            return ((normalizedQueuedDay / 7) + 1) * 7;
        }

        private static float NormalizeTimeOfDay(float timeOfDay)
        {
            if (float.IsNaN(timeOfDay) || float.IsInfinity(timeOfDay))
            {
                return 0f;
            }

            var normalized = timeOfDay % 24f;
            if (normalized < 0f)
            {
                normalized += 24f;
            }

            return normalized;
        }

        private static CivilianPopulationRecord CloneRecord(CivilianPopulationRecord source)
        {
            var isStyleAppearance = MainTownCuratedAppearanceRules.IsCuratedStyleBodyId(source?.BaseBodyId);
            MainTownCuratedAppearanceRules.TryInferGender(source?.BaseBodyId, source?.PresentationType, out var gender);
            return new CivilianPopulationRecord
            {
                PopulationSlotId = source?.PopulationSlotId ?? string.Empty,
                PoolId = source?.PoolId ?? string.Empty,
                CivilianId = source?.CivilianId ?? string.Empty,
                FirstName = source?.FirstName ?? string.Empty,
                LastName = source?.LastName ?? string.Empty,
                Nickname = source?.Nickname ?? string.Empty,
                IsAlive = source != null && source.IsAlive,
                IsContractEligible = source != null && source.IsContractEligible,
                IsProtectedFromContracts = source != null && source.IsProtectedFromContracts,
                BaseBodyId = source?.BaseBodyId ?? string.Empty,
                PresentationType = source?.PresentationType ?? string.Empty,
                HairId = source?.HairId ?? string.Empty,
                HairColorId = source?.HairColorId ?? string.Empty,
                EyebrowId = isStyleAppearance
                    ? MainTownCuratedAppearanceRules.NormalizeEyebrowId(source?.EyebrowId, source?.OutfitBottomId)
                    : source?.EyebrowId ?? string.Empty,
                BeardId = source?.BeardId ?? string.Empty,
                OutfitTopId = source?.OutfitTopId ?? string.Empty,
                OutfitBottomId = isStyleAppearance
                    ? MainTownCuratedAppearanceRules.NormalizeBottomId(gender, source?.OutfitBottomId)
                    : source?.OutfitBottomId ?? string.Empty,
                OuterwearId = source?.OuterwearId ?? string.Empty,
                MaterialColorIds = source?.MaterialColorIds != null ? new List<string>(source.MaterialColorIds) : new List<string>(),
                GeneratedDescriptionTags = source?.GeneratedDescriptionTags != null ? new List<string>(source.GeneratedDescriptionTags) : new List<string>(),
                SpawnAnchorId = source?.SpawnAnchorId ?? string.Empty,
                AreaTag = source?.AreaTag ?? string.Empty,
                CreatedAtDay = source?.CreatedAtDay ?? 0,
                RetiredAtDay = source?.RetiredAtDay ?? -1
            };
        }

        private static CivilianPopulationReplacementRecord CloneReplacement(CivilianPopulationReplacementRecord source)
        {
            return new CivilianPopulationReplacementRecord
            {
                VacatedCivilianId = source?.VacatedCivilianId ?? string.Empty,
                QueuedAtDay = source?.QueuedAtDay ?? 0,
                SpawnAnchorId = source?.SpawnAnchorId ?? string.Empty
            };
        }

        private bool HasPendingReplacement(string civilianId)
        {
            for (var i = 0; i < _runtime.PendingReplacements.Count; i++)
            {
                var replacement = _runtime.PendingReplacements[i];
                if (replacement != null && string.Equals(replacement.VacatedCivilianId, civilianId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private CivilianPopulationRecord FindCivilianById(string civilianId)
        {
            if (string.IsNullOrWhiteSpace(civilianId))
            {
                return null;
            }

            for (var i = 0; i < _runtime.Civilians.Count; i++)
            {
                var record = _runtime.Civilians[i];
                if (record != null && string.Equals(record.CivilianId, civilianId, StringComparison.Ordinal))
                {
                    return record;
                }
            }

            return null;
        }

        private HashSet<string> CollectOccupiedLivePopulationSlotIds()
        {
            var occupiedSlots = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < _runtime.Civilians.Count; i++)
            {
                var record = _runtime.Civilians[i];
                if (record != null && record.IsAlive && !string.IsNullOrWhiteSpace(record.PopulationSlotId))
                {
                    occupiedSlots.Add(record.PopulationSlotId);
                }
            }

            return occupiedSlots;
        }

        private string CreateNextCivilianId(string populationSlotId = null)
        {
            if (TryCreateNextPopulationSlotScopedCivilianId(populationSlotId, out var civilianId))
            {
                return civilianId;
            }

            return CreateSequentialCivilianId();
        }

        private bool IsDispatchReservePoliceRecord(CivilianPopulationRecord record)
        {
            if (record == null
                || !record.IsAlive
                || string.IsNullOrWhiteSpace(record.PopulationSlotId))
            {
                return false;
            }

            return IsDispatchReservePoliceSlotId(record.PopulationSlotId);
        }

        private bool IsActiveContractTarget(CivilianPopulationRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.CivilianId))
            {
                return false;
            }

            return TryGetTrackedContractSnapshot(out var snapshot)
                   && snapshot.HasActiveContract
                   && string.Equals(snapshot.TargetId, record.CivilianId, StringComparison.Ordinal);
        }

        private bool TryGetTrackedContractSnapshot(out ContractOfferSnapshot snapshot)
        {
            var provider = ResolveContractRuntimeProvider();
            if (provider != null && provider.TryGetContractSnapshot(out snapshot))
            {
                return snapshot.HasActiveContract || snapshot.HasAvailableContract;
            }

            snapshot = default;
            return false;
        }

        private IContractRuntimeProvider ResolveContractRuntimeProvider()
        {
            if (!IsReferenceAlive(_contractRuntimeProvider))
            {
                _contractRuntimeProvider = null;
            }

            if (_contractRuntimeProvider != null)
            {
                return _contractRuntimeProvider;
            }

            _contractRuntimeProvider = _contractRuntimeProviderBehaviour as IContractRuntimeProvider;
            return _contractRuntimeProvider;
        }

        private IContractTargetEliminationSink ResolveContractTargetEliminationSink()
        {
            if (!IsReferenceAlive(_contractTargetEliminationSink))
            {
                _contractTargetEliminationSink = null;
            }

            if (_contractTargetEliminationSink != null)
            {
                return _contractTargetEliminationSink;
            }

            _contractTargetEliminationSink = _contractRuntimeProviderBehaviour as IContractTargetEliminationSink;
            return _contractTargetEliminationSink;
        }

        private StaticContractRuntimeProvider ResolveStaticContractRuntimeProvider()
        {
            return _contractRuntimeProviderBehaviour as StaticContractRuntimeProvider;
        }

        private static bool IsReferenceAlive(object instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (instance is UnityEngine.Object unityObject && unityObject == null)
            {
                return false;
            }

            return true;
        }

        private bool IsDispatchReservePoliceSlotId(string populationSlotId)
        {
            return TryResolvePopulationSlot(populationSlotId, out var slot) && IsDispatchOnlyPoliceSlot(slot);
        }

        private static bool IsDispatchOnlyPoliceSlot(MainTownPopulationSlotDefinition slot)
        {
            return slot != null
                   && !slot.SpawnOnSceneLoad
                   && string.Equals(slot.PoolId, "cops", StringComparison.Ordinal);
        }

        private bool TryResolvePopulationSlot(string populationSlotId, out MainTownPopulationSlotDefinition slot)
        {
            slot = null;
            if (_populationDefinition == null || string.IsNullOrWhiteSpace(populationSlotId))
            {
                return false;
            }

            foreach (var candidate in _populationDefinition.GetSlotsForHabitat(MainTownPopulationHabitat.Any))
            {
                if (candidate != null &&
                    string.Equals(candidate.PopulationSlotId, populationSlotId, StringComparison.Ordinal))
                {
                    slot = candidate;
                    return true;
                }
            }

            return false;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            var delta = new Vector3(a.x - b.x, 0f, a.z - b.z);
            return delta.magnitude;
        }

        private static int CompareDispatchCandidates(PoliceResponderDispatchCandidate left, PoliceResponderDispatchCandidate right)
        {
            var distanceComparison = left.DistanceMeters.CompareTo(right.DistanceMeters);
            if (distanceComparison != 0)
            {
                return distanceComparison;
            }

            var slotComparison = string.CompareOrdinal(left.PopulationSlotId, right.PopulationSlotId);
            if (slotComparison != 0)
            {
                return slotComparison;
            }

            return string.CompareOrdinal(left.CivilianId, right.CivilianId);
        }

        private bool TryCreateNextPopulationSlotScopedCivilianId(string populationSlotId, out string civilianId)
        {
            civilianId = string.Empty;
            if (_populationDefinition == null || string.IsNullOrWhiteSpace(populationSlotId))
            {
                return false;
            }

            var slotOrdinal = 0;
            var matchedSlotOrdinal = 0;
            foreach (var slot in _populationDefinition.GetSlotsForHabitat(MainTownPopulationHabitat.Any))
            {
                if (slot == null)
                {
                    continue;
                }

                slotOrdinal++;
                if (matchedSlotOrdinal == 0 &&
                    string.Equals(slot.PopulationSlotId, populationSlotId, StringComparison.Ordinal))
                {
                    matchedSlotOrdinal = slotOrdinal;
                }
            }

            if (slotOrdinal == 0 || matchedSlotOrdinal == 0)
            {
                return false;
            }

            var generationIndex = 0;
            var maxNumericSuffix = 0;
            for (var i = 0; i < _runtime.Civilians.Count; i++)
            {
                var record = _runtime.Civilians[i];
                if (record == null)
                {
                    continue;
                }

                if (string.Equals(record.PopulationSlotId, populationSlotId, StringComparison.Ordinal))
                {
                    generationIndex++;
                }

                maxNumericSuffix = Math.Max(maxNumericSuffix, ExtractCivilianNumericSuffix(record.CivilianId));
            }

            var nextNumericSuffix = matchedSlotOrdinal + (generationIndex * slotOrdinal);
            while (nextNumericSuffix <= maxNumericSuffix)
            {
                nextNumericSuffix += slotOrdinal;
            }

            var idPrefix = string.IsNullOrWhiteSpace(_civilianIdPrefix) ? "citizen.mainTown" : _civilianIdPrefix.Trim();
            civilianId = $"{idPrefix}.{nextNumericSuffix:0000}";
            return true;
        }

        private string CreateSequentialCivilianId()
        {
            var idPrefix = string.IsNullOrWhiteSpace(_civilianIdPrefix) ? "citizen.mainTown" : _civilianIdPrefix.Trim();
            var nextIndex = 1;
            for (var i = 0; i < _runtime.Civilians.Count; i++)
            {
                var numericSuffix = ExtractCivilianNumericSuffix(_runtime.Civilians[i]?.CivilianId);
                if (numericSuffix >= nextIndex)
                {
                    nextIndex = numericSuffix + 1;
                }
            }

            return $"{idPrefix}.{nextIndex:0000}";
        }

        private string CreateDebugCivilianId()
        {
            var idPrefix = string.IsNullOrWhiteSpace(_civilianIdPrefix) ? "citizen.mainTown" : _civilianIdPrefix.Trim();
            return $"{idPrefix}.debug.{Guid.NewGuid():N}";
        }

        private int ExtractCivilianNumericSuffix(string civilianId)
        {
            if (string.IsNullOrWhiteSpace(civilianId))
            {
                return 0;
            }

            var separatorIndex = civilianId.LastIndexOf('.');
            if (separatorIndex < 0 || separatorIndex >= civilianId.Length - 1)
            {
                return 0;
            }

            return int.TryParse(civilianId.Substring(separatorIndex + 1), out var value) ? Math.Max(0, value) : 0;
        }

        private static string CreateFallbackPopulationSlotId(int index)
        {
            var normalizedIndex = Math.Max(0, index) + 1;
            return $"seeded.maintown.{normalizedIndex:0000}";
        }
    }

    public sealed class CivilianPopulationRuntimeState
    {
        public List<CivilianPopulationRecord> Civilians { get; } = new List<CivilianPopulationRecord>();
        public List<CivilianPopulationReplacementRecord> PendingReplacements { get; } =
            new List<CivilianPopulationReplacementRecord>();
        public string LastOfferedCivilianId { get; set; } = string.Empty;
        public int OfferRotationSeed { get; set; }
    }
}

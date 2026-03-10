using System;
using System.Collections.Generic;
using Reloader.Contracts.Runtime;
using Reloader.Core.Runtime;
using Reloader.Core.Save;
using Reloader.Core.Save.Modules;
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
        [SerializeField] private string[] _spawnAnchorIds = Array.Empty<string>();

        private readonly CivilianPopulationRuntimeState _runtime = new CivilianPopulationRuntimeState();
        private readonly CivilianAppearanceGenerator _generator = new CivilianAppearanceGenerator();
        private CoreWorldController _coreWorldController;
        private CoreWorldController _subscribedCoreWorldController;
        private AssassinationContractDefinition _proceduralAvailableContract;
        private int _lastObservedWorldDayCount = -1;
        private float _lastObservedWorldTimeOfDay = -1f;

        public CivilianPopulationRuntimeState Runtime => _runtime;

        public void SetCoreWorldController(CoreWorldController controller)
        {
            _coreWorldController = controller;
            SubscribeToCoreWorldController(_coreWorldController);
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

            for (var i = 0; i < _runtime.Civilians.Count; i++)
            {
                var record = _runtime.Civilians[i];
                if (record == null || !record.IsAlive)
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

            RefreshProceduralContractOffer();
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

        public void ReportContractTargetEliminated(string targetId, bool wasExposed)
        {
            var snapshot = ResolveCoreWorldController()?.CaptureSnapshot();
            var retiredAtDay = snapshot?.DayCount ?? 0;
            TryRetireCivilian(targetId, retiredAtDay);

            var provider = FindFirstObjectByType<StaticContractRuntimeProvider>(FindObjectsInactive.Include);
            if (provider != null)
            {
                provider.ReportContractTargetEliminated(targetId, wasExposed);
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

                var civilianId = CreateNextCivilianId();
                var seed = ExtractCivilianNumericSuffix(civilianId);
                _runtime.Civilians.Add(_generator.GenerateRecord(
                    _appearanceLibrary,
                    civilianId,
                    createdAtDay: normalizedDay,
                    vacated.SpawnAnchorId,
                    seed,
                    isContractEligible: !vacated.IsProtectedFromContracts,
                    populationSlotId: vacated.PopulationSlotId,
                    poolId: vacated.PoolId,
                    areaTag: vacated.AreaTag,
                    isProtectedFromContracts: vacated.IsProtectedFromContracts,
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
            var index = 0;
            var pools = _populationDefinition.Pools;
            for (var poolIndex = 0; poolIndex < pools.Length; poolIndex++)
            {
                var pool = pools[poolIndex];
                if (pool?.Slots == null)
                {
                    continue;
                }

                for (var slotIndex = 0; slotIndex < pool.Slots.Length; slotIndex++)
                {
                    var slot = pool.Slots[slotIndex];
                    if (slot == null)
                    {
                        continue;
                    }

                    index++;
                    var civilianId = $"{idPrefix}.{index:0000}";
                    _runtime.Civilians.Add(_generator.GenerateRecord(
                        _appearanceLibrary,
                        civilianId,
                        createdAtDay: 0,
                        slot.SpawnAnchorId,
                        seed: index,
                        isContractEligible: !slot.IsProtectedFromContracts,
                        populationSlotId: slot.PopulationSlotId,
                        poolId: slot.PoolId,
                        areaTag: slot.AreaTag,
                        isProtectedFromContracts: slot.IsProtectedFromContracts,
                        reservedPublicDisplayNames: CollectReservedPublicDisplayNames()));
                }
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
            EnsureCivilianActorComponents(civilian).Initialize(record);

            var agent = civilian.GetComponent<NpcAgent>();
            agent?.InitializeCapabilities();
        }

        private GameObject CreateCivilianActor(string civilianId)
        {
            var civilian = _npcActorPrefab != null
                ? Instantiate(_npcActorPrefab, transform, false)
                : new GameObject();

            civilian.name = $"Civilian_{civilianId}";
            civilian.transform.SetParent(transform, false);
            return civilian;
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

            var metadata = civilian.GetComponent<MainTownPopulationSpawnedCivilian>();
            if (metadata == null)
            {
                metadata = civilian.AddComponent<MainTownPopulationSpawnedCivilian>();
            }

            return metadata;
        }

        private static void ConfigureContractTargetIfEligible(GameObject civilian, CivilianPopulationRecord record)
        {
            if (civilian == null || record == null || !record.IsContractEligible || record.IsProtectedFromContracts)
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

            var sceneBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < sceneBehaviours.Length; i++)
            {
                if (sceneBehaviours[i] is IContractTargetEliminationSink sceneSink)
                {
                    return sceneSink;
                }
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
            var provider = FindFirstObjectByType<StaticContractRuntimeProvider>(FindObjectsInactive.Include);
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
                briefingText: "Locate and eliminate the live procedural target in MainTown.",
                distanceBand: ProceduralContractTargetDistanceMeters,
                payout: ProceduralContractPayout);

            _runtime.LastOfferedCivilianId = target.CivilianId ?? string.Empty;
            provider.SetAvailableContract(_proceduralAvailableContract);
        }

        private static string ResolveTrackedContractTargetId()
        {
            var provider = FindFirstObjectByType<StaticContractRuntimeProvider>(FindObjectsInactive.Include);
            if (provider == null || !provider.TryGetContractSnapshot(out var snapshot))
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
                if (record == null || !record.IsAlive || !record.IsContractEligible || record.IsProtectedFromContracts)
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
                BeardId = source?.BeardId ?? string.Empty,
                OutfitTopId = source?.OutfitTopId ?? string.Empty,
                OutfitBottomId = source?.OutfitBottomId ?? string.Empty,
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

        private string CreateNextCivilianId()
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

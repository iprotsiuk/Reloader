using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Runtime;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.Runtime;
using Reloader.NPCs.Runtime.Dialogue;
using Reloader.NPCs.Runtime.Capabilities;
using Reloader.NPCs.World;
using Reloader.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Reloader.World.Tests.PlayMode
{
    public class MainTownPopulationInfrastructurePlayModeTests
    {
        private const string MainTownSceneName = "MainTown";
        private const float SceneSwitchTimeoutSeconds = 8f;

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_HasPopulationDefinitionAndStarterPoolsConfigured()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var root = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(root, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var bridge = root!.GetComponent<CivilianPopulationRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null, "Expected CivilianPopulationRuntimeBridge on MainTownPopulationRuntime.");

            var bridgeType = typeof(CivilianPopulationRuntimeBridge);
            var definitionField = bridgeType.GetField("_populationDefinition", BindingFlags.Instance | BindingFlags.NonPublic);
            var libraryField = bridgeType.GetField("_appearanceLibrary", BindingFlags.Instance | BindingFlags.NonPublic);
            var actorPrefabField = bridgeType.GetField("_npcActorPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(definitionField, Is.Not.Null);
            Assert.That(libraryField, Is.Not.Null);
            Assert.That(actorPrefabField, Is.Not.Null);

            var definition = definitionField!.GetValue(bridge);
            Assert.That(definition, Is.Not.Null, "Expected MainTown population definition asset to be assigned.");

            var validateMethod = definition!.GetType().GetMethod("Validate", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(validateMethod, Is.Not.Null);
            Assert.DoesNotThrow(() => validateMethod!.Invoke(definition, null));

            var poolsProperty = definition.GetType().GetProperty("Pools", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(poolsProperty, Is.Not.Null);
            var pools = poolsProperty!.GetValue(definition) as System.Array;
            Assert.That(pools, Is.Not.Null);
            Assert.That(pools!.Length, Is.EqualTo(4), "Expected conservative starter pool count for MainTown.");

            var poolIds = pools.Cast<object>()
                .Select(pool => pool.GetType().GetProperty("PoolId", BindingFlags.Instance | BindingFlags.Public)!.GetValue(pool) as string)
                .ToArray();

            CollectionAssert.AreEquivalent(new[] { "townsfolk", "quarry_workers", "hobos", "cops" }, poolIds);

            var library = libraryField!.GetValue(bridge);
            Assert.That(library, Is.Not.Null, "Expected starter appearance library data to be serialized on the bridge.");

            var actorPrefab = actorPrefabField!.GetValue(bridge) as GameObject;
            Assert.That(actorPrefab, Is.Not.Null, "Expected MainTown to assign an authored NPC actor prefab to the population bridge.");
            Assert.That(actorPrefab!.GetComponent<NpcAgent>(), Is.Not.Null, "Expected the assigned actor prefab to carry the NPC foundation contract.");

            AssertArrayConfigured(library!, "BaseBodyIds");
            AssertArrayConfigured(library!, "PresentationTypes");
            AssertArrayConfigured(library!, "HairIds");
            AssertArrayConfigured(library!, "HairColorIds");
            AssertArrayConfigured(library!, "BeardIds");
            AssertArrayConfigured(library!, "OutfitTopIds");
            AssertArrayConfigured(library!, "OutfitBottomIds");
            AssertArrayConfigured(library!, "OuterwearIds");
        }

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_LoadScene_AutomaticallySeedsAndBuildsStarterPopulation()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var root = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(root, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var bridge = root!.GetComponent<CivilianPopulationRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null, "Expected CivilianPopulationRuntimeBridge on MainTownPopulationRuntime.");

            Assert.That(bridge!.Runtime.Civilians.Count, Is.EqualTo(4), "Expected automatic starter population seeding from the authored definition.");

            var spawnedAgents = root.GetComponentsInChildren<NpcAgent>(includeInactive: true);
            Assert.That(spawnedAgents.Length, Is.EqualTo(4), "Expected automatic runtime rebuild to spawn one placeholder civilian per starter slot.");

            var metadata = root.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            Assert.That(metadata.Length, Is.EqualTo(4), "Expected every auto-spawned civilian to carry slot metadata.");
            Assert.That(metadata.All(component => component.GetComponent<MainTownNpcAppearanceApplicator>() != null),
                Is.True,
                "Expected starter civilians to instantiate the authored NPC actor prefab instead of ad-hoc shell objects.");
            Assert.That(metadata.All(component => component.GetComponent<DialogueCapability>() != null),
                Is.True,
                "Expected every spawned civilian to expose runtime dialogue.");
            Assert.That(metadata.All(component =>
            {
                var capability = component.GetComponent<DialogueCapability>();
                return capability != null
                    && capability.Definition != null
                    && capability.Definition.IsValid(out _)
                    && capability.Definition.TryGetEntryNode(out var entryNode)
                    && entryNode != null
                    && !string.IsNullOrWhiteSpace(entryNode.SpeakerText);
            }), Is.True, "Expected every spawned civilian to receive a valid runtime-generated dialogue definition.");
            Assert.That(metadata.Select(component => component.transform.position).Distinct().Count(), Is.EqualTo(4),
                "Expected authored population slot anchors to occupy distinct scene positions.");

            CollectionAssert.AreEquivalent(
                new[] { "townsfolk.001", "quarry_workers.001", "hobos.001", "cops.001" },
                metadata.Select(component => component.PopulationSlotId).ToArray());
        }

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_RebuildScenePopulation_SpawnsLiveOccupantsAndSkipsDeadSlots()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var root = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(root, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var bridge = root!.GetComponent<CivilianPopulationRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null, "Expected CivilianPopulationRuntimeBridge on MainTownPopulationRuntime.");

            bridge!.Runtime.Civilians.Clear();
            bridge.Runtime.PendingReplacements.Clear();
            bridge.Runtime.Civilians.Add(CreateRecord(
                civilianId: "citizen.mainTown.0001",
                populationSlotId: "townsfolk.001",
                poolId: "townsfolk",
                spawnAnchorId: "Anchor_Townsfolk_01",
                areaTag: "maintown.square",
                isAlive: true,
                retiredAtDay: -1));
            bridge.Runtime.Civilians.Add(CreateRecord(
                civilianId: "citizen.mainTown.0002",
                populationSlotId: "cops.001",
                poolId: "cops",
                spawnAnchorId: "Anchor_Cop_01",
                areaTag: "maintown.watch",
                isAlive: false,
                retiredAtDay: 2));

            var bridgeType = typeof(CivilianPopulationRuntimeBridge);
            var rebuildMethod = bridgeType.GetMethod("RebuildScenePopulation", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(rebuildMethod, Is.Not.Null, "Expected a public RebuildScenePopulation() method.");

            Assert.DoesNotThrow(() => rebuildMethod!.Invoke(bridge, null));
            yield return null;

            var spawnedAgents = root.GetComponentsInChildren<NpcAgent>(includeInactive: true);
            Assert.That(spawnedAgents.Length, Is.EqualTo(1), "Expected only live civilian slots to produce runtime NPCs.");

            var metadataType = Type.GetType("Reloader.NPCs.Runtime.MainTownPopulationSpawnedCivilian, Reloader.NPCs", throwOnError: false);
            Assert.That(metadataType, Is.Not.Null, "Expected a runtime component carrying spawned civilian slot metadata.");

            var metadata = spawnedAgents[0].GetComponent(metadataType!);
            Assert.That(metadata, Is.Not.Null, "Expected spawned NPC to carry slot metadata.");
            Assert.That(GetProperty<string>(metadata!, "PopulationSlotId"), Is.EqualTo("townsfolk.001"));
            Assert.That(GetProperty<string>(metadata!, "CivilianId"), Is.EqualTo("citizen.mainTown.0001"));
            Assert.That(GetProperty<string>(metadata!, "PoolId"), Is.EqualTo("townsfolk"));

            var anchor = root.transform.Find("Anchor_Townsfolk_01");
            Assert.That(anchor, Is.Not.Null, "Expected authored spawn anchor to exist.");
            Assert.That(spawnedAgents[0].transform.position, Is.EqualTo(anchor!.position));
        }

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_RebuildScenePopulation_SameFrameLookupsOnlySeeReplacementCivilian()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var root = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(root, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var bridge = root!.GetComponent<CivilianPopulationRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null, "Expected CivilianPopulationRuntimeBridge on MainTownPopulationRuntime.");

            bridge!.Runtime.Civilians.Clear();
            bridge.Runtime.PendingReplacements.Clear();
            bridge.Runtime.Civilians.Add(CreateRecord(
                civilianId: "citizen.mainTown.0001",
                populationSlotId: "townsfolk.001",
                poolId: "townsfolk",
                spawnAnchorId: "Anchor_Townsfolk_01",
                areaTag: "maintown.square",
                isAlive: true,
                retiredAtDay: -1));

            bridge.RebuildScenePopulation();
            yield return null;

            Assert.That(bridge.TryResolveSpawnedCivilian("citizen.mainTown.0001", out _), Is.True,
                "Expected the initial rebuild to resolve the first civilian.");

            bridge.Runtime.Civilians.Clear();
            bridge.Runtime.Civilians.Add(CreateRecord(
                civilianId: "citizen.mainTown.0002",
                populationSlotId: "cops.001",
                poolId: "cops",
                spawnAnchorId: "Anchor_Cop_01",
                areaTag: "maintown.watch",
                isAlive: true,
                retiredAtDay: -1));

            bridge.RebuildScenePopulation();

            var spawned = root.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            Assert.That(spawned.Select(component => component.CivilianId).ToArray(), Is.EqualTo(new[] { "citizen.mainTown.0002" }),
                "Expected same-frame bridge children to exclude civilians scheduled for deferred destruction.");
            Assert.That(bridge.TryResolveSpawnedCivilian("citizen.mainTown.0001", out _), Is.False,
                "Expected same-frame lookups to stop resolving the prior civilian after rebuild.");
            Assert.That(bridge.TryResolveSpawnedCivilian("citizen.mainTown.0002", out var resolved), Is.True,
                "Expected same-frame lookups to resolve the replacement civilian immediately after rebuild.");
            Assert.That(resolved!.CivilianId, Is.EqualTo("citizen.mainTown.0002"));
        }

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_InteractInputOnSpawnedCivilian_OpensDialogueOnEPress()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var root = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(root, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var bridge = root!.GetComponent<CivilianPopulationRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null, "Expected CivilianPopulationRuntimeBridge on MainTownPopulationRuntime.");

            var spawned = root.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            Assert.That(spawned.Length, Is.GreaterThan(0), "Expected starter population civilians in MainTown.");

            var playerRoot = GameObject.Find("PlayerRoot");
            Assert.That(playerRoot, Is.Not.Null, "Expected PlayerRoot in MainTown.");

            var interactionController = playerRoot!.GetComponent<PlayerNpcInteractionController>();
            var resolver = playerRoot.GetComponent<PlayerNpcResolver>();
            var playerCamera = playerRoot.GetComponentInChildren<Camera>(includeInactive: true);
            Assert.That(interactionController, Is.Not.Null, "Expected PlayerNpcInteractionController on PlayerRoot.");
            Assert.That(resolver, Is.Not.Null, "Expected PlayerNpcResolver on PlayerRoot.");
            Assert.That(playerCamera, Is.Not.Null, "Expected player camera in MainTown.");

            var input = playerRoot.AddComponent<TestInputSource>();
            interactionController!.Configure(input, resolver!);

            var targetCivilian = spawned[0];
            var targetPoint = targetCivilian.transform.position + Vector3.up * 1.35f;
            var cameraOffset = playerCamera!.transform.position - playerRoot.transform.position;
            var approachDirection = targetCivilian.transform.forward.sqrMagnitude > 0.001f
                ? targetCivilian.transform.forward.normalized
                : Vector3.forward;
            playerRoot.transform.position = targetPoint - (approachDirection * 2f) - cameraOffset;
            playerCamera.transform.LookAt(targetPoint);
            Physics.SyncTransforms();

            NpcActionExecutionResult interactionResult = default;
            var interactionRaised = false;
            interactionController.InteractionProcessed += result =>
            {
                interactionRaised = true;
                interactionResult = result;
            };

            input.PickupPressedThisFrame = true;
            interactionController.Tick();
            yield return null;

            var runtime = playerRoot.GetComponent<DialogueRuntimeController>();
            Assert.That(interactionRaised, Is.True, "Expected E interaction to execute against the targeted civilian.");
            Assert.That(interactionResult.Success, Is.True);
            Assert.That(interactionResult.ActionKey, Is.EqualTo(DialogueCapability.ActionKey));
            Assert.That(runtime, Is.Not.Null, "Expected player host dialogue runtime after civilian interaction.");
            Assert.That(runtime!.HasActiveConversation, Is.True, "Expected spawned civilian dialogue to open on E press.");
            Assert.That(runtime.ActiveConversation.CurrentNode.SpeakerText, Is.EqualTo("Nice weather today."));
            Assert.That(runtime.ActiveConversation.SpeakerTransform.name, Is.Not.EqualTo("DialogueFocusTarget"),
                "Expected dialogue to frame the live civilian visual anchor instead of the synthetic fallback target.");
            Assert.That(runtime.ActiveConversation.SpeakerTransform.position.y, Is.GreaterThan(targetCivilian.transform.position.y + 1f),
                "Expected dialogue focus to lock near the civilian head rather than the root pivot.");

            var facingDot = -1f;
            var timeoutAt = Time.time + 1f;
            while (Time.time < timeoutAt)
            {
                var planarToPlayerDuringTurn = playerRoot.transform.position - targetCivilian.transform.position;
                planarToPlayerDuringTurn.y = 0f;
                if (planarToPlayerDuringTurn.sqrMagnitude > 0.0001f)
                {
                    facingDot = Vector3.Dot(targetCivilian.transform.forward.normalized, planarToPlayerDuringTurn.normalized);
                    if (facingDot > 0.8f)
                    {
                        break;
                    }
                }

                yield return null;
            }

            var planarToPlayer = playerRoot.transform.position - targetCivilian.transform.position;
            planarToPlayer.y = 0f;
            facingDot = Vector3.Dot(targetCivilian.transform.forward.normalized, planarToPlayer.normalized);
            Assert.That(facingDot, Is.GreaterThan(0.8f),
                "Expected the civilian to turn toward the player while the dialogue is active.");
        }

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_EventSystemUiModule_UsesCurrentProjectInputActions()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var eventSystem = GameObject.Find("EventSystem");
            Assert.That(eventSystem, Is.Not.Null, "Expected EventSystem root in MainTown.");

            var inputSystemUiModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            Assert.That(inputSystemUiModuleType, Is.Not.Null, "Expected InputSystemUIInputModule type to resolve.");

            var uiModule = eventSystem!.GetComponent(inputSystemUiModuleType!);
            Assert.That(uiModule, Is.Not.Null, "Expected InputSystemUIInputModule on MainTown EventSystem.");

            var playerInput = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>(FindObjectsInactive.Include);
            Assert.That(playerInput, Is.Not.Null, "Expected PlayerInputReader in MainTown.");

            var actionsField = typeof(PlayerInputReader).GetField("_actionsAsset", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(actionsField, Is.Not.Null, "Expected PlayerInputReader actions asset field.");

            var playerActions = actionsField!.GetValue(playerInput) as UnityEngine.Object;
            Assert.That(playerActions, Is.Not.Null, "Expected MainTown PlayerInputReader to reference the project input actions asset.");

            var actionsAssetProperty = inputSystemUiModuleType!.GetProperty("actionsAsset", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(actionsAssetProperty, Is.Not.Null, "Expected InputSystemUIInputModule.actionsAsset property.");
            Assert.That(actionsAssetProperty!.GetValue(uiModule), Is.SameAs(playerActions), "Expected EventSystem UI actions to use the same asset as the player input reader.");

            var pointProperty = inputSystemUiModuleType.GetProperty("point", BindingFlags.Instance | BindingFlags.Public);
            var leftClickProperty = inputSystemUiModuleType.GetProperty("leftClick", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(pointProperty, Is.Not.Null, "Expected InputSystemUIInputModule.point property.");
            Assert.That(leftClickProperty, Is.Not.Null, "Expected InputSystemUIInputModule.leftClick property.");

            var pointReference = pointProperty!.GetValue(uiModule);
            var leftClickReference = leftClickProperty!.GetValue(uiModule);
            Assert.That(pointReference, Is.Not.Null, "Expected UI point action reference to be assigned.");
            Assert.That(leftClickReference, Is.Not.Null, "Expected UI left-click action reference to be assigned.");

            var actionProperty = pointReference!.GetType().GetProperty("action", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(actionProperty, Is.Not.Null, "Expected InputActionReference.action property.");
            Assert.That(actionProperty!.GetValue(pointReference), Is.Not.Null, "Expected UI point action to resolve to a real input action.");
            Assert.That(actionProperty.GetValue(leftClickReference), Is.Not.Null, "Expected UI left-click action to resolve to a real input action.");
        }

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_EachSpawnedCivilian_AllowsTalkInteractionWhenTargeted()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var root = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(root, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var spawned = root!.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            Assert.That(spawned.Length, Is.GreaterThan(0), "Expected starter population civilians in MainTown.");

            var playerRoot = GameObject.Find("PlayerRoot");
            Assert.That(playerRoot, Is.Not.Null, "Expected PlayerRoot in MainTown.");

            var interactionController = playerRoot!.GetComponent<PlayerNpcInteractionController>();
            var resolver = playerRoot.GetComponent<PlayerNpcResolver>();
            var playerCamera = playerRoot.GetComponentInChildren<Camera>(includeInactive: true);
            Assert.That(interactionController, Is.Not.Null);
            Assert.That(resolver, Is.Not.Null);
            Assert.That(playerCamera, Is.Not.Null);

            var input = playerRoot.AddComponent<TestInputSource>();
            interactionController!.Configure(input, resolver!);

            var runtime = playerRoot.GetComponent<DialogueRuntimeController>();
            if (runtime == null)
            {
                runtime = playerRoot.AddComponent<DialogueRuntimeController>();
            }

            for (var i = 0; i < spawned.Length; i++)
            {
                var civilian = spawned[i];
                runtime.CloseConversation("test.reset");

                var targetPoint = civilian.transform.position + Vector3.up * 1.35f;
                var cameraOffset = playerCamera!.transform.position - playerRoot.transform.position;
                playerRoot.transform.position = targetPoint - (Vector3.forward * 2f) - cameraOffset;
                playerCamera.transform.LookAt(targetPoint);
                Physics.SyncTransforms();

                Assert.That(interactionController.TryGetInteractionCandidate(out var candidate), Is.True,
                    $"Expected interaction candidate for {civilian.PublicDisplayName}.");
                Assert.That(candidate.ActionText, Is.EqualTo("Talk"));
                Assert.That(candidate.SubjectText, Is.EqualTo(civilian.PublicDisplayName));

                input.PickupPressedThisFrame = true;
                interactionController.Tick();
                yield return null;

                Assert.That(runtime.HasActiveConversation, Is.True,
                    $"Expected dialogue to open for {civilian.PublicDisplayName}.");
                Assert.That(runtime.ActiveConversation.CurrentNode.SpeakerText, Is.Not.Empty);
            }
        }

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_SaveCoordinatorLoad_RebuildsAuthoredSceneFromLoadedPopulationModule()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var root = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(root, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var bridge = root!.GetComponent<CivilianPopulationRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null, "Expected CivilianPopulationRuntimeBridge on MainTownPopulationRuntime.");

            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var repository = new SaveFileRepository();
            var tempDir = Path.Combine(Path.GetTempPath(), "reloader-maintown-population-tests-" + Guid.NewGuid().ToString("N"));
            var savePath = Path.Combine(tempDir, "maintown-population-load.json");
            Directory.CreateDirectory(tempDir);

            try
            {
                var envelope = coordinator.CaptureEnvelope("0.6.0-dev");
                var module = new CivilianPopulationModule();
                module.Civilians.Add(CreateRecord(
                    civilianId: "citizen.mainTown.9001",
                    populationSlotId: "cops.001",
                    poolId: "cops",
                    spawnAnchorId: "Anchor_Cop_01",
                    areaTag: "maintown.watch",
                    isAlive: true,
                    retiredAtDay: -1));
                module.Civilians.Add(CreateRecord(
                    civilianId: "citizen.mainTown.9002",
                    populationSlotId: "hobos.001",
                    poolId: "hobos",
                    spawnAnchorId: "Anchor_Hobo_01",
                    areaTag: "maintown.alley",
                    isAlive: false,
                    retiredAtDay: 4));

                envelope.Modules["CivilianPopulation"] = new ModuleSaveBlock
                {
                    ModuleVersion = 2,
                    PayloadJson = module.CaptureModuleStateJson()
                };

                repository.WriteEnvelope(savePath, envelope);

                Assert.That(root.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true).Length, Is.EqualTo(4));

                coordinator.Load(savePath);
                yield return null;

                Assert.That(bridge!.Runtime.Civilians.Count, Is.EqualTo(2), "Expected runtime civilians to reflect the loaded save module.");

                var loadedCivilian = bridge.Runtime.Civilians.Single(record => record.CivilianId == "citizen.mainTown.9001");
                Assert.That(loadedCivilian.FirstName, Is.EqualTo("Orson"));
                Assert.That(loadedCivilian.LastName, Is.EqualTo("Vale"));

                var spawned = root.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
                Assert.That(spawned.Length, Is.EqualTo(1), "Expected only living civilians from the loaded save to rebuild into the scene.");
                Assert.That(spawned[0].CivilianId, Is.EqualTo("citizen.mainTown.9001"));
                Assert.That(spawned[0].PopulationSlotId, Is.EqualTo("cops.001"));
                Assert.That(spawned[0].PoolId, Is.EqualTo("cops"));

                var anchor = root.transform.Find("Anchor_Cop_01");
                Assert.That(anchor, Is.Not.Null, "Expected authored anchor for loaded civilian.");
                Assert.That(spawned[0].transform.position, Is.EqualTo(anchor!.position));
                Assert.That(root.transform.Find("Civilian_citizen.mainTown.0001"), Is.Null, "Expected starter civilians to be cleared when a save load rebuild runs.");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_ExecutePendingReplacements_AfterMondayRefresh_RebuildsStableSlotWithNewCivilian()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var root = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(root, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var bridge = root!.GetComponent<CivilianPopulationRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null, "Expected CivilianPopulationRuntimeBridge on MainTownPopulationRuntime.");

            bridge!.Runtime.Civilians.Clear();
            bridge.Runtime.PendingReplacements.Clear();
            bridge.Runtime.Civilians.Add(CreateRecord(
                civilianId: "citizen.mainTown.0007",
                populationSlotId: "townsfolk.001",
                poolId: "townsfolk",
                spawnAnchorId: "Anchor_Townsfolk_01",
                areaTag: "maintown.square",
                isAlive: false,
                retiredAtDay: 9));
            bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
            {
                VacatedCivilianId = "citizen.mainTown.0007",
                QueuedAtDay = 9,
                SpawnAnchorId = "Anchor_Townsfolk_01"
            });

            var replacedCount = bridge.ExecutePendingReplacements(currentDay: 14, currentTimeOfDay: 8f);
            yield return null;

            Assert.That(replacedCount, Is.EqualTo(1));
            Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0));

            var replacement = bridge.Runtime.Civilians.Single(record => record.CivilianId == "citizen.mainTown.0008");
            Assert.That(replacement.PopulationSlotId, Is.EqualTo("townsfolk.001"));
            Assert.That(replacement.PoolId, Is.EqualTo("townsfolk"));
            Assert.That(replacement.SpawnAnchorId, Is.EqualTo("Anchor_Townsfolk_01"));
            Assert.That(replacement.CreatedAtDay, Is.EqualTo(14));
            Assert.That(replacement.FirstName, Is.Not.Empty);
            Assert.That(replacement.LastName, Is.Not.Empty);
            Assert.That(
                string.Concat(replacement.FirstName, " ", replacement.LastName),
                Is.Not.EqualTo("Derek Mullen"),
                "Expected Monday replacement to become a different persistent person, not a cloned identity.");

            var spawned = root.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            Assert.That(spawned.Length, Is.EqualTo(1));
            Assert.That(spawned[0].CivilianId, Is.EqualTo("citizen.mainTown.0008"));
            Assert.That(spawned[0].PopulationSlotId, Is.EqualTo("townsfolk.001"));

            var anchor = root.transform.Find("Anchor_Townsfolk_01");
            Assert.That(anchor, Is.Not.Null);
            Assert.That(spawned[0].transform.position, Is.EqualTo(anchor!.position));
        }

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_WorldStateChanged_ExecutesReplacementWhenMondayRefreshTimeArrives()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var root = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(root, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var bridge = root!.GetComponent<CivilianPopulationRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null, "Expected CivilianPopulationRuntimeBridge on MainTownPopulationRuntime.");

            var coreWorldController = UnityEngine.Object.FindFirstObjectByType<CoreWorldController>(FindObjectsInactive.Include);
            Assert.That(coreWorldController, Is.Not.Null, "Expected CoreWorldController in MainTown.");

            bridge!.Runtime.Civilians.Clear();
            bridge.Runtime.PendingReplacements.Clear();
            bridge.Runtime.Civilians.Add(CreateRecord(
                civilianId: "citizen.mainTown.0007",
                populationSlotId: "townsfolk.001",
                poolId: "townsfolk",
                spawnAnchorId: "Anchor_Townsfolk_01",
                areaTag: "maintown.square",
                isAlive: false,
                retiredAtDay: 9));
            bridge.SetCoreWorldController(coreWorldController!);

            bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
            {
                VacatedCivilianId = "citizen.mainTown.0007",
                QueuedAtDay = 9,
                SpawnAnchorId = "Anchor_Townsfolk_01"
            });

            coreWorldController!.SetWorldState(14, 7.5f);
            yield return null;
            coreWorldController.SetWorldState(14, 7.75f);
            yield return null;

            Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(1), "Expected pre-refresh Monday world updates to keep replacement debt pending.");
            Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(1), "Expected no replacement until Monday 08:00 arrives.");

            coreWorldController.SetWorldState(14, 8f);
            yield return null;

            Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0));

            var replacement = bridge.Runtime.Civilians.Single(record => record.CivilianId == "citizen.mainTown.0008");
            Assert.That(replacement.PopulationSlotId, Is.EqualTo("townsfolk.001"));
            Assert.That(replacement.SpawnAnchorId, Is.EqualTo("Anchor_Townsfolk_01"));
            Assert.That(replacement.CreatedAtDay, Is.EqualTo(14));

            var spawned = root.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            Assert.That(spawned.Length, Is.EqualTo(1));
            Assert.That(spawned[0].CivilianId, Is.EqualTo("citizen.mainTown.0008"));
        }

        private static void AssertArrayConfigured(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}'.");
            var values = property!.GetValue(instance) as System.Array;
            Assert.That(values, Is.Not.Null, $"Expected '{propertyName}' to be an array.");
            Assert.That(values!.Length, Is.GreaterThan(0), $"Expected '{propertyName}' to have at least one configured value.");
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

            var elapsed = 0f;
            while (elapsed < SceneSwitchTimeoutSeconds)
            {
                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.IsValid() && activeScene.isLoaded && activeScene.name == sceneName)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.Fail($"Timed out waiting for active scene '{sceneName}'.");
        }

        private static CivilianPopulationRecord CreateRecord(
            string civilianId,
            string populationSlotId,
            string poolId,
            string spawnAnchorId,
            string areaTag,
            bool isAlive,
            int retiredAtDay)
        {
            return new CivilianPopulationRecord
            {
                CivilianId = civilianId,
                PopulationSlotId = populationSlotId,
                PoolId = poolId,
                FirstName = spawnAnchorId == "Anchor_Cop_01" ? "Orson" : "Derek",
                LastName = spawnAnchorId == "Anchor_Cop_01" ? "Vale" : "Mullen",
                Nickname = spawnAnchorId == "Anchor_Hobo_01" ? "Tincan" : string.Empty,
                IsAlive = isAlive,
                IsContractEligible = isAlive,
                IsProtectedFromContracts = false,
                BaseBodyId = "body.male.a",
                PresentationType = "masculine",
                HairId = "hair.short.01",
                HairColorId = "hair.black",
                BeardId = "beard.none",
                OutfitTopId = "top.coat.01",
                OutfitBottomId = "bottom.jeans.01",
                OuterwearId = "outer.gray.coat",
                MaterialColorIds = new System.Collections.Generic.List<string> { "color.gray" },
                GeneratedDescriptionTags = new System.Collections.Generic.List<string> { "gray coat" },
                SpawnAnchorId = spawnAnchorId,
                AreaTag = areaTag,
                CreatedAtDay = 0,
                RetiredAtDay = retiredAtDay
            };
        }

        private static T GetProperty<T>(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}'.");
            return (T)property!.GetValue(instance);
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool PickupPressedThisFrame;

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;
            public bool ConsumeDevConsoleTogglePressed() => false;
            public bool ConsumeAutocompletePressed() => false;
            public int ConsumeSuggestionDelta() => 0;
            public bool ConsumeAimTogglePressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;

            public bool ConsumePickupPressed()
            {
                if (!PickupPressedThisFrame)
                {
                    return false;
                }

                PickupPressedThisFrame = false;
                return true;
            }
        }
    }
}

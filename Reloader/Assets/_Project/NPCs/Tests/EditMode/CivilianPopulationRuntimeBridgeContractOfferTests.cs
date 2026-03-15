using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Core.Runtime;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.Generation;
using Reloader.NPCs.Runtime;
using Reloader.NPCs.Runtime.Capabilities;
using UnityEngine;
using static Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTestSupport;

namespace Reloader.NPCs.Tests.EditMode
{
    public class CivilianPopulationRuntimeBridgeContractOfferTests
    {
        [Test]
        public void RebuildScenePopulation_WhenActorPrefabIsAssigned_InstantiatesActorPrefabWithPopulationMetadata()
        {
            var (go, bridge) = CreateBridgeObject();
            CreateAnchor(go.transform, "Anchor_A", new Vector3(1f, 0f, 0f));
            var actorPrefab = new GameObject("NpcActorPrefab");
            actorPrefab.AddComponent<NpcAgent>();
            actorPrefab.AddComponent<CapsuleCollider>();
            new GameObject("VisualRoot").transform.SetParent(actorPrefab.transform, false);

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary(), npcActorPrefab: actorPrefab);
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0042",
                    FirstName = "Marta",
                    LastName = "Novak",
                    PopulationSlotId = "townsfolk.004",
                    PoolId = "townsfolk",
                    SpawnAnchorId = "Anchor_A",
                    AreaTag = "maintown.square",
                    IsAlive = true
                });

                bridge.RebuildScenePopulation();

                var spawned = go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(true).Single();
                Assert.That(spawned.CivilianId, Is.EqualTo("citizen.mainTown.0042"));
                Assert.That(spawned.GetComponent<NpcAgent>(), Is.Not.Null);
                Assert.That(spawned.GetComponent<AmbientCitizenCapability>(), Is.Not.Null);
                Assert.That(spawned.transform.Find("VisualRoot"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(actorPrefab);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RebuildScenePopulation_WhenCivilianSpawns_AssignsRuntimeDialogueWithGenericAmbientLine()
        {
            var (go, bridge) = CreateBridgeObject();
            CreateAnchor(go.transform, "Anchor_A", new Vector3(1f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary());
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0044",
                    FirstName = "Sonya",
                    LastName = "Novak",
                    PopulationSlotId = "townsfolk.044",
                    PoolId = "townsfolk",
                    SpawnAnchorId = "Anchor_A",
                    AreaTag = "maintown.square",
                    IsAlive = true,
                    IsContractEligible = true,
                    IsProtectedFromContracts = false
                });

                bridge.RebuildScenePopulation();

                var spawned = go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(true).Single();
                var capability = spawned.GetComponent<DialogueCapability>();
                Assert.That(capability, Is.Not.Null);
                Assert.That(capability!.Definition, Is.Not.Null);
                Assert.That(capability.Definition.IsValid(out _), Is.True);
                Assert.That(capability.Definition.TryGetEntryNode(out var entryNode), Is.True);
                Assert.That(entryNode.SpeakerText, Is.EqualTo("Nice weather today."));
                Assert.That(spawned.GetComponent<NpcAgent>()!.CollectActions().Any(action => action.ActionId == DialogueCapability.ActionKey), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RebuildScenePopulation_WhenProceduralOfferIsPublished_AddsContractTargetDamageableOnlyToOfferedCivilian()
        {
            var (go, bridge, providerGo, provider) = CreateBridgeWithProvider();
            CreateAnchor(go.transform, "Anchor_A", new Vector3(1f, 0f, 0f));
            CreateAnchor(go.transform, "Anchor_B", new Vector3(3f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary(), contractRuntimeProvider: provider);
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0101", "townsfolk.101", "Anchor_A", true, -1));
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0102",
                    FirstName = "Nadia",
                    LastName = "Kozak",
                    PopulationSlotId = "townsfolk.102",
                    PoolId = "townsfolk",
                    SpawnAnchorId = "Anchor_B",
                    AreaTag = "maintown.square",
                    IsAlive = true,
                    IsContractEligible = true,
                    IsProtectedFromContracts = false,
                    BaseBodyId = "body.male.a",
                    PresentationType = "masculine",
                    HairId = "hair.short.01",
                    HairColorId = "hair.black",
                    BeardId = "beard.none",
                    OutfitTopId = "top.coat.01",
                    OutfitBottomId = "bottom.jeans.01",
                    OuterwearId = "outer.gray.coat",
                    MaterialColorIds = new List<string> { "color.gray" },
                    GeneratedDescriptionTags = new List<string> { "gray coat" },
                    CreatedAtDay = 4,
                    RetiredAtDay = -1
                });
                bridge.Runtime.OfferRotationSeed = 2;

                bridge.RebuildScenePopulation();

                Assert.That(provider.TryGetContractSnapshot(out var snapshot), Is.True);
                Assert.That(snapshot.TargetId, Is.EqualTo("citizen.mainTown.0101"));
                Assert.That(snapshot.TargetDisplayName, Is.EqualTo("Derek Mullen"));

                var spawned = go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(true);
                var offered = spawned.Single(component => component.CivilianId == snapshot.TargetId);
                var other = spawned.Single(component => component.CivilianId == "citizen.mainTown.0102");

                var damageableType = System.Type.GetType("Reloader.Weapons.World.ContractTargetDamageable, Reloader.Weapons", throwOnError: false);
                Assert.That(damageableType, Is.Not.Null);

                var offeredDamageable = offered.GetComponent(damageableType!);
                Assert.That(offeredDamageable, Is.Not.Null);
                Assert.That((string)damageableType!.GetProperty("TargetId")!.GetValue(offeredDamageable)!, Is.EqualTo("citizen.mainTown.0101"));
                Assert.That((string)damageableType.GetProperty("DisplayName")!.GetValue(offeredDamageable)!, Is.EqualTo("Derek Mullen"));
                Assert.That(other.GetComponent(damageableType), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RebuildScenePopulation_WhenNoEligibleCiviliansRemain_ClearsAvailableProceduralContractOffer()
        {
            var (go, bridge, providerGo, provider) = CreateBridgeWithProvider();
            CreateAnchor(go.transform, "Anchor_A", new Vector3(1f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary(), contractRuntimeProvider: provider);
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0201", "townsfolk.201", "Anchor_A", true, -1));
                bridge.Runtime.OfferRotationSeed = 7;

                bridge.RebuildScenePopulation();
                Assert.That(provider.TryGetContractSnapshot(out var initialSnapshot), Is.True);
                Assert.That(initialSnapshot.HasAvailableContract, Is.True);
                Assert.That(initialSnapshot.TargetId, Is.EqualTo("citizen.mainTown.0201"));

                bridge.Runtime.Civilians[0].IsContractEligible = false;
                bridge.RebuildScenePopulation();

                Assert.That(provider.TryGetContractSnapshot(out _), Is.False);
            }
            finally
            {
                DestroyProceduralContractDefinition(bridge);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RebuildScenePopulation_WhenProceduralOfferIsPublished_DerivesTargetDescriptionFromPersistedAppearanceFields()
        {
            var (go, bridge, providerGo, provider) = CreateBridgeWithProvider();
            CreateAnchor(go.transform, "Anchor_A", new Vector3(1f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary(), contractRuntimeProvider: provider);
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0202",
                    FirstName = "Nadia",
                    LastName = "Kozak",
                    PopulationSlotId = "townsfolk.202",
                    PoolId = "townsfolk",
                    SpawnAnchorId = "Anchor_A",
                    AreaTag = "maintown.square",
                    IsAlive = true,
                    IsContractEligible = true,
                    IsProtectedFromContracts = false,
                    BaseBodyId = "female.body",
                    PresentationType = "feminine",
                    HairId = "hair.long",
                    HairColorId = "hair.brown",
                    BeardId = "beard.none",
                    OutfitTopId = "tshirt1",
                    OutfitBottomId = "pants1",
                    OuterwearId = "hoody",
                    MaterialColorIds = new List<string> { "color.red" },
                    GeneratedDescriptionTags = new List<string> { "old tag should not win" },
                    CreatedAtDay = 4,
                    RetiredAtDay = -1
                });

                bridge.RebuildScenePopulation();

                Assert.That(provider.TryGetContractSnapshot(out var snapshot), Is.True);
                Assert.That(snapshot.TargetId, Is.EqualTo("citizen.mainTown.0202"));
                Assert.That(snapshot.TargetDescription, Is.EqualTo("female, red hoodie, long hair"));
            }
            finally
            {
                DestroyProceduralContractDefinition(bridge);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RebuildScenePopulation_WhenProceduralOfferIsPublished_DerivesBriefingTextFromPoolAndArea()
        {
            var (go, bridge, providerGo, provider) = CreateBridgeWithProvider();
            CreateAnchor(go.transform, "Anchor_A", new Vector3(1f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary(), contractRuntimeProvider: provider);
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0203",
                    FirstName = "Derek",
                    LastName = "Mullen",
                    PopulationSlotId = "quarry.worker.003",
                    PoolId = "quarry_workers",
                    SpawnAnchorId = "Anchor_A",
                    AreaTag = "quarry",
                    IsAlive = true,
                    IsContractEligible = true,
                    IsProtectedFromContracts = false,
                    BaseBodyId = "male.body",
                    PresentationType = "masculine",
                    HairId = "hair.short",
                    HairColorId = "hair.black",
                    BeardId = "beard.full",
                    OutfitTopId = "top.coat.01",
                    OutfitBottomId = "pants1",
                    OuterwearId = "outer.gray.coat",
                    MaterialColorIds = new List<string> { "color.gray" },
                    GeneratedDescriptionTags = new List<string> { "stale placeholder text should not win" },
                    CreatedAtDay = 4,
                    RetiredAtDay = -1
                });

                bridge.RebuildScenePopulation();

                Assert.That(provider.TryGetContractSnapshot(out var snapshot), Is.True);
                Assert.That(snapshot.TargetId, Is.EqualTo("citizen.mainTown.0203"));
                Assert.That(snapshot.BriefingText, Is.EqualTo("Contractor notes: quarry worker, usually found around the quarry. Confirm the visual match before taking the shot."));
            }
            finally
            {
                DestroyProceduralContractDefinition(bridge);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RebuildScenePopulation_WhenHairColorSeedDoesNotGuaranteeVisiblePalette_OmitsHairColorFromTargetDescription()
        {
            var (go, bridge, providerGo, provider) = CreateBridgeWithProvider();
            CreateAnchor(go.transform, "Anchor_A", new Vector3(1f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary(), contractRuntimeProvider: provider);
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0204",
                    FirstName = "Sonya",
                    LastName = "Novak",
                    PopulationSlotId = "cops.204",
                    PoolId = "cops",
                    SpawnAnchorId = "Anchor_A",
                    AreaTag = "maintown.watch",
                    IsAlive = true,
                    IsContractEligible = true,
                    IsProtectedFromContracts = false,
                    BaseBodyId = "female.body",
                    PresentationType = "feminine",
                    HairId = "hair.long",
                    HairColorId = "hair.black",
                    BeardId = string.Empty,
                    OutfitTopId = "tshirt1",
                    OutfitBottomId = "pants1",
                    OuterwearId = "openJacket",
                    MaterialColorIds = new List<string> { "style.default" },
                    GeneratedDescriptionTags = new List<string> { "stale placeholder text should not win" },
                    CreatedAtDay = 4,
                    RetiredAtDay = -1
                });

                bridge.RebuildScenePopulation();

                Assert.That(provider.TryGetContractSnapshot(out var snapshot), Is.True);
                Assert.That(snapshot.TargetId, Is.EqualTo("citizen.mainTown.0204"));
                Assert.That(snapshot.TargetDescription, Is.EqualTo("female, open jacket, long hair"));
            }
            finally
            {
                DestroyProceduralContractDefinition(bridge);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RebuildScenePopulation_WhenMultipleEligibleCiviliansExist_OnlyOfferedCivilianGetsContractTargetDamageable()
        {
            var (go, bridge, providerGo, provider) = CreateBridgeWithProvider();
            CreateAnchor(go.transform, "Anchor_A", new Vector3(1f, 0f, 0f));
            CreateAnchor(go.transform, "Anchor_B", new Vector3(3f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary(), contractRuntimeProvider: provider);
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0401", "townsfolk.401", "Anchor_A", true, -1));
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0402", "townsfolk.402", "Anchor_B", true, -1));

                bridge.RebuildScenePopulation();

                Assert.That(provider.TryGetContractSnapshot(out var snapshot), Is.True);
                var spawned = go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(true);
                var offered = spawned.Single(component => component.CivilianId == snapshot.TargetId);
                var other = spawned.Single(component => component.CivilianId != snapshot.TargetId);

                var damageableType = System.Type.GetType("Reloader.Weapons.World.ContractTargetDamageable, Reloader.Weapons", throwOnError: false);
                var sharedReceiverType = System.Type.GetType("Reloader.NPCs.Combat.HumanoidDamageReceiver, Reloader.NPCs", throwOnError: false);
                Assert.That(damageableType, Is.Not.Null);
                Assert.That(sharedReceiverType, Is.Not.Null);
                Assert.That(offered.GetComponent(damageableType!), Is.Not.Null);
                Assert.That(other.GetComponent(damageableType!), Is.Null);
                Assert.That(spawned.All(component => component.GetComponent(sharedReceiverType!) != null), Is.True);
            }
            finally
            {
                DestroyProceduralContractDefinition(bridge);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RebuildScenePopulation_WhenDuplicateTrackedCivilianIdsExist_OnlyOneSpawnGetsContractTargetDamageable()
        {
            var (go, bridge, providerGo, provider) = CreateBridgeWithProvider();
            CreateAnchor(go.transform, "Anchor_A", new Vector3(1f, 0f, 0f));
            CreateAnchor(go.transform, "Anchor_B", new Vector3(3f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary(), contractRuntimeProvider: provider);
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0444", "townsfolk.444a", "Anchor_A", true, -1));
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0444", "townsfolk.444b", "Anchor_B", true, -1));
                bridge.Runtime.OfferRotationSeed = 5;

                bridge.RebuildScenePopulation();

                Assert.That(provider.TryGetContractSnapshot(out var snapshot), Is.True);
                Assert.That(snapshot.TargetId, Is.EqualTo("citizen.mainTown.0444"));

                var damageableType = System.Type.GetType("Reloader.Weapons.World.ContractTargetDamageable, Reloader.Weapons", throwOnError: false);
                var duplicateSpawns = go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(true)
                    .Where(component => component.CivilianId == snapshot.TargetId)
                    .ToArray();

                Assert.That(damageableType, Is.Not.Null);
                Assert.That(duplicateSpawns.Length, Is.EqualTo(2));
                Assert.That(duplicateSpawns.Count(component => component.GetComponent(damageableType!) != null), Is.EqualTo(1));
            }
            finally
            {
                DestroyProceduralContractDefinition(bridge);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RebuildScenePopulation_AfterAcceptedContractIsCanceled_RotatesOfferToNextEligibleCivilian()
        {
            var (go, bridge, providerGo, provider) = CreateBridgeWithProvider();
            CreateAnchor(go.transform, "Anchor_A", new Vector3(1f, 0f, 0f));
            CreateAnchor(go.transform, "Anchor_B", new Vector3(3f, 0f, 0f));
            CreateAnchor(go.transform, "Anchor_C", new Vector3(5f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary(), contractRuntimeProvider: provider);
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0501", "townsfolk.501", "Anchor_A", true, -1));
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0502", "townsfolk.502", "Anchor_B", true, -1));
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0503", "townsfolk.503", "Anchor_C", true, -1));
                bridge.Runtime.OfferRotationSeed = 3;

                bridge.RebuildScenePopulation();

                Assert.That(provider.TryGetContractSnapshot(out var initialSnapshot), Is.True);
                Assert.That(initialSnapshot.TargetId, Is.EqualTo("citizen.mainTown.0501"));
                Assert.That(provider.AcceptAvailableContract(), Is.True);
                Assert.That(provider.CancelActiveContract(), Is.True);

                bridge.RebuildScenePopulation();

                Assert.That(provider.TryGetContractSnapshot(out var rotatedSnapshot), Is.True);
                Assert.That(rotatedSnapshot.TargetId, Is.EqualTo("citizen.mainTown.0502"));
            }
            finally
            {
                DestroyProceduralContractDefinition(bridge);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RebuildScenePopulation_WhenNoPreviousOfferExists_UsesOfferRotationSeedInsteadOfFirstRosterEntry()
        {
            var (go, bridge, providerGo, provider) = CreateBridgeWithProvider();
            CreateAnchor(go.transform, "Anchor_A", new Vector3(1f, 0f, 0f));
            CreateAnchor(go.transform, "Anchor_B", new Vector3(3f, 0f, 0f));
            CreateAnchor(go.transform, "Anchor_C", new Vector3(5f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary(), contractRuntimeProvider: provider);
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0601", "townsfolk.601", "Anchor_A", true, -1));
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0602", "townsfolk.602", "Anchor_B", true, -1));
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0603", "townsfolk.603", "Anchor_C", true, -1));
                bridge.Runtime.OfferRotationSeed = 1;

                bridge.RebuildScenePopulation();

                Assert.That(provider.TryGetContractSnapshot(out var snapshot), Is.True);
                Assert.That(snapshot.TargetId, Is.EqualTo("citizen.mainTown.0602"));
            }
            finally
            {
                DestroyProceduralContractDefinition(bridge);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RebuildScenePopulation_WhenFirstEligibleCivilianIsUnspawnable_PublishesNextSpawnableOffer()
        {
            var (go, bridge, providerGo, provider) = CreateBridgeWithProvider();
            CreateAnchor(go.transform, "Anchor_B", new Vector3(3f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary(), contractRuntimeProvider: provider);
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0451", "townsfolk.451", "Anchor_Missing", true, -1));
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0452", "townsfolk.452", "Anchor_B", true, -1));

                bridge.RebuildScenePopulation();

                Assert.That(provider.TryGetContractSnapshot(out var snapshot), Is.True);
                Assert.That(snapshot.HasAvailableContract, Is.True);
                Assert.That(snapshot.TargetId, Is.EqualTo("citizen.mainTown.0452"));
                Assert.That(bridge.TryResolveSpawnedCivilian("citizen.mainTown.0451", out _), Is.False);
                Assert.That(bridge.TryResolveSpawnedCivilian("citizen.mainTown.0452", out _), Is.True);
            }
            finally
            {
                DestroyProceduralContractDefinition(bridge);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RebuildScenePopulation_WhenOfferWasConsumedByPreAcceptKill_DoesNotRepublishOrClearSearch()
        {
            var (go, bridge, providerGo, provider) = CreateBridgeWithProvider();
            CreateAnchor(go.transform, "Anchor_A", new Vector3(1f, 0f, 0f));

            try
            {
                RuntimeKernelBootstrapper.Configure(System.Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary(), contractRuntimeProvider: provider);
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0301", "townsfolk.301", "Anchor_A", true, -1));

                bridge.RebuildScenePopulation();
                Assert.That(provider.TryGetContractSnapshot(out var initialSnapshot), Is.True);

                provider.ReportContractTargetEliminated(initialSnapshot.TargetId, wasExposed: true);
                Assert.That(provider.TryGetContractSnapshot(out _), Is.False);
                Assert.That(GetCurrentHeatLevel(provider), Is.EqualTo(Reloader.Core.Events.PoliceHeatLevel.Search));

                bridge.RebuildScenePopulation();

                Assert.That(provider.TryGetContractSnapshot(out _), Is.False);
                Assert.That(GetCurrentHeatLevel(provider), Is.EqualTo(Reloader.Core.Events.PoliceHeatLevel.Search));
            }
            finally
            {
                RuntimeKernelBootstrapper.Configure(System.Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
                DestroyProceduralContractDefinition(bridge);
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(go);
            }
        }
    }
}

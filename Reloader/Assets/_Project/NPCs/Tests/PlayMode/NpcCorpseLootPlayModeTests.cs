using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reloader.Inventory;
using Reloader.NPCs.Combat;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime;
using Reloader.NPCs.Runtime.Capabilities;
using Reloader.NPCs.Runtime.Dialogue;
using UnityEngine;
using Reloader.Player.Interaction;
using UnityEngine.TestTools;

namespace Reloader.NPCs.Tests.PlayMode
{
    public sealed class NpcCorpseLootPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            StorageRuntimeBridge.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            StorageRuntimeBridge.ResetForTests();
        }

        [UnityTest]
        public IEnumerator LethalImpact_AddsUniqueCorpseStorageAndLeavesItEmpty()
        {
            GameObject npcRoot = null;
            GameObject torsoZone = null;
            try
            {
                var payloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
                var contractTargetDamageableType = ResolveType("Reloader.Weapons.World.ContractTargetDamageable", "Reloader.Weapons");
                Assert.That(payloadType, Is.Not.Null, "Expected ProjectileImpactPayload type.");
                Assert.That(contractTargetDamageableType, Is.Not.Null, "Expected ContractTargetDamageable type.");

                npcRoot = new GameObject("NpcCorpseA");
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                npcRoot.AddComponent<HumanoidCorpseLootController>();
                npcRoot.AddComponent(contractTargetDamageableType!);

                torsoZone = new GameObject("TorsoZone");
                torsoZone.transform.SetParent(npcRoot.transform, false);
                var torsoCollider = torsoZone.AddComponent<CapsuleCollider>();
                torsoCollider.enabled = false;
                var torsoBody = torsoZone.AddComponent<Rigidbody>();
                torsoBody.isKinematic = true;
                torsoBody.useGravity = false;
                torsoZone.AddComponent<BodyZoneHitbox>().Configure(HumanoidBodyZone.Torso);

                yield return null;

                InvokeApplyDamage(receiver, CreateImpactPayload(
                    payloadType!,
                    itemId: "weapon-kar98k",
                    point: torsoZone.transform.position,
                    normal: Vector3.back,
                    damage: 1f,
                    hitObject: torsoZone,
                    sourcePoint: torsoZone.transform.position + (Vector3.back * 25f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 240f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 900f));

                yield return new WaitForFixedUpdate();

                var container = npcRoot.GetComponent<WorldStorageContainer>();
                Assert.That(container, Is.Not.Null, "Expected corpse loot controller to add a world storage container on death.");
                Assert.That(container!.ContainerId, Is.Not.Null.And.Not.Empty, "Expected each corpse storage container to have a unique id.");

                var runtime = container.EnsureRegistered();
                Assert.That(runtime, Is.Not.Null, "Expected corpse container to register a storage runtime.");
                Assert.That(StorageRuntimeBridge.Registry.TryGet(container.ContainerId, out var registered), Is.True,
                    "Expected corpse container to be registered in the shared storage runtime bridge.");
                Assert.That(ReferenceEquals(runtime, registered), Is.True, "Expected EnsureRegistered to return the shared corpse runtime instance.");
                Assert.That(StorageRuntimeBridge.Registry.TryGet("chest.mainTown.workbench.001", out _), Is.False,
                    "Expected corpse storage registration to avoid leaking the default authored chest id.");
                Assert.That(npcRoot.activeSelf, Is.True, "Expected the corpse to remain active for looting rather than despawning.");

                for (var i = 0; i < runtime!.SlotCount; i++)
                {
                    Assert.That(runtime.GetSlotItemId(i), Is.Null, $"Expected corpse storage slot {i} to start empty.");
                }
            }
            finally
            {
                if (torsoZone != null)
                {
                    UnityEngine.Object.Destroy(torsoZone);
                }

                if (npcRoot != null)
                {
                    UnityEngine.Object.Destroy(npcRoot);
                }
            }
        }

        [UnityTest]
        public IEnumerator LethalImpact_SeparateCorpses_ReceiveUniqueStorageContainerIds()
        {
            var payloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            var contractTargetDamageableType = ResolveType("Reloader.Weapons.World.ContractTargetDamageable", "Reloader.Weapons");
            Assert.That(payloadType, Is.Not.Null, "Expected ProjectileImpactPayload type.");
            Assert.That(contractTargetDamageableType, Is.Not.Null, "Expected ContractTargetDamageable type.");

            GameObject firstRoot = null;
            GameObject firstZone = null;
            GameObject secondRoot = null;
            GameObject secondZone = null;
            try
            {
                firstRoot = CreateCorpseReadyNpc("NpcCorpseB", out firstZone);
                secondRoot = CreateCorpseReadyNpc("NpcCorpseC", out secondZone);

                yield return null;

                InvokeApplyDamage(firstRoot.GetComponent<HumanoidDamageReceiver>(), CreateImpactPayload(
                    payloadType!,
                    itemId: "weapon-kar98k",
                    point: firstZone.transform.position,
                    normal: Vector3.back,
                    damage: 1f,
                    hitObject: firstZone,
                    sourcePoint: firstZone.transform.position + (Vector3.back * 25f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 240f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 900f));

                InvokeApplyDamage(secondRoot.GetComponent<HumanoidDamageReceiver>(), CreateImpactPayload(
                    payloadType!,
                    itemId: "weapon-kar98k",
                    point: secondZone.transform.position,
                    normal: Vector3.back,
                    damage: 1f,
                    hitObject: secondZone,
                    sourcePoint: secondZone.transform.position + (Vector3.back * 25f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 240f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 900f));

                yield return new WaitForFixedUpdate();

                var firstContainer = firstRoot.GetComponent<WorldStorageContainer>();
                var secondContainer = secondRoot.GetComponent<WorldStorageContainer>();
                Assert.That(firstContainer, Is.Not.Null);
                Assert.That(secondContainer, Is.Not.Null);
                Assert.That(firstContainer!.ContainerId, Is.Not.EqualTo(secondContainer!.ContainerId),
                    "Expected every corpse to receive a unique storage container id.");
                Assert.That(StorageRuntimeBridge.Registry.TryGet(firstContainer.ContainerId, out var firstRuntime), Is.True);
                Assert.That(StorageRuntimeBridge.Registry.TryGet(secondContainer.ContainerId, out var secondRuntime), Is.True);
                Assert.That(ReferenceEquals(firstRuntime, secondRuntime), Is.False,
                    "Expected corpse storage instances to stay independent.");
            }
            finally
            {
                if (secondZone != null)
                {
                    UnityEngine.Object.Destroy(secondZone);
                }

                if (secondRoot != null)
                {
                    UnityEngine.Object.Destroy(secondRoot);
                }

                if (firstZone != null)
                {
                    UnityEngine.Object.Destroy(firstZone);
                }

                if (firstRoot != null)
                {
                    UnityEngine.Object.Destroy(firstRoot);
                }
            }
        }

        [UnityTest]
        public IEnumerator ResetRuntime_RestoresDisabledBehaviours_AndRemovesCorpseContainer()
        {
            var payloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            Assert.That(payloadType, Is.Not.Null, "Expected ProjectileImpactPayload type.");

            GameObject npcRoot = null;
            GameObject torsoZone = null;
            try
            {
                npcRoot = new GameObject("NpcCorpseReset");
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                var animator = npcRoot.AddComponent<Animator>();
                var aiController = npcRoot.AddComponent<NpcAiController>();
                var patrolMotion = npcRoot.AddComponent<ContractTargetPatrolMotion>();
                var corpseController = npcRoot.AddComponent<HumanoidCorpseLootController>();

                torsoZone = new GameObject("TorsoZone");
                torsoZone.transform.SetParent(npcRoot.transform, false);
                torsoZone.AddComponent<CapsuleCollider>().enabled = false;
                var torsoBody = torsoZone.AddComponent<Rigidbody>();
                torsoBody.isKinematic = true;
                torsoBody.useGravity = false;
                torsoZone.AddComponent<BodyZoneHitbox>().Configure(HumanoidBodyZone.Torso);

                yield return null;

                InvokeApplyDamage(receiver, CreateImpactPayload(
                    payloadType!,
                    itemId: "weapon-kar98k",
                    point: torsoZone.transform.position,
                    normal: Vector3.back,
                    damage: 1f,
                    hitObject: torsoZone,
                    sourcePoint: torsoZone.transform.position + (Vector3.back * 25f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 240f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 900f));

                yield return new WaitForFixedUpdate();

                Assert.That(animator.enabled, Is.False);
                Assert.That(aiController.enabled, Is.False);
                Assert.That(patrolMotion.enabled, Is.False);
                Assert.That(npcRoot.GetComponent<WorldStorageContainer>(), Is.Not.Null);

                corpseController.ResetRuntime();

                Assert.That(animator.enabled, Is.True, "Expected ResetRuntime to re-enable the animator.");
                Assert.That(aiController.enabled, Is.True, "Expected ResetRuntime to re-enable AI.");
                Assert.That(patrolMotion.enabled, Is.True, "Expected ResetRuntime to re-enable patrol motion.");
                Assert.That(npcRoot.GetComponent<WorldStorageContainer>(), Is.Null,
                    "Expected ResetRuntime to remove the runtime corpse storage container.");
            }
            finally
            {
                if (torsoZone != null)
                {
                    UnityEngine.Object.Destroy(torsoZone);
                }

                if (npcRoot != null)
                {
                    UnityEngine.Object.Destroy(npcRoot);
                }
            }
        }

        [UnityTest]
        public IEnumerator LethalImpact_DisablesNpcDialogueActions_AndExposesLootBodyPrompt()
        {
            var payloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            Assert.That(payloadType, Is.Not.Null, "Expected ProjectileImpactPayload type.");

            GameObject playerRoot = null;
            GameObject npcRoot = null;
            GameObject torsoZone = null;
            DialogueDefinition dialogueDefinition = null;
            try
            {
                playerRoot = new GameObject("PlayerRoot");
                var camera = playerRoot.AddComponent<Camera>();
                camera.transform.position = new Vector3(0f, 0.5f, 0f);
                camera.transform.forward = Vector3.forward;

                var storageResolver = playerRoot.AddComponent<PlayerStorageContainerResolver>();
                storageResolver.SetCameraForTests(camera);
                var storageController = playerRoot.AddComponent<PlayerStorageContainerController>();

                npcRoot = new GameObject("Yuri Antonov");
                npcRoot.transform.position = new Vector3(0f, 0f, 2.5f);
                npcRoot.AddComponent<SphereCollider>().radius = 0.45f;
                var agent = npcRoot.AddComponent<NpcAgent>();
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                var corpseController = npcRoot.AddComponent<HumanoidCorpseLootController>();
                dialogueDefinition = AttachDialogueCapabilityWithDefinition(npcRoot);

                torsoZone = new GameObject("TorsoZone");
                torsoZone.transform.SetParent(npcRoot.transform, false);
                torsoZone.AddComponent<CapsuleCollider>().enabled = false;
                var torsoBody = torsoZone.AddComponent<Rigidbody>();
                torsoBody.isKinematic = true;
                torsoBody.useGravity = false;
                torsoZone.AddComponent<BodyZoneHitbox>().Configure(HumanoidBodyZone.Torso);

                yield return null;

                Assert.That(agent.CollectActions().Any(action => action.ActionId == DialogueCapability.ActionKey), Is.True,
                    "Expected living NPCs to expose Talk before the corpse takeover runs.");

                InvokeApplyDamage(receiver, CreateImpactPayload(
                    payloadType!,
                    itemId: "weapon-kar98k",
                    point: torsoZone.transform.position,
                    normal: Vector3.back,
                    damage: 1f,
                    hitObject: torsoZone,
                    sourcePoint: torsoZone.transform.position + (Vector3.back * 25f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 240f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 900f));

                yield return new WaitForFixedUpdate();

                Assert.That(corpseController.CanPresentDeathState, Is.True, "Expected corpse controller to stay active after lethal takeover.");
                Assert.That(agent.CollectActions().Any(action => action.ActionId == DialogueCapability.ActionKey), Is.False,
                    "Dead NPCs should not continue exposing Talk actions.");

                Assert.That(storageController.TryGetInteractionCandidate(out PlayerInteractionCandidate candidate), Is.True,
                    "Expected corpse storage to become the active interaction candidate.");
                Assert.That(candidate.ActionText, Is.EqualTo("Loot body"));
                Assert.That(candidate.SubjectText, Is.EqualTo("Yuri Antonov"));
            }
            finally
            {
                if (dialogueDefinition != null)
                {
                    UnityEngine.Object.Destroy(dialogueDefinition);
                }

                if (torsoZone != null)
                {
                    UnityEngine.Object.Destroy(torsoZone);
                }

                if (npcRoot != null)
                {
                    UnityEngine.Object.Destroy(npcRoot);
                }

                if (playerRoot != null)
                {
                    UnityEngine.Object.Destroy(playerRoot);
                }
            }
        }

        private static GameObject CreateCorpseReadyNpc(string rootName, out GameObject torsoZone)
        {
            var root = new GameObject(rootName);
            root.AddComponent<HumanoidHitboxRig>();
            root.AddComponent<HumanoidDamageReceiver>();
            root.AddComponent<HumanoidCorpseLootController>();
            var contractTargetDamageableType = ResolveType("Reloader.Weapons.World.ContractTargetDamageable", "Reloader.Weapons");
            Assert.That(contractTargetDamageableType, Is.Not.Null, "Expected ContractTargetDamageable type.");
            root.AddComponent(contractTargetDamageableType!);

            torsoZone = new GameObject($"{rootName}_Torso");
            torsoZone.transform.SetParent(root.transform, false);
            torsoZone.AddComponent<CapsuleCollider>().enabled = false;
            var torsoBody = torsoZone.AddComponent<Rigidbody>();
            torsoBody.isKinematic = true;
            torsoBody.useGravity = false;
            torsoZone.AddComponent<BodyZoneHitbox>().Configure(HumanoidBodyZone.Torso);
            return root;
        }

        private static object CreateImpactPayload(
            Type payloadType,
            string itemId,
            Vector3 point,
            Vector3 normal,
            float damage,
            GameObject hitObject,
            Vector3? sourcePoint,
            Vector3? direction,
            float impactSpeedMetersPerSecond,
            float projectileMassGrains,
            float deliveredEnergyJoules)
        {
            return Activator.CreateInstance(
                payloadType,
                itemId,
                point,
                normal,
                damage,
                hitObject,
                sourcePoint,
                direction,
                impactSpeedMetersPerSecond,
                projectileMassGrains,
                deliveredEnergyJoules);
        }

        private static void InvokeApplyDamage(Component receiver, object payload)
        {
            var payloadType = payload.GetType();
            var method = receiver.GetType().GetMethod(
                "ApplyDamage",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { payloadType },
                modifiers: null);
            Assert.That(method, Is.Not.Null, "Expected HumanoidDamageReceiver.ApplyDamage to exist.");
            method!.Invoke(receiver, new[] { payload });
        }

        private static DialogueDefinition AttachDialogueCapabilityWithDefinition(GameObject npc)
        {
            var capability = npc.AddComponent<DialogueCapability>();
            var definition = CreateDialogueDefinition(
                "dialogue.test.corpse-loot",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Need something?",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.ok", "Talk.", string.Empty, string.Empty, string.Empty)
                    }));
            SetField(capability, "_definition", definition);
            return definition;
        }

        private static DialogueDefinition CreateDialogueDefinition(string dialogueId, string entryNodeId, params DialogueNodeDefinition[] nodes)
        {
            var definition = ScriptableObject.CreateInstance<DialogueDefinition>();
            SetField(definition, "_dialogueId", dialogueId);
            SetField(definition, "_entryNodeId", entryNodeId);
            SetField(definition, "_nodes", nodes);
            return definition;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {target.GetType().Name}.");
            field!.SetValue(target, value);
        }

        private static Type ResolveType(string fullName, string assemblyName)
        {
            var type = Type.GetType($"{fullName}, {assemblyName}", throwOnError: false);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}

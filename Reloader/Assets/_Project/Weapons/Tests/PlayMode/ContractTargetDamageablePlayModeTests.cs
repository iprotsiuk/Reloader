using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class ContractTargetDamageablePlayModeTests
    {
        [UnityTest]
        public IEnumerator ApplyDamage_LethalHit_ReportsEliminationAndDisablesTarget()
        {
            var sinkGo = new GameObject("ContractSink");
            var sink = sinkGo.AddComponent<ContractTargetSinkProbe>();
            var targetGo = new GameObject("ContractTarget");
            var target = targetGo.AddComponent<ContractTargetDamageable>();
            targetGo.AddComponent<BoxCollider>();

            SetPrivateField(target, "_eliminationSinkBehaviour", sink);
            SetPrivateField(target, "_targetId", "target.alpha");
            SetPrivateField(target, "_displayName", "Victor Hale");
            SetPrivateField(target, "_maxHealth", 10f);
            target.ResetRuntime();

            yield return null;

            target.ApplyDamage(new ProjectileImpactPayload(
                itemId: "weapon-kar98k",
                point: Vector3.zero,
                normal: Vector3.up,
                damage: 25f,
                hitObject: targetGo,
                sourcePoint: Vector3.back * 50f));

            Assert.That(sink.TargetId, Is.EqualTo("target.alpha"));
            Assert.That(sink.WasExposed, Is.True);
            Assert.That(targetGo.activeSelf, Is.False);

            UnityEngine.Object.Destroy(sinkGo);
            UnityEngine.Object.Destroy(targetGo);
        }

        [UnityTest]
        public IEnumerator ApplyDamage_WithoutExplicitSink_DoesNotReportToUnrelatedSceneSink()
        {
            var unrelatedSinkGo = new GameObject("UnrelatedContractSink");
            var unrelatedSink = unrelatedSinkGo.AddComponent<ContractTargetSinkProbe>();
            var targetGo = new GameObject("ContractTarget");
            var target = targetGo.AddComponent<ContractTargetDamageable>();
            targetGo.AddComponent<BoxCollider>();

            SetPrivateField(target, "_targetId", "target.alpha");
            SetPrivateField(target, "_displayName", "Victor Hale");
            SetPrivateField(target, "_maxHealth", 10f);

            yield return null;

            target.ApplyDamage(new ProjectileImpactPayload(
                itemId: "weapon-kar98k",
                point: Vector3.zero,
                normal: Vector3.up,
                damage: 25f,
                hitObject: targetGo,
                sourcePoint: Vector3.back * 50f));

            Assert.That(unrelatedSink.TargetId, Is.EqualTo(string.Empty));
            Assert.That(unrelatedSink.WasExposed, Is.False);
            Assert.That(targetGo.activeSelf, Is.False);

            UnityEngine.Object.Destroy(unrelatedSinkGo);
            UnityEngine.Object.Destroy(targetGo);
        }

        [UnityTest]
        public IEnumerator ApplyDamage_WithoutSharedReceiver_UsesFallbackAccumulatedHealth()
        {
            var sinkGo = new GameObject("ContractSink");
            var sink = sinkGo.AddComponent<ContractTargetSinkProbe>();
            var targetGo = new GameObject("ContractTarget");
            var target = targetGo.AddComponent<ContractTargetDamageable>();
            targetGo.AddComponent<BoxCollider>();

            SetPrivateField(target, "_eliminationSinkBehaviour", sink);
            SetPrivateField(target, "_targetId", "target.alpha");
            SetPrivateField(target, "_displayName", "Victor Hale");
            SetPrivateField(target, "_maxHealth", 10f);
            target.ResetRuntime();

            yield return null;

            target.ApplyDamage(new ProjectileImpactPayload(
                itemId: "weapon-kar98k",
                point: Vector3.zero,
                normal: Vector3.up,
                damage: 4f,
                hitObject: targetGo,
                sourcePoint: Vector3.back * 50f));

            Assert.That(sink.TargetId, Is.EqualTo(string.Empty));
            Assert.That(sink.WasExposed, Is.False);
            Assert.That(targetGo.activeSelf, Is.True);

            target.ApplyDamage(new ProjectileImpactPayload(
                itemId: "weapon-kar98k",
                point: Vector3.zero,
                normal: Vector3.up,
                damage: 5f,
                hitObject: targetGo,
                sourcePoint: Vector3.back * 50f));

            Assert.That(sink.TargetId, Is.EqualTo(string.Empty));
            Assert.That(sink.WasExposed, Is.False);
            Assert.That(targetGo.activeSelf, Is.True);

            target.ApplyDamage(new ProjectileImpactPayload(
                itemId: "weapon-kar98k",
                point: Vector3.zero,
                normal: Vector3.up,
                damage: 1f,
                hitObject: targetGo,
                sourcePoint: Vector3.back * 50f));

            Assert.That(sink.TargetId, Is.EqualTo("target.alpha"));
            Assert.That(sink.WasExposed, Is.True);
            Assert.That(targetGo.activeSelf, Is.False);

            UnityEngine.Object.Destroy(sinkGo);
            UnityEngine.Object.Destroy(targetGo);
        }

        [UnityTest]
        public IEnumerator ApplyDamage_WithDisabledRagdollController_StillDisablesTarget()
        {
            var ragdollControllerType = ResolveType("Reloader.NPCs.Combat.HumanoidRagdollController", "Reloader.NPCs");
            Assert.That(ragdollControllerType, Is.Not.Null, "Expected HumanoidRagdollController type.");

            var sinkGo = new GameObject("ContractSink");
            var sink = sinkGo.AddComponent<ContractTargetSinkProbe>();
            var targetGo = new GameObject("ContractTarget");
            var target = targetGo.AddComponent<ContractTargetDamageable>();
            targetGo.AddComponent<BoxCollider>();

            var ragdollController = targetGo.AddComponent(ragdollControllerType!);
            Assert.That(ragdollController, Is.InstanceOf<Behaviour>(), "Expected HumanoidRagdollController to be a Behaviour.");
            ((Behaviour)ragdollController).enabled = false;

            SetPrivateField(target, "_eliminationSinkBehaviour", sink);
            SetPrivateField(target, "_targetId", "target.alpha");
            SetPrivateField(target, "_displayName", "Victor Hale");
            SetPrivateField(target, "_maxHealth", 10f);
            target.ResetRuntime();

            yield return null;

            target.ApplyDamage(new ProjectileImpactPayload(
                itemId: "weapon-kar98k",
                point: Vector3.zero,
                normal: Vector3.up,
                damage: 25f,
                hitObject: targetGo,
                sourcePoint: Vector3.back * 50f));

            Assert.That(sink.TargetId, Is.EqualTo("target.alpha"));
            Assert.That(targetGo.activeSelf, Is.False,
                "Expected disabled ragdoll presentation to fall back to the previous hide-on-death behavior.");

            UnityEngine.Object.Destroy(sinkGo);
            UnityEngine.Object.Destroy(targetGo);
        }

        [UnityTest]
        public IEnumerator ApplyDamage_WithRagdollControllerButNoSharedReceiver_StillDisablesTarget()
        {
            var ragdollControllerType = ResolveType("Reloader.NPCs.Combat.HumanoidRagdollController", "Reloader.NPCs");
            Assert.That(ragdollControllerType, Is.Not.Null, "Expected HumanoidRagdollController type.");

            var sinkGo = new GameObject("ContractSink");
            var sink = sinkGo.AddComponent<ContractTargetSinkProbe>();
            var targetGo = new GameObject("ContractTarget");
            var target = targetGo.AddComponent<ContractTargetDamageable>();
            targetGo.AddComponent<BoxCollider>();
            targetGo.AddComponent(ragdollControllerType!);

            SetPrivateField(target, "_eliminationSinkBehaviour", sink);
            SetPrivateField(target, "_targetId", "target.alpha");
            SetPrivateField(target, "_displayName", "Victor Hale");
            SetPrivateField(target, "_maxHealth", 10f);
            target.ResetRuntime();

            yield return null;

            target.ApplyDamage(new ProjectileImpactPayload(
                itemId: "weapon-kar98k",
                point: Vector3.zero,
                normal: Vector3.up,
                damage: 25f,
                hitObject: targetGo,
                sourcePoint: Vector3.back * 50f));

            Assert.That(sink.TargetId, Is.EqualTo("target.alpha"));
            Assert.That(targetGo.activeSelf, Is.False,
                "Expected fallback eliminations without a shared humanoid receiver to keep the old hide-on-death behavior even if a ragdoll controller component is present.");

            UnityEngine.Object.Destroy(sinkGo);
            UnityEngine.Object.Destroy(targetGo);
        }

        [UnityTest]
        public IEnumerator SharedHumanoidReceiver_LethalHeadHit_ReportsEliminationSinkThroughContractBridge()
        {
            GameObject sinkGo = null;
            GameObject targetGo = null;
            GameObject headZoneGo = null;

            try
            {
                sinkGo = new GameObject("ContractSink");
                var sink = sinkGo.AddComponent<ContractTargetSinkProbe>();
                targetGo = new GameObject("ContractTarget");
                var target = targetGo.AddComponent<ContractTargetDamageable>();
                targetGo.AddComponent<BoxCollider>();

                var sharedReceiverType = ResolveType("Reloader.NPCs.Combat.HumanoidDamageReceiver", "Reloader.NPCs");
                var bodyZoneHitboxType = ResolveType("Reloader.NPCs.Combat.BodyZoneHitbox", "Reloader.NPCs");
                var bodyZoneType = ResolveType("Reloader.NPCs.Combat.HumanoidBodyZone", "Reloader.NPCs");

                Assert.That(sharedReceiverType, Is.Not.Null, "Expected shared humanoid receiver to exist.");
                Assert.That(bodyZoneHitboxType, Is.Not.Null, "Expected body-zone hitbox component to exist.");
                Assert.That(bodyZoneType, Is.Not.Null, "Expected HumanoidBodyZone enum to exist.");

                SetPrivateField(target, "_eliminationSinkBehaviour", sink);
                SetPrivateField(target, "_targetId", "target.alpha");
                SetPrivateField(target, "_displayName", "Victor Hale");
                SetPrivateField(target, "_maxHealth", 1000f);

                var sharedReceiver = targetGo.AddComponent(sharedReceiverType!);
                headZoneGo = new GameObject("HeadZone");
                headZoneGo.transform.SetParent(targetGo.transform, false);
                headZoneGo.transform.localPosition = new Vector3(0f, 0f, -0.4f);
                headZoneGo.AddComponent<SphereCollider>().radius = 0.2f;
                var hitbox = headZoneGo.AddComponent(bodyZoneHitboxType!);
                ConfigureBodyZoneHitbox(hitbox, bodyZoneType!, "Head");

                yield return null;

                InvokeApplyDamage(sharedReceiver, new ProjectileImpactPayload(
                    itemId: "weapon-kar98k",
                    point: headZoneGo.transform.position,
                    normal: Vector3.back,
                    damage: 1f,
                    hitObject: headZoneGo,
                    sourcePoint: targetGo.transform.position + (Vector3.back * 25f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 240f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 900f));

                var lastZone = ReadReceiverProperty(sharedReceiver, "LastZone");
                Assert.That(lastZone?.ToString(), Is.EqualTo("Head"));
                Assert.That(ReadLastResultIsLethal(sharedReceiver), Is.True);

                Assert.That(sink.TargetId, Is.EqualTo("target.alpha"));
                Assert.That(sink.WasExposed, Is.True);
                Assert.That(targetGo.activeSelf, Is.False);
            }
            finally
            {
                if (headZoneGo != null)
                {
                    UnityEngine.Object.Destroy(headZoneGo);
                }

                if (sinkGo != null)
                {
                    UnityEngine.Object.Destroy(sinkGo);
                }

                if (targetGo != null)
                {
                    UnityEngine.Object.Destroy(targetGo);
                }
            }
        }

        [UnityTest]
        public IEnumerator SharedHumanoidReceiver_WhenTargetIsReactivated_ResetsSharedDeathState()
        {
            GameObject targetGo = null;
            GameObject headZoneGo = null;
            try
            {
                var sharedReceiverType = ResolveType("Reloader.NPCs.Combat.HumanoidDamageReceiver", "Reloader.NPCs");
                var bodyZoneHitboxType = ResolveType("Reloader.NPCs.Combat.BodyZoneHitbox", "Reloader.NPCs");
                var bodyZoneType = ResolveType("Reloader.NPCs.Combat.HumanoidBodyZone", "Reloader.NPCs");
                Assert.That(sharedReceiverType, Is.Not.Null, "Expected shared humanoid receiver to exist.");
                Assert.That(bodyZoneHitboxType, Is.Not.Null, "Expected body-zone hitbox type.");
                Assert.That(bodyZoneType, Is.Not.Null, "Expected body-zone enum type.");

                targetGo = new GameObject("ContractTarget");
                var target = targetGo.AddComponent<ContractTargetDamageable>();
                targetGo.AddComponent<BoxCollider>();
                SetPrivateField(target, "_targetId", "target.alpha");
                SetPrivateField(target, "_displayName", "Victor Hale");
                SetPrivateField(target, "_maxHealth", 1000f);

                var sharedReceiver = targetGo.AddComponent(sharedReceiverType!);

                headZoneGo = new GameObject("HeadZone");
                headZoneGo.transform.SetParent(targetGo.transform, false);
                headZoneGo.transform.localPosition = new Vector3(0f, 0f, -0.4f);
                headZoneGo.AddComponent<SphereCollider>().radius = 0.2f;
                var hitbox = headZoneGo.AddComponent(bodyZoneHitboxType!);
                ConfigureBodyZoneHitbox(hitbox, bodyZoneType!, "Head");

                yield return null;

                InvokeApplyDamage(sharedReceiver, new ProjectileImpactPayload(
                    itemId: "weapon-kar98k",
                    point: headZoneGo.transform.position,
                    normal: Vector3.back,
                    damage: 1f,
                    hitObject: headZoneGo,
                    sourcePoint: targetGo.transform.position + (Vector3.back * 25f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 240f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 900f));

                Assert.That(targetGo.activeSelf, Is.False, "Expected lethal shared-receiver hit to deactivate the target.");

                targetGo.SetActive(true);
                yield return null;

                Assert.That(targetGo.activeSelf, Is.True,
                    "Expected reactivating a target to clear the shared receiver death state instead of immediately eliminating it again.");
                var isDead = ReadReceiverProperty(sharedReceiver, "IsDead");
                Assert.That(isDead, Is.EqualTo(false));
            }
            finally
            {
                if (headZoneGo != null)
                {
                    UnityEngine.Object.Destroy(headZoneGo);
                }

                if (targetGo != null)
                {
                    UnityEngine.Object.Destroy(targetGo);
                }
            }
        }

        [UnityTest]
        public IEnumerator SharedHumanoidReceiver_WhenTargetIsReenabled_RestoresRagdollRuntimeState()
        {
            GameObject targetGo = null;
            GameObject torsoZoneGo = null;
            try
            {
                var sharedReceiverType = ResolveType("Reloader.NPCs.Combat.HumanoidDamageReceiver", "Reloader.NPCs");
                var ragdollControllerType = ResolveType("Reloader.NPCs.Combat.HumanoidRagdollController", "Reloader.NPCs");
                var bodyZoneHitboxType = ResolveType("Reloader.NPCs.Combat.BodyZoneHitbox", "Reloader.NPCs");
                var bodyZoneType = ResolveType("Reloader.NPCs.Combat.HumanoidBodyZone", "Reloader.NPCs");
                var aiControllerType = ResolveType("Reloader.NPCs.Runtime.NpcAiController", "Reloader.NPCs");
                var patrolMotionType = ResolveType("Reloader.NPCs.Runtime.ContractTargetPatrolMotion", "Reloader.NPCs");

                Assert.That(sharedReceiverType, Is.Not.Null, "Expected shared humanoid receiver to exist.");
                Assert.That(ragdollControllerType, Is.Not.Null, "Expected humanoid ragdoll controller to exist.");
                Assert.That(bodyZoneHitboxType, Is.Not.Null, "Expected body-zone hitbox type.");
                Assert.That(bodyZoneType, Is.Not.Null, "Expected body-zone enum type.");
                Assert.That(aiControllerType, Is.Not.Null, "Expected NPC AI controller type.");
                Assert.That(patrolMotionType, Is.Not.Null, "Expected contract target patrol motion type.");

                targetGo = new GameObject("ContractTarget");
                var target = targetGo.AddComponent<ContractTargetDamageable>();
                var animator = targetGo.AddComponent<Animator>();
                var aiController = (Behaviour)targetGo.AddComponent(aiControllerType!);
                var patrolMotion = (Behaviour)targetGo.AddComponent(patrolMotionType!);
                var sharedReceiver = targetGo.AddComponent(sharedReceiverType!);
                targetGo.AddComponent(ragdollControllerType!);

                SetPrivateField(target, "_targetId", "target.alpha");
                SetPrivateField(target, "_displayName", "Victor Hale");
                SetPrivateField(target, "_maxHealth", 1000f);

                torsoZoneGo = new GameObject("TorsoZone");
                torsoZoneGo.transform.SetParent(targetGo.transform, false);
                var torsoCollider = torsoZoneGo.AddComponent<CapsuleCollider>();
                torsoCollider.enabled = false;
                var torsoBody = torsoZoneGo.AddComponent<Rigidbody>();
                torsoBody.isKinematic = true;
                torsoBody.useGravity = false;
                var hitbox = torsoZoneGo.AddComponent(bodyZoneHitboxType!);
                ConfigureBodyZoneHitbox(hitbox, bodyZoneType!, "Torso");

                yield return null;

                target.ApplyDamage(new ProjectileImpactPayload(
                    itemId: "weapon-kar98k",
                    point: torsoZoneGo.transform.position,
                    normal: Vector3.back,
                    damage: 1f,
                    hitObject: torsoZoneGo,
                    sourcePoint: targetGo.transform.position + (Vector3.back * 25f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 240f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 900f));

                yield return new WaitForFixedUpdate();

                Assert.That(animator.enabled, Is.False, "Expected lethal shared-receiver hit to hand control to the ragdoll path.");
                Assert.That(aiController.enabled, Is.False, "Expected lethal shared-receiver hit to disable NPC AI.");
                Assert.That(patrolMotion.enabled, Is.False, "Expected lethal shared-receiver hit to disable patrol motion.");
                Assert.That(torsoBody.isKinematic, Is.False, "Expected lethal shared-receiver hit to enable dynamic ragdoll bodies.");
                Assert.That(torsoCollider.enabled, Is.True, "Expected lethal shared-receiver hit to enable ragdoll colliders.");

                targetGo.SetActive(false);
                yield return null;
                targetGo.SetActive(true);
                yield return null;

                Assert.That(ReadReceiverProperty(sharedReceiver, "IsDead"), Is.EqualTo(false));
                Assert.That(animator.enabled, Is.True, "Expected re-enable to restore the animator after ragdoll takeover.");
                Assert.That(aiController.enabled, Is.True, "Expected re-enable to restore NPC AI after ragdoll takeover.");
                Assert.That(patrolMotion.enabled, Is.True, "Expected re-enable to restore patrol motion after ragdoll takeover.");
                Assert.That(torsoBody.isKinematic, Is.True, "Expected re-enable to return ragdoll bodies to their dormant kinematic state.");
                Assert.That(torsoBody.useGravity, Is.False, "Expected re-enable to restore dormant ragdoll gravity settings.");
                Assert.That(torsoBody.linearVelocity, Is.EqualTo(Vector3.zero));
                Assert.That(torsoCollider.enabled, Is.False, "Expected re-enable to restore dormant ragdoll colliders.");
            }
            finally
            {
                if (torsoZoneGo != null)
                {
                    UnityEngine.Object.Destroy(torsoZoneGo);
                }

                if (targetGo != null)
                {
                    UnityEngine.Object.Destroy(targetGo);
                }
            }
        }

        [UnityTest]
        public IEnumerator BodyZoneHitbox_SharedReceiverPath_StillIngestsImpactTelemetry()
        {
            var playerDeviceRuntimeStateType = ResolveType("Reloader.PlayerDevice.Runtime.PlayerDeviceRuntimeState", "Reloader.PlayerDevice");
            var deviceTargetBindingType = ResolveType("Reloader.PlayerDevice.Runtime.DeviceTargetBinding", "Reloader.PlayerDevice");
            var deviceAttachmentCatalogType = ResolveType("Reloader.PlayerDevice.World.DeviceAttachmentCatalog", "Reloader.PlayerDevice");
            var playerDeviceControllerType = ResolveType("Reloader.PlayerDevice.World.PlayerDeviceController", "Reloader.PlayerDevice");
            Assert.That(playerDeviceRuntimeStateType, Is.Not.Null, "Expected PlayerDeviceRuntimeState type.");
            Assert.That(deviceTargetBindingType, Is.Not.Null, "Expected DeviceTargetBinding type.");
            Assert.That(deviceAttachmentCatalogType, Is.Not.Null, "Expected DeviceAttachmentCatalog type.");
            Assert.That(playerDeviceControllerType, Is.Not.Null, "Expected PlayerDeviceController type.");

            object runtimeState = null;
            object playerDeviceController = null;
            GameObject sinkGo = null;
            GameObject targetGo = null;
            GameObject headZoneGo = null;
            try
            {
                runtimeState = Activator.CreateInstance(playerDeviceRuntimeStateType!);
                var targetBinding = Activator.CreateInstance(deviceTargetBindingType!, "target.alpha", "Victor Hale", 85f);
                var setTargetBindingMethod = playerDeviceRuntimeStateType!.GetMethod(
                    "SetSelectedTargetBinding",
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    types: new[] { deviceTargetBindingType! },
                    modifiers: null);
                Assert.That(setTargetBindingMethod, Is.Not.Null, "Expected SetSelectedTargetBinding on PlayerDeviceRuntimeState.");
                setTargetBindingMethod!.Invoke(runtimeState, new[] { targetBinding });

                var emptyCatalogProperty = deviceAttachmentCatalogType!.GetProperty(
                    "Empty",
                    BindingFlags.Static | BindingFlags.Public);
                Assert.That(emptyCatalogProperty, Is.Not.Null, "Expected static Empty catalog property.");
                var emptyCatalog = emptyCatalogProperty!.GetValue(null);
                Assert.That(emptyCatalog, Is.Not.Null, "Expected DeviceAttachmentCatalog.Empty value.");

                playerDeviceController = Activator.CreateInstance(
                    playerDeviceControllerType!,
                    new[] { runtimeState, null, emptyCatalog });
                Assert.That(playerDeviceController, Is.Not.Null, "Expected PlayerDeviceController instance.");

                sinkGo = new GameObject("ContractSink");
                var sink = sinkGo.AddComponent<ContractTargetSinkProbe>();
                targetGo = new GameObject("ContractTarget");
                var target = targetGo.AddComponent<ContractTargetDamageable>();
                targetGo.AddComponent<BoxCollider>();

                var rangeMetrics = targetGo.AddComponent<DummyTargetRangeMetrics>();
                rangeMetrics.Configure("target.alpha", "Victor Hale", 85f);

                var sharedReceiverType = ResolveType("Reloader.NPCs.Combat.HumanoidDamageReceiver", "Reloader.NPCs");
                var bodyZoneHitboxType = ResolveType("Reloader.NPCs.Combat.BodyZoneHitbox", "Reloader.NPCs");
                var bodyZoneType = ResolveType("Reloader.NPCs.Combat.HumanoidBodyZone", "Reloader.NPCs");
                Assert.That(sharedReceiverType, Is.Not.Null, "Expected shared receiver type.");
                Assert.That(bodyZoneHitboxType, Is.Not.Null, "Expected body-zone hitbox type.");
                Assert.That(bodyZoneType, Is.Not.Null, "Expected body-zone enum type.");

                SetPrivateField(target, "_eliminationSinkBehaviour", sink);
                SetPrivateField(target, "_targetId", "target.alpha");
                SetPrivateField(target, "_displayName", "Victor Hale");
                SetPrivateField(target, "_maxHealth", 1000f);

                targetGo.AddComponent(sharedReceiverType!);

                headZoneGo = new GameObject("HeadZone");
                headZoneGo.transform.SetParent(targetGo.transform, false);
                headZoneGo.transform.localPosition = new Vector3(0f, 0f, -0.4f);
                headZoneGo.AddComponent<SphereCollider>().radius = 0.2f;
                var hitbox = headZoneGo.AddComponent(bodyZoneHitboxType!);
                ConfigureBodyZoneHitbox(hitbox, bodyZoneType!, "Head");

                yield return null;

                InvokeApplyDamage(hitbox, new ProjectileImpactPayload(
                    itemId: "weapon-kar98k",
                    point: headZoneGo.transform.position,
                    normal: Vector3.back,
                    damage: 1f,
                    hitObject: headZoneGo,
                    sourcePoint: targetGo.transform.position + (Vector3.back * 25f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 240f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 900f));

                Assert.That(ReadActiveGroupShotCount(runtimeState, playerDeviceRuntimeStateType!), Is.EqualTo(1),
                    "Expected shared hitbox path to still ingest impact telemetry into player-device group metrics.");
            }
            finally
            {
                if (playerDeviceController != null)
                {
                    UnregisterActivePlayerDeviceController(playerDeviceController);
                }

                if (headZoneGo != null)
                {
                    UnityEngine.Object.Destroy(headZoneGo);
                }

                if (sinkGo != null)
                {
                    UnityEngine.Object.Destroy(sinkGo);
                }

                if (targetGo != null)
                {
                    UnityEngine.Object.Destroy(targetGo);
                }
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private static void ConfigureBodyZoneHitbox(Component bodyZoneHitbox, Type bodyZoneType, string zoneName)
        {
            var zoneValue = Enum.Parse(bodyZoneType, zoneName);
            var hitboxType = bodyZoneHitbox.GetType();

            var configureMethods = hitboxType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (var i = 0; i < configureMethods.Length; i++)
            {
                var method = configureMethods[i];
                if (!string.Equals(method.Name, "Configure", StringComparison.Ordinal))
                {
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == bodyZoneType)
                {
                    method.Invoke(bodyZoneHitbox, new[] { zoneValue });
                    return;
                }
            }

            if (TrySetZoneMember(bodyZoneHitbox, hitboxType, "_bodyZone", zoneValue) ||
                TrySetZoneMember(bodyZoneHitbox, hitboxType, "_zone", zoneValue) ||
                TrySetZoneMember(bodyZoneHitbox, hitboxType, "BodyZone", zoneValue) ||
                TrySetZoneMember(bodyZoneHitbox, hitboxType, "Zone", zoneValue))
            {
                return;
            }

            Assert.Fail("Expected BodyZoneHitbox to support assigning HumanoidBodyZone.");
        }

        private static bool TrySetZoneMember(object instance, Type type, string memberName, object value)
        {
            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == value.GetType())
            {
                field.SetValue(instance, value);
                return true;
            }

            var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite && property.PropertyType == value.GetType())
            {
                property.SetValue(instance, value);
                return true;
            }

            return false;
        }

        private static Type ResolveType(string fullTypeName, string assemblyName)
        {
            var type = Type.GetType($"{fullTypeName}, {assemblyName}", throwOnError: false);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullTypeName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static void InvokeApplyDamage(Component damageableComponent, ProjectileImpactPayload payload)
        {
            var method = damageableComponent.GetType().GetMethod(
                "ApplyDamage",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(ProjectileImpactPayload) },
                modifiers: null);

            Assert.That(method, Is.Not.Null, "Expected component to expose ApplyDamage(ProjectileImpactPayload).");
            method!.Invoke(damageableComponent, new object[] { payload });
        }

        private static object ReadReceiverProperty(Component sharedReceiver, string propertyName)
        {
            var property = sharedReceiver.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected shared receiver to expose {propertyName}.");
            return property!.GetValue(sharedReceiver);
        }

        private static bool ReadLastResultIsLethal(Component sharedReceiver)
        {
            var result = ReadReceiverProperty(sharedReceiver, "LastResult");
            Assert.That(result, Is.Not.Null, "Expected LastResult to be available.");

            var isLethalProperty = result!.GetType().GetProperty("IsLethal", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(isLethalProperty, Is.Not.Null, "Expected LastResult to expose IsLethal.");

            var isLethal = isLethalProperty!.GetValue(result);
            Assert.That(isLethal, Is.Not.Null, "Expected IsLethal to provide a value.");
            return (bool)isLethal!;
        }

        private static int ReadActiveGroupShotCount(object runtimeState, Type runtimeStateType)
        {
            var activeSessionProperty = runtimeStateType.GetProperty(
                "ActiveGroupSession",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(activeSessionProperty, Is.Not.Null, "Expected ActiveGroupSession property.");

            var session = activeSessionProperty!.GetValue(runtimeState);
            Assert.That(session, Is.Not.Null, "Expected active group session instance.");

            var shotCountProperty = session!.GetType().GetProperty(
                "ShotCount",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(shotCountProperty, Is.Not.Null, "Expected ShotCount on active group session.");
            return (int)shotCountProperty!.GetValue(session)!;
        }

        private static void UnregisterActivePlayerDeviceController(object controller)
        {
            var method = controller.GetType().GetMethod(
                "UnregisterAsActiveInstance",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, "Expected UnregisterAsActiveInstance on PlayerDeviceController.");
            method!.Invoke(controller, null);
        }

        private sealed class ContractTargetSinkProbe : MonoBehaviour, IContractTargetEliminationSink
        {
            public string TargetId { get; private set; } = string.Empty;
            public bool WasExposed { get; private set; }

            public void ReportContractTargetEliminated(string targetId, bool wasExposed)
            {
                TargetId = targetId;
                WasExposed = wasExposed;
            }
        }
    }
}

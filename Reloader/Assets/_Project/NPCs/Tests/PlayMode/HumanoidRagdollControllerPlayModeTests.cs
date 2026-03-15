using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Combat;
using Reloader.NPCs.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.NPCs.Tests.PlayMode
{
    public class HumanoidRagdollControllerPlayModeTests
    {
        [UnityTest]
        public IEnumerator Awake_WithAuthoredDynamicRagdollBody_ForcesDormantKinematicState()
        {
            GameObject npcRoot = null;
            GameObject torsoZone = null;
            try
            {
                npcRoot = new GameObject("NpcRoot");
                npcRoot.AddComponent<HumanoidHitboxRig>();
                npcRoot.AddComponent<HumanoidDamageReceiver>();

                torsoZone = new GameObject("TorsoZone");
                torsoZone.transform.SetParent(npcRoot.transform, false);
                torsoZone.AddComponent<CapsuleCollider>().enabled = false;
                var torsoBody = torsoZone.AddComponent<Rigidbody>();
                torsoBody.isKinematic = false;
                torsoBody.useGravity = true;
                torsoBody.linearVelocity = new Vector3(0f, 0f, 4f);
                torsoBody.angularVelocity = new Vector3(0f, 3f, 0f);
                torsoZone.AddComponent<BodyZoneHitbox>().Configure(HumanoidBodyZone.Torso);

                npcRoot.AddComponent<HumanoidRagdollController>();

                yield return null;

                Assert.That(torsoBody.isKinematic, Is.True,
                    "Expected the controller to force ragdoll bodies into a dormant kinematic state even when the prefab is authored as dynamic.");
                Assert.That(torsoBody.useGravity, Is.False,
                    "Expected dormant ragdoll bodies to ignore authored gravity until lethal takeover.");
                Assert.That(ReadLinearVelocity(torsoBody), Is.EqualTo(Vector3.zero),
                    "Expected dormant ragdoll setup to clear carried-over linear velocity.");
                Assert.That(torsoBody.angularVelocity, Is.EqualTo(Vector3.zero),
                    "Expected dormant ragdoll setup to clear carried-over angular velocity.");
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
        public IEnumerator LethalImpact_DisablesDependencies_EnablesRagdollBodies_AndPushesStruckBodyForward()
        {
            var controllerType = ResolveType("Reloader.NPCs.Combat.HumanoidRagdollController", "Reloader.NPCs");
            Assert.That(controllerType, Is.Not.Null, "Expected HumanoidRagdollController to exist.");

            GameObject npcRoot = null;
            GameObject torsoZone = null;
            try
            {
                var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
                Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

                npcRoot = new GameObject("NpcRoot");
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                var animator = npcRoot.AddComponent<Animator>();
                var aiController = npcRoot.AddComponent<NpcAiController>();
                var patrolMotion = npcRoot.AddComponent<ContractTargetPatrolMotion>();
                npcRoot.AddComponent(controllerType!);

                torsoZone = new GameObject("TorsoZone");
                torsoZone.transform.SetParent(npcRoot.transform, false);
                var torsoCollider = torsoZone.AddComponent<CapsuleCollider>();
                torsoCollider.enabled = false;
                var torsoBody = torsoZone.AddComponent<Rigidbody>();
                torsoBody.isKinematic = true;
                torsoBody.useGravity = false;
                var torsoHitbox = torsoZone.AddComponent<BodyZoneHitbox>();
                torsoHitbox.Configure(HumanoidBodyZone.Torso);

                yield return null;

                InvokeApplyDamage(receiver, CreateImpactPayload(
                    projectileImpactPayloadType!,
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

                Assert.That(animator.enabled, Is.False, "Expected lethal impact to disable the animator before ragdoll takeover.");
                Assert.That(aiController.enabled, Is.False, "Expected lethal impact to disable NPC AI.");
                Assert.That(patrolMotion.enabled, Is.False, "Expected lethal impact to disable patrol motion.");
                Assert.That(torsoBody.isKinematic, Is.False, "Expected ragdoll body to become dynamic on lethal impact.");
                Assert.That(torsoCollider.enabled, Is.True, "Expected ragdoll colliders to be enabled on lethal impact.");
                Assert.That(ReadLinearVelocity(torsoBody).z, Is.GreaterThan(0f),
                    "Expected lethal ragdoll impulse to push the struck body in projectile travel direction.");
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
        public IEnumerator LethalImpact_WithoutRigidBodyOnStruckZone_FallsBackToTorsoImpulse()
        {
            var controllerType = ResolveType("Reloader.NPCs.Combat.HumanoidRagdollController", "Reloader.NPCs");
            Assert.That(controllerType, Is.Not.Null, "Expected HumanoidRagdollController to exist.");

            GameObject npcRoot = null;
            GameObject torsoZone = null;
            GameObject headZone = null;
            try
            {
                var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
                Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

                npcRoot = new GameObject("NpcRoot");
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                npcRoot.AddComponent(controllerType!);

                torsoZone = new GameObject("TorsoZone");
                torsoZone.transform.SetParent(npcRoot.transform, false);
                torsoZone.AddComponent<CapsuleCollider>().enabled = false;
                var torsoBody = torsoZone.AddComponent<Rigidbody>();
                torsoBody.isKinematic = true;
                torsoBody.useGravity = false;
                torsoZone.AddComponent<BodyZoneHitbox>().Configure(HumanoidBodyZone.Torso);

                headZone = new GameObject("HeadZone");
                headZone.transform.SetParent(npcRoot.transform, false);
                headZone.AddComponent<SphereCollider>();
                headZone.AddComponent<BodyZoneHitbox>().Configure(HumanoidBodyZone.Head);

                yield return null;

                InvokeApplyDamage(receiver, CreateImpactPayload(
                    projectileImpactPayloadType!,
                    itemId: "weapon-kar98k",
                    point: headZone.transform.position,
                    normal: Vector3.back,
                    damage: 1f,
                    hitObject: headZone,
                    sourcePoint: headZone.transform.position + (Vector3.back * 25f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 240f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 900f));

                yield return new WaitForFixedUpdate();

                Assert.That(torsoBody.isKinematic, Is.False, "Expected torso fallback body to become dynamic when struck zone has no rigidbody.");
                Assert.That(ReadLinearVelocity(torsoBody).z, Is.GreaterThan(0f),
                    "Expected torso fallback ragdoll body to receive forward impulse when struck zone has no rigidbody.");
            }
            finally
            {
                if (headZone != null)
                {
                    UnityEngine.Object.Destroy(headZone);
                }

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
        public IEnumerator ResetRuntime_AfterLethalImpact_RestoresDormantBodiesAndDependencies()
        {
            var controllerType = ResolveType("Reloader.NPCs.Combat.HumanoidRagdollController", "Reloader.NPCs");
            Assert.That(controllerType, Is.Not.Null, "Expected HumanoidRagdollController to exist.");

            GameObject npcRoot = null;
            GameObject torsoZone = null;
            try
            {
                var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
                Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

                npcRoot = new GameObject("NpcRoot");
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                var animator = npcRoot.AddComponent<Animator>();
                var aiController = npcRoot.AddComponent<NpcAiController>();
                var patrolMotion = npcRoot.AddComponent<ContractTargetPatrolMotion>();
                var controller = npcRoot.AddComponent(controllerType!);

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
                    projectileImpactPayloadType!,
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

                InvokeResetRuntime(controller);

                Assert.That(animator.enabled, Is.True, "Expected ResetRuntime to re-enable the animator.");
                Assert.That(aiController.enabled, Is.True, "Expected ResetRuntime to re-enable NPC AI.");
                Assert.That(patrolMotion.enabled, Is.True, "Expected ResetRuntime to re-enable patrol motion.");
                Assert.That(torsoBody.isKinematic, Is.True, "Expected ResetRuntime to restore dormant kinematic ragdoll bodies.");
                Assert.That(torsoBody.useGravity, Is.False, "Expected ResetRuntime to restore dormant ragdoll gravity state.");
                Assert.That(torsoCollider.enabled, Is.False, "Expected ResetRuntime to restore dormant collider state.");
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

        private static void InvokeResetRuntime(Component controller)
        {
            var method = controller.GetType().GetMethod(
                "ResetRuntime",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, "Expected HumanoidRagdollController.ResetRuntime to exist.");
            method!.Invoke(controller, null);
        }

        private static Vector3 ReadLinearVelocity(Rigidbody rigidbody)
        {
            var linearVelocityProperty = typeof(Rigidbody).GetProperty("linearVelocity", BindingFlags.Instance | BindingFlags.Public);
            if (linearVelocityProperty != null && linearVelocityProperty.PropertyType == typeof(Vector3))
            {
                return (Vector3)linearVelocityProperty.GetValue(rigidbody);
            }

            return rigidbody.velocity;
        }
    }
}

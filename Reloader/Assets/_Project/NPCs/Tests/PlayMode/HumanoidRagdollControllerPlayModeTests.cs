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
        public IEnumerator Awake_WithCuratedRagdollOverrides_DoesNotForceUnlistedPhysicsIntoDormantState()
        {
            GameObject npcRoot = null;
            GameObject torsoZone = null;
            GameObject propZone = null;
            try
            {
                npcRoot = new GameObject("NpcRoot");
                npcRoot.SetActive(false);
                npcRoot.AddComponent<HumanoidHitboxRig>();
                npcRoot.AddComponent<HumanoidDamageReceiver>();

                torsoZone = new GameObject("TorsoZone");
                torsoZone.transform.SetParent(npcRoot.transform, false);
                var torsoCollider = torsoZone.AddComponent<CapsuleCollider>();
                torsoCollider.enabled = false;
                var torsoBody = torsoZone.AddComponent<Rigidbody>();
                torsoBody.isKinematic = false;
                torsoBody.useGravity = true;
                torsoZone.AddComponent<BodyZoneHitbox>().Configure(HumanoidBodyZone.Torso);

                propZone = new GameObject("PropZone");
                propZone.transform.SetParent(npcRoot.transform, false);
                var propCollider = propZone.AddComponent<BoxCollider>();
                propCollider.enabled = true;
                var propBody = propZone.AddComponent<Rigidbody>();
                propBody.isKinematic = false;
                propBody.useGravity = true;

                var controller = npcRoot.AddComponent<HumanoidRagdollController>();
                SetPrivateField(controller, "_ragdollBodies", new[] { torsoBody });
                SetPrivateField(controller, "_ragdollColliders", new Collider[] { torsoCollider });

                npcRoot.SetActive(true);
                yield return null;

                Assert.That(torsoBody.isKinematic, Is.True, "Expected curated ragdoll body to be driven into dormant state.");
                Assert.That(torsoCollider.enabled, Is.False, "Expected curated ragdoll collider to preserve its dormant state.");
                Assert.That(propBody.isKinematic, Is.False, "Expected unlisted helper rigidbody to stay untouched.");
                Assert.That(propBody.useGravity, Is.True, "Expected unlisted helper rigidbody gravity to stay untouched.");
                Assert.That(propCollider.enabled, Is.True, "Expected unlisted helper collider to stay untouched.");
            }
            finally
            {
                if (propZone != null)
                {
                    UnityEngine.Object.Destroy(propZone);
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

        [UnityTest]
        public IEnumerator LethalImpact_WithExplicitAliveColliderDisableList_DisablesAndRestoresRootCollider()
        {
            GameObject npcRoot = null;
            GameObject torsoZone = null;
            try
            {
                var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
                Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

                npcRoot = new GameObject("NpcRoot");
                var rootCollider = npcRoot.AddComponent<CapsuleCollider>();
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                var controller = npcRoot.AddComponent<HumanoidRagdollController>();

                torsoZone = new GameObject("TorsoZone");
                torsoZone.transform.SetParent(npcRoot.transform, false);
                var torsoCollider = torsoZone.AddComponent<CapsuleCollider>();
                torsoCollider.enabled = false;
                var torsoBody = torsoZone.AddComponent<Rigidbody>();
                torsoBody.isKinematic = true;
                torsoBody.useGravity = false;
                torsoZone.AddComponent<BodyZoneHitbox>().Configure(HumanoidBodyZone.Torso);

                SetPrivateField(controller, "_ragdollBodies", new[] { torsoBody });
                SetPrivateField(controller, "_ragdollColliders", new Collider[] { torsoCollider });
                SetPrivateField(controller, "_collidersToDisableOnDeath", new Collider[] { rootCollider });

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

                Assert.That(rootCollider.enabled, Is.False,
                    "Expected alive-state root collider to disable when authored ragdoll takeover begins.");

                InvokeResetRuntime(controller);

                Assert.That(rootCollider.enabled, Is.True,
                    "Expected ResetRuntime to restore alive-state root collider after ragdoll cleanup.");
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
        public IEnumerator PostDeathImpact_OnStruckRagdollBody_AddsFreshImpulse()
        {
            GameObject npcRoot = null;
            GameObject torsoZone = null;
            try
            {
                var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
                Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

                npcRoot = new GameObject("NpcRoot");
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                npcRoot.AddComponent<HumanoidRagdollController>();

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

                Assert.That(receiver.IsDead, Is.True, "Expected the first lethal hit to kill the NPC before corpse re-hit validation.");

                torsoBody.linearVelocity = Vector3.zero;
                torsoBody.angularVelocity = Vector3.zero;
                yield return new WaitForFixedUpdate();

                InvokeApplyDamage(receiver, CreateImpactPayload(
                    projectileImpactPayloadType!,
                    itemId: "weapon-kar98k",
                    point: torsoZone.transform.position,
                    normal: Vector3.left,
                    damage: 1f,
                    hitObject: torsoZone,
                    sourcePoint: torsoZone.transform.position + (Vector3.left * 25f),
                    direction: Vector3.right,
                    impactSpeedMetersPerSecond: 240f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 900f));

                yield return new WaitForFixedUpdate();

                Assert.That(ReadLinearVelocity(torsoBody).x, Is.GreaterThan(0.1f),
                    "Expected a post-death hit to push the already-dead ragdoll body in the new shot direction.");
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
        public IEnumerator Awake_WithoutAuthoredRagdollBody_AddsRootFallbackAndAppliesLethalImpulse()
        {
            GameObject npcRoot = null;
            try
            {
                var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
                Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

                npcRoot = new GameObject("NpcRoot");
                var rootCollider = npcRoot.AddComponent<CapsuleCollider>();
                rootCollider.center = new Vector3(0f, 0.9f, 0f);
                rootCollider.height = 1.8f;
                rootCollider.radius = 0.35f;
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                npcRoot.AddComponent<HumanoidRagdollController>();

                yield return null;

                var rootBody = npcRoot.GetComponent<Rigidbody>();
                Assert.That(rootBody, Is.Not.Null, "Expected thin NPC roots to receive a fallback ragdoll rigidbody.");
                Assert.That(rootBody!.isKinematic, Is.True, "Expected fallback ragdoll body to stay dormant while alive.");
                Assert.That(rootBody.useGravity, Is.False, "Expected fallback ragdoll body to ignore gravity while alive.");

                InvokeApplyDamage(receiver, CreateImpactPayload(
                    projectileImpactPayloadType!,
                    itemId: "weapon-kar98k",
                    point: npcRoot.transform.position + Vector3.up,
                    normal: Vector3.back,
                    damage: 1f,
                    hitObject: npcRoot,
                    sourcePoint: npcRoot.transform.position + (Vector3.back * 25f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 240f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 900f));

                yield return new WaitForFixedUpdate();

                Assert.That(rootBody.isKinematic, Is.False, "Expected fallback ragdoll body to become dynamic on lethal impact.");
                Assert.That(rootBody.useGravity, Is.True, "Expected fallback ragdoll body to enable gravity on lethal impact.");
                Assert.That(ReadLinearVelocity(rootBody).z, Is.GreaterThan(0f),
                    "Expected fallback ragdoll body to receive the final impulse when no authored limb bodies exist.");
            }
            finally
            {
                if (npcRoot != null)
                {
                    UnityEngine.Object.Destroy(npcRoot);
                }
            }
        }

        [UnityTest]
        public IEnumerator LethalImpact_WithAuthoredJointedMultiBodyRagdoll_EnablesWholeChain_WithoutRootFallback()
        {
            var controllerType = ResolveType("Reloader.NPCs.Combat.HumanoidRagdollController", "Reloader.NPCs");
            Assert.That(controllerType, Is.Not.Null, "Expected HumanoidRagdollController to exist.");

            GameObject npcRoot = null;
            GameObject bodyRoot = null;
            GameObject pelvisZone = null;
            GameObject chestZone = null;
            GameObject headZone = null;
            try
            {
                var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
                Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

                npcRoot = new GameObject("NpcRoot");
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();

                bodyRoot = new GameObject("Body");
                bodyRoot.transform.SetParent(npcRoot.transform, false);
                var aliveCollider = bodyRoot.AddComponent<CapsuleCollider>();
                aliveCollider.enabled = true;

                pelvisZone = CreateJointedRagdollBody(npcRoot.transform, "PelvisZone", HumanoidBodyZone.Torso, null, out var pelvisBody);
                chestZone = CreateJointedRagdollBody(npcRoot.transform, "ChestZone", HumanoidBodyZone.Torso, pelvisBody, out var chestBody);
                headZone = CreateJointedRagdollBody(npcRoot.transform, "HeadZone", HumanoidBodyZone.Head, chestBody, out var headBody);
                npcRoot.AddComponent<HumanoidRagdollController>();

                yield return null;

                Assert.That(npcRoot.GetComponent<Rigidbody>(), Is.Null,
                    "Expected authored ragdoll bodies to avoid the root fallback rigidbody.");
                Assert.That(pelvisBody.isKinematic, Is.True, "Expected authored pelvis body to start dormant.");
                Assert.That(chestBody.isKinematic, Is.True, "Expected authored chest body to start dormant.");
                Assert.That(headBody.isKinematic, Is.True, "Expected authored head body to start dormant.");
                Assert.That(pelvisBody.useGravity, Is.False, "Expected authored pelvis body to start gravity-disabled.");
                Assert.That(chestBody.useGravity, Is.False, "Expected authored chest body to start gravity-disabled.");
                Assert.That(headBody.useGravity, Is.False, "Expected authored head body to start gravity-disabled.");
                Assert.That(aliveCollider.enabled, Is.True, "Expected the alive-state collider to remain active before lethal takeover.");

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

                Assert.That(pelvisBody.isKinematic, Is.False, "Expected authored pelvis body to become dynamic on lethal takeover.");
                Assert.That(chestBody.isKinematic, Is.False, "Expected authored chest body to become dynamic on lethal takeover.");
                Assert.That(headBody.isKinematic, Is.False, "Expected authored head body to become dynamic on lethal takeover.");
                Assert.That(pelvisBody.useGravity, Is.True, "Expected authored pelvis body to enable gravity on lethal takeover.");
                Assert.That(chestBody.useGravity, Is.True, "Expected authored chest body to enable gravity on lethal takeover.");
                Assert.That(headBody.useGravity, Is.True, "Expected authored head body to enable gravity on lethal takeover.");
                Assert.That(aliveCollider.enabled, Is.False, "Expected lethal takeover to disable the alive-state collider.");
                Assert.That(ReadLinearVelocity(headBody).z, Is.GreaterThan(0f),
                    "Expected the struck authored body to receive the lethal impulse.");
                Assert.That(pelvisZone.GetComponent<CharacterJoint>(), Is.Null,
                    "Expected the pelvis to remain the ragdoll root.");
                Assert.That(chestZone.GetComponent<CharacterJoint>()!.connectedBody, Is.SameAs(pelvisBody),
                    "Expected the chest joint to stay connected to the authored pelvis body.");
                Assert.That(headZone.GetComponent<CharacterJoint>()!.connectedBody, Is.SameAs(chestBody),
                    "Expected the head joint to stay connected to the authored chest body.");
            }
            finally
            {
                if (bodyRoot != null)
                {
                    UnityEngine.Object.Destroy(bodyRoot);
                }

                if (headZone != null)
                {
                    UnityEngine.Object.Destroy(headZone);
                }

                if (chestZone != null)
                {
                    UnityEngine.Object.Destroy(chestZone);
                }

                if (pelvisZone != null)
                {
                    UnityEngine.Object.Destroy(pelvisZone);
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

        private static void SetPrivateField(Component component, string fieldName, object value)
        {
            var field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {component.GetType().Name}.");
            field!.SetValue(component, value);
        }

        private static GameObject CreateJointedRagdollBody(
            Transform parent,
            string name,
            HumanoidBodyZone bodyZone,
            Rigidbody connectedBody,
            out Rigidbody body)
        {
            var zone = new GameObject(name);
            zone.transform.SetParent(parent, false);

            var collider = zone.AddComponent<CapsuleCollider>();
            collider.enabled = false;

            body = zone.AddComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = true;

            var hitbox = zone.AddComponent<BodyZoneHitbox>();
            hitbox.Configure(bodyZone);

            if (connectedBody != null)
            {
                var joint = zone.AddComponent<CharacterJoint>();
                joint.connectedBody = connectedBody;
            }

            return zone;
        }

        private static Vector3 ReadLinearVelocity(Rigidbody rigidbody)
        {
            return rigidbody.linearVelocity;
        }
    }
}

using NUnit.Framework;
using Reloader.NPCs.Combat;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Reloader.NPCs.Tests.EditMode
{
    public sealed class NpcFoundationPrefabEditModeTests
    {
        private const string NpcFoundationPrefabPath = "Assets/_Project/NPCs/Prefabs/NpcFoundation.prefab";

        [Test]
        public void NpcFoundationPrefab_HasAuthoredRagdollBodiesCollidersAndJoints()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(NpcFoundationPrefabPath);

            try
            {
                Assert.That(prefabRoot, Is.Not.Null, "Expected NpcFoundation prefab to load.");

                var rigidbodies = prefabRoot.GetComponentsInChildren<Rigidbody>(true);
                var colliders = prefabRoot.GetComponentsInChildren<Collider>(true);
                var joints = prefabRoot.GetComponentsInChildren<Joint>(true);

                Assert.That(prefabRoot.GetComponentInChildren<HumanoidRagdollController>(true), Is.Not.Null,
                    "Expected NpcFoundation to author HumanoidRagdollController on the canonical NPC root.");
                Assert.That(rigidbodies.Length, Is.GreaterThanOrEqualTo(5),
                    "Expected NpcFoundation to author a multi-body ragdoll, not a single fallback body.");
                Assert.That(colliders.Length, Is.GreaterThanOrEqualTo(5),
                    "Expected NpcFoundation to author colliders for the ragdoll body chain.");
                Assert.That(joints.Length, Is.GreaterThanOrEqualTo(4),
                    "Expected NpcFoundation to author physical joints between ragdoll bodies.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [Test]
        public void NpcFoundationPrefab_HumanoidDamageReceiverSerializesOneHundredMaxHealth()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(NpcFoundationPrefabPath);

            try
            {
                var receiver = prefabRoot.GetComponent<HumanoidDamageReceiver>();
                Assert.That(receiver, Is.Not.Null, "Expected NpcFoundation to author the shared humanoid damage receiver.");

                var serializedReceiver = new SerializedObject(receiver);
                var maxHealth = serializedReceiver.FindProperty("_maxHealth");
                Assert.That(maxHealth, Is.Not.Null, "Expected HumanoidDamageReceiver to serialize _maxHealth.");
                Assert.That(maxHealth.floatValue, Is.EqualTo(100f));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [Test]
        public void NpcFoundationPrefab_HasEnabledHumanoidBloodControllerConfiguredWithDefaultCatalog()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(NpcFoundationPrefabPath);
            var expectedCatalog = AssetDatabase.LoadAssetAtPath<BloodVfxCatalog>(BloodVfxCatalog.DefaultCatalogAssetPath);

            try
            {
                Assert.That(expectedCatalog, Is.Not.Null, $"Expected default blood catalog at {BloodVfxCatalog.DefaultCatalogAssetPath}.");

                var controller = prefabRoot.GetComponent<HumanoidBloodController>();
                Assert.That(controller, Is.Not.Null, "Expected NpcFoundation to author HumanoidBloodController on the canonical NPC root.");
                Assert.That(controller.enabled, Is.True, "Expected NpcFoundation blood controller to be enabled for gameplay NPCs.");

                var serializedController = new SerializedObject(controller);
                var catalog = serializedController.FindProperty("_catalog");
                Assert.That(catalog, Is.Not.Null, "Expected HumanoidBloodController to serialize _catalog.");
                Assert.That(catalog.objectReferenceValue, Is.SameAs(expectedCatalog),
                    "Expected NpcFoundation blood controller to use the project-owned default BloodVfxCatalog asset.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [Test]
        public void NpcFoundationPrefab_HasLiveEnabledNonTriggerBodyZoneHitboxCollidersForEveryStandardZone()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(NpcFoundationPrefabPath);

            try
            {
                Assert.That(prefabRoot, Is.Not.Null, "Expected NpcFoundation prefab to load.");

                var liveColliderCountByZone = new Dictionary<HumanoidBodyZone, int>();
                var hitboxes = prefabRoot.GetComponentsInChildren<BodyZoneHitbox>(includeInactive: true);
                for (var i = 0; i < hitboxes.Length; i++)
                {
                    var hitbox = hitboxes[i];
                    if (hitbox == null)
                    {
                        continue;
                    }

                    var collider = hitbox.GetComponent<Collider>();
                    if (collider == null || !collider.enabled || collider.isTrigger)
                    {
                        continue;
                    }

                    liveColliderCountByZone.TryGetValue(hitbox.BodyZone, out var count);
                    liveColliderCountByZone[hitbox.BodyZone] = count + 1;
                }

                AssertLiveZoneCollider(liveColliderCountByZone, HumanoidBodyZone.Head);
                AssertLiveZoneCollider(liveColliderCountByZone, HumanoidBodyZone.Neck);
                AssertLiveZoneCollider(liveColliderCountByZone, HumanoidBodyZone.Torso);
                AssertLiveZoneCollider(liveColliderCountByZone, HumanoidBodyZone.Pelvis);
                AssertLiveZoneCollider(liveColliderCountByZone, HumanoidBodyZone.ArmL);
                AssertLiveZoneCollider(liveColliderCountByZone, HumanoidBodyZone.ArmR);
                AssertLiveZoneCollider(liveColliderCountByZone, HumanoidBodyZone.LegL);
                AssertLiveZoneCollider(liveColliderCountByZone, HumanoidBodyZone.LegR);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [Test]
        public void NpcFoundationPrefab_HumanoidRagdollController_ReferencesAuthoredRagdollSet()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(NpcFoundationPrefabPath);

            try
            {
                Assert.That(prefabRoot, Is.Not.Null, "Expected NpcFoundation prefab to load.");

                var controller = prefabRoot.GetComponentInChildren<HumanoidRagdollController>(true);
                Assert.That(controller, Is.Not.Null, "Expected NpcFoundation to author HumanoidRagdollController.");

                var serializedController = new SerializedObject(controller);
                var ragdollBodies = serializedController.FindProperty("_ragdollBodies");
                var ragdollColliders = serializedController.FindProperty("_ragdollColliders");
                var collidersToDisableOnDeath = serializedController.FindProperty("_collidersToDisableOnDeath");

                Assert.That(ragdollBodies, Is.Not.Null, "Expected HumanoidRagdollController to serialize ragdoll bodies.");
                Assert.That(ragdollColliders, Is.Not.Null, "Expected HumanoidRagdollController to serialize ragdoll colliders.");
                Assert.That(collidersToDisableOnDeath, Is.Not.Null,
                    "Expected HumanoidRagdollController to serialize alive-state colliders that must disable on death.");
                Assert.That(ragdollBodies.arraySize, Is.GreaterThanOrEqualTo(5),
                    "Expected controller to reference authored ragdoll bodies instead of relying on fallback discovery.");
                Assert.That(ragdollColliders.arraySize, Is.GreaterThanOrEqualTo(5),
                    "Expected controller to reference authored ragdoll colliders instead of relying on fallback discovery.");
                Assert.That(collidersToDisableOnDeath.arraySize, Is.GreaterThanOrEqualTo(1),
                    "Expected controller to reference the alive-state root collider that should disable during ragdoll takeover.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void AssertLiveZoneCollider(
            IReadOnlyDictionary<HumanoidBodyZone, int> liveColliderCountByZone,
            HumanoidBodyZone zone)
        {
            Assert.That(liveColliderCountByZone.TryGetValue(zone, out var count), Is.True,
                $"Expected NpcFoundation to author at least one live, enabled, non-trigger BodyZoneHitbox collider for {zone}.");
            Assert.That(count, Is.GreaterThanOrEqualTo(1),
                $"Expected NpcFoundation to author at least one live, enabled, non-trigger BodyZoneHitbox collider for {zone}.");
        }
    }
}

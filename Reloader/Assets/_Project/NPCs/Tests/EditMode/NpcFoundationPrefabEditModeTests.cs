using NUnit.Framework;
using Reloader.NPCs.Combat;
using UnityEditor;
using UnityEngine;

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

        [Test]
        public void NpcFoundationPrefab_HumanoidBloodController_ReferencesDamageReceiverDefaultCatalogAndDeathPuddleMaterial()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(NpcFoundationPrefabPath);

            try
            {
                Assert.That(prefabRoot, Is.Not.Null, "Expected NpcFoundation prefab to load.");

                var controller = prefabRoot.GetComponentInChildren<HumanoidBloodController>(true);
                Assert.That(controller, Is.Not.Null, "Expected NpcFoundation to author HumanoidBloodController for hit VFX.");

                var serializedController = new SerializedObject(controller);
                var damageReceiver = serializedController.FindProperty("_damageReceiver");
                var catalog = serializedController.FindProperty("_catalog");
                var deathPuddleMaterial = serializedController.FindProperty("_deathPuddleMaterial");

                Assert.That(damageReceiver, Is.Not.Null, "Expected HumanoidBloodController to serialize its damage receiver reference.");
                Assert.That(catalog, Is.Not.Null, "Expected HumanoidBloodController to serialize its blood VFX catalog reference.");
                Assert.That(deathPuddleMaterial, Is.Not.Null, "Expected HumanoidBloodController to serialize its death puddle material reference.");
                Assert.That(damageReceiver!.objectReferenceValue, Is.Not.Null,
                    "Expected HumanoidBloodController to target the authored HumanoidDamageReceiver on NpcFoundation.");
                Assert.That(catalog!.objectReferenceValue, Is.Not.Null,
                    "Expected HumanoidBloodController to target the default authored blood VFX catalog.");
                Assert.That(deathPuddleMaterial!.objectReferenceValue, Is.Not.Null,
                    "Expected HumanoidBloodController to target the vendor-authored death puddle material fallback.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }
}

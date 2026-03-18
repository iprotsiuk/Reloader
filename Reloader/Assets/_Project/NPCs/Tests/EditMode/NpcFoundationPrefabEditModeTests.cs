using NUnit.Framework;
using Reloader.NPCs.Combat;
using UnityEditor;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public sealed class NpcFoundationPrefabEditModeTests
    {
        private const string NpcFoundationPrefabPath = "Assets/_Project/NPCs/Prefabs/NpcFoundation.prefab";
        private const string BloodVfxCatalogPath = "Assets/_Project/NPCs/Content/Blood/BloodVfxCatalog_Default.asset";
        private const string ExpectedDeathPuddlePrefabPath = "Assets/HIVEMIND/RealisticBloodVFX/URP/RealisticBlood/Decals/Prefabs/Mesh-Driven Decal/BloodDecalMesh_Quad.prefab";
        private const string BloodImpactPrefabPath = "Assets/HIVEMIND/RealisticBloodVFX/URP/RealisticBlood/Particle Systems/PS_Blood.prefab";
        private const string ExpectedDeathPuddleMaterialPath = "Assets/_Project/NPCs/Content/Blood/Materials/M_BloodPuddle_URP_Unlit.mat";

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
                    "Expected HumanoidBloodController to target the authored local URP death puddle material fallback.");
                Assert.That(AssetDatabase.GetAssetPath(deathPuddleMaterial.objectReferenceValue), Is.EqualTo(ExpectedDeathPuddleMaterialPath),
                    "Expected NpcFoundation blood controller to point at the local URP-safe death puddle material.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [Test]
        public void DefaultBloodCatalog_DeathPuddleEntry_ReferencesVendorPuddlePrefab()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BloodVfxCatalog>(BloodVfxCatalogPath);
            Assert.That(catalog, Is.Not.Null, "Expected the default blood VFX catalog asset to exist.");

            var serializedCatalog = new SerializedObject(catalog);
            var entries = serializedCatalog.FindProperty("_effectEntries");
            Assert.That(entries, Is.Not.Null, "Expected BloodVfxCatalog to serialize effect entries.");

            Object puddlePrefab = null;
            for (var i = 0; i < entries!.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("Kind")!.enumValueIndex != (int)BloodEffectKind.DeathPuddle)
                {
                    continue;
                }

                puddlePrefab = entry.FindPropertyRelative("Prefab")!.objectReferenceValue;
                break;
            }

            Assert.That(puddlePrefab, Is.Not.Null,
                "Expected the default blood catalog to author a vendor puddle prefab instead of relying on the square runtime fallback.");
            Assert.That(AssetDatabase.GetAssetPath(puddlePrefab), Is.EqualTo(ExpectedDeathPuddlePrefabPath),
                "Expected the default blood catalog death puddle entry to point at the vendor puddle prefab path.");
        }

        [Test]
        public void DeathPuddleMaterial_UsesMaskedBaseMapTexture()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(ExpectedDeathPuddleMaterialPath);
            Assert.That(material, Is.Not.Null, "Expected local death puddle material to exist.");
            Assert.That(material!.HasProperty("_BaseMap"), Is.True, "Expected the puddle material shader to expose _BaseMap.");
            Assert.That(material.GetTexture("_BaseMap"), Is.Not.Null,
                "Expected the puddle material to use an authored mask texture so puddles are not square.");
        }

        [Test]
        public void BloodImpactPrefab_UsesExtendedParticleLifetime()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(BloodImpactPrefabPath);
            try
            {
                Assert.That(prefabRoot, Is.Not.Null, "Expected PS_Blood prefab to load.");

                var particleSystems = prefabRoot.GetComponentsInChildren<ParticleSystem>(true);
                Assert.That(particleSystems.Length, Is.GreaterThanOrEqualTo(1),
                    "Expected PS_Blood prefab to contain particle systems.");

                var longestLifetimeSeconds = 0f;
                for (var i = 0; i < particleSystems.Length; i++)
                {
                    var main = particleSystems[i].main;
                    longestLifetimeSeconds = Mathf.Max(
                        longestLifetimeSeconds,
                        Mathf.Max(main.startLifetime.constantMin, main.startLifetime.constantMax));
                }

                Assert.That(longestLifetimeSeconds, Is.GreaterThanOrEqualTo(5f),
                    "Expected PS_Blood particle lifetimes to be authored much longer than the short default burst.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [Test]
        public void DeathPuddlePrefab_MeshIsReadableForVendorConformPass()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(ExpectedDeathPuddlePrefabPath);
            try
            {
                Assert.That(prefabRoot, Is.Not.Null, "Expected the vendor death puddle prefab to load.");

                var meshFilter = prefabRoot.GetComponent<MeshFilter>();
                Assert.That(meshFilter, Is.Not.Null, "Expected the vendor death puddle prefab to include a MeshFilter.");
                Assert.That(meshFilter!.sharedMesh, Is.Not.Null, "Expected the vendor death puddle prefab to reference a mesh.");
                Assert.That(meshFilter.sharedMesh.isReadable, Is.True,
                    "Expected the vendor death puddle mesh to be readable so its conform script can reshape it at runtime.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }
}

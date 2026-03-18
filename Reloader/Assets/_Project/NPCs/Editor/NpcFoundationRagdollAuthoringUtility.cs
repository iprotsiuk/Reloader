using System.Collections.Generic;
using Reloader.NPCs.Combat;
using UnityEditor;
using UnityEngine;

namespace Reloader.NPCs.Editor
{
    public static class NpcFoundationRagdollAuthoringUtility
    {
        private const string NpcFoundationPrefabPath = "Assets/_Project/NPCs/Prefabs/NpcFoundation.prefab";
        private const string BloodContentFolderPath = "Assets/_Project/NPCs/Content";
        private const string BloodVfxFolderPath = BloodContentFolderPath + "/Blood";
        private const string BloodVfxCatalogPath = BloodVfxFolderPath + "/BloodVfxCatalog_Default.asset";
        private const string BloodImpactPrefabPath = "Assets/HIVEMIND/RealisticBloodVFX/URP/RealisticBlood/Particle Systems/PS_Blood.prefab";

        private static readonly BoneRecipe[] Recipes =
        {
            new BoneRecipe("root/pelvis", HumanoidBodyZone.Pelvis, ColliderRecipe.Box(new Vector3(0f, 0f, 0f), new Vector3(0.22f, 0.18f, 0.18f)), 12f, null),
            new BoneRecipe("root/pelvis/spine_01/spine_02/spine_03", HumanoidBodyZone.Torso, ColliderRecipe.Box(new Vector3(0f, 0.02f, 0f), new Vector3(0.28f, 0.28f, 0.20f)), 14f, "root/pelvis"),
            new BoneRecipe("root/pelvis/spine_01/spine_02/spine_03/neck_01/head", HumanoidBodyZone.Head, ColliderRecipe.Sphere(0.12f), 5f, "root/pelvis/spine_01/spine_02/spine_03"),
            new BoneRecipe("root/pelvis/spine_01/spine_02/spine_03/clavicle_l/upperarm_l", HumanoidBodyZone.ArmL, ColliderRecipe.Capsule("lowerarm_l"), 2f, "root/pelvis/spine_01/spine_02/spine_03"),
            new BoneRecipe("root/pelvis/spine_01/spine_02/spine_03/clavicle_l/upperarm_l/lowerarm_l", HumanoidBodyZone.ArmL, ColliderRecipe.Capsule("hand_l"), 1.5f, "root/pelvis/spine_01/spine_02/spine_03/clavicle_l/upperarm_l"),
            new BoneRecipe("root/pelvis/spine_01/spine_02/spine_03/clavicle_r/upperarm_r", HumanoidBodyZone.ArmR, ColliderRecipe.Capsule("lowerarm_r"), 2f, "root/pelvis/spine_01/spine_02/spine_03"),
            new BoneRecipe("root/pelvis/spine_01/spine_02/spine_03/clavicle_r/upperarm_r/lowerarm_r", HumanoidBodyZone.ArmR, ColliderRecipe.Capsule("hand_r"), 1.5f, "root/pelvis/spine_01/spine_02/spine_03/clavicle_r/upperarm_r"),
            new BoneRecipe("root/pelvis/thigh_l", HumanoidBodyZone.LegL, ColliderRecipe.Capsule("calf_l"), 7f, "root/pelvis"),
            new BoneRecipe("root/pelvis/thigh_l/calf_l", HumanoidBodyZone.LegL, ColliderRecipe.Capsule("foot_l"), 5f, "root/pelvis/thigh_l"),
            new BoneRecipe("root/pelvis/thigh_l/calf_l/foot_l", HumanoidBodyZone.LegL, ColliderRecipe.Box(new Vector3(0f, 0f, 0.08f), new Vector3(0.09f, 0.08f, 0.22f)), 1.5f, "root/pelvis/thigh_l/calf_l"),
            new BoneRecipe("root/pelvis/thigh_r", HumanoidBodyZone.LegR, ColliderRecipe.Capsule("calf_r"), 7f, "root/pelvis"),
            new BoneRecipe("root/pelvis/thigh_r/calf_r", HumanoidBodyZone.LegR, ColliderRecipe.Capsule("foot_r"), 5f, "root/pelvis/thigh_r"),
            new BoneRecipe("root/pelvis/thigh_r/calf_r/foot_r", HumanoidBodyZone.LegR, ColliderRecipe.Box(new Vector3(0f, 0f, 0.08f), new Vector3(0.09f, 0.08f, 0.22f)), 1.5f, "root/pelvis/thigh_r/calf_r")
        };

        [MenuItem("Reloader/NPCs/Foundation/Apply Authored Ragdoll To Npc Foundation")]
        public static void ApplyAuthoredRagdollToNpcFoundation()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(NpcFoundationPrefabPath);
            try
            {
                if (prefabRoot == null)
                {
                    Debug.LogError($"Unable to load prefab at '{NpcFoundationPrefabPath}'.");
                    return;
                }

                ApplyAuthoredRagdoll(prefabRoot);
                EditorUtility.SetDirty(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, NpcFoundationPrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        public static void ApplyAuthoredRagdoll(GameObject prefabRoot)
        {
            if (prefabRoot == null)
            {
                return;
            }

            var ragdollBodies = new List<Rigidbody>();
            var ragdollColliders = new List<Collider>();

            AuthorRoot(prefabRoot.transform.Find("VisualRoot/StyleMaleRoot"), ragdollBodies, ragdollColliders);
            AuthorRoot(prefabRoot.transform.Find("VisualRoot/StyleFemaleRoot"), ragdollBodies, ragdollColliders);
            EnsureCombatComponents(prefabRoot, ragdollBodies, ragdollColliders);
        }

        private static void AuthorRoot(Transform styleRoot, List<Rigidbody> ragdollBodies, List<Collider> ragdollColliders)
        {
            if (styleRoot == null)
            {
                return;
            }

            var bodyByPath = new Dictionary<string, Rigidbody>();
            for (var i = 0; i < Recipes.Length; i++)
            {
                var recipe = Recipes[i];
                var bone = styleRoot.Find(recipe.BonePath);
                if (bone == null)
                {
                    Debug.LogWarning($"NpcFoundation ragdoll authoring skipped missing bone '{styleRoot.name}/{recipe.BonePath}'.");
                    continue;
                }

                var body = bone.GetComponent<Rigidbody>();
                if (body == null)
                {
                    body = bone.gameObject.AddComponent<Rigidbody>();
                }

                ConfigureRigidbody(body, recipe.Mass);
                bodyByPath[recipe.BonePath] = body;
                ragdollBodies.Add(body);

                var collider = EnsureCollider(bone, recipe.Collider);
                collider.enabled = false;
                ragdollColliders.Add(collider);

                var hitbox = bone.GetComponent<BodyZoneHitbox>();
                if (hitbox == null)
                {
                    hitbox = bone.gameObject.AddComponent<BodyZoneHitbox>();
                }

                var ownerRig = styleRoot.GetComponentInParent<HumanoidHitboxRig>();
                hitbox.Configure(ownerRig, recipe.BodyZone);
            }

            for (var i = 0; i < Recipes.Length; i++)
            {
                var recipe = Recipes[i];
                if (!bodyByPath.TryGetValue(recipe.BonePath, out var body))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(recipe.ConnectedBodyPath))
                {
                    var rootJoint = body.GetComponent<CharacterJoint>();
                    if (rootJoint != null)
                    {
                        Object.DestroyImmediate(rootJoint);
                    }

                    continue;
                }

                if (!bodyByPath.TryGetValue(recipe.ConnectedBodyPath, out var connectedBody))
                {
                    continue;
                }

                var joint = body.GetComponent<CharacterJoint>();
                if (joint == null)
                {
                    joint = body.gameObject.AddComponent<CharacterJoint>();
                }

                joint.connectedBody = connectedBody;
                joint.enableProjection = true;
            }
        }

        private static void EnsureCombatComponents(GameObject prefabRoot, List<Rigidbody> ragdollBodies, List<Collider> ragdollColliders)
        {
            var hitboxRig = prefabRoot.GetComponent<HumanoidHitboxRig>();
            if (hitboxRig == null)
            {
                hitboxRig = prefabRoot.AddComponent<HumanoidHitboxRig>();
            }

            var damageReceiver = prefabRoot.GetComponent<HumanoidDamageReceiver>();
            if (damageReceiver == null)
            {
                damageReceiver = prefabRoot.AddComponent<HumanoidDamageReceiver>();
            }

            var ragdollController = prefabRoot.GetComponent<HumanoidRagdollController>();
            if (ragdollController == null)
            {
                ragdollController = prefabRoot.AddComponent<HumanoidRagdollController>();
            }

            if (prefabRoot.GetComponent<HumanoidCorpseLootController>() == null)
            {
                prefabRoot.AddComponent<HumanoidCorpseLootController>();
            }

            var bloodController = prefabRoot.GetComponent<HumanoidBloodController>();
            if (bloodController == null)
            {
                bloodController = prefabRoot.AddComponent<HumanoidBloodController>();
            }

            var bodyCollider = prefabRoot.transform.Find("Body")?.GetComponent<Collider>();
            var bloodCatalog = EnsureBloodVfxCatalogAsset();

            var serializedDamageReceiver = new SerializedObject(damageReceiver);
            serializedDamageReceiver.FindProperty("_hitboxRig")!.objectReferenceValue = hitboxRig;
            serializedDamageReceiver.ApplyModifiedPropertiesWithoutUndo();

            var serializedController = new SerializedObject(ragdollController);
            serializedController.FindProperty("_damageReceiver")!.objectReferenceValue = damageReceiver;
            AssignObjectReferences(serializedController.FindProperty("_ragdollBodies"), ragdollBodies);
            AssignObjectReferences(serializedController.FindProperty("_ragdollColliders"), ragdollColliders);
            AssignObjectReferences(
                serializedController.FindProperty("_collidersToDisableOnDeath"),
                bodyCollider != null ? new[] { bodyCollider } : System.Array.Empty<Collider>());
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            var serializedBloodController = new SerializedObject(bloodController);
            serializedBloodController.FindProperty("_damageReceiver")!.objectReferenceValue = damageReceiver;
            serializedBloodController.FindProperty("_catalog")!.objectReferenceValue = bloodCatalog;
            serializedBloodController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BloodVfxCatalog EnsureBloodVfxCatalogAsset()
        {
            EnsureFolderExists(BloodContentFolderPath);
            EnsureFolderExists(BloodVfxFolderPath);

            var catalog = AssetDatabase.LoadAssetAtPath<BloodVfxCatalog>(BloodVfxCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BloodVfxCatalog>();
                AssetDatabase.CreateAsset(catalog, BloodVfxCatalogPath);
            }

            var impactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BloodImpactPrefabPath);
            if (impactPrefab == null)
            {
                return catalog;
            }

            var serializedCatalog = new SerializedObject(catalog);
            var entries = serializedCatalog.FindProperty("_effectEntries");
            if (entries == null)
            {
                return catalog;
            }

            entries.arraySize = 6;
            SetBloodEffectEntry(entries.GetArrayElementAtIndex(0), BloodEffectKind.HeadImpact, impactPrefab);
            SetBloodEffectEntry(entries.GetArrayElementAtIndex(1), BloodEffectKind.NeckImpact, impactPrefab);
            SetBloodEffectEntry(entries.GetArrayElementAtIndex(2), BloodEffectKind.TorsoImpact, impactPrefab);
            SetBloodEffectEntry(entries.GetArrayElementAtIndex(3), BloodEffectKind.ArmImpact, impactPrefab);
            SetBloodEffectEntry(entries.GetArrayElementAtIndex(4), BloodEffectKind.LegImpact, impactPrefab);
            SetBloodEffectEntry(entries.GetArrayElementAtIndex(5), BloodEffectKind.DeathPuddle, null);
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void SetBloodEffectEntry(SerializedProperty entryProperty, BloodEffectKind kind, GameObject prefab)
        {
            if (entryProperty == null)
            {
                return;
            }

            entryProperty.FindPropertyRelative("Kind")!.enumValueIndex = (int)kind;
            entryProperty.FindPropertyRelative("Prefab")!.objectReferenceValue = prefab;
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parentPath = System.IO.Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
            var folderName = System.IO.Path.GetFileName(folderPath);
            if (string.IsNullOrWhiteSpace(parentPath) || string.IsNullOrWhiteSpace(folderName))
            {
                return;
            }

            EnsureFolderExists(parentPath);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        private static void ConfigureRigidbody(Rigidbody body, float mass)
        {
            body.mass = mass;
            body.linearDamping = 0.05f;
            body.angularDamping = 0.05f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.useGravity = false;
        }

        private static Collider EnsureCollider(Transform bone, ColliderRecipe recipe)
        {
            switch (recipe.Kind)
            {
                case ColliderKind.Box:
                {
                    var collider = bone.GetComponent<BoxCollider>();
                    if (collider == null)
                    {
                        collider = bone.gameObject.AddComponent<BoxCollider>();
                    }

                    collider.center = recipe.Center;
                    collider.size = recipe.Size;
                    return collider;
                }
                case ColliderKind.Sphere:
                {
                    var collider = bone.GetComponent<SphereCollider>();
                    if (collider == null)
                    {
                        collider = bone.gameObject.AddComponent<SphereCollider>();
                    }

                    collider.center = recipe.Center;
                    collider.radius = recipe.Radius;
                    return collider;
                }
                default:
                {
                    var collider = bone.GetComponent<CapsuleCollider>();
                    if (collider == null)
                    {
                        collider = bone.gameObject.AddComponent<CapsuleCollider>();
                    }

                    ConfigureCapsuleCollider(bone, collider, recipe.EndpointName);
                    return collider;
                }
            }
        }

        private static void ConfigureCapsuleCollider(Transform bone, CapsuleCollider collider, string endpointName)
        {
            var endpoint = bone.Find(endpointName);
            if (endpoint == null)
            {
                collider.center = Vector3.zero;
                collider.radius = 0.08f;
                collider.height = 0.18f;
                collider.direction = 1;
                return;
            }

            var localOffset = bone.InverseTransformPoint(endpoint.position);
            var distance = localOffset.magnitude;
            if (distance < 0.001f)
            {
                collider.center = Vector3.zero;
                collider.radius = 0.08f;
                collider.height = 0.18f;
                collider.direction = 1;
                return;
            }

            var axis = 1;
            var abs = new Vector3(Mathf.Abs(localOffset.x), Mathf.Abs(localOffset.y), Mathf.Abs(localOffset.z));
            if (abs.x >= abs.y && abs.x >= abs.z)
            {
                axis = 0;
            }
            else if (abs.z >= abs.x && abs.z >= abs.y)
            {
                axis = 2;
            }

            collider.direction = axis;
            collider.center = localOffset * 0.5f;
            collider.radius = Mathf.Clamp(distance * 0.2f, 0.05f, 0.12f);
            collider.height = Mathf.Max((collider.radius * 2f) + 0.02f, distance * 1.05f);
        }

        private static void AssignObjectReferences<T>(SerializedProperty property, IReadOnlyList<T> values)
            where T : Object
        {
            if (property == null)
            {
                return;
            }

            property.arraySize = values?.Count ?? 0;
            for (var i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private readonly struct BoneRecipe
        {
            public BoneRecipe(string bonePath, HumanoidBodyZone bodyZone, ColliderRecipe collider, float mass, string connectedBodyPath)
            {
                BonePath = bonePath;
                BodyZone = bodyZone;
                Collider = collider;
                Mass = mass;
                ConnectedBodyPath = connectedBodyPath;
            }

            public string BonePath { get; }
            public HumanoidBodyZone BodyZone { get; }
            public ColliderRecipe Collider { get; }
            public float Mass { get; }
            public string ConnectedBodyPath { get; }
        }

        private readonly struct ColliderRecipe
        {
            private ColliderRecipe(ColliderKind kind, Vector3 center, Vector3 size, float radius, string endpointName)
            {
                Kind = kind;
                Center = center;
                Size = size;
                Radius = radius;
                EndpointName = endpointName;
            }

            public ColliderKind Kind { get; }
            public Vector3 Center { get; }
            public Vector3 Size { get; }
            public float Radius { get; }
            public string EndpointName { get; }

            public static ColliderRecipe Box(Vector3 center, Vector3 size) => new ColliderRecipe(ColliderKind.Box, center, size, 0f, null);
            public static ColliderRecipe Sphere(float radius) => new ColliderRecipe(ColliderKind.Sphere, Vector3.zero, Vector3.zero, radius, null);
            public static ColliderRecipe Capsule(string endpointName) => new ColliderRecipe(ColliderKind.Capsule, Vector3.zero, Vector3.zero, 0f, endpointName);
        }

        private enum ColliderKind
        {
            Box,
            Sphere,
            Capsule
        }
    }
}

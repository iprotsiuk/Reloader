using System.Reflection;
using Reloader.Core.Items;
using Reloader.Core.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.Inventory
{
    public static class RuntimeDroppedItemSpawnRestorer
    {
        private static readonly FieldInfo ObjectIdField = typeof(WorldObjectIdentity).GetField("_objectId", BindingFlags.Instance | BindingFlags.NonPublic);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            WorldObjectPersistenceRuntimeBridge.RegisterRuntimeSpawnRestorer(TryRestore);
        }

        private static bool TryRestore(Scene scene, WorldObjectStateRecord record)
        {
            if (!scene.IsValid() || !scene.isLoaded || record == null)
            {
                return false;
            }

            var itemId = ResolveItemId(record);
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            var definition = ResolveItemDefinition(itemId);
            var quantity = Mathf.Max(1, record.StackQuantity);
            var dropRoot = new GameObject($"drop-{itemId}");
            SceneManager.MoveGameObjectToScene(dropRoot, scene);
            dropRoot.transform.SetPositionAndRotation(record.Position, record.Rotation);

            var identity = dropRoot.AddComponent<WorldObjectIdentity>();
            if (identity == null)
            {
                Object.Destroy(dropRoot);
                return false;
            }

            if (ObjectIdField != null)
            {
                ObjectIdField.SetValue(identity, record.ObjectId);
            }

            if (identity.ObjectId != record.ObjectId)
            {
                Object.Destroy(dropRoot);
                return false;
            }

            var collider = dropRoot.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.24f, 0.24f, 0.24f);

            var rigidbody = dropRoot.AddComponent<Rigidbody>();
            rigidbody.mass = ResolveDropMass(definition, quantity);
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (definition != null)
            {
                var spawnDefinition = ScriptableObject.CreateInstance<ItemSpawnDefinition>();
                spawnDefinition.SetValuesForTests(definition, quantity);

                var definitionPickup = dropRoot.AddComponent<DefinitionPickupTarget>();
                definitionPickup.SetSpawnDefinitionForTests(spawnDefinition);

                var lifetime = dropRoot.AddComponent<RuntimeDropSpawnLifetime>();
                lifetime.Assign(spawnDefinition);
            }
            else
            {
                var pickupTarget = dropRoot.AddComponent<RuntimeStackPickupTarget>();
                pickupTarget.SetValuesForTests(itemId, quantity);
            }

            CreateVisual(definition, dropRoot.transform);
            return true;
        }

        private static string ResolveItemId(WorldObjectStateRecord record)
        {
            if (!string.IsNullOrWhiteSpace(record.ItemDefinitionId))
            {
                return record.ItemDefinitionId;
            }

            return record.ItemInstanceId;
        }

        private static ItemDefinition ResolveItemDefinition(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            var controllers = Object.FindObjectsByType<PlayerInventoryController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var controllerIndex = 0; controllerIndex < controllers.Length; controllerIndex++)
            {
                var controller = controllers[controllerIndex];
                if (controller == null)
                {
                    continue;
                }

                var definitions = controller.GetItemDefinitionRegistrySnapshot();
                if (definitions == null)
                {
                    continue;
                }

                for (var i = 0; i < definitions.Count; i++)
                {
                    var definition = definitions[i];
                    if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId))
                    {
                        continue;
                    }

                    if (definition.DefinitionId == itemId)
                    {
                        return definition;
                    }
                }
            }

            var loadedDefinitions = Resources.FindObjectsOfTypeAll<ItemDefinition>();
            for (var i = 0; i < loadedDefinitions.Length; i++)
            {
                var definition = loadedDefinitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId))
                {
                    continue;
                }

                if (definition.DefinitionId == itemId)
                {
                    return definition;
                }
            }

            return null;
        }

        private static void CreateVisual(ItemDefinition definition, Transform parent)
        {
            if (definition != null && definition.IconSourcePrefab != null)
            {
                var visualRoot = Object.Instantiate(definition.IconSourcePrefab, parent);
                visualRoot.name = "visual";
                StripPhysicsComponents(visualRoot);
                visualRoot.transform.localPosition = Vector3.zero;
                visualRoot.transform.localRotation = Quaternion.identity;
                return;
            }

            var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fallback.name = "visual";
            fallback.transform.SetParent(parent, false);
            fallback.transform.localScale = Vector3.one * 0.22f;
            var collider = fallback.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
        }

        private static void StripPhysicsComponents(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    Object.Destroy(colliders[i]);
                }
            }

            var rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (var i = 0; i < rigidbodies.Length; i++)
            {
                if (rigidbodies[i] != null)
                {
                    Object.Destroy(rigidbodies[i]);
                }
            }
        }

        private static float ResolveDropMass(ItemDefinition definition, int quantity)
        {
            var perItemMass = 0.25f;
            if (definition != null)
            {
                perItemMass = definition.Category switch
                {
                    ItemCategory.Powder => 0.02f,
                    ItemCategory.Bullet => 0.03f,
                    ItemCategory.Case => 0.02f,
                    ItemCategory.Primer => 0.01f,
                    ItemCategory.Weapon => 2.8f,
                    ItemCategory.Tool => 0.8f,
                    ItemCategory.Consumable => 0.25f,
                    _ => 0.2f
                };
            }

            var total = Mathf.Max(0.05f, perItemMass * Mathf.Max(1, quantity));
            return Mathf.Clamp(total, 0.05f, 20f);
        }

        private sealed class RuntimeDropSpawnLifetime : MonoBehaviour
        {
            private ItemSpawnDefinition _runtimeSpawnDefinition;

            public void Assign(ItemSpawnDefinition runtimeSpawnDefinition)
            {
                _runtimeSpawnDefinition = runtimeSpawnDefinition;
            }

            private void OnDestroy()
            {
                if (_runtimeSpawnDefinition != null)
                {
                    Object.Destroy(_runtimeSpawnDefinition);
                    _runtimeSpawnDefinition = null;
                }
            }
        }
    }
}

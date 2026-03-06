using Reloader.Core.Items;
using Reloader.Core.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.Inventory
{
    public static class RuntimeDroppedItemFactory
    {
        public static readonly Vector3 DefaultFallbackColliderSize = new Vector3(0.24f, 0.24f, 0.24f);

        public static bool TryCreate(
            string itemId,
            int quantity,
            ItemDefinition definition,
            Vector3 position,
            Quaternion rotation,
            out GameObject dropRoot,
            out Rigidbody rigidbody,
            Vector3? colliderSize = null,
            Scene targetScene = default,
            string restoreObjectId = null,
            string runtimeDropInstanceId = null)
        {
            dropRoot = null;
            rigidbody = null;

            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            {
                return false;
            }

            if (!TryCreateVisualRoot(itemId, definition, out var visualRoot))
            {
                return false;
            }

            dropRoot = new GameObject($"drop-{itemId}");
            if (targetScene.IsValid() && targetScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(dropRoot, targetScene);
            }

            dropRoot.transform.SetPositionAndRotation(position, rotation);

            var identity = dropRoot.AddComponent<WorldObjectIdentity>();
            if (identity == null)
            {
                Object.Destroy(dropRoot);
                dropRoot = null;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(restoreObjectId)
                && !identity.AssignObjectIdForRestore(restoreObjectId))
            {
                Object.Destroy(dropRoot);
                dropRoot = null;
                return false;
            }

            var boxCollider = dropRoot.AddComponent<BoxCollider>();
            boxCollider.size = colliderSize ?? DefaultFallbackColliderSize;

            rigidbody = dropRoot.AddComponent<Rigidbody>();
            rigidbody.mass = ResolveDropMass(definition, quantity);
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var spawnDefinition = ScriptableObject.CreateInstance<ItemSpawnDefinition>();
            spawnDefinition.SetValuesForTests(definition, quantity);

            var definitionPickup = dropRoot.AddComponent<DefinitionPickupTarget>();
            definitionPickup.SetSpawnDefinitionForTests(spawnDefinition);

            var spawnLifetime = dropRoot.AddComponent<RuntimeDropSpawnLifetime>();
            spawnLifetime.Assign(spawnDefinition);

            AttachVisual(dropRoot.transform, visualRoot);

            var persistenceTracker = dropRoot.AddComponent<RuntimeDroppedObjectPersistenceTracker>();
            persistenceTracker.Configure(itemId, quantity, runtimeDropInstanceId);
            return true;
        }

        private static bool TryCreateVisualRoot(string itemId, ItemDefinition definition, out GameObject visualRoot)
        {
            visualRoot = null;

            if (definition == null)
            {
                Debug.LogError(
                    $"[RuntimeDroppedItemFactory] Cannot create dropped item '{itemId}' because no ItemDefinition was resolved.");
                return false;
            }

            if (definition.IconSourcePrefab == null)
            {
                Debug.LogError(
                    $"[RuntimeDroppedItemFactory] Cannot create dropped item '{itemId}' because ItemDefinition '{definition.DefinitionId}' has no IconSourcePrefab.");
                return false;
            }

            visualRoot = TryInstantiateVisualRoot(definition.IconSourcePrefab);
            if (visualRoot == null)
            {
                Debug.LogError(
                    $"[RuntimeDroppedItemFactory] Cannot create dropped item '{itemId}' because IconSourcePrefab '{definition.IconSourcePrefab.name}' could not be instantiated.");
                return false;
            }

            return true;
        }

        private static void AttachVisual(Transform parent, GameObject visualRoot)
        {
            if (parent == null || visualRoot == null)
            {
                return;
            }

            visualRoot.name = "visual";
            visualRoot.transform.SetParent(parent, false);
            StripPhysicsComponents(visualRoot);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;
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
                    Object.DestroyImmediate(colliders[i]);
                }
            }

            var rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (var i = 0; i < rigidbodies.Length; i++)
            {
                if (rigidbodies[i] != null)
                {
                    Object.DestroyImmediate(rigidbodies[i]);
                }
            }
        }

        private static GameObject TryInstantiateVisualRoot(GameObject sourcePrefab)
        {
            if (sourcePrefab == null)
            {
                return null;
            }

            try
            {
                var rawInstance = Object.Instantiate((Object)sourcePrefab);
                if (rawInstance is GameObject go)
                {
                    return go;
                }

                if (rawInstance is Component component)
                {
                    return component.gameObject;
                }
            }
            catch (System.InvalidCastException)
            {
                return null;
            }

            return null;
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

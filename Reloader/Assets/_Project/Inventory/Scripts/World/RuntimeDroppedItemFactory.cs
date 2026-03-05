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

            if (definition != null)
            {
                var spawnDefinition = ScriptableObject.CreateInstance<ItemSpawnDefinition>();
                spawnDefinition.SetValuesForTests(definition, quantity);

                var definitionPickup = dropRoot.AddComponent<DefinitionPickupTarget>();
                definitionPickup.SetSpawnDefinitionForTests(spawnDefinition);

                var spawnLifetime = dropRoot.AddComponent<RuntimeDropSpawnLifetime>();
                spawnLifetime.Assign(spawnDefinition);
            }
            else
            {
                var stackPickup = dropRoot.AddComponent<RuntimeStackPickupTarget>();
                stackPickup.SetValuesForTests(itemId, quantity);
            }

            CreateVisual(definition, dropRoot.transform);

            var persistenceTracker = dropRoot.AddComponent<RuntimeDroppedObjectPersistenceTracker>();
            persistenceTracker.Configure(itemId, quantity, runtimeDropInstanceId);
            return true;
        }

        private static void CreateVisual(ItemDefinition definition, Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            if (definition != null && definition.IconSourcePrefab != null)
            {
                var visualRoot = TryInstantiateVisualRoot(definition.IconSourcePrefab, parent);
                if (visualRoot != null)
                {
                    visualRoot.name = "visual";
                    StripPhysicsComponents(visualRoot);
                    visualRoot.transform.localPosition = Vector3.zero;
                    visualRoot.transform.localRotation = Quaternion.identity;
                    return;
                }

                Debug.LogWarning(
                    $"[RuntimeDroppedItemFactory] Failed to instantiate IconSourcePrefab '{definition.IconSourcePrefab.name}' for item '{definition.DefinitionId}'. Falling back to cube.",
                    parent);
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

        private static GameObject TryInstantiateVisualRoot(GameObject sourcePrefab, Transform parent)
        {
            if (sourcePrefab == null)
            {
                return null;
            }

            try
            {
                var rawInstance = Object.Instantiate((Object)sourcePrefab, parent);
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

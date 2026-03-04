using Reloader.Core.Items;
using Reloader.Core.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.Inventory
{
    public static class RuntimeDroppedItemSpawnRestorer
    {
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
            return RuntimeDroppedItemFactory.TryCreate(
                itemId,
                quantity,
                definition,
                record.Position,
                record.Rotation,
                out _,
                out _,
                RuntimeDroppedItemFactory.DefaultFallbackColliderSize,
                scene,
                record.ObjectId,
                record.ItemInstanceId);
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

    }
}

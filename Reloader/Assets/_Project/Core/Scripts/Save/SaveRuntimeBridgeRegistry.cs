using System.Collections.Generic;
using UnityEngine;

namespace Reloader.Core.Save
{
    public static class SaveRuntimeBridgeRegistry
    {
        private static readonly List<ISaveRuntimeBridge> Bridges = new List<ISaveRuntimeBridge>();

        public static void Register(ISaveRuntimeBridge bridge)
        {
            if (bridge == null || Bridges.Contains(bridge))
            {
                return;
            }

            Bridges.Add(bridge);
        }

        public static void Unregister(ISaveRuntimeBridge bridge)
        {
            if (bridge == null)
            {
                return;
            }

            Bridges.Remove(bridge);
        }

        public static void PrepareForSave(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            PruneDestroyedUnityBridges();
            for (var i = 0; i < Bridges.Count; i++)
            {
                Bridges[i].PrepareForSave(moduleRegistrations);
            }
        }

        public static void FinalizeAfterLoad(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            PruneDestroyedUnityBridges();
            for (var i = 0; i < Bridges.Count; i++)
            {
                Bridges[i].FinalizeAfterLoad(moduleRegistrations);
            }
        }

        private static void PruneDestroyedUnityBridges()
        {
            for (var i = Bridges.Count - 1; i >= 0; i--)
            {
                var bridge = Bridges[i];
                if (bridge == null)
                {
                    Bridges.RemoveAt(i);
                    continue;
                }

                if (bridge is Object unityObject && unityObject == null)
                {
                    Bridges.RemoveAt(i);
                }
            }
        }
    }
}

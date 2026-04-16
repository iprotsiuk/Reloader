using System;
using System.Reflection;
using Reloader.NPCs.Combat;
using UnityEngine;

namespace Reloader.UI.Toolkit.HealthHud
{
    internal static class HealthHudPlayerRootResolver
    {
        private const string PersistentPlayerRootTypeName = "Reloader.World.Runtime.PersistentPlayerRoot, Reloader.World";
        private const string RuntimePlayerRootName = "RuntimePlayerRoot";
        private const string PlayerRootName = "PlayerRoot";

        public static HumanoidDamageReceiver ResolvePlayerDamageReceiver()
        {
            if (TryResolveFromPersistentPlayerRoot(out var receiver))
            {
                return receiver;
            }

            return ResolveReceiverFromSceneRoots();
        }

        private static bool TryResolveFromPersistentPlayerRoot(out HumanoidDamageReceiver receiver)
        {
            receiver = null;

            var persistentPlayerRootType = Type.GetType(PersistentPlayerRootTypeName, throwOnError: false);
            if (persistentPlayerRootType == null)
            {
                return false;
            }

            var instance = persistentPlayerRootType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
            if (instance == null)
            {
                return false;
            }

            var playerRootTransform = persistentPlayerRootType.GetProperty("PlayerRootTransform", BindingFlags.Instance | BindingFlags.Public)?.GetValue(instance) as Transform;
            return TryResolveReceiverFromTransform(playerRootTransform, out receiver);
        }

        private static HumanoidDamageReceiver ResolveReceiverFromSceneRoots()
        {
            var receivers = UnityEngine.Object.FindObjectsByType<HumanoidDamageReceiver>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < receivers.Length; i++)
            {
                if (TryResolveCanonicalRootReceiver(receivers[i], out var resolved))
                {
                    return resolved;
                }
            }

            return null;
        }

        private static bool TryResolveCanonicalRootReceiver(HumanoidDamageReceiver receiver, out HumanoidDamageReceiver resolved)
        {
            resolved = null;
            if (receiver == null)
            {
                return false;
            }

            var root = receiver.transform != null ? receiver.transform.root : null;
            if (!IsCanonicalPlayerRoot(root))
            {
                return false;
            }

            resolved = receiver;
            return true;
        }

        private static bool TryResolveReceiverFromTransform(Transform playerRootTransform, out HumanoidDamageReceiver receiver)
        {
            receiver = null;
            if (!IsCanonicalPlayerRoot(playerRootTransform))
            {
                return false;
            }

            receiver = playerRootTransform.GetComponent<HumanoidDamageReceiver>();
            return receiver != null;
        }

        private static bool IsCanonicalPlayerRoot(Transform transform)
        {
            if (transform == null)
            {
                return false;
            }

            return string.Equals(transform.name, RuntimePlayerRootName, StringComparison.Ordinal)
                   || string.Equals(transform.name, PlayerRootName, StringComparison.Ordinal);
        }
    }
}

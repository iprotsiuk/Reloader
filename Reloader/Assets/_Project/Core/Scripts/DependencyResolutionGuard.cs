using System;
using UnityEngine;

namespace Reloader.Core
{
    public static class DependencyResolutionGuard
    {
        public static bool HasRequiredReferences(ref bool logged, UnityEngine.Object context, string logMessage, params object[] dependencies)
        {
            if (dependencies == null || dependencies.Length == 0)
            {
                if (!logged)
                {
                    Debug.LogError(logMessage, context);
                    logged = true;
                }

                return false;
            }

            var hasAll = true;
            for (var i = 0; i < dependencies.Length; i++)
            {
                if (dependencies[i] == null)
                {
                    hasAll = false;
                    break;
                }
            }

            if (hasAll)
            {
                return true;
            }

            if (!logged)
            {
                Debug.LogError(logMessage, context);
                logged = true;
            }

            return false;
        }

        public static void ResolveOnce<T>(ref T dependency, ref bool attempted, Func<T> resolver) where T : class
        {
            if (dependency != null || attempted)
            {
                return;
            }

            attempted = true;
            dependency = resolver();
        }

        public static TInterface FindInterface<TInterface>(MonoBehaviour[] behaviours) where TInterface : class
        {
            if (behaviours == null)
            {
                return null;
            }

            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is TInterface typed)
                {
                    return typed;
                }
            }

            return null;
        }
    }
}

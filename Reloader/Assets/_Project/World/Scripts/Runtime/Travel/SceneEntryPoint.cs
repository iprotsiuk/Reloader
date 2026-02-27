using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloader.World.Travel
{
    [DisallowMultipleComponent]
    public sealed class SceneEntryPoint : MonoBehaviour
    {
        [SerializeField] private string _entryPointId = "";

        public string EntryPointId => _entryPointId;

        public void EnsureStableId()
        {
            if (string.IsNullOrWhiteSpace(_entryPointId))
            {
                _entryPointId = Guid.NewGuid().ToString("N");
                return;
            }

            _entryPointId = _entryPointId.Trim();
        }

        public static bool TryFindById(
            IEnumerable<SceneEntryPoint> entryPoints,
            string entryPointId,
            out SceneEntryPoint entryPoint)
        {
            entryPoint = null;
            if (entryPoints == null || string.IsNullOrWhiteSpace(entryPointId))
            {
                return false;
            }

            var normalizedId = entryPointId.Trim();
            foreach (var candidate in entryPoints)
            {
                if (candidate == null || string.IsNullOrWhiteSpace(candidate._entryPointId))
                {
                    continue;
                }

                if (string.Equals(candidate._entryPointId.Trim(), normalizedId, StringComparison.Ordinal))
                {
                    entryPoint = candidate;
                    return true;
                }
            }

            return false;
        }

        private void Reset()
        {
            EnsureStableId();
        }

        private void OnValidate()
        {
            EnsureStableId();
        }
    }
}

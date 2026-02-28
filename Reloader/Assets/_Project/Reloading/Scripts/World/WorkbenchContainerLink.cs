using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloader.Reloading.World
{
    public sealed class WorkbenchContainerLink : MonoBehaviour
    {
        [SerializeField] private string[] _linkedContainerIds = Array.Empty<string>();

        public IEnumerable<string> EnumerateLinkedContainerIds()
        {
            if (_linkedContainerIds == null || _linkedContainerIds.Length == 0)
            {
                yield break;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < _linkedContainerIds.Length; i++)
            {
                var raw = _linkedContainerIds[i];
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var trimmed = raw.Trim();
                if (!seen.Add(trimmed))
                {
                    continue;
                }

                yield return trimmed;
            }
        }
    }
}

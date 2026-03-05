using System;
using System.Collections.Generic;
using Reloader.Weapons.Data;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.Weapons.Runtime
{
    public sealed class WeaponRegistry : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition[] _definitions = Array.Empty<WeaponDefinition>();

        private readonly Dictionary<string, WeaponDefinition> _byItemId = new Dictionary<string, WeaponDefinition>();
        private bool _initialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        public bool TryGetWeaponDefinition(string itemId, out WeaponDefinition definition)
        {
            EnsureInitialized();
            if (_byItemId.TryGetValue(itemId, out definition))
            {
                return true;
            }

#if UNITY_EDITOR
            if (TryResolveFromProjectAssets(itemId, out definition))
            {
                _byItemId[itemId] = definition;
                return true;
            }
#endif

            return false;
        }

        public void SetDefinitionsForTests(WeaponDefinition[] definitions)
        {
            _definitions = definitions ?? Array.Empty<WeaponDefinition>();
            _initialized = false;
            _byItemId.Clear();
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _byItemId.Clear();
            if (_definitions == null)
            {
                return;
            }

            for (var i = 0; i < _definitions.Length; i++)
            {
                var definition = _definitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.ItemId))
                {
                    continue;
                }

                _byItemId[definition.ItemId] = definition;
            }
        }

#if UNITY_EDITOR
        private static bool TryResolveFromProjectAssets(string itemId, out WeaponDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            var guids = AssetDatabase.FindAssets("t:WeaponDefinition");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var candidate = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
                if (candidate != null && candidate.ItemId == itemId)
                {
                    definition = candidate;
                    return true;
                }
            }

            return false;
        }
#endif
    }
}

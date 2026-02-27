using System;
using System.Collections.Generic;
using Reloader.Weapons.Data;
using UnityEngine;

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
            return _byItemId.TryGetValue(itemId ?? string.Empty, out definition);
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
    }
}

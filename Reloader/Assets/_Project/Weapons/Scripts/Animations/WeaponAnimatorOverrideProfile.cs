using System;
using UnityEngine;

namespace Reloader.Weapons.Animations
{
    [Serializable]
    public struct WeaponAnimatorOverrideEntry
    {
        [SerializeField] private string _itemId;
        [SerializeField] private RuntimeAnimatorController _controller;

        public string ItemId => _itemId;
        public RuntimeAnimatorController Controller => _controller;
    }

    [CreateAssetMenu(
        fileName = "PlayerWeaponAnimatorOverrideProfile",
        menuName = "Reloader/Weapons/Animation Profile",
        order = 0)]
    public sealed class WeaponAnimatorOverrideProfile : ScriptableObject
    {
        [SerializeField] private RuntimeAnimatorController _defaultController;
        [SerializeField] private WeaponAnimatorOverrideEntry[] _entries = Array.Empty<WeaponAnimatorOverrideEntry>();

        public RuntimeAnimatorController ResolveController(string itemId)
        {
            if (!string.IsNullOrWhiteSpace(itemId))
            {
                for (var i = 0; i < _entries.Length; i++)
                {
                    var entry = _entries[i];
                    if (!string.IsNullOrWhiteSpace(entry.ItemId)
                        && entry.ItemId == itemId
                        && entry.Controller != null)
                    {
                        return entry.Controller;
                    }
                }
            }

            return _defaultController;
        }
    }
}

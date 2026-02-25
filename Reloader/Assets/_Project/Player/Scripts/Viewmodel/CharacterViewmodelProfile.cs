using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloader.Player.Viewmodel
{
    [Serializable]
    public struct WeaponFamilyOffsetOverride
    {
        [SerializeField] private string _weaponFamilyId;
        [SerializeField] private Vector3 _positionOffset;

        public string WeaponFamilyId => _weaponFamilyId;
        public Vector3 PositionOffset => _positionOffset;
    }

    [CreateAssetMenu(fileName = "CharacterViewmodelProfile", menuName = "Reloader/Player/Character Viewmodel Profile")]
    public sealed class CharacterViewmodelProfile : ScriptableObject
    {
        [SerializeField] private List<WeaponFamilyOffsetOverride> _weaponFamilyOffsetOverrides = new List<WeaponFamilyOffsetOverride>();

        public IReadOnlyList<WeaponFamilyOffsetOverride> WeaponFamilyOffsetOverrides => _weaponFamilyOffsetOverrides;
    }
}

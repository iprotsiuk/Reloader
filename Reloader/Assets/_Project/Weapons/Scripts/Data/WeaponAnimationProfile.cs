using UnityEngine;

namespace Reloader.Weapons.Data
{
    [CreateAssetMenu(fileName = "WeaponAnimationProfile", menuName = "Reloader/Weapons/Weapon Animation Profile")]
    public sealed class WeaponAnimationProfile : ScriptableObject
    {
        [SerializeField, Range(0.1f, 1f)] private float _adsSpeedMultiplier = 0.7f;

        public float AdsSpeedMultiplier => _adsSpeedMultiplier;
    }
}

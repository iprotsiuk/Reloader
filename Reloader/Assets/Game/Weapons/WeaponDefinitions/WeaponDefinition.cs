using UnityEngine;

namespace Reloader.Game.Weapons
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Reloader/Game/Weapons/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private string _weaponId = string.Empty;
        [SerializeField] private GameObject _viewModelPrefab;
        [SerializeField, Min(0.01f)] private float _adsInTime = 0.12f;
        [SerializeField, Min(0.01f)] private float _adsOutTime = 0.1f;
        [SerializeField, Min(0f)] private float _baseAdsSensitivityScale = 1f;
        [SerializeField, Min(0f)] private float _baseAdsSwayScale = 1f;
        [SerializeField, Range(20f, 120f)] private float _defaultWorldFov = 75f;
        [SerializeField, Range(20f, 120f)] private float _defaultViewmodelFov = 60f;

        public string WeaponId => string.IsNullOrWhiteSpace(_weaponId) ? string.Empty : _weaponId;
        public GameObject ViewModelPrefab => _viewModelPrefab;
        public float AdsInTime => Mathf.Max(0.01f, _adsInTime);
        public float AdsOutTime => Mathf.Max(0.01f, _adsOutTime);
        public float BaseAdsSensitivityScale => Mathf.Max(0f, _baseAdsSensitivityScale);
        public float BaseAdsSwayScale => Mathf.Max(0f, _baseAdsSwayScale);
        public float DefaultWorldFov => Mathf.Clamp(_defaultWorldFov, 20f, 120f);
        public float DefaultViewmodelFov => Mathf.Clamp(_defaultViewmodelFov, 20f, 120f);
    }
}

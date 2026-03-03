using UnityEngine;
using Reloader.Weapons.PackRuntime;

namespace Reloader.Weapons.Data
{
    [System.Serializable]
    public struct WeaponScopeConfiguration
    {
        [SerializeField] private bool _isScopedWeapon;
        [SerializeField] private float _minZoom;
        [SerializeField] private float _maxZoom;
        [SerializeField] private float _defaultZoom;
        [SerializeField] private string _reticleId;
        [SerializeField] private int _defaultZeroMeters;
        [SerializeField] private int _zeroStepMeters;

        public bool IsScopedWeapon => _isScopedWeapon;
        public float MinZoom => Mathf.Max(1f, _minZoom);
        public float MaxZoom => Mathf.Max(MinZoom, _maxZoom);
        public float DefaultZoom => Mathf.Clamp(_defaultZoom, MinZoom, MaxZoom);
        public string ReticleId => string.IsNullOrWhiteSpace(_reticleId) ? string.Empty : _reticleId;
        public int DefaultZeroMeters => Mathf.Max(0, _defaultZeroMeters);
        public int ZeroStepMeters => Mathf.Max(1, _zeroStepMeters);

        public float ClampZoom(float zoom)
        {
            return Mathf.Clamp(zoom, MinZoom, MaxZoom);
        }

        public int ClampZeroMeters(int zeroMeters)
        {
            return Mathf.Clamp(zeroMeters, 0, 3000);
        }

        public static WeaponScopeConfiguration Create(
            bool isScopedWeapon,
            float minZoom,
            float maxZoom,
            float defaultZoom,
            string reticleId,
            int defaultZeroMeters,
            int zeroStepMeters)
        {
            var config = new WeaponScopeConfiguration
            {
                _isScopedWeapon = isScopedWeapon,
                _minZoom = minZoom,
                _maxZoom = maxZoom,
                _defaultZoom = defaultZoom,
                _reticleId = reticleId,
                _defaultZeroMeters = defaultZeroMeters,
                _zeroStepMeters = zeroStepMeters
            };

            return config;
        }
    }

    [System.Serializable]
    public struct WeaponPackPresentationConfiguration
    {
        [SerializeField] private bool _useCustomConfig;
        [SerializeField] private PackWeaponPresentationConfig _customConfig;

        public bool UseCustomConfig => _useCustomConfig;
        public PackWeaponPresentationConfig CustomConfig => _customConfig;

        public PackWeaponPresentationConfig ResolveOrDefault(PackWeaponPresentationConfig fallback)
        {
            var safeFallback = fallback ?? new PackWeaponPresentationConfig();
            if (!_useCustomConfig)
            {
                return safeFallback;
            }

            return _customConfig ?? safeFallback;
        }

        public static WeaponPackPresentationConfiguration Create(
            bool useCustomConfig,
            PackWeaponPresentationConfig customConfig = null)
        {
            return new WeaponPackPresentationConfiguration
            {
                _useCustomConfig = useCustomConfig,
                _customConfig = customConfig
            };
        }
    }

    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Reloader/Weapons/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private string _itemId;
        [SerializeField] private string _displayName;
        [SerializeField] private GameObject _iconSourcePrefab;
        [SerializeField] private int _magazineCapacity = 5;
        [SerializeField] private float _fireIntervalSeconds = 0.2f;
        [SerializeField] private float _projectileSpeed = 100f;
        [SerializeField] private float _projectileGravityMultiplier = 1f;
        [SerializeField] private float _baseDamage = 20f;
        [SerializeField] private float _maxRangeMeters = 150f;
        [SerializeField, Range(0.1f, 1f)] private float _adsSpeedMultiplier = 0.7f;
        [SerializeField] private int _startingMagazineCount = 4;
        [SerializeField] private int _startingReserveCount = 24;
        [SerializeField] private bool _startingChamberLoaded = true;
        [SerializeField] private WeaponScopeConfiguration _scopeConfiguration;
        [SerializeField] private WeaponPackPresentationConfiguration _packPresentationConfiguration;

        public string ItemId => _itemId;
        public string DisplayName => _displayName;
        public GameObject IconSourcePrefab => _iconSourcePrefab;
        public int MagazineCapacity => Mathf.Max(0, _magazineCapacity);
        public float FireIntervalSeconds => Mathf.Max(0.01f, _fireIntervalSeconds);
        public float ProjectileSpeed => Mathf.Max(0f, _projectileSpeed);
        public float ProjectileGravityMultiplier => Mathf.Max(0f, _projectileGravityMultiplier);
        public float BaseDamage => Mathf.Max(0f, _baseDamage);
        public float MaxRangeMeters => Mathf.Max(0f, _maxRangeMeters);
        public float AdsSpeedMultiplier => Mathf.Clamp(_adsSpeedMultiplier, 0.1f, 1f);
        public int StartingMagazineCount => Mathf.Clamp(_startingMagazineCount, 0, MagazineCapacity);
        public int StartingReserveCount => Mathf.Max(0, _startingReserveCount);
        public bool StartingChamberLoaded => _startingChamberLoaded;
        public WeaponScopeConfiguration ScopeConfiguration => _scopeConfiguration;
        public WeaponPackPresentationConfiguration PackPresentationConfiguration => _packPresentationConfiguration;

        public PackWeaponPresentationConfig ResolvePackPresentationConfig(PackWeaponPresentationConfig fallbackConfig)
        {
            return _packPresentationConfiguration.ResolveOrDefault(fallbackConfig);
        }

        public void SetRuntimeValuesForTests(
            string itemId,
            string displayName,
            int magazineCapacity,
            float fireIntervalSeconds,
            float projectileSpeed,
            float projectileGravityMultiplier,
            float baseDamage,
            float maxRangeMeters,
            int startingMagazineCount = 4,
            int startingReserveCount = 24,
            bool startingChamberLoaded = true,
            float adsSpeedMultiplier = 0.7f,
            WeaponScopeConfiguration? scopeConfiguration = null,
            WeaponPackPresentationConfiguration? packPresentationConfiguration = null)
        {
            _itemId = itemId;
            _displayName = displayName;
            _iconSourcePrefab = null;
            _magazineCapacity = magazineCapacity;
            _fireIntervalSeconds = fireIntervalSeconds;
            _projectileSpeed = projectileSpeed;
            _projectileGravityMultiplier = projectileGravityMultiplier;
            _baseDamage = baseDamage;
            _maxRangeMeters = maxRangeMeters;
            _adsSpeedMultiplier = adsSpeedMultiplier;
            _startingMagazineCount = startingMagazineCount;
            _startingReserveCount = startingReserveCount;
            _startingChamberLoaded = startingChamberLoaded;
            _scopeConfiguration = scopeConfiguration ?? default;
            _packPresentationConfiguration = packPresentationConfiguration ?? default;
        }
    }
}

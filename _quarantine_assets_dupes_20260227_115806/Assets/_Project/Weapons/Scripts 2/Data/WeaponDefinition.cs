using UnityEngine;

namespace Reloader.Weapons.Data
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Reloader/Weapons/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private string _itemId;
        [SerializeField] private string _displayName;
        [SerializeField] private int _magazineCapacity = 5;
        [SerializeField] private float _fireIntervalSeconds = 0.2f;
        [SerializeField] private float _projectileSpeed = 100f;
        [SerializeField] private float _projectileGravityMultiplier = 1f;
        [SerializeField] private float _baseDamage = 20f;
        [SerializeField] private float _maxRangeMeters = 150f;
        [SerializeField] private int _startingMagazineCount = 4;
        [SerializeField] private int _startingReserveCount = 24;
        [SerializeField] private bool _startingChamberLoaded = true;

        public string ItemId => _itemId;
        public string DisplayName => _displayName;
        public int MagazineCapacity => Mathf.Max(0, _magazineCapacity);
        public float FireIntervalSeconds => Mathf.Max(0.01f, _fireIntervalSeconds);
        public float ProjectileSpeed => Mathf.Max(0f, _projectileSpeed);
        public float ProjectileGravityMultiplier => Mathf.Max(0f, _projectileGravityMultiplier);
        public float BaseDamage => Mathf.Max(0f, _baseDamage);
        public float MaxRangeMeters => Mathf.Max(0f, _maxRangeMeters);
        public int StartingMagazineCount => Mathf.Clamp(_startingMagazineCount, 0, MagazineCapacity);
        public int StartingReserveCount => Mathf.Max(0, _startingReserveCount);
        public bool StartingChamberLoaded => _startingChamberLoaded;

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
            bool startingChamberLoaded = true)
        {
            _itemId = itemId;
            _displayName = displayName;
            _magazineCapacity = magazineCapacity;
            _fireIntervalSeconds = fireIntervalSeconds;
            _projectileSpeed = projectileSpeed;
            _projectileGravityMultiplier = projectileGravityMultiplier;
            _baseDamage = baseDamage;
            _maxRangeMeters = maxRangeMeters;
            _startingMagazineCount = startingMagazineCount;
            _startingReserveCount = startingReserveCount;
            _startingChamberLoaded = startingChamberLoaded;
        }
    }
}

using UnityEngine;

namespace Reloader.Weapons.PackRuntime
{
    [System.Serializable]
    public sealed class PackWeaponPresentationConfig
    {
        [Header("Aim")]
        [SerializeField] private float _adsFieldOfView = 45f;
        [SerializeField] private float _adsFovLerpTime = 0.08f;

        [Header("Reload")]
        [SerializeField] private float _reloadDurationSeconds = 0.35f;

        [Header("Animator Hooks")]
        [SerializeField] private bool _useAnimatorHooks = true;
        [SerializeField] private string _equippedBoolParameter = "IsEquipped";
        [SerializeField] private string _holsteredBoolParameter = "Holstered";
        [SerializeField] private string _aimBoolParameter = "IsAiming";
        [SerializeField] private string _aimFloatParameter = "Aiming";
        [SerializeField] private float _aimOffValue = 0f;
        [SerializeField] private float _aimOnValue = 1f;
        [SerializeField] private string _aimingSpeedMultiplierFloatParameter = "Aiming Speed Multiplier";
        [SerializeField] private float _aimingSpeedMultiplier = 1f;
        [SerializeField] private string _reloadBoolParameter = "IsReloading";
        [SerializeField] private string _holsterPlayRateFloatParameter = "Play Rate Holster";
        [SerializeField] private float _holsterPlayRate = 1f;
        [SerializeField] private string _unholsterPlayRateFloatParameter = "Play Rate Unholster";
        [SerializeField] private float _unholsterPlayRate = 1f;
        [SerializeField] private string _reloadTriggerParameter = "Reload";
        [SerializeField] private string _fireTriggerParameter = "Fire";
        [SerializeField] private string _reloadStateName = "Layer Actions.Reload";
        [SerializeField] private string _fireStateName = "Layer Actions.Fire";

        public float AdsFieldOfView => Mathf.Clamp(_adsFieldOfView, 5f, 179f);
        public float AdsFovLerpTime => Mathf.Clamp(_adsFovLerpTime, 0.01f, 0.5f);
        public float ReloadDurationSeconds => Mathf.Max(0.01f, _reloadDurationSeconds);

        public bool UseAnimatorHooks => _useAnimatorHooks;
        public string EquippedBoolParameter => _equippedBoolParameter ?? string.Empty;
        public string HolsteredBoolParameter => _holsteredBoolParameter ?? string.Empty;
        public string AimBoolParameter => _aimBoolParameter ?? string.Empty;
        public string AimFloatParameter => _aimFloatParameter ?? string.Empty;
        public float AimOffValue => _aimOffValue;
        public float AimOnValue => _aimOnValue;
        public string AimingSpeedMultiplierFloatParameter => _aimingSpeedMultiplierFloatParameter ?? string.Empty;
        public float AimingSpeedMultiplier => _aimingSpeedMultiplier;
        public string ReloadBoolParameter => _reloadBoolParameter ?? string.Empty;
        public string HolsterPlayRateFloatParameter => _holsterPlayRateFloatParameter ?? string.Empty;
        public float HolsterPlayRate => Mathf.Max(0.01f, _holsterPlayRate);
        public string UnholsterPlayRateFloatParameter => _unholsterPlayRateFloatParameter ?? string.Empty;
        public float UnholsterPlayRate => Mathf.Max(0.01f, _unholsterPlayRate);
        public string ReloadTriggerParameter => _reloadTriggerParameter ?? string.Empty;
        public string FireTriggerParameter => _fireTriggerParameter ?? string.Empty;
        public string ReloadStateName => _reloadStateName ?? string.Empty;
        public string FireStateName => _fireStateName ?? string.Empty;
    }
}

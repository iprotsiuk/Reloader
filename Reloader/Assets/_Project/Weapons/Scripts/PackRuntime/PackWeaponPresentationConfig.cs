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
        [SerializeField] private string _aimBoolParameter = "IsAiming";
        [SerializeField] private string _reloadBoolParameter = "IsReloading";
        [SerializeField] private string _reloadTriggerParameter = "Reload";
        [SerializeField] private string _fireTriggerParameter = "Fire";

        public float AdsFieldOfView => Mathf.Clamp(_adsFieldOfView, 5f, 179f);
        public float AdsFovLerpTime => Mathf.Clamp(_adsFovLerpTime, 0.01f, 0.5f);
        public float ReloadDurationSeconds => Mathf.Max(0.01f, _reloadDurationSeconds);

        public bool UseAnimatorHooks => _useAnimatorHooks;
        public string EquippedBoolParameter => _equippedBoolParameter ?? string.Empty;
        public string AimBoolParameter => _aimBoolParameter ?? string.Empty;
        public string ReloadBoolParameter => _reloadBoolParameter ?? string.Empty;
        public string ReloadTriggerParameter => _reloadTriggerParameter ?? string.Empty;
        public string FireTriggerParameter => _fireTriggerParameter ?? string.Empty;
    }
}

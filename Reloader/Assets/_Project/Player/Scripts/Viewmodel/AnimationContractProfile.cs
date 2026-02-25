using UnityEngine;

namespace Reloader.Player.Viewmodel
{
    [CreateAssetMenu(fileName = "AnimationContractProfile", menuName = "Reloader/Player/Animation Contract Profile")]
    public sealed class AnimationContractProfile : ScriptableObject
    {
        [SerializeField] private int _majorVersion = 1;
        [SerializeField] private int _minorVersion = 0;
        [SerializeField] private string _moveSpeedParameter = "MoveSpeed01";
        [SerializeField] private string _aimWeightParameter = "AimWeight";
        [SerializeField] private string _isAimingParameter = "IsAiming";
        [SerializeField] private string _isReloadingParameter = "IsReloading";
        [SerializeField] private string _fireTrigger = "Fire";
        [SerializeField] private string _reloadTrigger = "Reload";

        public int MajorVersion => _majorVersion;
        public int MinorVersion => _minorVersion;
        public string MoveSpeedParameter => _moveSpeedParameter;
        public string AimWeightParameter => _aimWeightParameter;
        public string IsAimingParameter => _isAimingParameter;
        public string IsReloadingParameter => _isReloadingParameter;
        public string FireTrigger => _fireTrigger;
        public string ReloadTrigger => _reloadTrigger;
    }
}

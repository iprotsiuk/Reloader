using UnityEngine;

namespace Reloader.Weapons.Ballistics
{
    public sealed class LightCoverPenetrable : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _requiredPenetrationPower = 1f;
        [SerializeField, Range(0f, 1f)] private float _energyRetentionMultiplier = 0.65f;
        [SerializeField, Min(0f)] private float _exitOffsetMeters = 0.05f;

        public float RequiredPenetrationPower => Mathf.Max(0f, _requiredPenetrationPower);
        public float EnergyRetentionMultiplier => Mathf.Clamp01(_energyRetentionMultiplier);
        public float ExitOffsetMeters => Mathf.Max(0f, _exitOffsetMeters);
    }
}

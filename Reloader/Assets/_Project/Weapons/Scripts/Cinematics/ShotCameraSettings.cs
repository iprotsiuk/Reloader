using Reloader.Weapons.Ballistics;
using UnityEngine;

namespace Reloader.Weapons.Cinematics
{
    [System.Serializable]
    public struct ShotCameraSettings
    {
        [SerializeField] private bool _enabled;
        [SerializeField, Min(0f)] private float _minimumPredictedDistanceMeters;
        [SerializeField, Range(0.01f, 1f)] private float _slowMotionTimeScale;
        [SerializeField, Range(0.01f, 1f)] private float _speedUpTimeScale;

        public ShotCameraSettings(bool enabled, float minimumPredictedDistanceMeters, float slowMotionTimeScale, float speedUpTimeScale)
        {
            _enabled = enabled;
            _minimumPredictedDistanceMeters = Mathf.Max(0f, minimumPredictedDistanceMeters);
            _slowMotionTimeScale = Mathf.Clamp(slowMotionTimeScale, 0.01f, 1f);
            _speedUpTimeScale = Mathf.Clamp(speedUpTimeScale, 0.01f, 1f);
        }

        public bool Enabled => _enabled;
        public float MinimumPredictedDistanceMeters => Mathf.Max(0f, _minimumPredictedDistanceMeters);
        public float SlowMotionTimeScale => Mathf.Clamp(_slowMotionTimeScale, 0.01f, 1f);
        public float SpeedUpTimeScale => Mathf.Clamp(_speedUpTimeScale, 0.01f, 1f);
    }

    public readonly struct ShotCameraRequest
    {
        public ShotCameraRequest(
            WeaponProjectile projectile,
            Vector3 projectileOrigin,
            Vector3 predictedImpactPoint,
            float predictedDistanceMeters,
            ShotCameraSettings settings)
        {
            Projectile = projectile;
            ProjectileOrigin = projectileOrigin;
            PredictedImpactPoint = predictedImpactPoint;
            PredictedDistanceMeters = predictedDistanceMeters;
            Settings = settings;
        }

        public WeaponProjectile Projectile { get; }
        public Vector3 ProjectileOrigin { get; }
        public Vector3 PredictedImpactPoint { get; }
        public float PredictedDistanceMeters { get; }
        public ShotCameraSettings Settings { get; }
    }

    public interface IShotCameraRuntime
    {
        bool TryRegisterShot(ShotCameraRequest request);
    }
}

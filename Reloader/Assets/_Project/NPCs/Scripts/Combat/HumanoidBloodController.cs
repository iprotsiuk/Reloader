using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloader.NPCs.Combat
{
    [DisallowMultipleComponent]
    public sealed class HumanoidBloodController : MonoBehaviour
    {
        private const float MinimumTransientEffectLifetimeSeconds = 0.5f;
        private const float TransientEffectLifetimePaddingSeconds = 0.25f;

        [SerializeField] private HumanoidDamageReceiver _damageReceiver;
        [SerializeField] private BloodVfxCatalog _catalog;

        private readonly List<BloodEffectKind> _requestedEffects = new List<BloodEffectKind>();
        private readonly List<Vector3> _requestedEffectPositions = new List<Vector3>();

        public IReadOnlyList<BloodEffectKind> RequestedEffects => _requestedEffects;
        public IReadOnlyList<Vector3> RequestedEffectPositions => _requestedEffectPositions;

        private void Reset()
        {
            ResolveReceiver();
        }

        private void Awake()
        {
            ResolveReceiver();
        }

        private void OnEnable()
        {
            ResolveReceiver();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void ResetRuntime()
        {
            _requestedEffects.Clear();
            _requestedEffectPositions.Clear();
        }

        private void HandleResultResolved()
        {
            if (_damageReceiver == null || !_damageReceiver.HasLastResult)
            {
                return;
            }

            var effectKind = ResolveImpactEffectKind(_damageReceiver.LastZone);
            RequestEffect(effectKind, _damageReceiver.LastPayload.Point, _damageReceiver.LastPayload.Normal, false);
        }

        private void HandleDied()
        {
            RequestEffect(BloodEffectKind.DeathPuddle, transform.position, Vector3.up, true);
        }

        private void ResolveReceiver()
        {
            _damageReceiver ??= GetComponent<HumanoidDamageReceiver>();
        }

        private void Subscribe()
        {
            if (_damageReceiver == null)
            {
                return;
            }

            Unsubscribe();
            _damageReceiver.ResultResolved += HandleResultResolved;
            _damageReceiver.Died += HandleDied;
        }

        private void Unsubscribe()
        {
            if (_damageReceiver == null)
            {
                return;
            }

            _damageReceiver.ResultResolved -= HandleResultResolved;
            _damageReceiver.Died -= HandleDied;
        }

        private void RequestEffect(BloodEffectKind effectKind, Vector3 position, Vector3 normal, bool keepAlive)
        {
            _requestedEffects.Add(effectKind);
            _requestedEffectPositions.Add(position);
            if (_catalog == null || !_catalog.TryGetPrefab(effectKind, out var prefab) || prefab == null)
            {
                return;
            }

            var rotation = ResolveSpawnRotation(normal);
            var instance = Instantiate(prefab, position, rotation);
            if (!keepAlive)
            {
                PrepareTransientEffect(instance);
            }
        }

        private static Quaternion ResolveSpawnRotation(Vector3 normal)
        {
            if (normal.sqrMagnitude <= 0.0001f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(normal.normalized);
        }

        private static void PrepareTransientEffect(GameObject effectInstance)
        {
            if (effectInstance == null)
            {
                return;
            }

            var particleSystems = effectInstance.GetComponentsInChildren<ParticleSystem>(true);
            if (particleSystems == null || particleSystems.Length == 0)
            {
                return;
            }

            var destroyDelay = 0f;
            for (var i = 0; i < particleSystems.Length; i++)
            {
                var system = particleSystems[i];
                if (system == null)
                {
                    continue;
                }

                var main = system.main;
                main.loop = false;
                destroyDelay = Mathf.Max(destroyDelay, ResolveAutoDestroyDelay(main));
                system.Play(withChildren: true);
            }

            Destroy(effectInstance, Mathf.Max(MinimumTransientEffectLifetimeSeconds, destroyDelay + TransientEffectLifetimePaddingSeconds));
        }

        private static float ResolveAutoDestroyDelay(ParticleSystem.MainModule main)
        {
            var startDelay = ResolveMaxCurveValue(main.startDelay);
            var duration = main.duration;
            var lifetime = ResolveMaxCurveValue(main.startLifetime);
            return startDelay + duration + lifetime;
        }

        private static float ResolveMaxCurveValue(ParticleSystem.MinMaxCurve curve)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return curve.constant;
                case ParticleSystemCurveMode.TwoConstants:
                    return Mathf.Max(curve.constantMin, curve.constantMax);
                case ParticleSystemCurveMode.Curve:
                    return ResolveCurvePeak(curve.curveMultiplier, curve.curve);
                case ParticleSystemCurveMode.TwoCurves:
                    return Mathf.Max(
                        ResolveCurvePeak(curve.curveMultiplier, curve.curveMin),
                        ResolveCurvePeak(curve.curveMultiplier, curve.curveMax));
                default:
                    return curve.constant;
            }
        }

        private static float ResolveCurvePeak(float multiplier, AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
            {
                return 0f;
            }

            var peak = 0f;
            for (var i = 0; i < curve.length; i++)
            {
                peak = Mathf.Max(peak, curve.keys[i].value);
            }

            return peak * multiplier;
        }

        private static BloodEffectKind ResolveImpactEffectKind(HumanoidBodyZone zone)
        {
            switch (zone)
            {
                case HumanoidBodyZone.Head:
                    return BloodEffectKind.HeadImpact;
                case HumanoidBodyZone.Neck:
                    return BloodEffectKind.NeckImpact;
                case HumanoidBodyZone.ArmL:
                case HumanoidBodyZone.ArmR:
                    return BloodEffectKind.ArmImpact;
                case HumanoidBodyZone.LegL:
                case HumanoidBodyZone.LegR:
                    return BloodEffectKind.LegImpact;
                case HumanoidBodyZone.Pelvis:
                case HumanoidBodyZone.Torso:
                default:
                    return BloodEffectKind.TorsoImpact;
            }
        }
    }
}

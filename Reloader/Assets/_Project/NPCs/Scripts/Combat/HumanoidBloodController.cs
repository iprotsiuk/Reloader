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
        private const float DefaultDeathPuddleLifetimeSeconds = 45f;
        private const float DefaultDeathPuddleScale = 0.65f;
        private const float DeathPuddleSurfaceProbeHeight = 1.5f;
        private const float DeathPuddleSurfaceProbeDistance = 4f;

        [SerializeField] private HumanoidDamageReceiver _damageReceiver;
        [SerializeField] private BloodVfxCatalog _catalog;
        [SerializeField] private Material _deathPuddleMaterial;

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
            var hitTransform = ResolveImpactAnchorTransform();
            RequestEffect(effectKind, _damageReceiver.LastPayload.Point, _damageReceiver.LastPayload.Normal, false, hitTransform);
        }

        private void HandleDied()
        {
            RequestEffect(BloodEffectKind.DeathPuddle, transform.position, Vector3.up, true, null);
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

        private void RequestEffect(BloodEffectKind effectKind, Vector3 position, Vector3 normal, bool keepAlive, Transform attachTarget)
        {
            _requestedEffects.Add(effectKind);
            _requestedEffectPositions.Add(position);
            GameObject prefab = null;
            var hasPrefab = _catalog != null && _catalog.TryGetPrefab(effectKind, out prefab) && prefab != null;
            if (!hasPrefab)
            {
                if (effectKind == BloodEffectKind.DeathPuddle)
                {
                    SpawnDeathPuddle(position);
                }

                return;
            }

            var rotation = ResolveSpawnRotation(normal);
            var instance = Instantiate(prefab, position, rotation);
            if (!keepAlive && attachTarget != null)
            {
                instance.transform.SetParent(attachTarget, true);
            }

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
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
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

        private Transform ResolveImpactAnchorTransform()
        {
            if (_damageReceiver == null)
            {
                return null;
            }

            var hitboxRig = _damageReceiver.HitboxRig;
            if (hitboxRig != null && hitboxRig.TryResolveBone(_damageReceiver.LastZone, out var zoneBone) && zoneBone != null)
            {
                return zoneBone;
            }

            var hitObject = _damageReceiver.LastPayload.HitObject;
            if (hitObject == null)
            {
                return null;
            }

            if (hitObject.TryGetComponent<Rigidbody>(out var attachedBody) && attachedBody != null)
            {
                return attachedBody.transform;
            }

            var parentBody = hitObject.GetComponentInParent<Rigidbody>();
            if (parentBody != null)
            {
                return parentBody.transform;
            }

            return hitObject.transform;
        }

        private void SpawnDeathPuddle(Vector3 position)
        {
            if (_deathPuddleMaterial == null)
            {
                return;
            }

            var puddle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            puddle.name = "BloodPuddle";
            ResolveDeathPuddlePose(position, out var puddlePosition, out var puddleRotation);
            puddle.transform.SetPositionAndRotation(puddlePosition, puddleRotation);
            puddle.transform.localScale = Vector3.one * DefaultDeathPuddleScale;

            var collider = puddle.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = puddle.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = _deathPuddleMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            Destroy(puddle, DefaultDeathPuddleLifetimeSeconds);
        }

        private static void ResolveDeathPuddlePose(Vector3 origin, out Vector3 position, out Quaternion rotation)
        {
            var spawnPosition = origin + (Vector3.up * 0.02f);
            var surfaceNormal = Vector3.up;

            var probeOrigin = origin + (Vector3.up * DeathPuddleSurfaceProbeHeight);
            if (Physics.Raycast(probeOrigin, Vector3.down, out var hit, DeathPuddleSurfaceProbeDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                spawnPosition = hit.point + (hit.normal * 0.02f);
                surfaceNormal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector3.up;
            }

            var baseRotation = Quaternion.LookRotation(-surfaceNormal);
            var randomTwist = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), surfaceNormal);
            position = spawnPosition;
            rotation = randomTwist * baseRotation;
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

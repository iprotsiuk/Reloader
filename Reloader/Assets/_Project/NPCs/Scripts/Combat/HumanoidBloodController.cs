using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloader.NPCs.Combat
{
    [DisallowMultipleComponent]
    public sealed class HumanoidBloodController : MonoBehaviour
    {
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
            RequestEffect(effectKind, _damageReceiver.LastPayload.Point);
        }

        private void HandleDied()
        {
            RequestEffect(BloodEffectKind.DeathPuddle, transform.position);
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

        private void RequestEffect(BloodEffectKind effectKind, Vector3 position)
        {
            _requestedEffects.Add(effectKind);
            _requestedEffectPositions.Add(position);
            if (_catalog == null || !_catalog.TryGetPrefab(effectKind, out var prefab) || prefab == null)
            {
                return;
            }

            Instantiate(prefab, position, Quaternion.identity);
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

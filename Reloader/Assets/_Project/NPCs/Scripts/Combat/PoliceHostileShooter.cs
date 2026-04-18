using System;
using Reloader.Contracts.Runtime;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.NPCs.Runtime;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Runtime;
using UnityEngine;

namespace Reloader.NPCs.Combat
{
    [DisallowMultipleComponent]
    public sealed class PoliceHostileShooter : MonoBehaviour
    {
        private const string ProjectileItemId = "weapon.police.hostile";

        [SerializeField] private Transform _muzzleOrigin;
        [SerializeField, Min(0.5f)] private float _rangeMeters = 35f;
        [SerializeField, Min(0f)] private float _fireCooldownSeconds = 0.75f;
        [SerializeField, Min(1f)] private float _projectileSpeedMetersPerSecond = 650f;
        [SerializeField, Min(0f)] private float _projectileGravityMultiplier;
        [SerializeField, Min(0f)] private float _projectileDamage = 20f;
        [SerializeField, Min(0.01f)] private float _projectileBallisticCoefficientG1 = 0.45f;
        [SerializeField, Min(1f)] private float _projectileMassGrains = WeaponAmmoDefaults.DefaultProjectileMassGrains;
        [SerializeField] private LayerMask _lineOfSightMask = ~0;
        [SerializeField] private WeaponProjectile _projectilePrefab;
        [SerializeField] private bool _syncWithPoliceHeat = true;
        [SerializeField] private bool _startHostile;

        private bool _isHeatHostile;
        private bool? _hostileOverride;
        private float _cooldownRemaining;
        private Transform _playerTargetOverride;
        private ILawEnforcementEvents _subscribedLawEnforcementEvents;

        private void Awake()
        {
            ResolveMuzzleOrigin();
        }

        private void OnEnable()
        {
            ResolveMuzzleOrigin();
            SubscribeToLawEnforcementEvents();
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            RuntimeKernelBootstrapper.EventsReconfigured += HandleRuntimeEventsReconfigured;
        }

        private void OnDisable()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            UnsubscribeFromLawEnforcementEvents();
        }

        private void Update()
        {
            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - Time.deltaTime);
            }

            if (_cooldownRemaining > 0f || !IsHostile())
            {
                return;
            }

            if (!TryResolvePlayerTarget(out var playerTarget))
            {
                return;
            }

            var origin = ResolveMuzzleOrigin().position;
            var aimPoint = ResolveAimPoint(playerTarget);
            var shotVector = aimPoint - origin;
            var distance = shotVector.magnitude;
            if (distance <= 0.001f || distance > _rangeMeters)
            {
                return;
            }

            var direction = shotVector / distance;
            if (!HasLineOfSight(playerTarget, origin, direction, distance))
            {
                return;
            }

            FireProjectile(origin, direction);
            _cooldownRemaining = _fireCooldownSeconds;
        }

        public void ConfigureRuntimeOrigin(Transform muzzleOrigin)
        {
            _muzzleOrigin = muzzleOrigin != null ? muzzleOrigin : transform;
        }

        private void SetPlayerTargetForTests(Transform playerTarget)
        {
            _playerTargetOverride = playerTarget;
        }

        private void SetHostileOverrideForTests(bool isHostile)
        {
            _hostileOverride = isHostile;
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            SubscribeToLawEnforcementEvents();
        }

        private void HandleHeatChanged(PoliceHeatState state)
        {
            _isHeatHostile = _syncWithPoliceHeat
                && state.Level != PoliceHeatLevel.Clear
                && state.IsPlayerIdentified;
        }

        private void SubscribeToLawEnforcementEvents()
        {
            var next = RuntimeKernelBootstrapper.LawEnforcementEvents;
            if (ReferenceEquals(_subscribedLawEnforcementEvents, next))
            {
                return;
            }

            UnsubscribeFromLawEnforcementEvents();
            _subscribedLawEnforcementEvents = next;
            if (_subscribedLawEnforcementEvents != null)
            {
                _subscribedLawEnforcementEvents.OnHeatChanged += HandleHeatChanged;
            }

            RefreshHeatHostilityFromCurrentRuntime();
        }

        private void UnsubscribeFromLawEnforcementEvents()
        {
            if (_subscribedLawEnforcementEvents == null)
            {
                return;
            }

            _subscribedLawEnforcementEvents.OnHeatChanged -= HandleHeatChanged;
            _subscribedLawEnforcementEvents = null;
        }

        private bool IsHostile()
        {
            return _hostileOverride ?? (_startHostile || (_syncWithPoliceHeat && _isHeatHostile));
        }

        private void RefreshHeatHostilityFromCurrentRuntime()
        {
            if (!_syncWithPoliceHeat)
            {
                _isHeatHostile = false;
                return;
            }

            var provider = FindFirstObjectByType<StaticContractRuntimeProvider>(FindObjectsInactive.Include);
            if (provider == null)
            {
                _isHeatHostile = false;
                return;
            }

            HandleHeatChanged(provider.CurrentHeatState);
        }

        private bool TryResolvePlayerTarget(out Transform playerTarget)
        {
            playerTarget = _playerTargetOverride;
            if (playerTarget != null)
            {
                return true;
            }

            var bridge = FindFirstObjectByType<PlayerDeathContractBridge>(FindObjectsInactive.Include);
            if (bridge == null)
            {
                return false;
            }

            playerTarget = bridge.transform;
            return playerTarget != null;
        }

        private Transform ResolveMuzzleOrigin()
        {
            if (_muzzleOrigin != null)
            {
                return _muzzleOrigin;
            }

            var spawnedCivilian = GetComponent<MainTownPopulationSpawnedCivilian>();
            if (spawnedCivilian != null)
            {
                _muzzleOrigin = spawnedCivilian.ResolveDialogueFocusTarget();
            }

            _muzzleOrigin ??= transform;
            return _muzzleOrigin;
        }

        private static Vector3 ResolveAimPoint(Transform playerTarget)
        {
            if (playerTarget == null)
            {
                return Vector3.zero;
            }

            var collider = playerTarget.GetComponentInChildren<Collider>();
            return collider != null ? collider.bounds.center : playerTarget.position;
        }

        private bool HasLineOfSight(Transform playerTarget, Vector3 origin, Vector3 direction, float distance)
        {
            var hits = Physics.RaycastAll(origin, direction, distance, _lineOfSightMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                return true;
            }

            Array.Sort(hits, static (a, b) => a.distance.CompareTo(b.distance));
            for (var i = 0; i < hits.Length; i++)
            {
                var hitTransform = hits[i].collider != null ? hits[i].collider.transform : null;
                if (hitTransform == null || hitTransform.IsChildOf(transform))
                {
                    continue;
                }

                return hitTransform.IsChildOf(playerTarget);
            }

            return true;
        }

        private void FireProjectile(Vector3 origin, Vector3 direction)
        {
            var projectile = CreateProjectileInstance(origin, direction);
            projectile.Configure();
            projectile.Initialize(
                ProjectileItemId,
                direction,
                _projectileSpeedMetersPerSecond,
                _projectileGravityMultiplier,
                _projectileDamage,
                _projectileBallisticCoefficientG1,
                _projectileMassGrains,
                coverPenetrationPower: 0f,
                shooterRoot: transform);
        }

        private WeaponProjectile CreateProjectileInstance(Vector3 origin, Vector3 direction)
        {
            if (_projectilePrefab != null)
            {
                return Instantiate(_projectilePrefab, origin, Quaternion.LookRotation(direction));
            }

            var projectileGo = new GameObject("PoliceProjectile");
            projectileGo.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
            return projectileGo.AddComponent<WeaponProjectile>();
        }
    }
}

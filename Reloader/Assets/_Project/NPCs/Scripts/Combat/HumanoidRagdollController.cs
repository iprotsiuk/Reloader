using System.Collections.Generic;
using Reloader.NPCs.Runtime;
using Reloader.Weapons.Ballistics;
using UnityEngine;

namespace Reloader.NPCs.Combat
{
    [DisallowMultipleComponent]
    public sealed class HumanoidRagdollController : MonoBehaviour
    {
        private struct RagdollBodyDormantState
        {
            public RagdollBodyDormantState(bool isKinematic, bool useGravity)
            {
                IsKinematic = isKinematic;
                UseGravity = useGravity;
            }

            public bool IsKinematic { get; }
            public bool UseGravity { get; }
        }

        [SerializeField] private HumanoidDamageReceiver _damageReceiver;
        [SerializeField] private Animator _animator;
        [SerializeField] private Behaviour[] _disableBehavioursOnDeath = System.Array.Empty<Behaviour>();
        [SerializeField] private Rigidbody[] _ragdollBodies = System.Array.Empty<Rigidbody>();
        [SerializeField] private Collider[] _ragdollColliders = System.Array.Empty<Collider>();
        [SerializeField] private Rigidbody _torsoFallbackBody;
        [SerializeField] private ForceMode _impulseForceMode = ForceMode.Impulse;

        private readonly List<Behaviour> _resolvedDisableBehaviours = new List<Behaviour>();
        private readonly Dictionary<Behaviour, bool> _initialBehaviourEnabledStates = new Dictionary<Behaviour, bool>();
        private readonly Dictionary<Collider, bool> _initialColliderEnabledStates = new Dictionary<Collider, bool>();
        private readonly Dictionary<Rigidbody, RagdollBodyDormantState> _initialBodyDormantStates = new Dictionary<Rigidbody, RagdollBodyDormantState>();

        public bool HasTakenOver { get; private set; }
        public bool CanPresentDeathState
        {
            get
            {
                ResolveDependencies();
                if (!isActiveAndEnabled || _ragdollBodies == null)
                {
                    return false;
                }

                for (var i = 0; i < _ragdollBodies.Length; i++)
                {
                    if (_ragdollBodies[i] != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private void Reset()
        {
            ResolveDependencies();
        }

        private void Awake()
        {
            ResolveDependencies();
            EnsureDormantRagdollState();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            Subscribe();
            if (!HasTakenOver)
            {
                EnsureDormantRagdollState();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void ResetRuntime()
        {
            ResolveDependencies();
            HasTakenOver = false;
            RestoreDependencies();
            RestoreColliderState();
            EnsureDormantRagdollState();
        }

        private void HandleDied()
        {
            if (_damageReceiver == null || !_damageReceiver.HasLastResult || HasTakenOver)
            {
                return;
            }

            ResolveDependencies();
            HasTakenOver = true;
            DisableDependencies();
            EnableRagdollBodies();
            ApplyLethalImpulse(_damageReceiver.LastPayload, _damageReceiver.LastResult);
        }

        private void ResolveDependencies()
        {
            _damageReceiver ??= GetComponent<HumanoidDamageReceiver>();
            _animator ??= GetComponentInChildren<Animator>(includeInactive: true);

            if (_ragdollBodies == null || _ragdollBodies.Length == 0)
            {
                _ragdollBodies = GetComponentsInChildren<Rigidbody>(includeInactive: true);
            }

            if (_ragdollColliders == null || _ragdollColliders.Length == 0)
            {
                _ragdollColliders = GetComponentsInChildren<Collider>(includeInactive: true);
            }

            if (_torsoFallbackBody == null)
            {
                _torsoFallbackBody = ResolveTorsoFallbackBody();
            }

            _resolvedDisableBehaviours.Clear();
            AddDisableBehaviour(_animator);
            AddDisableBehaviour(GetComponent<NpcAiController>());
            AddDisableBehaviour(GetComponent<ContractTargetPatrolMotion>());

            if (_disableBehavioursOnDeath != null)
            {
                for (var i = 0; i < _disableBehavioursOnDeath.Length; i++)
                {
                    AddDisableBehaviour(_disableBehavioursOnDeath[i]);
                }
            }

            CaptureInitialState();
        }

        private void Subscribe()
        {
            if (_damageReceiver == null)
            {
                return;
            }

            Unsubscribe();
            _damageReceiver.Died += HandleDied;
        }

        private void Unsubscribe()
        {
            if (_damageReceiver == null)
            {
                return;
            }

            _damageReceiver.Died -= HandleDied;
        }

        private void EnsureDormantRagdollState()
        {
            for (var i = 0; i < _ragdollBodies.Length; i++)
            {
                var body = _ragdollBodies[i];
                if (body == null)
                {
                    continue;
                }

                if (_initialBodyDormantStates.TryGetValue(body, out var dormantState))
                {
                    body.isKinematic = dormantState.IsKinematic;
                    body.useGravity = dormantState.UseGravity;
                }
                else
                {
                    body.isKinematic = true;
                    body.useGravity = false;
                }
            }
        }

        private void DisableDependencies()
        {
            for (var i = 0; i < _resolvedDisableBehaviours.Count; i++)
            {
                var behaviour = _resolvedDisableBehaviours[i];
                if (behaviour != null)
                {
                    behaviour.enabled = false;
                }
            }
        }

        private void EnableRagdollBodies()
        {
            for (var i = 0; i < _ragdollColliders.Length; i++)
            {
                var collider = _ragdollColliders[i];
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }

            for (var i = 0; i < _ragdollBodies.Length; i++)
            {
                var body = _ragdollBodies[i];
                if (body == null)
                {
                    continue;
                }

                body.isKinematic = false;
                body.useGravity = true;
                body.WakeUp();
            }
        }

        private void ApplyLethalImpulse(ProjectileImpactPayload payload, HumanoidImpactResolutionResult result)
        {
            var targetBody = ResolveImpulseTarget(payload.HitObject) ?? _torsoFallbackBody;
            if (targetBody == null)
            {
                return;
            }

            var direction = payload.Direction.sqrMagnitude > 0.0001f ? payload.Direction.normalized : transform.forward;
            var impulseMagnitude = Mathf.Max(0.2f, result.RecommendedRagdollImpulseScalar);
            targetBody.AddForceAtPosition(direction * impulseMagnitude, payload.Point, _impulseForceMode);
        }

        private Rigidbody ResolveImpulseTarget(GameObject hitObject)
        {
            if (hitObject == null)
            {
                return null;
            }

            var body = hitObject.GetComponent<Rigidbody>() ?? hitObject.GetComponentInParent<Rigidbody>();
            if (body == null)
            {
                return null;
            }

            for (var i = 0; i < _ragdollBodies.Length; i++)
            {
                if (_ragdollBodies[i] == body)
                {
                    return body;
                }
            }

            return null;
        }

        private Rigidbody ResolveTorsoFallbackBody()
        {
            for (var i = 0; i < _ragdollBodies.Length; i++)
            {
                var body = _ragdollBodies[i];
                if (body == null)
                {
                    continue;
                }

                var hitbox = body.GetComponent<BodyZoneHitbox>() ?? body.GetComponentInParent<BodyZoneHitbox>();
                if (hitbox != null && hitbox.BodyZone == HumanoidBodyZone.Torso)
                {
                    return body;
                }
            }

            return _ragdollBodies.Length > 0 ? _ragdollBodies[0] : null;
        }

        private void AddDisableBehaviour(Behaviour behaviour)
        {
            if (behaviour == null || _resolvedDisableBehaviours.Contains(behaviour))
            {
                return;
            }

            _resolvedDisableBehaviours.Add(behaviour);
        }

        private void CaptureInitialState()
        {
            for (var i = 0; i < _resolvedDisableBehaviours.Count; i++)
            {
                var behaviour = _resolvedDisableBehaviours[i];
                if (behaviour != null && !_initialBehaviourEnabledStates.ContainsKey(behaviour))
                {
                    _initialBehaviourEnabledStates.Add(behaviour, behaviour.enabled);
                }
            }

            for (var i = 0; i < _ragdollColliders.Length; i++)
            {
                var collider = _ragdollColliders[i];
                if (collider != null && !_initialColliderEnabledStates.ContainsKey(collider))
                {
                    _initialColliderEnabledStates.Add(collider, collider.enabled);
                }
            }

            for (var i = 0; i < _ragdollBodies.Length; i++)
            {
                var body = _ragdollBodies[i];
                if (body != null && !_initialBodyDormantStates.ContainsKey(body))
                {
                    _initialBodyDormantStates.Add(body, new RagdollBodyDormantState(body.isKinematic, body.useGravity));
                }
            }
        }

        private void RestoreDependencies()
        {
            foreach (var pair in _initialBehaviourEnabledStates)
            {
                if (pair.Key != null)
                {
                    pair.Key.enabled = pair.Value;
                }
            }
        }

        private void RestoreColliderState()
        {
            foreach (var pair in _initialColliderEnabledStates)
            {
                if (pair.Key != null)
                {
                    pair.Key.enabled = pair.Value;
                }
            }
        }
    }
}

using System.Collections.Generic;
using Reloader.NPCs.Runtime;
using Reloader.Weapons.Ballistics;
using UnityEngine;

namespace Reloader.NPCs.Combat
{
    [DisallowMultipleComponent]
    public sealed class HumanoidRagdollController : MonoBehaviour
    {
        private const string RootRagdollBodyName = "RootRagdollBody";
        private const float TestImpulseMultiplier = 100f;

        [SerializeField] private HumanoidDamageReceiver _damageReceiver;
        [SerializeField] private Animator _animator;
        [SerializeField] private Behaviour[] _disableBehavioursOnDeath = System.Array.Empty<Behaviour>();
        [SerializeField] private Rigidbody[] _ragdollBodies = System.Array.Empty<Rigidbody>();
        [SerializeField] private Collider[] _ragdollColliders = System.Array.Empty<Collider>();
        [SerializeField] private Collider[] _collidersToDisableOnDeath = System.Array.Empty<Collider>();
        [SerializeField] private Rigidbody _torsoFallbackBody;
        [SerializeField] private ForceMode _impulseForceMode = ForceMode.Impulse;

        private readonly List<Behaviour> _resolvedDisableBehaviours = new List<Behaviour>();
        private readonly Dictionary<Behaviour, bool> _initialBehaviourEnabledStates = new Dictionary<Behaviour, bool>();
        private readonly Dictionary<Collider, bool> _initialColliderEnabledStates = new Dictionary<Collider, bool>();
        private Rigidbody[] _resolvedRagdollBodies = System.Array.Empty<Rigidbody>();
        private Collider[] _resolvedRagdollColliders = System.Array.Empty<Collider>();
        private Collider[] _resolvedCollidersToDisableOnDeath = System.Array.Empty<Collider>();
        public bool HasTakenOver { get; private set; }
        public bool CanPresentDeathState
        {
            get
            {
                ResolveDependencies();
                if (!isActiveAndEnabled || _resolvedRagdollBodies == null)
                {
                    return false;
                }

                for (var i = 0; i < _resolvedRagdollBodies.Length; i++)
                {
                    if (_resolvedRagdollBodies[i] != null)
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
            DisableLiveColliders();
            EnableRagdollBodies();
            ApplyImpactImpulse(_damageReceiver.LastPayload, _damageReceiver.LastResult);
        }

        private void ResolveDependencies()
        {
            _damageReceiver ??= GetComponent<HumanoidDamageReceiver>();
            _animator ??= GetComponentInChildren<Animator>(includeInactive: true);

            var discoveredBodies = FilterDiscoveredRagdollBodies(GetComponentsInChildren<Rigidbody>(includeInactive: true));
            var refreshedRagdollBodies = false;
            var shouldRefreshCachedBodies = ShouldRefreshCachedRagdollBodies(discoveredBodies);
            if (!HasAnyComponent(_ragdollBodies) || shouldRefreshCachedBodies)
            {
                if ((discoveredBodies == null || discoveredBodies.Length == 0) && Application.isPlaying)
                {
                    EnsureRootFallbackBody();
                    discoveredBodies = FilterDiscoveredRagdollBodies(GetComponentsInChildren<Rigidbody>(includeInactive: true));
                }

                if (shouldRefreshCachedBodies)
                {
                    RemoveObsoleteRootFallbackBody();
                }

                _ragdollBodies = discoveredBodies ?? System.Array.Empty<Rigidbody>();
                refreshedRagdollBodies = true;
            }

            if (!HasAnyComponent(_ragdollColliders) || refreshedRagdollBodies)
            {
                _ragdollColliders = DiscoverRagdollColliders(_ragdollBodies);
            }

            if (!HasAnyComponent(_collidersToDisableOnDeath))
            {
                _collidersToDisableOnDeath = DiscoverLiveColliders();
            }

            _resolvedRagdollBodies = ResolveActiveComponents(_ragdollBodies);
            _resolvedRagdollColliders = ResolveActiveComponents(_ragdollColliders);
            _resolvedCollidersToDisableOnDeath = ResolveActiveComponents(_collidersToDisableOnDeath);
            _torsoFallbackBody = ResolveTorsoFallbackBody();

            _resolvedDisableBehaviours.Clear();
            var animators = GetComponentsInChildren<Animator>(includeInactive: true);
            for (var i = 0; i < animators.Length; i++)
            {
                AddDisableBehaviour(animators[i]);
            }

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

        private void EnsureRootFallbackBody()
        {
            var rootBody = GetComponent<Rigidbody>();
            if (rootBody == null)
            {
                rootBody = gameObject.AddComponent<Rigidbody>();
                rootBody.mass = 70f;
                rootBody.interpolation = RigidbodyInterpolation.Interpolate;
            }

            if (string.IsNullOrWhiteSpace(rootBody.gameObject.name))
            {
                rootBody.gameObject.name = RootRagdollBodyName;
            }
        }

        private void Subscribe()
        {
            if (_damageReceiver == null)
            {
                return;
            }

            Unsubscribe();
            _damageReceiver.ResultResolved += HandleImpactResolved;
            _damageReceiver.Died += HandleDied;
        }

        private void Unsubscribe()
        {
            if (_damageReceiver == null)
            {
                return;
            }

            _damageReceiver.ResultResolved -= HandleImpactResolved;
            _damageReceiver.Died -= HandleDied;
        }

        private void HandleImpactResolved()
        {
            if (_damageReceiver == null || !_damageReceiver.HasLastResult || !HasTakenOver)
            {
                return;
            }

            ApplyImpactImpulse(_damageReceiver.LastPayload, _damageReceiver.LastResult);
        }

        private void EnsureDormantRagdollState()
        {
            for (var i = 0; i < _resolvedRagdollBodies.Length; i++)
            {
                var body = _resolvedRagdollBodies[i];
                if (body == null)
                {
                    continue;
                }

                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                body.isKinematic = true;
                body.useGravity = false;
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
            for (var i = 0; i < _resolvedRagdollColliders.Length; i++)
            {
                var collider = _resolvedRagdollColliders[i];
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }

            for (var i = 0; i < _resolvedRagdollBodies.Length; i++)
            {
                var body = _resolvedRagdollBodies[i];
                if (body == null)
                {
                    continue;
                }

                body.isKinematic = false;
                body.useGravity = true;
                body.WakeUp();
            }
        }

        private void DisableLiveColliders()
        {
            for (var i = 0; i < _resolvedCollidersToDisableOnDeath.Length; i++)
            {
                var collider = _resolvedCollidersToDisableOnDeath[i];
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }

        private void ApplyImpactImpulse(ProjectileImpactPayload payload, HumanoidImpactResolutionResult result)
        {
            var targetBody = ResolveImpulseTarget(payload.HitObject) ?? _torsoFallbackBody;
            if (targetBody == null)
            {
                return;
            }

            var direction = payload.Direction.sqrMagnitude > 0.0001f ? payload.Direction.normalized : transform.forward;
            var impulseMagnitude = Mathf.Max(0.2f, result.RecommendedRagdollImpulseScalar) * TestImpulseMultiplier;
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

            for (var i = 0; i < _resolvedRagdollBodies.Length; i++)
            {
                if (_resolvedRagdollBodies[i] == body)
                {
                    return body;
                }
            }

            return null;
        }

        private Rigidbody ResolveTorsoFallbackBody()
        {
            for (var i = 0; i < _resolvedRagdollBodies.Length; i++)
            {
                var body = _resolvedRagdollBodies[i];
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

            return _resolvedRagdollBodies.Length > 0 ? _resolvedRagdollBodies[0] : null;
        }

        private void AddDisableBehaviour(Behaviour behaviour)
        {
            if (behaviour == null || _resolvedDisableBehaviours.Contains(behaviour))
            {
                return;
            }

            _resolvedDisableBehaviours.Add(behaviour);
        }

        private static bool HasAnyComponent<T>(T[] components)
            where T : Component
        {
            if (components == null)
            {
                return false;
            }

            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldRefreshCachedRagdollBodies(Rigidbody[] discoveredBodies)
        {
            if (!HasOnlyRootFallbackBody(_ragdollBodies))
            {
                return false;
            }

            for (var i = 0; i < discoveredBodies.Length; i++)
            {
                var body = discoveredBodies[i];
                if (body != null && !ReferenceEquals(body.gameObject, gameObject))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasOnlyRootFallbackBody(Rigidbody[] bodies)
        {
            if (bodies == null || bodies.Length != 1)
            {
                return false;
            }

            var body = bodies[0];
            return body != null && ReferenceEquals(body.gameObject, gameObject);
        }

        private void RemoveObsoleteRootFallbackBody()
        {
            var rootBody = GetComponent<Rigidbody>();
            if (rootBody == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(rootBody);
                return;
            }

            DestroyImmediate(rootBody);
        }

        private Collider[] DiscoverRagdollColliders(Rigidbody[] ragdollBodies)
        {
            if (ragdollBodies == null || ragdollBodies.Length == 0)
            {
                return GetComponentsInChildren<Collider>(includeInactive: true) ?? System.Array.Empty<Collider>();
            }

            var colliders = new List<Collider>();
            for (var i = 0; i < ragdollBodies.Length; i++)
            {
                var body = ragdollBodies[i];
                if (body == null)
                {
                    continue;
                }

                var collider = body.GetComponent<Collider>();
                if (collider != null)
                {
                    colliders.Add(collider);
                }
            }

            return colliders.Count > 0
                ? colliders.ToArray()
                : GetComponentsInChildren<Collider>(includeInactive: true) ?? System.Array.Empty<Collider>();
        }

        private Rigidbody[] FilterDiscoveredRagdollBodies(Rigidbody[] bodies)
        {
            if (bodies == null || bodies.Length == 0)
            {
                return System.Array.Empty<Rigidbody>();
            }

            var childBodies = new List<Rigidbody>();
            for (var i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i];
                if (body != null && !ReferenceEquals(body.gameObject, gameObject))
                {
                    childBodies.Add(body);
                }
            }

            return childBodies.Count > 0 ? childBodies.ToArray() : bodies;
        }

        private static T[] ResolveActiveComponents<T>(T[] components)
            where T : Component
        {
            if (components == null || components.Length == 0)
            {
                return System.Array.Empty<T>();
            }

            var activeCount = 0;
            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] != null && components[i].gameObject.activeInHierarchy)
                {
                    activeCount++;
                }
            }

            if (activeCount > 0)
            {
                var activeComponents = new T[activeCount];
                var writeIndex = 0;
                for (var i = 0; i < components.Length; i++)
                {
                    var component = components[i];
                    if (component == null || !component.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    activeComponents[writeIndex++] = component;
                }

                return activeComponents;
            }

            var resolvedCount = 0;
            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                {
                    resolvedCount++;
                }
            }

            if (resolvedCount == 0)
            {
                return System.Array.Empty<T>();
            }

            var resolvedComponents = new T[resolvedCount];
            var resolvedIndex = 0;
            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                {
                    resolvedComponents[resolvedIndex++] = components[i];
                }
            }

            return resolvedComponents;
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

            for (var i = 0; i < _resolvedRagdollColliders.Length; i++)
            {
                var collider = _resolvedRagdollColliders[i];
                if (collider != null && !_initialColliderEnabledStates.ContainsKey(collider))
                {
                    _initialColliderEnabledStates.Add(collider, collider.enabled);
                }
            }

            for (var i = 0; i < _resolvedCollidersToDisableOnDeath.Length; i++)
            {
                var collider = _resolvedCollidersToDisableOnDeath[i];
                if (collider != null && !_initialColliderEnabledStates.ContainsKey(collider))
                {
                    _initialColliderEnabledStates.Add(collider, collider.enabled);
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

        private Collider[] DiscoverLiveColliders()
        {
            var allColliders = GetComponentsInChildren<Collider>(includeInactive: true);
            if (allColliders == null || allColliders.Length == 0)
            {
                return System.Array.Empty<Collider>();
            }

            var liveColliders = new List<Collider>();
            for (var i = 0; i < allColliders.Length; i++)
            {
                var collider = allColliders[i];
                if (collider == null || ContainsComponent(_ragdollColliders, collider))
                {
                    continue;
                }

                var attachedBody = collider.attachedRigidbody;
                if (attachedBody != null && ContainsComponent(_ragdollBodies, attachedBody))
                {
                    continue;
                }

                liveColliders.Add(collider);
            }

            return liveColliders.ToArray();
        }

        private static bool ContainsComponent<T>(T[] components, T candidate)
            where T : Component
        {
            if (candidate == null || components == null)
            {
                return false;
            }

            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] == candidate)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

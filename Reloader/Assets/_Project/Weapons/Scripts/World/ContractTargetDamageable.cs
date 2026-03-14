using System;
using Reloader.Contracts.Runtime;
using Reloader.PlayerDevice.Runtime;
using Reloader.PlayerDevice.World;
using Reloader.Weapons.Ballistics;
using UnityEngine;

namespace Reloader.Weapons.World
{
    public sealed class ContractTargetDamageable : MonoBehaviour, IDamageable, IRangeTargetMetrics
    {
        private const string SharedReceiverTypeName = "Reloader.NPCs.Combat.HumanoidDamageReceiver, Reloader.NPCs";
        private const string SharedReceiverLethalEventName = "LethalResolved";
        private const string SharedReceiverDeathEventName = "Died";
        private const string SharedReceiverIsDeadPropertyName = "IsDead";

        [SerializeField] private MonoBehaviour _eliminationSinkBehaviour;
        [SerializeField] private string _targetId = string.Empty;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField] private float _authoritativeDistanceMeters = 100f;
        // Legacy field kept for serialized scene compatibility while shared-receiver routing owns lethality.
        [SerializeField] private float _maxHealth = 1f;
        [SerializeField] private bool _reportAsExposed = true;
        [SerializeField] private bool _disableGameObjectOnElimination = true;

        private IContractTargetEliminationSink _eliminationSink;
        private object _sharedReceiver;
        private System.Reflection.EventInfo _sharedReceiverLethalEvent;
        private System.Reflection.MethodInfo _sharedReceiverApplyDamageMethod;
        private Action _sharedReceiverLethalHandler;
        private bool _isEliminated;

        public string TargetId => string.IsNullOrWhiteSpace(_targetId) ? gameObject.name : _targetId;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? gameObject.name : _displayName;
        public float DistanceMeters => Mathf.Max(0f, _authoritativeDistanceMeters);

        private void Awake()
        {
            ResetRuntime();
            ResolveEliminationSink();
            BindSharedReceiver();
        }

        private void OnEnable()
        {
            if (_isEliminated)
            {
                ResetRuntime();
            }

            ResolveEliminationSink();
            BindSharedReceiver();
        }

        private void Start()
        {
            BindSharedReceiver();
        }

        private void OnDisable()
        {
            UnbindSharedReceiver();
        }

        public void Configure(
            IContractTargetEliminationSink eliminationSink,
            string targetId,
            string displayName,
            float authoritativeDistanceMeters,
            float maxHealth,
            bool reportAsExposed = true,
            bool disableGameObjectOnElimination = true)
        {
            _eliminationSinkBehaviour = eliminationSink as MonoBehaviour;
            _eliminationSink = eliminationSink;
            _targetId = targetId ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _authoritativeDistanceMeters = Mathf.Max(0f, authoritativeDistanceMeters);
            // Preserve serialized inspector contracts while shared-receiver routing controls lethality.
            _maxHealth = Mathf.Max(0.01f, maxHealth);
            _reportAsExposed = reportAsExposed;
            _disableGameObjectOnElimination = disableGameObjectOnElimination;
            ResetRuntime();
            ResolveEliminationSink();
            BindSharedReceiver();
        }

        public void ApplyDamage(ProjectileImpactPayload payload)
        {
            if (_isEliminated)
            {
                return;
            }

            PlayerDeviceController.ActiveInstance?.IngestImpact(payload.Point, payload.HitObject, payload.SourcePoint);

            if (ForwardDamageToSharedReceiver(payload))
            {
                if (ReadSharedReceiverDeadState())
                {
                    EliminateTarget();
                }

                return;
            }

            EliminateTarget();
        }

        public void ResetRuntime()
        {
            _isEliminated = false;
        }

        private IContractTargetEliminationSink ResolveEliminationSink()
        {
            if (!IsReferenceAlive(_eliminationSink))
            {
                _eliminationSink = null;
            }

            if (_eliminationSink != null)
            {
                return _eliminationSink;
            }

            if (_eliminationSinkBehaviour is IContractTargetEliminationSink directFromField)
            {
                _eliminationSink = directFromField;
                return _eliminationSink;
            }

            var localBehaviours = GetComponents<MonoBehaviour>();
            for (var i = 0; i < localBehaviours.Length; i++)
            {
                if (localBehaviours[i] is IContractTargetEliminationSink localSink)
                {
                    _eliminationSink = localSink;
                    return _eliminationSink;
                }
            }

            return null;
        }

        private bool BindSharedReceiver()
        {
            if (!IsReferenceAlive(_sharedReceiver))
            {
                _sharedReceiver = null;
                _sharedReceiverLethalEvent = null;
                _sharedReceiverApplyDamageMethod = null;
            }

            var sharedReceiverType = System.Type.GetType(SharedReceiverTypeName, throwOnError: false);
            if (sharedReceiverType == null)
            {
                UnbindSharedReceiver();
                return false;
            }

            var receiver = GetComponent(sharedReceiverType);
            if (receiver == null)
            {
                UnbindSharedReceiver();
                return false;
            }

            if (!ReferenceEquals(receiver, _sharedReceiver))
            {
                UnbindSharedReceiver();
                _sharedReceiver = receiver;
                _sharedReceiverApplyDamageMethod = sharedReceiverType.GetMethod(
                    "ApplyDamage",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public,
                    binder: null,
                    types: new[] { typeof(ProjectileImpactPayload) },
                    modifiers: null);
                _sharedReceiverLethalEvent = sharedReceiverType.GetEvent(SharedReceiverLethalEventName)
                    ?? sharedReceiverType.GetEvent(SharedReceiverDeathEventName);

                if (_sharedReceiverLethalEvent != null && _sharedReceiverLethalEvent.EventHandlerType == typeof(Action))
                {
                    _sharedReceiverLethalHandler ??= HandleSharedReceiverLethal;
                    _sharedReceiverLethalEvent.AddEventHandler(_sharedReceiver, _sharedReceiverLethalHandler);
                }
            }

            if (ReadSharedReceiverDeadState())
            {
                EliminateTarget();
            }

            return _sharedReceiverApplyDamageMethod != null;
        }

        private void UnbindSharedReceiver()
        {
            if (_sharedReceiver != null &&
                _sharedReceiverLethalEvent != null &&
                _sharedReceiverLethalHandler != null)
            {
                _sharedReceiverLethalEvent.RemoveEventHandler(_sharedReceiver, _sharedReceiverLethalHandler);
            }

            _sharedReceiver = null;
            _sharedReceiverLethalEvent = null;
            _sharedReceiverApplyDamageMethod = null;
        }

        private bool ForwardDamageToSharedReceiver(ProjectileImpactPayload payload)
        {
            if (!BindSharedReceiver() || _sharedReceiverApplyDamageMethod == null || _sharedReceiver == null)
            {
                return false;
            }

            _sharedReceiverApplyDamageMethod.Invoke(_sharedReceiver, new object[] { payload });
            return true;
        }

        private bool ReadSharedReceiverDeadState()
        {
            if (_sharedReceiver == null)
            {
                return false;
            }

            var property = _sharedReceiver.GetType().GetProperty(
                SharedReceiverIsDeadPropertyName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (property == null || property.PropertyType != typeof(bool))
            {
                return false;
            }

            var value = property.GetValue(_sharedReceiver);
            return value is bool isDead && isDead;
        }

        private void HandleSharedReceiverLethal()
        {
            EliminateTarget();
        }

        private void EliminateTarget()
        {
            if (_isEliminated)
            {
                return;
            }

            _isEliminated = true;
            ResolveEliminationSink()?.ReportContractTargetEliminated(TargetId, _reportAsExposed);
            if (_disableGameObjectOnElimination)
            {
                gameObject.SetActive(false);
            }
        }

        private static bool IsReferenceAlive(object instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (instance is UnityEngine.Object unityObject && unityObject == null)
            {
                return false;
            }

            return true;
        }
    }
}

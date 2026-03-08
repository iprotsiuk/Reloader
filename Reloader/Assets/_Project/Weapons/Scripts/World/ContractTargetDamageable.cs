using Reloader.Contracts.Runtime;
using Reloader.PlayerDevice.Runtime;
using Reloader.PlayerDevice.World;
using Reloader.Weapons.Ballistics;
using UnityEngine;

namespace Reloader.Weapons.World
{
    public sealed class ContractTargetDamageable : MonoBehaviour, IDamageable, IRangeTargetMetrics
    {
        [SerializeField] private MonoBehaviour _eliminationSinkBehaviour;
        [SerializeField] private string _targetId = string.Empty;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField] private float _authoritativeDistanceMeters = 100f;
        [SerializeField] private float _maxHealth = 1f;
        [SerializeField] private bool _reportAsExposed = true;
        [SerializeField] private bool _disableGameObjectOnElimination = true;

        private IContractTargetEliminationSink _eliminationSink;
        private float _currentHealth;
        private bool _isEliminated;

        public string TargetId => string.IsNullOrWhiteSpace(_targetId) ? gameObject.name : _targetId;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? gameObject.name : _displayName;
        public float DistanceMeters => Mathf.Max(0f, _authoritativeDistanceMeters);

        private void Awake()
        {
            ResetRuntime();
            ResolveEliminationSink();
        }

        private void OnEnable()
        {
            if (_currentHealth <= 0f || _isEliminated)
            {
                ResetRuntime();
            }

            ResolveEliminationSink();
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
            _maxHealth = Mathf.Max(0.01f, maxHealth);
            _reportAsExposed = reportAsExposed;
            _disableGameObjectOnElimination = disableGameObjectOnElimination;
            ResetRuntime();
            ResolveEliminationSink();
        }

        public void ApplyDamage(ProjectileImpactPayload payload)
        {
            if (_isEliminated)
            {
                return;
            }

            PlayerDeviceController.ActiveInstance?.IngestImpact(payload.Point, payload.HitObject, payload.SourcePoint);

            _currentHealth -= Mathf.Max(0f, payload.Damage);
            if (_currentHealth > 0f)
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

        public void ResetRuntime()
        {
            _currentHealth = Mathf.Max(0.01f, _maxHealth);
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

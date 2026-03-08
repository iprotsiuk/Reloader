using Reloader.Core;
using Reloader.Core.Runtime;
using UnityEngine;

namespace Reloader.Contracts.Runtime
{
    public sealed class StaticContractRuntimeProvider : MonoBehaviour, IContractRuntimeProvider, IContractTargetEliminationSink
    {
        [SerializeField] private AssassinationContractDefinition _availableContract;
        [SerializeField] private MonoBehaviour _payoutReceiverBehaviour;
        [SerializeField] private float _searchDurationSeconds = 45f;

        private ContractEscapeResolutionRuntime _runtime;
        private IContractPayoutReceiver _payoutReceiver;

        private void Awake()
        {
            EnsureRuntime();
        }

        private void OnEnable()
        {
            RebuildRuntime();
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            RuntimeKernelBootstrapper.EventsReconfigured += HandleRuntimeEventsReconfigured;
        }

        private void Update()
        {
            if (_runtime == null)
            {
                return;
            }

            _runtime.Advance(Time.deltaTime);
        }

        private void OnDisable()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
        }

        public bool TryGetContractSnapshot(out ContractOfferSnapshot snapshot)
        {
            return EnsureRuntime().TryGetSnapshot(out snapshot);
        }

        public bool AcceptAvailableContract()
        {
            return EnsureRuntime().AcceptAvailableContract();
        }

        public bool CancelActiveContract()
        {
            return EnsureRuntime().CancelActiveContract();
        }

        public bool ClaimCompletedContractReward()
        {
            return EnsureRuntime().ClaimCompletedContractReward();
        }

        public void ReportContractTargetEliminated(string targetId, bool wasExposed)
        {
            EnsureRuntime().ReportTargetEliminated(targetId, wasExposed);
        }

        public bool CanPublishAvailableContract()
        {
            return EnsureRuntime().CanPublishAvailableContract();
        }

        public void SetAvailableContract(AssassinationContractDefinition availableContract)
        {
            _availableContract = availableContract;
            EnsureRuntime().SetAvailableContract(availableContract);
        }

        public void SetPayoutReceiver(IContractPayoutReceiver payoutReceiver)
        {
            _payoutReceiver = payoutReceiver;
            EnsureRuntime().ConfigurePayoutReceiver(payoutReceiver);
        }

        public void AdvanceRuntime(float deltaTimeSeconds)
        {
            EnsureRuntime().Advance(deltaTimeSeconds);
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            RebuildRuntime();
        }

        private ContractEscapeResolutionRuntime EnsureRuntime()
        {
            if (_runtime == null)
            {
                RebuildRuntime();
            }
            else
            {
                var payoutReceiver = ResolvePayoutReceiver();
                if (!ReferenceEquals(_payoutReceiver, payoutReceiver))
                {
                    _payoutReceiver = payoutReceiver;
                    _runtime.ConfigurePayoutReceiver(_payoutReceiver);
                }
            }

            return _runtime;
        }

        private void RebuildRuntime()
        {
            _payoutReceiver = ResolvePayoutReceiver();
            if (_runtime == null)
            {
                _runtime = new ContractEscapeResolutionRuntime(
                    _availableContract,
                    _searchDurationSeconds,
                    _payoutReceiver,
                    RuntimeKernelBootstrapper.LawEnforcementEvents);
                return;
            }

            var state = _runtime.CaptureRuntimeState();
            _runtime = ContractEscapeResolutionRuntime.RestoreRuntimeState(
                state,
                _searchDurationSeconds,
                _payoutReceiver,
                RuntimeKernelBootstrapper.LawEnforcementEvents);
        }

        private IContractPayoutReceiver ResolvePayoutReceiver()
        {
            if (!IsReferenceAlive(_payoutReceiver))
            {
                _payoutReceiver = null;
            }

            if (_payoutReceiver != null)
            {
                return _payoutReceiver;
            }

            if (_payoutReceiverBehaviour is IContractPayoutReceiver directFromField)
            {
                _payoutReceiver = directFromField;
                return _payoutReceiver;
            }

            _payoutReceiver = DependencyResolutionGuard.FindInterface<IContractPayoutReceiver>(GetComponents<MonoBehaviour>());
            if (_payoutReceiver != null)
            {
                return _payoutReceiver;
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

using Reloader.Core.Events;
using Reloader.Core.Runtime;
using UnityEngine;

namespace Reloader.NPCs.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HumanoidDamageReceiver))]
    public sealed class CivilianWitnessReporter : MonoBehaviour
    {
        [SerializeField] private HumanoidDamageReceiver _damageReceiver;

        private ILawEnforcementCrimeReporter _reporter;
        private bool _hasReported;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            ResolveReferences();
        }

        public void Configure(ILawEnforcementCrimeReporter reporter)
        {
            _reporter = reporter;
        }

        private void HandleDied()
        {
            if (_hasReported || _reporter == null)
            {
                return;
            }

            _hasReported = true;
            _reporter.ReportCrime(CrimeType.Murder);
        }

        private void ResolveReferences()
        {
            _damageReceiver = GetComponent<HumanoidDamageReceiver>();
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
    }
}

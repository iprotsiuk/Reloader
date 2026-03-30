using Reloader.Contracts.Runtime;
using UnityEngine;

namespace Reloader.NPCs.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HumanoidDamageReceiver))]
    public sealed class PlayerDeathContractBridge : MonoBehaviour
    {
        [SerializeField] private HumanoidDamageReceiver _damageReceiver;

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

        private void HandleDied()
        {
            if (_damageReceiver == null || !_damageReceiver.IsDead)
            {
                return;
            }

            var provider = FindFirstObjectByType<StaticContractRuntimeProvider>(FindObjectsInactive.Include);
            provider?.HandlePlayerDeath();
        }

        private void ResolveReferences()
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

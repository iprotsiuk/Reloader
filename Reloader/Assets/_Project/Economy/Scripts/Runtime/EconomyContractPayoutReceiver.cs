using Reloader.Contracts.Runtime;
using UnityEngine;

namespace Reloader.Economy
{
    public sealed class EconomyContractPayoutReceiver : MonoBehaviour, IContractPayoutReceiver
    {
        [SerializeField] private EconomyController _economyController;

        public bool TryAwardContractPayout(int amount)
        {
            ResolveEconomyController();
            return _economyController != null && _economyController.TryAwardMoney(amount);
        }

        public void SetEconomyController(EconomyController economyController)
        {
            _economyController = economyController;
        }

        private void ResolveEconomyController()
        {
            if (_economyController != null)
            {
                return;
            }

            _economyController = GetComponent<EconomyController>();
            if (_economyController != null)
            {
                return;
            }

            _economyController = FindFirstObjectByType<EconomyController>();
        }
    }
}

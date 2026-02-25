using UnityEngine;

namespace Reloader.Reloading.World
{
    public sealed class ReloadingBenchTarget : MonoBehaviour, IReloadingBenchTarget
    {
        [SerializeField] private bool _isWorkbenchOpen;
        public bool IsWorkbenchOpen => _isWorkbenchOpen;

        public void OpenWorkbench()
        {
            _isWorkbenchOpen = true;
        }

        public void CloseWorkbench()
        {
            _isWorkbenchOpen = false;
        }
    }
}

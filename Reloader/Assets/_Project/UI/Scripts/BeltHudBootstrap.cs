using Reloader.UI.Toolkit.Runtime;
using UnityEngine;

namespace Reloader.UI
{
    public sealed class BeltHudBootstrap : MonoBehaviour
    {
        [SerializeField] private UiToolkitRuntimeInstaller _runtimeInstaller;

        private bool _executed;

        private void Start()
        {
            ExecuteCutover();
        }

        public void ExecuteCutover()
        {
            if (_executed)
            {
                return;
            }

            _executed = true;
            if (_runtimeInstaller == null)
            {
                _runtimeInstaller = FindFirstObjectByType<UiToolkitRuntimeInstaller>(FindObjectsInactive.Include);
                if (_runtimeInstaller == null)
                {
                    var go = new GameObject("UiToolkitRuntimeInstaller");
                    _runtimeInstaller = go.AddComponent<UiToolkitRuntimeInstaller>();
                }
            }

            _runtimeInstaller.ExecuteCutover();
        }
    }
}

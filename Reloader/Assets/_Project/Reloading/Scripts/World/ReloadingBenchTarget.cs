using Reloader.Reloading.Runtime;
using UnityEngine;

namespace Reloader.Reloading.World
{
    public sealed class ReloadingBenchTarget : MonoBehaviour, IReloadingBenchTarget
    {
        [SerializeField] private bool _isWorkbenchOpen;
        [SerializeField] private WorkbenchDefinition _workbenchDefinition;

        private WorkbenchRuntimeState _runtimeState;
        private WorkbenchLoadoutController _loadoutController;

        public bool IsWorkbenchOpen => _isWorkbenchOpen;

        public WorkbenchRuntimeState RuntimeState => EnsureLoadoutInitialized() ? _runtimeState : null;

        public WorkbenchLoadoutController LoadoutController => EnsureLoadoutInitialized() ? _loadoutController : null;

        public void OpenWorkbench()
        {
            _isWorkbenchOpen = true;
            EnsureLoadoutInitialized();
        }

        public void CloseWorkbench()
        {
            _isWorkbenchOpen = false;
        }

        public void SetWorkbenchDefinitionForTests(WorkbenchDefinition definition)
        {
            _workbenchDefinition = definition;
            _runtimeState = null;
            _loadoutController = null;
        }

        private bool EnsureLoadoutInitialized()
        {
            if (_loadoutController != null)
            {
                return true;
            }

            if (_workbenchDefinition == null)
            {
                return false;
            }

            _runtimeState = new WorkbenchRuntimeState(_workbenchDefinition);
            _loadoutController = new WorkbenchLoadoutController(_runtimeState, new WorkbenchCompatibilityEvaluator());
            return true;
        }
    }
}

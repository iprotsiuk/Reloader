using Reloader.Reloading.Runtime;
using UnityEngine;

namespace Reloader.Reloading.World
{
    public sealed class ReloadingBenchTarget : MonoBehaviour, IReloadingBenchTarget
    {
        private const string TemporaryWorkbenchId = "bench.temporary";
        private const string PressSlotId = "press-slot";
        private const string ResizeDieSlotId = "resize-die-slot";
        private const string SeatingDieSlotId = "seating-die-slot";

        [SerializeField] private bool _isWorkbenchOpen;
        [SerializeField] private WorkbenchDefinition _workbenchDefinition;
        [SerializeField] private bool _useTemporaryAutoLoadout = true;

        private WorkbenchRuntimeState _runtimeState;
        private WorkbenchLoadoutController _loadoutController;
        private WorkbenchDefinition _temporaryWorkbenchDefinition;
        private MountableItemDefinition _temporaryPressItem;
        private MountableItemDefinition _temporaryResizeDieItem;
        private MountableItemDefinition _temporarySeatingDieItem;

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
                if (!_useTemporaryAutoLoadout)
                {
                    return false;
                }

                EnsureTemporaryAutoLoadout();
                return _loadoutController != null;
            }

            _runtimeState = new WorkbenchRuntimeState(_workbenchDefinition);
            _loadoutController = new WorkbenchLoadoutController(_runtimeState, new WorkbenchCompatibilityEvaluator());
            return true;
        }

        private void EnsureTemporaryAutoLoadout()
        {
            if (_loadoutController != null)
            {
                return;
            }

            _temporaryWorkbenchDefinition = ScriptableObject.CreateInstance<WorkbenchDefinition>();
            _temporaryWorkbenchDefinition.SetValuesForTests(
                TemporaryWorkbenchId,
                new[]
                {
                    new MountSlotDefinition(PressSlotId, new[] { "cap.press" }),
                    new MountSlotDefinition(ResizeDieSlotId, new[] { "cap.die" }),
                    new MountSlotDefinition(SeatingDieSlotId, new[] { "cap.die" })
                });

            _temporaryPressItem = ScriptableObject.CreateInstance<MountableItemDefinition>();
            _temporaryPressItem.SetValuesForTests("press.single", new[] { "cap.press" }, childSlots: null);

            _temporaryResizeDieItem = ScriptableObject.CreateInstance<MountableItemDefinition>();
            _temporaryResizeDieItem.SetValuesForTests("die.full-length", new[] { "cap.die", "cap.die.resize" }, childSlots: null);

            _temporarySeatingDieItem = ScriptableObject.CreateInstance<MountableItemDefinition>();
            _temporarySeatingDieItem.SetValuesForTests("die.seating", new[] { "cap.die", "cap.die.seat" }, childSlots: null);

            _runtimeState = new WorkbenchRuntimeState(_temporaryWorkbenchDefinition);
            _loadoutController = new WorkbenchLoadoutController(_runtimeState, new WorkbenchCompatibilityEvaluator());

            _loadoutController.TryInstall(PressSlotId, _temporaryPressItem, out _);
            _loadoutController.TryInstall(ResizeDieSlotId, _temporaryResizeDieItem, out _);
            _loadoutController.TryInstall(SeatingDieSlotId, _temporarySeatingDieItem, out _);
        }
    }
}

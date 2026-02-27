using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GDS.Core {

    public class HighlightSlotManipulator : PointerManipulator {
        Store store;
        SlotView oldSlot;
        SlotView newSlot;

        public int PollingInterval = 75;
        IVisualElementScheduledItem scheduleId;

        float arHor, arVer;
        Vector2 screenPos;

        public HighlightSlotManipulator(Store store) {
            this.store = store;
        }

        protected override void RegisterCallbacksOnTarget() {
            scheduleId = target.schedule.Execute(OnTick);
            scheduleId.Every(PollingInterval);
            scheduleId.Pause();

            target.Observe(store.Ghost, value => {
                if (value == null) Disable(); else Enable();
            });
        }

        protected override void UnregisterCallbacksFromTarget() {
            scheduleId.Pause();
        }

        void Enable() {
            scheduleId.Resume();
        }

        void Disable() {
            scheduleId.Pause();
            HideHighlight();
        }

        void OnTick() {
            arHor = target.worldBound.width / Screen.width;
            arVer = target.worldBound.height / Screen.height;

            screenPos = Pointer.current.position.ReadValue();
            screenPos.x *= arHor;
            screenPos.y = (Screen.height - screenPos.y) * arVer;

            // Why not return early if screen pos hasn't changed?
            // The item under cursor may have changed or moved
            newSlot = target.panel.Pick(screenPos)?.GetFirstOfType<SlotView>();

            if (newSlot is null) {
                HideHighlight();
            } else {
                ShowHighlight(newSlot, store.Ghost.Value);
            }
        }

        void ShowHighlight(SlotView newSlot, Item ghost) {
            if (newSlot == oldSlot) return;
            if (oldSlot != null) {
                oldSlot.RemoveFromClassList("valid");
                oldSlot.RemoveFromClassList("invalid");
            }

            newSlot.WithClass(GetClass(newSlot.Bag, newSlot.Slot, ghost));
            oldSlot = newSlot;

        }

        void HideHighlight() {
            if (oldSlot == null) return;
            oldSlot.RemoveFromClassList("valid");
            oldSlot.RemoveFromClassList("invalid");
            oldSlot = null;
        }

        string GetClass(Bag bag, Slot slot, Item item) => bag.Accepts(item) && slot.Accepts(item) ? "valid" : "invalid";


    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using GDS.Core.Events;
using UnityEngine;

namespace GDS.Core {
    [Serializable]
    public class ListBag : Bag {

        public event Action<ListSlot> ItemChanged;
        public event Action CollectionChanged;
        public event Action CollectionReset;

        [SerializeReference]
        public List<ListSlot> Slots = new();

        public override IEnumerable<Item> Items => Slots.Where(s => s.Full()).Select(s => s.Item);

        public int Size { get => Slots.Count; set => Slots = CreateSlots(value); }

        protected List<Item> CreateItems(int size) => Enumerable.Range(0, size).Select<int, Item>(i => null).ToList();
        protected List<ListSlot> CreateSlots(int size) => Enumerable.Range(0, size).Select(i => new ListSlot() { Index = i }).ToList();
        public bool Full => Slots.Count(s => s.Empty()) == 0;
        public override Slot FindSlot(Item item) => Slots.Find(s => s.Item == item);

        protected void Notify(ListSlot slot) {
            ItemChanged?.Invoke(slot);
            CollectionChanged?.Invoke();
        }

        protected void Notify() {
            CollectionChanged?.Invoke();
        }

        protected void NotifyReset() {
            CollectionReset?.Invoke();
        }

        public override void Reset() {
            Clear();
            NotifyReset();
        }

        public override void Clear() {
            Slots.ForEach(s => s.Clear());
        }

        public override Result CanAdd(Item item) {
            if (!Accepts(item)) return Result.Fail;
            if (Full) return Result.Fail;
            return Result.Success;
        }

        public override Result Add(Item item) {
            if (!Accepts(item)) return Result.Fail;
            var index = Slots.FindIndex(s => s.Item == null);
            if (index == -1) return new BagFull(this);
            var slot = Slots[index];
            slot.Item = item;
            Notify(slot);
            return new PlaceItemSuccess(item, null);
        }

        public bool AddWithoutNotify(Item item) {
            var index = Slots.FindIndex(s => s.Item == null);
            if (index == -1) return false;
            var slot = Slots[index];
            slot.Item = item;
            return true;
        }

        public override Result AddAt(Slot slot, Item item) {
            if (slot is not ListSlot s) return Result.Fail;
            if (!Accepts(item)) return Result.Fail;
            if (!s.Accepts(item)) return Result.Fail;
            if (AllowStacking() && slot.Item != null && ItemExt.CanStack(item, slot.Item)) return TransferAll(item, slot, slot.Item);
            var replaced = s.Item;
            s.Item = item;
            Notify(s);
            return new PlaceItemSuccess(item, replaced);
        }

        public override Result AddRange(IEnumerable<Item> items) {
            bool s = true;
            foreach (var item in items) { if (!AddWithoutNotify(item)) s = false; }
            NotifyReset();
            return s == true ? Result.Success : Result.Fail;
        }

        public override Result Remove(Item item) {
            if (FindSlot(item) is not ListSlot slot) { Debug.LogWarning($"could not find item {item} in {this}"); return Result.Fail; }
            slot.Item = null;
            Notify(slot);
            return new PickItemSuccess(item);
        }

        public override Result TransferAll(Item fromItem, Slot toSlot, Item toItem) {
            if (toSlot is not ListSlot s) return Result.Fail;
            if (!Accepts(fromItem)) return Result.Fail;
            if (!AllowStacking()) return Result.Fail;
            ItemExt.TransferAll(fromItem, toItem);
            toSlot.Item = toItem;
            Notify(s);
            return new PlaceItemSuccess(toItem, fromItem.StackSize > 0 ? fromItem : null);
        }

        public override Result TransferOne(Item fromItem, Slot toSlot, Item _) {
            if (toSlot is not ListSlot s) return Result.Fail;
            if (!Accepts(fromItem)) return Result.Fail;
            if (!AllowStacking()) return Result.Fail;
            var (newFromItem, newToItem) = ItemExt.TransferOne(fromItem, toSlot.Item);
            toSlot.Item = newToItem;
            Notify(s);
            return new PlaceItemSuccess(newToItem, newFromItem.StackSize > 0 ? newFromItem : null);
        }

        public override Result SplitHalf(Item item) {
            if (FindSlot(item) is not ListSlot slot) return Result.Fail;
            if (!AllowStacking()) return Result.Fail;
            var (newFromItem, newToItem) = ItemExt.SplitHalf(slot.Item);
            slot.Item = newFromItem.StackSize > 0 ? newFromItem : null;
            Notify(slot);
            return new PickItemSuccess(newToItem);
        }


    }
}
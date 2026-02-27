using System;
using System.Collections.Generic;
using System.Linq;
using GDS.Core.Events;
using UnityEngine;

namespace GDS.Core {
    [Serializable]
    public class SetBag : Bag {

        public event Action<SetSlot> ItemChanged;
        public event Action CollectionChanged;
        public event Action CollectionReset;

        [SerializeReference]
        public List<SetSlot> Slots = new();

        public override IEnumerable<Item> Items => Slots.Where(s => s.Full()).Select(s => s.Item);

        protected SetSlot GetSlot(string key) => Slots.Find(s => s.Key == key);
        protected SetSlot CloneSlot(SetSlot slot) => new SetSlot() { Key = slot.Key, Tags = slot.Tags.Select(t => t).ToList(), Item = slot == null ? null : ItemExt.Clone(slot.Item, true) };
        public override Slot FindSlot(Item item) => Slots.Find(s => s.Item == item);

        protected void Notify(SetSlot slot) {
            ItemChanged?.Invoke(slot);
            CollectionChanged?.Invoke();
        }

        public override void Reset() {
            CollectionReset?.Invoke();
            CollectionChanged?.Invoke();
        }

        public override Result AddAt(Slot slot, Item item) {
            if (slot is not SetSlot s) return Result.Fail;
            if (!Accepts(item)) return Result.Fail;
            if (!slot.Accepts(item)) return Result.Fail;
            if (AllowStacking() && slot.Item != null && ItemExt.CanStack(item, slot.Item)) return TransferAll(item, slot, slot.Item);
            var replaced = s.Item;
            s.Item = item;
            Notify(s);
            return new PlaceItemSuccess(item, replaced);
        }

        public override Result Remove(Item item) {
            if (FindSlot(item) is not SetSlot slot) return Result.Fail;
            if (slot.Item == null) return Result.Fail;
            var removed = slot.Item;
            slot.Item = null;
            Notify(slot);
            return new PickItemSuccess(removed);
        }

        public override Result TransferAll(Item fromItem, Slot toSlot, Item toItem) {
            if (toSlot is not SetSlot s) return Result.Fail;
            if (!Accepts(fromItem)) return Result.Fail;
            if (!AllowStacking()) return Result.Fail;
            ItemExt.TransferAll(fromItem, toItem);
            toSlot.Item = toItem;
            Notify(s);
            return new PlaceItemSuccess(toItem, fromItem.StackSize > 0 ? fromItem : null);
        }

        public override Result TransferOne(Item fromItem, Slot toSlot, Item _) {
            if (toSlot is not SetSlot s) return Result.Fail;
            if (!Accepts(fromItem)) return Result.Fail;
            if (!AllowStacking()) return Result.Fail;
            var (newFromItem, newToItem) = ItemExt.TransferOne(fromItem, toSlot.Item);
            toSlot.Item = newToItem;
            Notify(s);
            return new PlaceItemSuccess(newToItem, newFromItem.StackSize > 0 ? newFromItem : null);
        }

        public override Result SplitHalf(Item item) {
            if (FindSlot(item) is not SetSlot slot) return Result.Fail;
            if (!AllowStacking()) return Result.Fail;
            var (newFromItem, newToItem) = ItemExt.SplitHalf(slot.Item);
            slot.Item = newFromItem.StackSize > 0 ? newFromItem : null;
            Notify(slot);
            return new PickItemSuccess(newToItem);
        }

    }
}
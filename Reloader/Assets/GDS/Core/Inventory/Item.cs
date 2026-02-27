using System;
using System.Linq;
using UnityEngine;

namespace GDS.Core {



    [Serializable]
    public class Item {

        public Item() => Id = IdExt.ShortId();

        public ItemBase Base;

        public string Id;
        public string Name;
        public int StackSize;

        public Sprite Icon => Base.Icon;
        public bool Stackable => Base.Stackable;
        public int MaxStackSize => Base.MaxStackSize;

        // TODO: Use Describe decorator instead of ToString (to avoid repetition in extended classes)
        public override string ToString() {
            var desc = Stackable
                ? $"Id: {Id}, StackSize:{StackSize}"
                : $"Id: {Id}";
            return $"{Name} {{ {desc} }}";
        }
    }

    public static class ItemExt {
        public static string ToPrettyString(Item item) => item == null ? "<null>".Gray() : item + $" ({item.GetType()})".Gray();
        public static string ItemNameWithQuant(this Item item) => item.Stackable ? $"{item.Name} ({item.StackSize})" : item.Name;
        public static Item Clone(this Item item, bool keepId = false) {
            // We are using reflection here because item can be a subclass of Item
            var clone = (Item)Activator.CreateInstance(item.GetType());
            clone.Base = item.Base;
            clone.Name = item.Name;
            clone.StackSize = item.StackSize;
            if (keepId) clone.Id = item.Id;
            return clone;
        }

        public static bool CanStack(this Item fromItem, Item toItem) =>
            (fromItem.Base.Stackable && toItem == null) ||
            (fromItem.Base.Stackable && fromItem.Base == toItem.Base && toItem.StackSize < toItem.MaxStackSize);

        public static (Item newFromItem, Item newToItem) TransferAll(this Item fromItem, Item toItem) {
            var total = toItem.StackSize + fromItem.StackSize;
            toItem.StackSize = Math.Min(total, toItem.MaxStackSize);
            fromItem.StackSize = total - toItem.StackSize;
            return (fromItem, toItem);
        }

        public static (Item newFromItem, Item newToItem) TransferOne(this Item fromItem, Item toItem) {
            if (toItem == null) {
                toItem = fromItem.Clone();
                toItem.StackSize = 0;
            }
            if (toItem.StackSize < toItem.MaxStackSize) {
                fromItem.StackSize--;
                toItem.StackSize++;
            }
            return (fromItem, toItem);
        }

        public static (Item newFromItem, Item newToItem) SplitHalf(this Item item) {
            var newItem = item.Clone();
            int half = item.StackSize / 2;
            item.StackSize = half;
            newItem.StackSize -= half;
            return (item, newItem);
        }
    }

    public static class IdExt {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        static System.Random random = new System.Random();
        public static string ShortId(int length = 6) {
            return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }
    }
}
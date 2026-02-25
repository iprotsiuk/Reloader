using System;
using UnityEngine.UIElements;

namespace GDS.Core.Events {

    public abstract class CustomEvent { }

    // Command
    public class Command : CustomEvent { }

    public class ItemCommand : Command {
        public Bag Bag;
        public Slot Slot;
        public Item Item;

        public IPointerEvent PointerEvent;
        public ItemCommand(Bag bag, Slot slot, Item item, IPointerEvent pointerEvent) => (Bag, Slot, Item, PointerEvent) = (bag, slot, item, pointerEvent);
    };

    public class PickItem : ItemCommand {
        public PickItem(Bag bag, Slot slot, Item item, IPointerEvent pointerEvent) : base(bag, slot, item, pointerEvent) { }
    };

    public class PlaceItem : ItemCommand {
        public PlaceItem(Bag bag, Slot slot, Item item, IPointerEvent pointerEvent) : base(bag, slot, item, pointerEvent) { }
    };

    public class OpenUI : Command { }
    public class CloseUI : Command { }
    public class ToggleUI : Command { }


    public class WindowCommand : Command {
        public object Handle;
        public WindowCommand(object handle) => Handle = handle;
    }
    public class OpenWindow : WindowCommand {
        public OpenWindow(object handle) : base(handle) { }
    }
    public class CloseWindow : WindowCommand {
        public CloseWindow(object handle) : base(handle) { }
    }

    // Result
    public class Result : CustomEvent {
        public static Result Success = new Success();
        public static Result Fail = new Fail();
    };
    public class Success : Result { }
    public class Fail : Result { }

    // Success
    public class ItemSuccess : Success {
        public Item Item;
        public ItemSuccess(Item item) => Item = item;
    }

    public class PickItemSuccess : ItemSuccess {
        public PickItemSuccess(Item item) : base(item) { }
    }

    public class PlaceItemSuccess : ItemSuccess {
        public Item Replaced;
        public PlaceItemSuccess(Item item, Item replaced) : base(item) => Replaced = replaced;
    }

    public class BuyItemSuccess : PickItemSuccess {
        public BuyItemSuccess(Item item) : base(item) { }
    }

    public class SellItemSuccess : PlaceItemSuccess {
        public SellItemSuccess(Item item, Item replaced) : base(item, replaced) { }
    }

    public class RotateItem : Success { }




    // Fail
    public class BagFull : Fail {
        public Bag Bag;
        public BagFull(Bag bag) => Bag = bag;
    }

    // Extensions
    public static class ResultExt {
        public static Result MapTo(this Result result, Success success, Fail fail = null) => result switch {
            Success => success,
            Fail when fail != null => fail,
            _ => Result.Fail
        };

        public static Result MapTo(this Result result, Func<Result> action) => result is Success ? action() : result;
        public static Result And(this Result result, Func<Result> action) => result is Success ? action() : result;

        // public static Result And(this Result result, Result target) => result is Fail ? Result.Fail : target;
    }
}
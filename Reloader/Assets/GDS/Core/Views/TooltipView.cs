using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Core {

    public class TooltipView : VisualElement {
        protected Label ItemName = new();

        public TooltipView() {
            this.Add("tooltip", ItemName.WithClass("tooltip-item-name")).PickIgnoreAll();
        }

        virtual public void Render(IHoveredItemContext context) {
            ItemName.text = $"{context.Item.ItemNameWithQuant()}\nid: {context.Item.Id.Gray()}\ntype: " + context.Item.GetType().ToString().Gray();
        }
    }
}
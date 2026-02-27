using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Core {

    public class ItemView : VisualElement {

        public ItemView() {
            this.Add("item-view",
                image = new Image().WithClass("item-image"),
                quant = new Label().WithClass("item-quant")
            ).PickIgnoreAll();
        }

        protected Image image;
        protected Label quant;

        Item item;
        public Item Item {
            get => item;
            set { item = value; Render(); }
        }

        public VisualElement View { get => this; }

        public virtual void Render() {
            // Debug.Log($"inside render item view: {item}");

            if (item == null) { this.Hide(); return; }
            this.Show();

            image.sprite = item.Icon;
            quant.text = item.StackSize.ToString();
            quant.SetVisible(item.Stackable);
        }
    }
}
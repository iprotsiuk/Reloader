using GDS.Core;

namespace GDS.Demos.Basic {

    [System.Serializable]
    public class PlayerInventory : Bag {
        public Equipment Equipment = new();
        public Inventory Inventory = new();

        public override void Reset() {
            Equipment.Init();
            Inventory.Init();
        }
    }

}
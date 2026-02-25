using Reloader.Core.Items;
using UnityEngine;

namespace Reloader.Inventory
{
    [CreateAssetMenu(fileName = "ItemSpawnDefinition", menuName = "Reloader/Items/Spawn Definition")]
    public sealed class ItemSpawnDefinition : ScriptableObject
    {
        [SerializeField] private ItemDefinition _itemDefinition;
        [SerializeField] private int _quantity = 1;
        [SerializeField] private float _durability01 = 1f;
        [SerializeField] private int _conditionFlags;
        [SerializeField] private string _runtimeStateJson = "{}";

        public ItemDefinition ItemDefinition => _itemDefinition;
        public string ItemId => _itemDefinition != null ? _itemDefinition.DefinitionId : null;
        public int Quantity
        {
            get
            {
                var quantity = Mathf.Max(1, _quantity);
                if (_itemDefinition == null)
                {
                    return quantity;
                }

                var maxStack = _itemDefinition.StackPolicy == ItemStackPolicy.NonStackable
                    ? 1
                    : _itemDefinition.MaxStack;
                return Mathf.Min(quantity, Mathf.Max(1, maxStack));
            }
        }
        public float Durability01 => Mathf.Clamp01(_durability01);
        public int ConditionFlags => _conditionFlags;
        public string RuntimeStateJson => string.IsNullOrWhiteSpace(_runtimeStateJson) ? "{}" : _runtimeStateJson;
        public bool IsValid => _itemDefinition != null && !string.IsNullOrWhiteSpace(_itemDefinition.DefinitionId);

        public void SetValuesForTests(ItemDefinition definition, int quantity = 1, float durability01 = 1f, int conditionFlags = 0, string runtimeStateJson = "{}")
        {
            _itemDefinition = definition;
            _quantity = quantity;
            _durability01 = durability01;
            _conditionFlags = conditionFlags;
            _runtimeStateJson = runtimeStateJson;
        }
    }
}

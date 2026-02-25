using UnityEngine;

namespace Reloader.Core.Items
{
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "Reloader/Items/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string _definitionId;
        [SerializeField] private ItemCategory _category = ItemCategory.Misc;
        [SerializeField] private string _displayName;
        [SerializeField] private ItemStackPolicy _stackPolicy = ItemStackPolicy.NonStackable;
        [SerializeField] private int _maxStack = 1;
        [SerializeField] private GameObject _iconSourcePrefab;

        public string DefinitionId => _definitionId;
        public ItemCategory Category => _category;
        public string DisplayName => _displayName;
        public ItemStackPolicy StackPolicy => _stackPolicy;
        public int MaxStack => Mathf.Max(1, _maxStack);
        public GameObject IconSourcePrefab => _iconSourcePrefab;

        public void SetValuesForTests(
            string definitionId,
            ItemCategory category = ItemCategory.Misc,
            string displayName = null,
            ItemStackPolicy stackPolicy = ItemStackPolicy.NonStackable,
            int maxStack = 1,
            GameObject iconSourcePrefab = null)
        {
            _definitionId = definitionId;
            _category = category;
            _displayName = displayName;
            _stackPolicy = stackPolicy;
            _maxStack = maxStack;
            _iconSourcePrefab = iconSourcePrefab;
        }
    }
}

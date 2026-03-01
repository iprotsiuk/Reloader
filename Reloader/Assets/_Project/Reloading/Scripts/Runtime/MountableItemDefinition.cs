using System.Collections.Generic;
using UnityEngine;

namespace Reloader.Reloading.Runtime
{
    [CreateAssetMenu(menuName = "Reloader/Reloading/Mountable Item Definition", fileName = "MountableItemDefinition")]
    public sealed class MountableItemDefinition : ScriptableObject
    {
        [SerializeField] private string _itemId;
        [SerializeField] private List<string> _tags = new List<string>();
        [SerializeField] private List<MountSlotDefinition> _childSlots = new List<MountSlotDefinition>();
        [SerializeField] private CompatibilityRuleSet _ruleSet = new CompatibilityRuleSet();

        public string ItemId => _itemId;

        public IReadOnlyList<string> Tags => _tags;

        public IReadOnlyList<MountSlotDefinition> ChildSlots => _childSlots;

        public CompatibilityRuleSet RuleSet => _ruleSet;

        public void SetValuesForTests(string itemId, IEnumerable<string> tags, IEnumerable<MountSlotDefinition> childSlots)
        {
            _itemId = itemId;
            _tags = ToList(tags);
            _childSlots = ToList(childSlots);
            _ruleSet = _ruleSet ?? new CompatibilityRuleSet();
        }

        private static List<T> ToList<T>(IEnumerable<T> source)
        {
            if (source == null)
            {
                return new List<T>();
            }

            var result = new List<T>();
            foreach (var value in source)
            {
                result.Add(value);
            }

            return result;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Reloader.Reloading.Runtime
{
    [CreateAssetMenu(menuName = "Reloader/Reloading/Workbench Definition", fileName = "WorkbenchDefinition")]
    public sealed class WorkbenchDefinition : ScriptableObject
    {
        [SerializeField] private string _workbenchId;
        [SerializeField] private List<MountSlotDefinition> _topLevelSlots = new List<MountSlotDefinition>();

        public string WorkbenchId => _workbenchId;

        public IReadOnlyList<MountSlotDefinition> TopLevelSlots => _topLevelSlots;

        public void SetValuesForTests(string workbenchId, IEnumerable<MountSlotDefinition> topLevelSlots)
        {
            _workbenchId = workbenchId;
            _topLevelSlots = ToList(topLevelSlots);
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

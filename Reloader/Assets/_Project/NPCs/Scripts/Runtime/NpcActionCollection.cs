using System.Collections;
using System.Collections.Generic;

namespace Reloader.NPCs.Runtime
{
    public sealed class NpcActionCollection : IReadOnlyList<NpcActionDefinition>
    {
        private readonly List<NpcActionDefinition> _actions;

        public NpcActionCollection(List<NpcActionDefinition> actions)
        {
            _actions = actions ?? new List<NpcActionDefinition>();
        }

        public int Count => _actions.Count;

        public NpcActionDefinition this[int index] => _actions[index];

        public IEnumerator<NpcActionDefinition> GetEnumerator()
        {
            return _actions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

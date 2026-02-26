using System.Collections.Generic;
using Reloader.NPCs.Runtime;
using Reloader.UI.Toolkit.Contracts;

namespace Reloader.UI.Toolkit.NpcActionPicker
{
    public sealed class NpcActionPickerUiState : UiRenderState
    {
        public NpcActionPickerUiState(IReadOnlyList<NpcActionDefinition> actions, int selectedIndex, string resultText)
            : base("npc-action-picker")
        {
            Actions = actions;
            SelectedIndex = selectedIndex;
            ResultText = resultText ?? string.Empty;
        }

        public IReadOnlyList<NpcActionDefinition> Actions { get; }
        public int SelectedIndex { get; }
        public string ResultText { get; }
    }
}

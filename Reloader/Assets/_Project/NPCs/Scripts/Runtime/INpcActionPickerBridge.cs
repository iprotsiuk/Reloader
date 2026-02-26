using System;
using System.Collections.Generic;

namespace Reloader.NPCs.Runtime
{
    public interface INpcActionPickerBridge
    {
        event Action<IReadOnlyList<NpcActionDefinition>, string> AvailableActionsChanged;
        event Action<NpcActionExecutionResult> ActionExecuted;
        event Action<string, string> ExecuteActionRequested;

        void RequestExecuteAction(string actionKey, string payload);
    }
}

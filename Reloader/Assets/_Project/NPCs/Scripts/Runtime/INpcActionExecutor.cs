namespace Reloader.NPCs.Runtime
{
    public interface INpcActionExecutor
    {
        bool CanExecuteAction(string actionKey);
        bool TryExecuteAction(in NpcActionExecutionContext context, out NpcActionExecutionResult result);
    }
}

namespace Reloader.Reloading.World
{
    public interface IReloadingBenchTarget
    {
        bool IsWorkbenchOpen { get; }
        void OpenWorkbench();
        void CloseWorkbench();
    }
}

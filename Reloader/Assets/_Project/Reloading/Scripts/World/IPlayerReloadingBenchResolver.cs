namespace Reloader.Reloading.World
{
    public interface IPlayerReloadingBenchResolver
    {
        bool TryResolveBenchTarget(out IReloadingBenchTarget target);
    }
}

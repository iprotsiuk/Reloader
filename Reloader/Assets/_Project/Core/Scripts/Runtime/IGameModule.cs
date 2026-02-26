namespace Reloader.Core.Runtime
{
    public interface IGameModule
    {
        string ModuleKey { get; }
        void Initialize(IRuntimeEvents runtimeEvents);
        void Start();
        void Stop();
    }
}

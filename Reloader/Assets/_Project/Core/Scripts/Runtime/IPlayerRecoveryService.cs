namespace Reloader.Core.Runtime
{
    public interface IPlayerRecoveryService
    {
        bool TryApplyArrestRecovery();
        bool TryApplyDeathRecovery();
    }
}

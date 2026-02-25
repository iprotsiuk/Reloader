namespace Reloader.Core.Save
{
    public interface ISaveDomainModule
    {
        string ModuleKey { get; }
        int ModuleVersion { get; }
        string CaptureModuleStateJson();
        void RestoreModuleStateFromJson(string payloadJson);
        void ValidateModuleState();
    }
}

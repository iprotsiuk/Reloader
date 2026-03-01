using System.Collections.Generic;

namespace Reloader.Core.Save
{
    public interface ISaveRuntimeBridge
    {
        void PrepareForSave(IReadOnlyList<SaveModuleRegistration> moduleRegistrations);
        void FinalizeAfterLoad(IReadOnlyList<SaveModuleRegistration> moduleRegistrations);
    }
}

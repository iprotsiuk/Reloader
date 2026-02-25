using System;

namespace Reloader.Core.Save
{
    public sealed class SaveModuleRegistration
    {
        public SaveModuleRegistration(int order, ISaveDomainModule module)
        {
            if (order < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(order), "Module order must be zero or greater.");
            }

            Module = module ?? throw new ArgumentNullException(nameof(module));
            Order = order;
        }

        public int Order { get; }
        public ISaveDomainModule Module { get; }
    }
}

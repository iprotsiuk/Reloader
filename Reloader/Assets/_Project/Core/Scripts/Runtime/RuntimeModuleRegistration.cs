using System;

namespace Reloader.Core.Runtime
{
    public sealed class RuntimeModuleRegistration
    {
        public RuntimeModuleRegistration(int order, IGameModule module)
        {
            if (order < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(order), "Module order must be zero or greater.");
            }

            Order = order;
            Module = module ?? throw new ArgumentNullException(nameof(module));
        }

        public int Order { get; }
        public IGameModule Module { get; }
    }
}

using System;
using System.Collections.Generic;

namespace Reloader.Core.Runtime
{
    public static class RuntimeKernelBootstrapper
    {
        private static RuntimeKernel kernel = new RuntimeKernel(Array.Empty<RuntimeModuleRegistration>());

        public static RuntimeKernel Kernel => kernel;

        public static IGameEventsRuntimeHub Events
        {
            get => kernel.Events;
            set => kernel.Events = value ?? throw new ArgumentNullException(nameof(value));
        }

        public static IInventoryEvents InventoryEvents => kernel.Events;
        public static IWeaponEvents WeaponEvents => kernel.Events;
        public static IShopEvents ShopEvents => kernel.Events;
        public static IUiStateEvents UiStateEvents => kernel.Events;

        public static RuntimeKernel Configure(IEnumerable<RuntimeModuleRegistration> registrations, IGameEventsRuntimeHub eventsImplementation = null)
        {
            kernel = new RuntimeKernel(registrations, eventsImplementation ?? kernel.Events);
            return kernel;
        }

        internal static void ResetForTests()
        {
            kernel = new RuntimeKernel(Array.Empty<RuntimeModuleRegistration>());
        }
    }
}

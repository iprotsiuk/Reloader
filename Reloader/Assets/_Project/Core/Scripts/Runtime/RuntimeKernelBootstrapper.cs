using System;
using System.Collections.Generic;

namespace Reloader.Core.Runtime
{
    public static class RuntimeKernelBootstrapper
    {
        private static RuntimeKernel kernel = new RuntimeKernel(Array.Empty<RuntimeModuleRegistration>());
        public static event Action EventsReconfigured;

        public static RuntimeKernel Kernel => kernel;

        public static IGameEventsRuntimeHub Events
        {
            get => kernel.Events;
            set
            {
                var previousEvents = kernel.Events;
                kernel.Events = value ?? throw new ArgumentNullException(nameof(value));
                if (!ReferenceEquals(previousEvents, kernel.Events))
                {
                    EventsReconfigured?.Invoke();
                }
            }
        }

        public static IInventoryEvents InventoryEvents => kernel.Events;
        public static IWeaponEvents WeaponEvents => kernel.Events;
        public static IShopEvents ShopEvents => kernel.Events;
        public static IUiStateEvents UiStateEvents => kernel.Events;

        public static RuntimeKernel Configure(IEnumerable<RuntimeModuleRegistration> registrations, IGameEventsRuntimeHub eventsImplementation = null)
        {
            var previousEvents = kernel.Events;
            kernel = new RuntimeKernel(registrations, eventsImplementation ?? previousEvents);
            if (!ReferenceEquals(previousEvents, kernel.Events))
            {
                EventsReconfigured?.Invoke();
            }

            return kernel;
        }

        internal static void ResetForTests()
        {
            kernel = new RuntimeKernel(Array.Empty<RuntimeModuleRegistration>());
        }
    }
}

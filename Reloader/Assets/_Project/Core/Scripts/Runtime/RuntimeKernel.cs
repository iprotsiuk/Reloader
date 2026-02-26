using System;
using System.Collections.Generic;

namespace Reloader.Core.Runtime
{
    public sealed class RuntimeKernel
    {
        private readonly List<RuntimeModuleRegistration> registrations;
        private bool isInitialized;
        private bool isStarted;
        private IGameEventsRuntimeHub runtimeEvents;

        public RuntimeKernel(IEnumerable<RuntimeModuleRegistration> moduleRegistrations, IGameEventsRuntimeHub eventsImplementation = null)
        {
            if (moduleRegistrations == null)
            {
                throw new ArgumentNullException(nameof(moduleRegistrations));
            }

            runtimeEvents = eventsImplementation ?? new DefaultRuntimeEvents();
            registrations = new List<RuntimeModuleRegistration>(moduleRegistrations);
            registrations.Sort((left, right) => left.Order.CompareTo(right.Order));

            EnsureUniqueModuleOrders(registrations);
            EnsureUniqueModuleKeys(registrations);
        }

        public IGameEventsRuntimeHub Events
        {
            get => runtimeEvents;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (isInitialized)
                {
                    throw new InvalidOperationException("Runtime events can only be replaced before Initialize is called.");
                }

                runtimeEvents = value;
            }
        }

        public void Initialize()
        {
            if (isInitialized)
            {
                throw new InvalidOperationException("Runtime kernel is already initialized.");
            }

            var initializedCount = 0;
            try
            {
                foreach (var registration in registrations)
                {
                    registration.Module.Initialize(runtimeEvents);
                    initializedCount++;
                }

                isInitialized = true;
            }
            catch
            {
                UnwindStops(initializedCount - 1);
                throw;
            }
        }

        public void Start()
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException("Runtime kernel must be initialized before start.");
            }

            if (isStarted)
            {
                throw new InvalidOperationException("Runtime kernel is already started.");
            }

            var startedCount = 0;
            try
            {
                foreach (var registration in registrations)
                {
                    registration.Module.Start();
                    startedCount++;
                }

                isStarted = true;
            }
            catch
            {
                UnwindStops(startedCount - 1);
                throw;
            }
        }

        public void Stop()
        {
            if (!isStarted)
            {
                throw new InvalidOperationException("Runtime kernel is not started.");
            }

            List<Exception> stopExceptions = null;
            for (var index = registrations.Count - 1; index >= 0; index--)
            {
                try
                {
                    registrations[index].Module.Stop();
                }
                catch (Exception ex)
                {
                    stopExceptions ??= new List<Exception>();
                    stopExceptions.Add(ex);
                }
            }

            isStarted = false;

            if (stopExceptions == null)
            {
                return;
            }

            if (stopExceptions.Count == 1)
            {
                throw stopExceptions[0];
            }

            throw new AggregateException("One or more runtime modules failed during stop.", stopExceptions);
        }

        private static void EnsureUniqueModuleKeys(List<RuntimeModuleRegistration> moduleRegistrations)
        {
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var registration in moduleRegistrations)
            {
                var key = registration.Module.ModuleKey;
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new InvalidOperationException("Runtime module key must be a non-empty string.");
                }

                if (!seenKeys.Add(key))
                {
                    throw new InvalidOperationException($"Duplicate runtime module key is not allowed: {key}");
                }
            }
        }

        private static void EnsureUniqueModuleOrders(List<RuntimeModuleRegistration> moduleRegistrations)
        {
            var seenOrders = new HashSet<int>();
            foreach (var registration in moduleRegistrations)
            {
                if (!seenOrders.Add(registration.Order))
                {
                    throw new InvalidOperationException($"Duplicate runtime module order is not allowed: {registration.Order}");
                }
            }
        }

        private void UnwindStops(int lastIndex)
        {
            for (var index = lastIndex; index >= 0; index--)
            {
                try
                {
                    registrations[index].Module.Stop();
                }
                catch
                {
                    // Best effort unwind: preserve original exception.
                }
            }
        }
    }
}

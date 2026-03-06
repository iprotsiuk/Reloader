using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Runtime;

namespace Reloader.Core.Tests.EditMode
{
    public class RuntimeKernelTests
    {
        [Test]
        public void InitializeStartStop_UsesDeterministicRegistrationOrder()
        {
            var lifecycle = new List<string>();
            var moduleA = new RecordingGameModule("module-a", lifecycle);
            var moduleB = new RecordingGameModule("module-b", lifecycle);
            var moduleC = new RecordingGameModule("module-c", lifecycle);

            var kernel = new RuntimeKernel(new[]
            {
                new RuntimeModuleRegistration(10, moduleC),
                new RuntimeModuleRegistration(0, moduleA),
                new RuntimeModuleRegistration(5, moduleB)
            });

            kernel.Initialize();
            kernel.Start();
            kernel.Stop();

            CollectionAssert.AreEqual(new[]
            {
                "Initialize:module-a",
                "Initialize:module-b",
                "Initialize:module-c",
                "Start:module-a",
                "Start:module-b",
                "Start:module-c",
                "Stop:module-c",
                "Stop:module-b",
                "Stop:module-a"
            }, lifecycle);
        }

        [Test]
        public void Constructor_DuplicateModuleKeys_Throws()
        {
            var duplicateA = new RecordingGameModule("duplicate", new List<string>());
            var duplicateB = new RecordingGameModule("duplicate", new List<string>());

            var exception = Assert.Throws<InvalidOperationException>(() => new RuntimeKernel(new[]
            {
                new RuntimeModuleRegistration(0, duplicateA),
                new RuntimeModuleRegistration(1, duplicateB)
            }));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("duplicate"));
        }

        [Test]
        public void Constructor_DuplicateRegistrationOrders_Throws()
        {
            var moduleA = new RecordingGameModule("module-a", new List<string>());
            var moduleB = new RecordingGameModule("module-b", new List<string>());

            var exception = Assert.Throws<InvalidOperationException>(() => new RuntimeKernel(new[]
            {
                new RuntimeModuleRegistration(3, moduleA),
                new RuntimeModuleRegistration(3, moduleB)
            }));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("Duplicate runtime module order"));
        }

        [Test]
        public void RuntimeEvents_CanBeReplaced()
        {
            var kernel = new RuntimeKernel(Array.Empty<RuntimeModuleRegistration>());
            var replacementEvents = new DefaultRuntimeEvents();

            kernel.Events = replacementEvents;

            Assert.That(kernel.Events, Is.SameAs(replacementEvents));
        }

        [Test]
        public void Start_BeforeInitialize_Throws()
        {
            var kernel = new RuntimeKernel(Array.Empty<RuntimeModuleRegistration>());

            Assert.Throws<InvalidOperationException>(() => kernel.Start());
        }

        [Test]
        public void Initialize_Twice_Throws()
        {
            var kernel = new RuntimeKernel(Array.Empty<RuntimeModuleRegistration>());
            kernel.Initialize();

            Assert.Throws<InvalidOperationException>(() => kernel.Initialize());
        }

        [Test]
        public void Start_Twice_Throws()
        {
            var kernel = new RuntimeKernel(Array.Empty<RuntimeModuleRegistration>());
            kernel.Initialize();
            kernel.Start();

            Assert.Throws<InvalidOperationException>(() => kernel.Start());
        }

        [Test]
        public void Stop_BeforeStart_Throws()
        {
            var kernel = new RuntimeKernel(Array.Empty<RuntimeModuleRegistration>());

            Assert.Throws<InvalidOperationException>(() => kernel.Stop());
        }

        [Test]
        public void ReplaceEvents_AfterInitialize_Throws()
        {
            var kernel = new RuntimeKernel(Array.Empty<RuntimeModuleRegistration>());
            kernel.Initialize();

            var ex = Assert.Throws<InvalidOperationException>(() => kernel.Events = new DefaultRuntimeEvents());
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex!.Message, Does.Contain("before Initialize"));
        }

        [Test]
        public void RuntimeKernel_Surface_RequiresGameEventsRuntimeHubType()
        {
            var constructor = typeof(RuntimeKernel).GetConstructors()
                .Single(info => info.GetParameters().Length == 2);
            var constructorEventsType = constructor.GetParameters()[1].ParameterType;
            var propertyEventsType = typeof(RuntimeKernel)
                .GetProperty(nameof(RuntimeKernel.Events), BindingFlags.Public | BindingFlags.Instance)!
                .PropertyType;

            Assert.That(constructorEventsType, Is.EqualTo(typeof(IGameEventsRuntimeHub)));
            Assert.That(propertyEventsType, Is.EqualTo(typeof(IGameEventsRuntimeHub)));
        }

        [Test]
        public void RuntimeKernelBootstrapper_Surface_RequiresGameEventsRuntimeHubType()
        {
            var configureMethod = typeof(RuntimeKernelBootstrapper).GetMethod(nameof(RuntimeKernelBootstrapper.Configure));
            Assert.That(configureMethod, Is.Not.Null);

            var configureParameters = configureMethod!.GetParameters();
            var propertyEventsType = typeof(RuntimeKernelBootstrapper)
                .GetProperty(nameof(RuntimeKernelBootstrapper.Events), BindingFlags.Public | BindingFlags.Static)!
                .PropertyType;

            Assert.That(configureParameters[1].ParameterType, Is.EqualTo(typeof(IGameEventsRuntimeHub)));
            Assert.That(propertyEventsType, Is.EqualTo(typeof(IGameEventsRuntimeHub)));
        }

        [Test]
        public void RuntimeKernelBootstrapper_Surface_ExposesTypedDomainChannels()
        {
            AssertTypedChannel("ContractEvents", typeof(IContractEvents));
            AssertTypedChannel("LawEnforcementEvents", typeof(ILawEnforcementEvents));
            AssertTypedChannel("InventoryEvents", typeof(IInventoryEvents));
            AssertTypedChannel("WeaponEvents", typeof(IWeaponEvents));
            AssertTypedChannel("ShopEvents", typeof(IShopEvents));
            AssertTypedChannel("UiStateEvents", typeof(IUiStateEvents));
            AssertTypedChannel("InteractionHintEvents", typeof(IInteractionHintEvents));
        }

        [Test]
        public void RuntimeKernelBootstrapper_TypedDomainChannels_ResolveCurrentHub()
        {
            var hub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), hub);

            Assert.That(ReadTypedChannel("ContractEvents"), Is.SameAs(hub));
            Assert.That(ReadTypedChannel("LawEnforcementEvents"), Is.SameAs(hub));
            Assert.That(ReadTypedChannel("InventoryEvents"), Is.SameAs(hub));
            Assert.That(ReadTypedChannel("WeaponEvents"), Is.SameAs(hub));
            Assert.That(ReadTypedChannel("ShopEvents"), Is.SameAs(hub));
            Assert.That(ReadTypedChannel("UiStateEvents"), Is.SameAs(hub));
            Assert.That(ReadTypedChannel("InteractionHintEvents"), Is.SameAs(hub));
        }

        [Test]
        public void RuntimeKernelBootstrapper_ConfigureWithoutExplicitHub_ReusesCurrentHubInstance()
        {
            var initialHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), initialHub);

            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>());

            Assert.That(RuntimeKernelBootstrapper.Events, Is.SameAs(initialHub));
            Assert.That(ReadTypedChannel("ContractEvents"), Is.SameAs(initialHub));
            Assert.That(ReadTypedChannel("LawEnforcementEvents"), Is.SameAs(initialHub));
            Assert.That(ReadTypedChannel("InventoryEvents"), Is.SameAs(initialHub));
            Assert.That(ReadTypedChannel("WeaponEvents"), Is.SameAs(initialHub));
            Assert.That(ReadTypedChannel("ShopEvents"), Is.SameAs(initialHub));
            Assert.That(ReadTypedChannel("UiStateEvents"), Is.SameAs(initialHub));
            Assert.That(ReadTypedChannel("InteractionHintEvents"), Is.SameAs(initialHub));
        }

        [Test]
        public void RuntimeKernelBootstrapper_ConfigureWithExplicitHub_ReplacesCurrentHubInstance()
        {
            var initialHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), initialHub);

            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), replacementHub);

            Assert.That(RuntimeKernelBootstrapper.Events, Is.SameAs(replacementHub));
            Assert.That(RuntimeKernelBootstrapper.Events, Is.Not.SameAs(initialHub));
        }

        [Test]
        public void IRuntimeEvents_Surface_ExposesDomainPortAccessors()
        {
            AssertRuntimePortAccessor("ContractEvents", typeof(IContractEvents));
            AssertRuntimePortAccessor("LawEnforcementEvents", typeof(ILawEnforcementEvents));
            AssertRuntimePortAccessor("InventoryEvents", typeof(IInventoryEvents));
            AssertRuntimePortAccessor("WeaponEvents", typeof(IWeaponEvents));
            AssertRuntimePortAccessor("ShopEvents", typeof(IShopEvents));
            AssertRuntimePortAccessor("UiStateEvents", typeof(IUiStateEvents));
            AssertRuntimePortAccessor("InteractionHintEvents", typeof(IInteractionHintEvents));
        }

        [Test]
        public void DefaultRuntimeEvents_IRuntimeEventsPorts_ResolveToCurrentHubInstance()
        {
            IRuntimeEvents runtimeEvents = new DefaultRuntimeEvents();

            Assert.That(ReadRuntimePort(runtimeEvents, "ContractEvents"), Is.SameAs(runtimeEvents));
            Assert.That(ReadRuntimePort(runtimeEvents, "LawEnforcementEvents"), Is.SameAs(runtimeEvents));
            Assert.That(ReadRuntimePort(runtimeEvents, "InventoryEvents"), Is.SameAs(runtimeEvents));
            Assert.That(ReadRuntimePort(runtimeEvents, "WeaponEvents"), Is.SameAs(runtimeEvents));
            Assert.That(ReadRuntimePort(runtimeEvents, "ShopEvents"), Is.SameAs(runtimeEvents));
            Assert.That(ReadRuntimePort(runtimeEvents, "UiStateEvents"), Is.SameAs(runtimeEvents));
            Assert.That(ReadRuntimePort(runtimeEvents, "InteractionHintEvents"), Is.SameAs(runtimeEvents));
        }

        [Test]
        public void Initialize_WhenModuleThrows_UnwindsPreviouslyInitializedModulesAndRethrows()
        {
            var lifecycle = new List<string>();
            var moduleA = new RecordingGameModule("module-a", lifecycle);
            var moduleB = new RecordingGameModule("module-b", lifecycle) { ThrowOnInitialize = true };
            var moduleC = new RecordingGameModule("module-c", lifecycle);

            var kernel = new RuntimeKernel(new[]
            {
                new RuntimeModuleRegistration(0, moduleA),
                new RuntimeModuleRegistration(1, moduleB),
                new RuntimeModuleRegistration(2, moduleC)
            });

            Assert.Throws<InvalidOperationException>(() => kernel.Initialize());

            CollectionAssert.AreEqual(new[]
            {
                "Initialize:module-a",
                "Initialize:module-b",
                "Stop:module-a"
            }, lifecycle);
        }

        [Test]
        public void Start_WhenModuleThrows_UnwindsPreviouslyStartedModulesAndRethrows()
        {
            var lifecycle = new List<string>();
            var moduleA = new RecordingGameModule("module-a", lifecycle);
            var moduleB = new RecordingGameModule("module-b", lifecycle) { ThrowOnStart = true };
            var moduleC = new RecordingGameModule("module-c", lifecycle);

            var kernel = new RuntimeKernel(new[]
            {
                new RuntimeModuleRegistration(0, moduleA),
                new RuntimeModuleRegistration(1, moduleB),
                new RuntimeModuleRegistration(2, moduleC)
            });

            kernel.Initialize();
            Assert.Throws<InvalidOperationException>(() => kernel.Start());

            CollectionAssert.AreEqual(new[]
            {
                "Initialize:module-a",
                "Initialize:module-b",
                "Initialize:module-c",
                "Start:module-a",
                "Start:module-b",
                "Stop:module-a"
            }, lifecycle);
        }

        [Test]
        public void Stop_WhenModuleThrows_ContinuesStoppingAndKernelIsNotLeftStarted()
        {
            var lifecycle = new List<string>();
            var moduleA = new RecordingGameModule("module-a", lifecycle);
            var moduleB = new RecordingGameModule("module-b", lifecycle) { ThrowOnStop = true };
            var moduleC = new RecordingGameModule("module-c", lifecycle);

            var kernel = new RuntimeKernel(new[]
            {
                new RuntimeModuleRegistration(0, moduleA),
                new RuntimeModuleRegistration(1, moduleB),
                new RuntimeModuleRegistration(2, moduleC)
            });

            kernel.Initialize();
            kernel.Start();

            Assert.Throws<InvalidOperationException>(() => kernel.Stop());

            CollectionAssert.AreEqual(new[]
            {
                "Initialize:module-a",
                "Initialize:module-b",
                "Initialize:module-c",
                "Start:module-a",
                "Start:module-b",
                "Start:module-c",
                "Stop:module-c",
                "Stop:module-b",
                "Stop:module-a"
            }, lifecycle);

            Assert.Throws<InvalidOperationException>(() => kernel.Stop());
        }

        private sealed class RecordingGameModule : IGameModule
        {
            private readonly List<string> lifecycle;

            public RecordingGameModule(string moduleKey, List<string> lifecycle)
            {
                ModuleKey = moduleKey;
                this.lifecycle = lifecycle;
            }

            public string ModuleKey { get; }
            public bool ThrowOnInitialize { get; set; }
            public bool ThrowOnStart { get; set; }
            public bool ThrowOnStop { get; set; }

            public void Initialize(IRuntimeEvents runtimeEvents)
            {
                lifecycle.Add($"Initialize:{ModuleKey}");
                if (ThrowOnInitialize)
                {
                    throw new InvalidOperationException($"Initialize failed for {ModuleKey}");
                }
            }

            public void Start()
            {
                lifecycle.Add($"Start:{ModuleKey}");
                if (ThrowOnStart)
                {
                    throw new InvalidOperationException($"Start failed for {ModuleKey}");
                }
            }

            public void Stop()
            {
                lifecycle.Add($"Stop:{ModuleKey}");
                if (ThrowOnStop)
                {
                    throw new InvalidOperationException($"Stop failed for {ModuleKey}");
                }
            }
        }

        private static void AssertTypedChannel(string propertyName, Type expectedType)
        {
            var property = typeof(RuntimeKernelBootstrapper).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            Assert.That(property, Is.Not.Null);
            Assert.That(property!.PropertyType, Is.EqualTo(expectedType));
            Assert.That(property.PropertyType.IsAssignableFrom(typeof(IGameEventsRuntimeHub)), Is.True);
        }

        private static object ReadTypedChannel(string propertyName)
        {
            var property = typeof(RuntimeKernelBootstrapper).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            Assert.That(property, Is.Not.Null);
            return property!.GetValue(null);
        }

        private static void AssertRuntimePortAccessor(string propertyName, Type expectedType)
        {
            var property = typeof(IRuntimeEvents).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null);
            Assert.That(property!.PropertyType, Is.EqualTo(expectedType));
        }

        private static object ReadRuntimePort(IRuntimeEvents runtimeEvents, string propertyName)
        {
            var property = typeof(IRuntimeEvents).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null);
            return property!.GetValue(runtimeEvents);
        }
    }
}

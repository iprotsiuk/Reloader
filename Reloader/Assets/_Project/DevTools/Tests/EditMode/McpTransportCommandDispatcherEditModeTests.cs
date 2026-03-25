using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Reloader.DevTools.Tests.EditMode
{
    public sealed class McpTransportCommandDispatcherEditModeTests
    {
        private Type _commandRegistryType = null!;
        private Type _dispatcherType = null!;
        private FieldInfo _initializedField = null!;
        private FieldInfo _handlersField = null!;
        private FieldInfo _pendingField = null!;
        private MethodInfo _initializeMethod = null!;
        private MethodInfo _executeCommandJsonAsyncMethod = null!;
        private bool _registryWasInitialized;

        [SetUp]
        public void SetUp()
        {
            _commandRegistryType = Type.GetType("MCPForUnity.Editor.Tools.CommandRegistry, MCPForUnity.Editor")
                ?? throw new InvalidOperationException("Could not load CommandRegistry.");
            _dispatcherType = Type.GetType("MCPForUnity.Editor.Services.Transport.TransportCommandDispatcher, MCPForUnity.Editor")
                ?? throw new InvalidOperationException("Could not load TransportCommandDispatcher.");

            _initializedField = _commandRegistryType.GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Could not find CommandRegistry._initialized.");
            _handlersField = _commandRegistryType.GetField("_handlers", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Could not find CommandRegistry._handlers.");
            _initializeMethod = _commandRegistryType.GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public)
                ?? throw new InvalidOperationException("Could not find CommandRegistry.Initialize.");
            _executeCommandJsonAsyncMethod = _dispatcherType.GetMethod("ExecuteCommandJsonAsync", BindingFlags.Static | BindingFlags.Public)
                ?? throw new InvalidOperationException("Could not find TransportCommandDispatcher.ExecuteCommandJsonAsync.");
            _pendingField = _dispatcherType.GetField("Pending", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Could not find TransportCommandDispatcher.Pending.");

            _registryWasInitialized = (bool)_initializedField.GetValue(null);
            ClearPending();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                RestoreRegistryState();
            }
            finally
            {
                ClearPending();
            }
        }

        [Test]
        public void ExecuteCommandJsonAsync_InitializesCommandRegistryOnDemand()
        {
            ResetRegistryState();

            using var cts = new CancellationTokenSource();

            var task = (Task<string>)_executeCommandJsonAsyncMethod.Invoke(
                null,
                new object[] { "ping", cts.Token });

            Assert.That((bool)_initializedField.GetValue(null), Is.True, "The dispatcher should initialize the registry when a command is submitted.");

            cts.Cancel();
            try
            {
                task.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Expected when the cancellation token is triggered before the queued command runs.
            }
        }

        private void ResetRegistryState()
        {
            ((IDictionary)_handlersField.GetValue(null)).Clear();
            _initializedField.SetValue(null, false);
        }

        private void RestoreRegistryState()
        {
            ResetRegistryState();

            if (_registryWasInitialized)
            {
                _initializeMethod.Invoke(null, null);
            }
        }

        private void ClearPending()
        {
            ((IDictionary)_pendingField.GetValue(null)).Clear();
        }
    }
}

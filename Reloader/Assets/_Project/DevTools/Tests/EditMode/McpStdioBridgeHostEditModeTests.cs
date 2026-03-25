using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Reloader.DevTools.Tests.EditMode
{
    public sealed class McpStdioBridgeHostEditModeTests
    {
        private Type _bridgeHostType = null!;
        private Type _queuedCommandType = null!;
        private FieldInfo _commandQueueField = null!;
        private FieldInfo _isRunningField = null!;
        private MethodInfo _processCommandsMethod = null!;
        private MethodInfo _stopMethod = null!;

        [SetUp]
        public void SetUp()
        {
            _bridgeHostType = Type.GetType("MCPForUnity.Editor.Services.Transport.Transports.StdioBridgeHost, MCPForUnity.Editor")
                ?? throw new InvalidOperationException("Could not load StdioBridgeHost.");
            _queuedCommandType = Type.GetType("MCPForUnity.Editor.Services.Transport.Transports.QueuedCommand, MCPForUnity.Editor")
                ?? throw new InvalidOperationException("Could not load QueuedCommand.");
            _commandQueueField = _bridgeHostType.GetField("commandQueue", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Could not find commandQueue field.");
            _isRunningField = _bridgeHostType.GetField("isRunning", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Could not find isRunning field.");
            _processCommandsMethod = _bridgeHostType.GetMethod("ProcessCommands", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Could not find ProcessCommands method.");
            _stopMethod = _bridgeHostType.GetMethod("Stop", BindingFlags.Static | BindingFlags.Public)
                ?? throw new InvalidOperationException("Could not find Stop method.");

            ClearQueue();
            _isRunningField.SetValue(null, false);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _stopMethod.Invoke(null, null);
            }
            catch
            {
                // Best-effort cleanup only; tests should not fail on bridge shutdown noise.
            }

            ClearQueue();
            _isRunningField.SetValue(null, false);
        }

        [Test]
        public void ProcessCommands_WhenNoClientsRemain_DrainsExecutingQueuedCommands()
        {
            AddQueuedCommand(isExecuting: true);
            _isRunningField.SetValue(null, true);

            _processCommandsMethod.Invoke(null, null);

            Assert.That(GetQueue().Count, Is.EqualTo(0), "Orphaned queued commands should be removed when no clients are connected.");
        }

        [Test]
        public void Stop_ClearsQueuedCommands()
        {
            AddQueuedCommand(isExecuting: true);
            _isRunningField.SetValue(null, true);

            _stopMethod.Invoke(null, null);

            Assert.That(GetQueue().Count, Is.EqualTo(0), "Stopping the bridge should clear any queued commands.");
        }

        private void AddQueuedCommand(bool isExecuting)
        {
            var queuedCommand = Activator.CreateInstance(_queuedCommandType)
                ?? throw new InvalidOperationException("Could not create QueuedCommand.");

            SetField(queuedCommand, "CommandJson", "{\"jsonrpc\":\"2.0\",\"method\":\"ping\"}");
            SetField(queuedCommand, "Tcs", new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously));
            SetField(queuedCommand, "IsExecuting", isExecuting);
            SetField(queuedCommand, "EnqueuedAtMs", 0L);
            SetField(queuedCommand, "ExecutionCts", new System.Threading.CancellationTokenSource());
            SetField(queuedCommand, "ResponseAbandoned", false);

            GetQueue().Add(Guid.NewGuid().ToString("N"), queuedCommand);
        }

        private IDictionary GetQueue()
        {
            return (IDictionary)(_commandQueueField.GetValue(null)
                ?? throw new InvalidOperationException("commandQueue was null."));
        }

        private void ClearQueue()
        {
            GetQueue().Clear();
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Could not find field '{fieldName}'.");
            field.SetValue(instance, value);
        }
    }
}

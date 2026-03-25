using System;
using System.Reflection;
using NUnit.Framework;

namespace Reloader.DevTools.Tests.EditMode
{
    public sealed class McpWebSocketTransportClientShutdownTests
    {
        private static readonly Type TransportType =
            Type.GetType("MCPForUnity.Editor.Services.Transport.Transports.WebSocketTransportClient, MCPForUnity.Editor")
            ?? throw new InvalidOperationException("Could not load WebSocketTransportClient.");

        private static readonly MethodInfo ShutdownClassifier =
            TransportType.GetMethod("IsExpectedShutdownException", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Could not load shutdown classifier.");

        [Test]
        public void ShutdownClassifier_TreatsCancellationAsExpected()
        {
            Assert.That(InvokeClassifier(new OperationCanceledException()), Is.True);
        }

        [Test]
        public void ShutdownClassifier_TreatsDisposedObjectsAsExpected()
        {
            Assert.That(InvokeClassifier(new ObjectDisposedException("socket")), Is.True);
        }

        [Test]
        public void ShutdownClassifier_TreatsKnownWebSocketStateErrorsAsExpected()
        {
            Assert.That(InvokeClassifier(new InvalidOperationException("WebSocket is not initialised")), Is.True);
            Assert.That(InvokeClassifier(new InvalidOperationException("WebSocket is not open")), Is.True);
        }

        [Test]
        public void ShutdownClassifier_LeavesUnexpectedErrorsAlone()
        {
            Assert.That(InvokeClassifier(new InvalidOperationException("boom")), Is.False);
            Assert.That(InvokeClassifier(new Exception("boom")), Is.False);
        }

        private static bool InvokeClassifier(Exception exception)
        {
            return (bool)ShutdownClassifier.Invoke(null, new object[] { exception });
        }
    }
}

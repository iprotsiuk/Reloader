using System;
using System.Reflection;
using NUnit.Framework;
using Reloader.DevTools.Runtime;
using UnityEngine;

namespace Reloader.DevTools.Tests.PlayMode
{
    public sealed class DevToolsRuntimePlayModeTests
    {
        [Test]
        public void Dispose_ReleasesTraceRuntimeDriverObject()
        {
            DestroyTraceRuntimeRoots();

            var runtime = new DevToolsRuntime();
            var disposeMethod = typeof(DevToolsRuntime).GetMethod("Dispose", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            try
            {
                Assert.That(disposeMethod, Is.Not.Null);
                Assert.That(CountTraceRuntimeRoots(), Is.Zero);

                var executed = runtime.TryExecute("trace -1", out var resultMessage);
                Assert.That(executed, Is.True);
                Assert.That(resultMessage, Does.Contain("Trace TTL set to permanent."));
                Assert.That(CountTraceRuntimeRoots(), Is.EqualTo(1));

                disposeMethod!.Invoke(runtime, null);

                Assert.That(CountTraceRuntimeRoots(), Is.Zero);
            }
            finally
            {
                DestroyTraceRuntimeRoots();
            }
        }

        [Test]
        public void TryExecute_TraceNegativeOne_EnablesPermanentTraces()
        {
            var runtime = new DevToolsRuntime();

            try
            {
                var executed = runtime.TryExecute("trace -1", out var resultMessage);

                Assert.That(executed, Is.True);
                Assert.That(runtime.State.TraceTtlSeconds, Is.EqualTo(-1f));
                Assert.That(resultMessage, Is.EqualTo("Trace TTL set to permanent."));
            }
            finally
            {
                runtime.Dispose();
            }
        }

        [Test]
        public void TryExecute_TraceOne_SetsFiniteTraceTtl()
        {
            var runtime = new DevToolsRuntime();

            try
            {
                var executed = runtime.TryExecute("trace 1", out var resultMessage);

                Assert.That(executed, Is.True);
                Assert.That(runtime.State.TraceTtlSeconds, Is.EqualTo(1f));
                Assert.That(resultMessage, Is.EqualTo("Trace TTL set to 1 second."));
            }
            finally
            {
                runtime.Dispose();
            }
        }

        [Test]
        public void TryExecute_TraceZero_DisablesAndClearsTraces()
        {
            var runtime = new DevToolsRuntime();

            try
            {
                Assert.That(runtime.TryExecute("trace 1", out _), Is.True);

                var executed = runtime.TryExecute("trace 0", out var resultMessage);

                Assert.That(executed, Is.True);
                Assert.That(runtime.State.TraceTtlSeconds, Is.Zero);
                Assert.That(resultMessage, Is.EqualTo("Trace TTL disabled and visible traces cleared."));
            }
            finally
            {
                runtime.Dispose();
            }
        }

        private static int CountTraceRuntimeRoots()
        {
            var count = 0;
            var objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null && objects[i].name == "DevTraceRuntime")
                {
                    count++;
                }
            }

            return count;
        }

        private static void DestroyTraceRuntimeRoots()
        {
            var objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null && objects[i].name == "DevTraceRuntime")
                {
                    UnityEngine.Object.DestroyImmediate(objects[i]);
                }
            }
        }
    }
}

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

                var executed = runtime.TryExecute("traces persistent on", out var resultMessage);
                Assert.That(executed, Is.True);
                Assert.That(resultMessage, Does.Contain("Persistent traces enabled."));
                Assert.That(CountTraceRuntimeRoots(), Is.EqualTo(1));

                disposeMethod!.Invoke(runtime, null);

                Assert.That(CountTraceRuntimeRoots(), Is.Zero);
            }
            finally
            {
                DestroyTraceRuntimeRoots();
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

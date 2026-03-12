using System.Collections;
using NUnit.Framework;
using Reloader.Core.Runtime;
using Reloader.DevTools.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.DevTools.Tests.PlayMode
{
    public sealed class DevTraceRuntimePlayModeTests
    {
        [UnityTest]
        public IEnumerator PersistentTracesEnabled_WeaponFireCreatesVisibleTraceSegment()
        {
            var state = new DevToolsState { PersistentTracesEnabled = true };
            var runtimeEvents = new DefaultRuntimeEvents();
            using var traceRuntime = new DevTraceRuntime(state, runtimeEvents);

            runtimeEvents.RaiseWeaponFired("rifle-01", Vector3.zero, Vector3.forward);
            runtimeEvents.RaiseProjectileHit("rifle-01", new Vector3(0f, 0f, 12f), 10f);

            yield return null;

            var segments = Object.FindObjectsByType<DevTraceSegmentView>(FindObjectsSortMode.None);
            Assert.That(segments, Has.Length.EqualTo(1));
            Assert.That(segments[0].IsVisible, Is.True);
            Assert.That(segments[0].StartPoint, Is.EqualTo(Vector3.zero).Using(Vector3EqualityComparer.Instance));
            Assert.That(segments[0].EndPoint, Is.EqualTo(new Vector3(0f, 0f, 12f)).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator PersistentTracesDisabled_DoesNotCreateTraceSegments()
        {
            var state = new DevToolsState { PersistentTracesEnabled = false };
            var runtimeEvents = new DefaultRuntimeEvents();
            using var traceRuntime = new DevTraceRuntime(state, runtimeEvents);

            runtimeEvents.RaiseWeaponFired("rifle-01", Vector3.zero, Vector3.forward);
            runtimeEvents.RaiseProjectileHit("rifle-01", new Vector3(0f, 0f, 12f), 10f);

            yield return null;

            var segments = Object.FindObjectsByType<DevTraceSegmentView>(FindObjectsSortMode.None);
            Assert.That(segments, Is.Empty);
        }

        private sealed class Vector3EqualityComparer : IEqualityComparer
        {
            public static readonly Vector3EqualityComparer Instance = new();

            public new bool Equals(object x, object y)
            {
                if (x is not Vector3 left || y is not Vector3 right)
                {
                    return false;
                }

                return Vector3.Distance(left, right) <= 0.01f;
            }

            public int GetHashCode(object obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }
    }
}

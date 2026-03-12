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
        public IEnumerator PositiveTraceTtl_WeaponFireCreatesVisibleTraceSegment()
        {
            var state = new DevToolsState { TraceTtlSeconds = 1f };
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
        public IEnumerator PositiveTraceTtl_ProjectileObserverRendersExactPolylinePath()
        {
            var state = new DevToolsState { TraceTtlSeconds = 1f };
            var runtimeEvents = new DefaultRuntimeEvents();
            using var traceRuntime = new DevTraceRuntime(state, runtimeEvents);
            var observer = traceRuntime.CreateProjectilePathObserver();

            observer.RecordSegment(new Vector3(0f, 3f, 0f), new Vector3(0f, 2.95f, 5f));
            observer.RecordSegment(new Vector3(0f, 2.95f, 5f), new Vector3(0f, 2.8f, 10f));
            observer.RecordSegment(new Vector3(0f, 2.8f, 10f), new Vector3(0f, 2.55f, 14f));
            observer.Complete(new Vector3(0f, 2.55f, 14f), didHit: true);

            yield return null;

            var segments = Object.FindObjectsByType<DevTraceSegmentView>(FindObjectsSortMode.None);
            Assert.That(segments, Has.Length.EqualTo(1));
            Assert.That(segments[0].IsVisible, Is.True);
            Assert.That(segments[0].PointCount, Is.EqualTo(4));
            Assert.That(segments[0].GetPoint(0), Is.EqualTo(new Vector3(0f, 3f, 0f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(segments[0].GetPoint(1), Is.EqualTo(new Vector3(0f, 2.95f, 5f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(segments[0].GetPoint(2), Is.EqualTo(new Vector3(0f, 2.8f, 10f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(segments[0].GetPoint(3), Is.EqualTo(new Vector3(0f, 2.55f, 14f)).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator PositiveTraceTtl_FallbackShotsStillRenderAfterObserverBackedShot()
        {
            var state = new DevToolsState { TraceTtlSeconds = 1f };
            var runtimeEvents = new DefaultRuntimeEvents();
            using var traceRuntime = new DevTraceRuntime(state, runtimeEvents);

            var observer = traceRuntime.CreateProjectilePathObserver();
            observer.RecordSegment(Vector3.zero, new Vector3(0f, 0f, 6f));
            observer.Complete(new Vector3(0f, 0f, 6f), didHit: true);
            runtimeEvents.RaiseWeaponFired("rifle-01", new Vector3(1f, 0f, 0f), Vector3.forward);
            runtimeEvents.RaiseProjectileHit("rifle-01", new Vector3(1f, 0f, 12f), 10f);

            yield return null;

            var segments = Object.FindObjectsByType<DevTraceSegmentView>(FindObjectsSortMode.None);
            Assert.That(segments, Has.Length.EqualTo(2));
            Assert.That(HasVisiblePath(segments, Vector3.zero, new Vector3(0f, 0f, 6f)), Is.True);
            Assert.That(HasVisibleSegment(segments, new Vector3(1f, 0f, 0f), new Vector3(1f, 0f, 12f)), Is.True);
        }

        [UnityTest]
        public IEnumerator ZeroTraceTtl_DoesNotCreateTraceSegments()
        {
            var state = new DevToolsState { TraceTtlSeconds = 0f };
            var runtimeEvents = new DefaultRuntimeEvents();
            using var traceRuntime = new DevTraceRuntime(state, runtimeEvents);

            runtimeEvents.RaiseWeaponFired("rifle-01", Vector3.zero, Vector3.forward);
            runtimeEvents.RaiseProjectileHit("rifle-01", new Vector3(0f, 0f, 12f), 10f);

            yield return null;

            var segments = Object.FindObjectsByType<DevTraceSegmentView>(FindObjectsSortMode.None);
            Assert.That(segments, Is.Empty);
        }

        [UnityTest]
        public IEnumerator PositiveTraceTtl_DelayedProjectileHit_UpdatesFallbackSegment()
        {
            var state = new DevToolsState { TraceTtlSeconds = 1f };
            var runtimeEvents = new DefaultRuntimeEvents();
            using var traceRuntime = new DevTraceRuntime(state, runtimeEvents);

            runtimeEvents.RaiseWeaponFired("rifle-01", Vector3.zero, Vector3.forward);

            yield return new WaitForSecondsRealtime(0.1f);

            var segments = Object.FindObjectsByType<DevTraceSegmentView>(FindObjectsSortMode.None);
            Assert.That(segments, Has.Length.EqualTo(1));
            Assert.That(segments[0].IsVisible, Is.True);
            Assert.That(segments[0].EndPoint.z, Is.EqualTo(120f).Within(0.01f));

            runtimeEvents.RaiseProjectileHit("rifle-01", new Vector3(0f, 0f, 12f), 10f);

            yield return null;

            segments = Object.FindObjectsByType<DevTraceSegmentView>(FindObjectsSortMode.None);
            Assert.That(segments, Has.Length.EqualTo(1));
            Assert.That(segments[0].IsVisible, Is.True);
            Assert.That(segments[0].StartPoint, Is.EqualTo(Vector3.zero).Using(Vector3EqualityComparer.Instance));
            Assert.That(segments[0].EndPoint, Is.EqualTo(new Vector3(0f, 0f, 12f)).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator PositiveTraceTtl_MultiplePendingShotsFromSameWeapon_PreserveSeparateOrigins()
        {
            var state = new DevToolsState { TraceTtlSeconds = 1f };
            var runtimeEvents = new DefaultRuntimeEvents();
            using var traceRuntime = new DevTraceRuntime(state, runtimeEvents);

            runtimeEvents.RaiseWeaponFired("rifle-01", Vector3.zero, Vector3.forward);
            runtimeEvents.RaiseWeaponFired("rifle-01", new Vector3(1f, 0f, 0f), Vector3.forward);
            runtimeEvents.RaiseProjectileHit("rifle-01", new Vector3(0f, 0f, 10f), 10f);
            runtimeEvents.RaiseProjectileHit("rifle-01", new Vector3(1f, 0f, 11f), 10f);

            yield return null;

            var segments = Object.FindObjectsByType<DevTraceSegmentView>(FindObjectsSortMode.None);
            Assert.That(segments, Has.Length.EqualTo(2));
            Assert.That(HasVisibleSegment(segments, Vector3.zero, new Vector3(0f, 0f, 10f)), Is.True);
            Assert.That(HasVisibleSegment(segments, new Vector3(1f, 0f, 0f), new Vector3(1f, 0f, 11f)), Is.True);
        }

        [UnityTest]
        public IEnumerator PositiveTraceTtl_ExpiresVisibleSegmentsAfterConfiguredLifetime()
        {
            var state = new DevToolsState { TraceTtlSeconds = 0.1f };
            var runtimeEvents = new DefaultRuntimeEvents();
            using var traceRuntime = new DevTraceRuntime(state, runtimeEvents);

            runtimeEvents.RaiseWeaponFired("rifle-01", Vector3.zero, Vector3.forward);
            runtimeEvents.RaiseProjectileHit("rifle-01", new Vector3(0f, 0f, 12f), 10f);

            yield return null;

            var segments = Object.FindObjectsByType<DevTraceSegmentView>(FindObjectsSortMode.None);
            Assert.That(segments, Has.Length.EqualTo(1));
            Assert.That(segments[0].IsVisible, Is.True);

            yield return new WaitForSecondsRealtime(0.15f);

            segments = Object.FindObjectsByType<DevTraceSegmentView>(FindObjectsSortMode.None);
            Assert.That(segments, Has.Length.EqualTo(1));
            Assert.That(segments[0].IsVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator ZeroTraceTtl_ClearsVisibleSegmentsImmediately()
        {
            var state = new DevToolsState { TraceTtlSeconds = 1f };
            var runtimeEvents = new DefaultRuntimeEvents();
            using var traceRuntime = new DevTraceRuntime(state, runtimeEvents);

            runtimeEvents.RaiseWeaponFired("rifle-01", Vector3.zero, Vector3.forward);
            runtimeEvents.RaiseProjectileHit("rifle-01", new Vector3(0f, 0f, 12f), 10f);

            yield return null;

            var segments = Object.FindObjectsByType<DevTraceSegmentView>(FindObjectsSortMode.None);
            Assert.That(segments, Has.Length.EqualTo(1));
            Assert.That(segments[0].IsVisible, Is.True);

            traceRuntime.SetTraceTtlSeconds(0f);
            yield return null;

            segments = Object.FindObjectsByType<DevTraceSegmentView>(FindObjectsSortMode.None);
            Assert.That(segments, Has.Length.EqualTo(1));
            Assert.That(segments[0].IsVisible, Is.False);
        }

        private static bool HasVisibleSegment(DevTraceSegmentView[] segments, Vector3 startPoint, Vector3 endPoint)
        {
            for (var i = 0; i < segments.Length; i++)
            {
                if (!segments[i].IsVisible)
                {
                    continue;
                }

                if (Vector3EqualityComparer.Instance.Equals(segments[i].StartPoint, startPoint)
                    && Vector3EqualityComparer.Instance.Equals(segments[i].EndPoint, endPoint))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasVisiblePath(DevTraceSegmentView[] segments, Vector3 firstPoint, Vector3 lastPoint)
        {
            for (var i = 0; i < segments.Length; i++)
            {
                if (!segments[i].IsVisible || segments[i].PointCount < 2)
                {
                    continue;
                }

                if (Vector3EqualityComparer.Instance.Equals(segments[i].GetPoint(0), firstPoint)
                    && Vector3EqualityComparer.Instance.Equals(segments[i].GetPoint(segments[i].PointCount - 1), lastPoint))
                {
                    return true;
                }
            }

            return false;
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

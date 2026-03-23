using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Reloader.World.Tests.EditMode
{
    public sealed class StableWorldCoordinateBridgeEditModeTests
    {
        private GameObject _owner;

        [TearDown]
        public void TearDown()
        {
            if (_owner != null)
            {
                Object.DestroyImmediate(_owner);
                _owner = null;
            }
        }

        [Test]
        public void Bridge_LocalAndStablePositionConversions_RoundTripAcrossRebases()
        {
            var state = AddOriginComponent("DynamicOriginRebaseState");
            var bridge = AddOriginComponent("StableWorldCoordinateBridge");
            Invoke(bridge, "Initialize", state);

            Assert.That(InvokeVector3(bridge, "LocalToStable", new Vector3(12f, 3f, -4f)), Is.EqualTo(new Vector3(12f, 3f, -4f)));
            Assert.That(InvokeVector3(bridge, "StableToLocal", new Vector3(-7f, 5f, 11f)), Is.EqualTo(new Vector3(-7f, 5f, 11f)));

            Invoke(state, "ApplyRebase", new Vector3(-500f, 0f, 225f), new Vector3(500f, 0f, -225f), 12f);

            var local = new Vector3(25f, 9f, -40f);
            var stable = InvokeVector3(bridge, "LocalToStable", local);

            Assert.That(stable, Is.EqualTo(new Vector3(525f, 9f, -265f)));
            Assert.That(InvokeVector3(bridge, "StableToLocal", stable), Is.EqualTo(local));
        }

        [Test]
        public void Bridge_LocalDirectionToStable_IsOffsetInvariantAndPreservesMagnitude()
        {
            var state = AddOriginComponent("DynamicOriginRebaseState");
            var bridge = AddOriginComponent("StableWorldCoordinateBridge");
            Invoke(bridge, "Initialize", state);

            var direction = new Vector3(7f, -2f, 3f);
            Assert.That(InvokeVector3(bridge, "LocalDirectionToStable", direction), Is.EqualTo(direction));

            Invoke(state, "ApplyRebase", new Vector3(-300f, 0f, 125f), new Vector3(300f, 0f, -125f), 3f);

            var converted = InvokeVector3(bridge, "LocalDirectionToStable", direction);
            Assert.That(converted, Is.EqualTo(direction));
            Assert.That(converted.magnitude, Is.EqualTo(direction.magnitude).Within(0.0001f));
        }

        [Test]
        public void Bridge_ComputeHorizontalDistanceFromLocalOrigin_IgnoresVerticalOffset()
        {
            var state = AddOriginComponent("DynamicOriginRebaseState");
            var bridge = AddOriginComponent("StableWorldCoordinateBridge");
            Invoke(bridge, "Initialize", state);

            var distance = InvokeFloat(bridge, "ComputeHorizontalDistanceFromLocalOrigin", new Vector3(300f, 999f, 400f));

            Assert.That(distance, Is.EqualTo(500f).Within(0.0001f));
        }

        private Component AddOriginComponent(string typeName)
        {
            if (_owner == null)
            {
                _owner = new GameObject("OriginBridgeTests");
            }

            return _owner.AddComponent(GetOriginType(typeName));
        }

        private static System.Type GetOriginType(string typeName)
        {
            var resolvedType = System.Type.GetType($"Reloader.World.Runtime.Origin.{typeName}, Reloader.World");
            Assert.That(resolvedType, Is.Not.Null, $"Expected origin runtime type '{typeName}' in assembly 'Reloader.World'.");
            return resolvedType;
        }

        private static void Invoke(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected {target.GetType().Name}.{methodName} to exist.");
            method.Invoke(target, args);
        }

        private static Vector3 InvokeVector3(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected {target.GetType().Name}.{methodName} to exist.");
            return (Vector3)method.Invoke(target, args);
        }

        private static float InvokeFloat(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected {target.GetType().Name}.{methodName} to exist.");
            return (float)method.Invoke(target, args);
        }
    }
}

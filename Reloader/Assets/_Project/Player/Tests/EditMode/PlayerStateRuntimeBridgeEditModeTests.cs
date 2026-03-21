using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Reloader.Player.Tests.EditMode
{
    public sealed class PlayerStateRuntimeBridgeEditModeTests
    {
        [Test]
        public void CaptureToModule_CopiesTransformSceneAnchorSelectedSlotAndRecoveryMetadata()
        {
            var bridgeType = ResolveBridgeType();
            var moduleType = ResolveModuleType();
            var root = new GameObject("PlayerRoot");
            var bridge = root.AddComponent(bridgeType);
            var module = Activator.CreateInstance(moduleType);
            var inventoryRuntime = new InventoryRuntimeProbe { SelectedBeltIndex = 4 };

            root.transform.SetPositionAndRotation(
                new Vector3(7.5f, 1.1f, -2.25f),
                Quaternion.Euler(0f, 45f, 0f));

            Invoke(bridge, "SetPlayerStateModuleForRuntime", module);
            Invoke(bridge, "SetInventoryRuntimeForRuntime", inventoryRuntime);
            Invoke(bridge, "SetCurrentAnchorState", "Assets/_Project/World/Scenes/IndoorRangeInstance.unity", "entry.indoor.arrival");
            Invoke(bridge, "SetRecoveryState", "arrest", "Assets/_Project/World/Scenes/MainTown.unity", "entry.maintown.respawn.police");
            Invoke(bridge, "CaptureToModule");

            Assert.That(GetProperty<string>(module, "CurrentScenePath"), Is.EqualTo("Assets/_Project/World/Scenes/IndoorRangeInstance.unity"));
            Assert.That(GetProperty<string>(module, "CurrentAnchorId"), Is.EqualTo("entry.indoor.arrival"));
            Assert.That(GetProperty<float>(module, "PositionX"), Is.EqualTo(7.5f));
            Assert.That(GetProperty<float>(module, "PositionY"), Is.EqualTo(1.1f));
            Assert.That(GetProperty<float>(module, "PositionZ"), Is.EqualTo(-2.25f));
            Assert.That(GetProperty<int>(module, "SelectedBeltSlotIndex"), Is.EqualTo(4));
            Assert.That(GetProperty<string>(module, "RecoveryReasonId"), Is.EqualTo("arrest"));
            Assert.That(GetProperty<string>(module, "RecoveryScenePath"), Is.EqualTo("Assets/_Project/World/Scenes/MainTown.unity"));
            Assert.That(GetProperty<string>(module, "RecoveryAnchorId"), Is.EqualTo("entry.maintown.respawn.police"));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void RestoreFromModule_RehydratesTransformSelectedSlotAndCanonicalMetadata()
        {
            var bridgeType = ResolveBridgeType();
            var moduleType = ResolveModuleType();
            var root = new GameObject("PlayerRoot");
            var bridge = root.AddComponent(bridgeType);
            var module = Activator.CreateInstance(moduleType);
            var inventoryRuntime = new InventoryRuntimeProbe { SelectedBeltIndex = -1 };

            SetProperty(module, "CurrentScenePath", "Assets/_Project/World/Scenes/MainTown.unity");
            SetProperty(module, "CurrentAnchorId", "entry.maintown.return");
            SetProperty(module, "PositionX", -1.5f);
            SetProperty(module, "PositionY", 0.75f);
            SetProperty(module, "PositionZ", 16.25f);
            SetProperty(module, "RotationX", 0f);
            SetProperty(module, "RotationY", 0.3826834f);
            SetProperty(module, "RotationZ", 0f);
            SetProperty(module, "RotationW", 0.9238795f);
            SetProperty(module, "SelectedBeltSlotIndex", 2);
            SetProperty(module, "RecoveryReasonId", "death");
            SetProperty(module, "RecoveryScenePath", "Assets/_Project/World/Scenes/MainTown.unity");
            SetProperty(module, "RecoveryAnchorId", "entry.maintown.respawn.hospital");

            Invoke(bridge, "SetPlayerStateModuleForRuntime", module);
            Invoke(bridge, "SetInventoryRuntimeForRuntime", inventoryRuntime);
            Invoke(bridge, "RestoreFromModule");

            Assert.That(root.transform.position, Is.EqualTo(new Vector3(-1.5f, 0.75f, 16.25f)));

            var restoredRotation = root.transform.rotation;
            Assert.That(restoredRotation.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(restoredRotation.y, Is.EqualTo(0.3826834f).Within(0.0001f));
            Assert.That(restoredRotation.z, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(restoredRotation.w, Is.EqualTo(0.9238795f).Within(0.0001f));
            Assert.That(inventoryRuntime.SelectedBeltIndex, Is.EqualTo(2));
            Assert.That(GetProperty<string>(bridge, "CurrentScenePath"), Is.EqualTo("Assets/_Project/World/Scenes/MainTown.unity"));
            Assert.That(GetProperty<string>(bridge, "CurrentAnchorId"), Is.EqualTo("entry.maintown.return"));
            Assert.That(GetProperty<string>(bridge, "RecoveryReasonId"), Is.EqualTo("death"));
            Assert.That(GetProperty<string>(bridge, "RecoveryScenePath"), Is.EqualTo("Assets/_Project/World/Scenes/MainTown.unity"));
            Assert.That(GetProperty<string>(bridge, "RecoveryAnchorId"), Is.EqualTo("entry.maintown.respawn.hospital"));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void CaptureToModule_WhenCurrentAndResolvedAnchorsAreEmpty_ThrowsInsteadOfPersistingInvalidAnchor()
        {
            var bridgeType = ResolveBridgeType();
            var moduleType = ResolveModuleType();
            var root = new GameObject("PlayerRoot");
            var bridge = root.AddComponent(bridgeType);
            var module = Activator.CreateInstance(moduleType);

            Invoke(bridge, "SetPlayerStateModuleForRuntime", module);
            Invoke(bridge, "SetCurrentAnchorState", string.Empty, string.Empty);

            var ex = Assert.Throws<TargetInvocationException>(() => Invoke(bridge, "CaptureToModule"));
            Assert.That(ex!.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(ex.InnerException!.Message, Does.Contain("CurrentAnchorId"));
            Assert.That(GetProperty<string>(module, "CurrentAnchorId"), Is.EqualTo(string.Empty));

            Object.DestroyImmediate(root);
        }

        private static Type ResolveBridgeType()
        {
            var type = Type.GetType("Reloader.Player.PlayerStateRuntimeBridge, Reloader.Player");
            Assert.That(type, Is.Not.Null, "PlayerStateRuntimeBridge type should exist.");
            return type;
        }

        private static Type ResolveModuleType()
        {
            var type = Type.GetType("Reloader.Core.Save.Modules.PlayerStateModule, Reloader.Core");
            Assert.That(type, Is.Not.Null, "PlayerStateModule type should exist.");
            return type;
        }

        private static void Invoke(Component component, string methodName, params object[] args)
        {
            var method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, $"Expected public method '{methodName}'.");
            method!.Invoke(component, args);
        }

        private static T GetProperty<T>(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}'.");
            return (T)property!.GetValue(instance);
        }

        private static void SetProperty(object instance, string propertyName, object value)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}'.");
            property!.SetValue(instance, value);
        }

        private sealed class InventoryRuntimeProbe
        {
            public int SelectedBeltIndex { get; set; }

            public void SelectBeltSlot(int beltSlotIndex)
            {
                SelectedBeltIndex = beltSlotIndex;
            }
        }
    }
}

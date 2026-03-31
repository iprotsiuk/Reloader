using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Reloader.Player.Tests.EditMode
{
    public sealed class PlayerStateRuntimeBridgeEditModeTests
    {
        private const string IndoorRangeScenePath = "Assets/_Project/World/Scenes/IndoorRangeInstance.unity";

        [Test]
        public void CaptureToModule_CopiesTransformSceneAnchorSelectedSlotAndRecoveryMetadata()
        {
            var bridgeType = ResolveBridgeType();
            var moduleType = ResolveModuleType();
            var sharedReceiverType = ResolveSharedReceiverType();
            var root = new GameObject("PlayerRoot");
            var bridge = root.AddComponent(bridgeType);
            var sharedReceiver = root.AddComponent(sharedReceiverType);
            var module = Activator.CreateInstance(moduleType);
            var inventoryRuntime = new InventoryRuntimeProbe { SelectedBeltIndex = 4 };

            root.transform.SetPositionAndRotation(
                new Vector3(7.5f, 1.1f, -2.25f),
                Quaternion.Euler(0f, 45f, 0f));

            SetSharedReceiverHealthState(sharedReceiver, currentHealth: 7f, maxHealth: 10f);
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
            Assert.That(GetProperty<float>(module, "CurrentHealth"), Is.EqualTo(7f));
            Assert.That(GetProperty<float>(module, "MaxHealth"), Is.EqualTo(10f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void RestoreFromModule_RehydratesTransformSelectedSlotAndCanonicalMetadata()
        {
            var bridgeType = ResolveBridgeType();
            var moduleType = ResolveModuleType();
            var sharedReceiverType = ResolveSharedReceiverType();
            var root = new GameObject("PlayerRoot");
            var bridge = root.AddComponent(bridgeType);
            var sharedReceiver = root.AddComponent(sharedReceiverType);
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
            SetProperty(module, "CurrentHealth", 4f);
            SetProperty(module, "MaxHealth", 10f);

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
            Assert.That(ReadSharedReceiverHealth(sharedReceiver, "CurrentHealth"), Is.EqualTo(4f));
            Assert.That(ReadSharedReceiverHealth(sharedReceiver, "MaxHealth"), Is.EqualTo(10f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void RestoreFromModule_WhenSelectedBeltSlotIsMinusOne_ClearsExistingSelection()
        {
            var bridgeType = ResolveBridgeType();
            var moduleType = ResolveModuleType();
            var root = new GameObject("PlayerRoot");
            var bridge = root.AddComponent(bridgeType);
            var module = Activator.CreateInstance(moduleType);
            var inventoryRuntime = new InventoryRuntimeProbe { SelectedBeltIndex = 3 };

            SetProperty(module, "CurrentScenePath", "Assets/_Project/World/Scenes/MainTown.unity");
            SetProperty(module, "CurrentAnchorId", "entry.maintown.return");
            SetProperty(module, "RotationW", 1f);
            SetProperty(module, "SelectedBeltSlotIndex", -1);

            Invoke(bridge, "SetPlayerStateModuleForRuntime", module);
            Invoke(bridge, "SetInventoryRuntimeForRuntime", inventoryRuntime);
            Invoke(bridge, "RestoreFromModule");

            Assert.That(inventoryRuntime.SelectedBeltIndex, Is.EqualTo(-1));
            Assert.That(inventoryRuntime.ClearSelectedBeltSlotCallCount, Is.EqualTo(1));
            Assert.That(inventoryRuntime.SelectBeltSlotCallCount, Is.EqualTo(0));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryApplyArrestRecovery_ClearsInventoryAndStartsPoliceRespawnTravel()
        {
            var bridgeType = ResolveBridgeType();
            var root = new GameObject("PlayerRoot");
            var bridge = root.AddComponent(bridgeType);
            var inventoryRuntime = new InventoryRuntimeProbe();
            var travelCoordinator = new RecoveryTravelCoordinatorProbe();

            Invoke(bridge, "SetPlayerRootTransformForRuntime", root.transform);
            Invoke(bridge, "SetInventoryRuntimeForRuntime", inventoryRuntime);
            Invoke(bridge, "SetRecoveryTravelCoordinatorForRuntime", travelCoordinator);
            Invoke(bridge, "SetCurrentAnchorState", IndoorRangeScenePath, "entry.indoor.arrival");

            var applied = Invoke<bool>(bridge, "TryApplyArrestRecovery");

            Assert.That(applied, Is.True);
            Assert.That(inventoryRuntime.ClearCarriedItemsCallCount, Is.EqualTo(1));
            Assert.That(GetProperty<string>(bridge, "CurrentScenePath"), Is.EqualTo("Assets/_Project/World/Scenes/MainTown.unity"));
            Assert.That(GetProperty<string>(bridge, "CurrentAnchorId"), Is.EqualTo("entry.maintown.respawn.police"));
            Assert.That(GetProperty<string>(bridge, "RecoveryReasonId"), Is.EqualTo("arrest"));
            Assert.That(GetProperty<string>(bridge, "RecoveryScenePath"), Is.EqualTo("Assets/_Project/World/Scenes/MainTown.unity"));
            Assert.That(GetProperty<string>(bridge, "RecoveryAnchorId"), Is.EqualTo("entry.maintown.respawn.police"));
            Assert.That(travelCoordinator.TryTravelToSceneEntryCallCount, Is.EqualTo(1));
            Assert.That(travelCoordinator.LastSceneName, Is.EqualTo("MainTown"));
            Assert.That(travelCoordinator.LastEntryPointId, Is.EqualTo("entry.maintown.respawn.police"));
            Assert.That(travelCoordinator.TryMoveRuntimePlayerToLoadedEntryPointCallCount, Is.EqualTo(0));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryApplyDeathRecovery_WhileAlreadyInMainTown_UsesLoadedSceneRespawnMove()
        {
            var bridgeType = ResolveBridgeType();
            var sharedReceiverType = ResolveSharedReceiverType();
            var root = new GameObject("PlayerRoot");
            var bridge = root.AddComponent(bridgeType);
            var sharedReceiver = root.AddComponent(sharedReceiverType);
            var inventoryRuntime = new InventoryRuntimeProbe();
            var travelCoordinator = new RecoveryTravelCoordinatorProbe();

            SetSharedReceiverHealthState(sharedReceiver, currentHealth: 2f, maxHealth: 10f);
            Invoke(bridge, "SetPlayerRootTransformForRuntime", root.transform);
            Invoke(bridge, "SetInventoryRuntimeForRuntime", inventoryRuntime);
            Invoke(bridge, "SetRecoveryTravelCoordinatorForRuntime", travelCoordinator);
            Invoke(bridge, "SetCurrentAnchorState", "Assets/_Project/World/Scenes/MainTown.unity", "entry.maintown.return");

            var applied = Invoke<bool>(bridge, "TryApplyDeathRecovery");

            Assert.That(applied, Is.True);
            Assert.That(inventoryRuntime.ClearCarriedItemsCallCount, Is.EqualTo(1));
            Assert.That(GetProperty<string>(bridge, "CurrentScenePath"), Is.EqualTo("Assets/_Project/World/Scenes/MainTown.unity"));
            Assert.That(GetProperty<string>(bridge, "CurrentAnchorId"), Is.EqualTo("entry.maintown.respawn.hospital"));
            Assert.That(GetProperty<string>(bridge, "RecoveryReasonId"), Is.EqualTo("death"));
            Assert.That(GetProperty<string>(bridge, "RecoveryScenePath"), Is.EqualTo("Assets/_Project/World/Scenes/MainTown.unity"));
            Assert.That(GetProperty<string>(bridge, "RecoveryAnchorId"), Is.EqualTo("entry.maintown.respawn.hospital"));
            Assert.That(travelCoordinator.TryMoveRuntimePlayerToLoadedEntryPointCallCount, Is.EqualTo(1));
            Assert.That(travelCoordinator.LastScenePath, Is.EqualTo("Assets/_Project/World/Scenes/MainTown.unity"));
            Assert.That(travelCoordinator.LastEntryPointId, Is.EqualTo("entry.maintown.respawn.hospital"));
            Assert.That(travelCoordinator.TryTravelToSceneEntryCallCount, Is.EqualTo(0));
            Assert.That(ReadSharedReceiverHealth(sharedReceiver, "CurrentHealth"), Is.EqualTo(10f),
                "Expected successful death recovery to restore the player's shared humanoid health budget.");
            Assert.That(ReadSharedReceiverHealth(sharedReceiver, "MaxHealth"), Is.EqualTo(10f));
            Assert.That(ReadSharedReceiverIsDead(sharedReceiver), Is.False);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryApplyArrestRecovery_WhenTravelFails_DoesNotMutateInventoryRecoveryMetadataOrAnchorState()
        {
            var bridgeType = ResolveBridgeType();
            var sharedReceiverType = ResolveSharedReceiverType();
            var root = new GameObject("PlayerRoot");
            var bridge = root.AddComponent(bridgeType);
            var sharedReceiver = root.AddComponent(sharedReceiverType);
            var inventoryRuntime = new InventoryRuntimeProbe { SelectedBeltIndex = 5 };
            var travelCoordinator = new RecoveryTravelCoordinatorProbe
            {
                TryTravelToSceneEntryResult = false
            };

            SetSharedReceiverHealthState(sharedReceiver, currentHealth: 3f, maxHealth: 10f);
            Invoke(bridge, "SetPlayerRootTransformForRuntime", root.transform);
            Invoke(bridge, "SetInventoryRuntimeForRuntime", inventoryRuntime);
            Invoke(bridge, "SetRecoveryTravelCoordinatorForRuntime", travelCoordinator);
            Invoke(bridge, "SetCurrentAnchorState", IndoorRangeScenePath, "entry.indoor.arrival");
            Invoke(bridge, "SetRecoveryState", "existing", "Assets/_Project/World/Scenes/Other.unity", "entry.other.spawn");

            var applied = Invoke<bool>(bridge, "TryApplyArrestRecovery");

            Assert.That(applied, Is.False);
            Assert.That(inventoryRuntime.ClearCarriedItemsCallCount, Is.EqualTo(0));
            Assert.That(inventoryRuntime.SelectedBeltIndex, Is.EqualTo(5));
            Assert.That(GetProperty<string>(bridge, "CurrentScenePath"), Is.EqualTo(IndoorRangeScenePath));
            Assert.That(GetProperty<string>(bridge, "CurrentAnchorId"), Is.EqualTo("entry.indoor.arrival"));
            Assert.That(GetProperty<string>(bridge, "RecoveryReasonId"), Is.EqualTo("existing"));
            Assert.That(GetProperty<string>(bridge, "RecoveryScenePath"), Is.EqualTo("Assets/_Project/World/Scenes/Other.unity"));
            Assert.That(GetProperty<string>(bridge, "RecoveryAnchorId"), Is.EqualTo("entry.other.spawn"));
            Assert.That(travelCoordinator.TryTravelToSceneEntryCallCount, Is.EqualTo(1));
            Assert.That(travelCoordinator.TryMoveRuntimePlayerToLoadedEntryPointCallCount, Is.EqualTo(0));
            Assert.That(ReadSharedReceiverHealth(sharedReceiver, "CurrentHealth"), Is.EqualTo(3f));
            Assert.That(ReadSharedReceiverHealth(sharedReceiver, "MaxHealth"), Is.EqualTo(10f));
            Assert.That(ReadSharedReceiverIsDead(sharedReceiver), Is.False);

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

        [Test]
        public void CaptureToModule_PrefersLiveTravelSceneAndAnchorOverRestoredSaveState()
        {
            var bridgeType = ResolveBridgeType();
            var moduleType = ResolveModuleType();
            var originalScene = SceneManager.GetActiveScene();
            var travelScene = EditorSceneManager.OpenScene(IndoorRangeScenePath, OpenSceneMode.Additive);
            var root = new GameObject("RuntimePlayerRoot");
            SceneManager.MoveGameObjectToScene(root, travelScene);
            var bridge = root.AddComponent(bridgeType);
            var module = Activator.CreateInstance(moduleType);

            try
            {
                SetProperty(module, "CurrentScenePath", "Assets/_Project/World/Scenes/MainTown.unity");
                SetProperty(module, "CurrentAnchorId", "entry.maintown.spawn");
                SetProperty(module, "RotationW", 1f);
                Invoke(bridge, "SetPlayerStateModuleForRuntime", module);
                Invoke(bridge, "SetPlayerRootTransformForRuntime", root.transform);
                Invoke(bridge, "RestoreFromModule");

                SetWorldTravelCoordinatorLastResolvedEntryPointId("entry.indoor.arrival");

                Invoke(bridge, "CaptureToModule");

                Assert.That(GetProperty<string>(module, "CurrentScenePath"), Is.EqualTo(IndoorRangeScenePath));
                Assert.That(GetProperty<string>(module, "CurrentAnchorId"), Is.EqualTo("entry.indoor.arrival"));
            }
            finally
            {
                SetWorldTravelCoordinatorLastResolvedEntryPointId(string.Empty);
                Object.DestroyImmediate(root);
                EditorSceneManager.CloseScene(travelScene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
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

        private static Type ResolveSharedReceiverType()
        {
            var type = Type.GetType("Reloader.NPCs.Combat.HumanoidDamageReceiver, Reloader.NPCs");
            Assert.That(type, Is.Not.Null, "HumanoidDamageReceiver type should exist for player health bridging.");
            return type;
        }

        private static void Invoke(Component component, string methodName, params object[] args)
        {
            var method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, $"Expected public method '{methodName}'.");
            method!.Invoke(component, args);
        }

        private static T Invoke<T>(Component component, string methodName, params object[] args)
        {
            var method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, $"Expected public method '{methodName}'.");
            return (T)method!.Invoke(component, args);
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

        private static void SetWorldTravelCoordinatorLastResolvedEntryPointId(string entryPointId)
        {
            var coordinatorType = Type.GetType("Reloader.World.Travel.WorldTravelCoordinator, Reloader.World");
            Assert.That(coordinatorType, Is.Not.Null, "Expected WorldTravelCoordinator type.");

            var field = coordinatorType!.GetField("<LastResolvedEntryPointId>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Expected WorldTravelCoordinator.LastResolvedEntryPointId backing field.");
            field!.SetValue(null, string.IsNullOrWhiteSpace(entryPointId) ? null : entryPointId);
        }

        private static void SetSharedReceiverHealthState(Component sharedReceiver, float currentHealth, float maxHealth)
        {
            var method = sharedReceiver.GetType().GetMethod("SetHealthStateForRuntime", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, "Expected HumanoidDamageReceiver.SetHealthStateForRuntime(float, float).");
            method!.Invoke(sharedReceiver, new object[] { currentHealth, maxHealth });
        }

        private static float ReadSharedReceiverHealth(Component sharedReceiver, string propertyName)
        {
            var property = sharedReceiver.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected HumanoidDamageReceiver.{propertyName}.");
            return (float)property!.GetValue(sharedReceiver);
        }

        private static bool ReadSharedReceiverIsDead(Component sharedReceiver)
        {
            var property = sharedReceiver.GetType().GetProperty("IsDead", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, "Expected HumanoidDamageReceiver.IsDead.");
            return (bool)property!.GetValue(sharedReceiver);
        }

        private sealed class InventoryRuntimeProbe
        {
            public int SelectedBeltIndex { get; set; }
            public int SelectBeltSlotCallCount { get; private set; }
            public int ClearSelectedBeltSlotCallCount { get; private set; }
            public int ClearCarriedItemsCallCount { get; private set; }

            public void SelectBeltSlot(int beltSlotIndex)
            {
                SelectBeltSlotCallCount++;
                SelectedBeltIndex = beltSlotIndex;
            }

            public void ClearSelectedBeltSlot()
            {
                ClearSelectedBeltSlotCallCount++;
                SelectedBeltIndex = -1;
            }

            public void ClearCarriedItems()
            {
                ClearCarriedItemsCallCount++;
                SelectedBeltIndex = -1;
            }
        }

        private sealed class RecoveryTravelCoordinatorProbe : IPlayerRecoveryTravelCoordinator
        {
            public bool TryTravelToSceneEntryResult { get; set; } = true;
            public bool TryMoveRuntimePlayerToLoadedEntryPointResult { get; set; } = true;
            public int TryTravelToSceneEntryCallCount { get; private set; }
            public int TryMoveRuntimePlayerToLoadedEntryPointCallCount { get; private set; }
            public string LastSceneName { get; private set; } = string.Empty;
            public string LastScenePath { get; private set; } = string.Empty;
            public string LastEntryPointId { get; private set; } = string.Empty;

            public bool TryTravelToSceneEntry(string sceneName, string entryPointId)
            {
                TryTravelToSceneEntryCallCount++;
                LastSceneName = sceneName;
                LastEntryPointId = entryPointId;
                return TryTravelToSceneEntryResult;
            }

            public bool TryMoveRuntimePlayerToLoadedEntryPoint(string scenePath, string entryPointId)
            {
                TryMoveRuntimePlayerToLoadedEntryPointCallCount++;
                LastScenePath = scenePath;
                LastEntryPointId = entryPointId;
                return TryMoveRuntimePlayerToLoadedEntryPointResult;
            }
        }
    }
}

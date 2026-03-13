using System;
using System.Reflection;
using NUnit.Framework;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.UI.Toolkit.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class CompassHudRuntimeBridgePlayModeTests
    {
        [Test]
        public void RuntimeBridge_BindCompassHud_RendersCardinalsFromViewerHeading()
        {
            var bridgeGo = new GameObject("CompassHudBridge");
            var bridge = bridgeGo.AddComponent<UiToolkitScreenRuntimeBridge>();
            var inventoryController = bridgeGo.AddComponent<PlayerInventoryController>();
            inventoryController.Configure(null, null, new PlayerInventoryRuntime());
            inventoryController.transform.forward = Vector3.forward;

            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindCompassHud",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var root = BuildRoot();
            var subscription = bindMethod.Invoke(
                bridge,
                new object[] { root, UiRuntimeCompositionIds.ControllerObjectNames.CompassHud, inventoryController }) as IDisposable;
            Assert.That(subscription, Is.Not.Null);

            try
            {
                var controller = bridgeGo.transform.Find(UiRuntimeCompositionIds.ControllerObjectNames.CompassHud)?.GetComponent(Type.GetType("Reloader.UI.Toolkit.CompassHud.CompassHudController, Reloader.UI"));
                Assert.That(controller, Is.Not.Null);
                controller!.GetType().GetMethod("Refresh", BindingFlags.Instance | BindingFlags.Public)?.Invoke(controller, null);

                var entries = root.Q<VisualElement>("compass-hud__entries");
                Assert.That(entries, Is.Not.Null);
                Assert.That(entries.childCount, Is.GreaterThanOrEqualTo(3));

                var north = FindLabel(entries, "N");
                Assert.That(north, Is.Not.Null);
                Assert.That(north!.style.left.value.value, Is.EqualTo(180f).Within(0.01f));
            }
            finally
            {
                subscription.Dispose();
                UnityEngine.Object.DestroyImmediate(bridgeGo);
            }
        }

        [Test]
        public void RuntimeBridge_BindCompassHud_WhenActiveContractTargetResolves_ShowsMarkerForHorizontalBearing()
        {
            var providerType = Type.GetType("Reloader.Contracts.Runtime.StaticContractRuntimeProvider, Reloader.Core");
            var definitionType = Type.GetType("Reloader.Contracts.Runtime.AssassinationContractDefinition, Reloader.Contracts");
            var bridgeType = Type.GetType("Reloader.NPCs.Runtime.CivilianPopulationRuntimeBridge, Reloader.NPCs");
            var spawnedCivilianType = Type.GetType("Reloader.NPCs.Runtime.MainTownPopulationSpawnedCivilian, Reloader.NPCs");
            Assert.That(providerType, Is.Not.Null);
            Assert.That(definitionType, Is.Not.Null);
            Assert.That(bridgeType, Is.Not.Null);
            Assert.That(spawnedCivilianType, Is.Not.Null);

            var bridgeGo = new GameObject("CompassHudBridgeMarker");
            var bridge = bridgeGo.AddComponent<UiToolkitScreenRuntimeBridge>();
            var inventoryController = bridgeGo.AddComponent<PlayerInventoryController>();
            inventoryController.Configure(null, null, new PlayerInventoryRuntime());
            inventoryController.transform.position = Vector3.zero;
            inventoryController.transform.forward = Vector3.forward;

            var providerGo = new GameObject("StaticContractRuntimeProviderCompass");
            var provider = providerGo.AddComponent(providerType);
            var definition = ScriptableObject.CreateInstance(definitionType);
            SetPrivateField(definitionType, definition, "_contractId", "contract.compass");
            SetPrivateField(definitionType, definition, "_targetId", "target.compass");
            SetPrivateField(definitionType, definition, "_title", "Compass Contract");
            SetPrivateField(definitionType, definition, "_targetDisplayName", "Target Compass");
            var setAvailableContract = providerType.GetMethod("SetAvailableContract", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(setAvailableContract, Is.Not.Null);
            setAvailableContract.Invoke(provider, new object[] { definition });
            providerType.GetMethod("AcceptAvailableContract", BindingFlags.Instance | BindingFlags.Public)?.Invoke(provider, null);

            var populationGo = new GameObject("CivilianPopulationRuntimeBridgeCompass");
            var populationBridge = populationGo.AddComponent(bridgeType);
            var spawnedGo = new GameObject("SpawnedContractTarget");
            spawnedGo.transform.SetParent(populationGo.transform, false);
            spawnedGo.transform.position = new Vector3(10f, 5f, 0f);
            var spawnedCivilian = spawnedGo.AddComponent(spawnedCivilianType);
            SetPrivateField(spawnedCivilianType, spawnedCivilian, "_civilianId", "target.compass");

            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindCompassHud",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var root = BuildRoot();
            var subscription = bindMethod.Invoke(
                bridge,
                new object[] { root, UiRuntimeCompositionIds.ControllerObjectNames.CompassHud, inventoryController }) as IDisposable;
            Assert.That(subscription, Is.Not.Null);

            try
            {
                var controller = bridgeGo.transform.Find(UiRuntimeCompositionIds.ControllerObjectNames.CompassHud)?.GetComponent(Type.GetType("Reloader.UI.Toolkit.CompassHud.CompassHudController, Reloader.UI"));
                Assert.That(controller, Is.Not.Null);
                controller!.GetType().GetMethod("Refresh", BindingFlags.Instance | BindingFlags.Public)?.Invoke(controller, null);

                var entries = root.Q<VisualElement>("compass-hud__entries");
                Assert.That(entries, Is.Not.Null);
                var marker = FindEntry(entries, "contract-target");
                Assert.That(marker, Is.Not.Null);
                Assert.That(marker.style.left.value.value, Is.GreaterThan(180f));
            }
            finally
            {
                subscription.Dispose();
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(providerGo);
                UnityEngine.Object.DestroyImmediate(populationGo);
                UnityEngine.Object.DestroyImmediate(bridgeGo);
            }
        }

        private static VisualElement BuildRoot()
        {
            var screen = new VisualElement { name = "compass-hud__screen" };
            var root = new VisualElement { name = "compass-hud__root" };
            var frame = new VisualElement { name = "compass-hud__frame" };
            var lane = new VisualElement { name = "compass-hud__lane" };
            var entries = new VisualElement { name = "compass-hud__entries" };
            lane.Add(entries);
            frame.Add(lane);
            root.Add(frame);
            screen.Add(root);
            return screen;
        }

        private static Label FindLabel(VisualElement root, string text)
        {
            var labels = root.Query<Label>().ToList();
            for (var i = 0; i < labels.Count; i++)
            {
                if (string.Equals(labels[i].text, text, StringComparison.Ordinal))
                {
                    return labels[i];
                }
            }

            return null;
        }

        private static VisualElement FindEntry(VisualElement root, string entryKey)
        {
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root[i];
                if (string.Equals(child.viewDataKey, entryKey, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static void SetPrivateField(Type type, object target, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {type.Name}.");
            field!.SetValue(target, value);
        }
    }
}

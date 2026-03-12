using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using Reloader.Core.Items;
using Reloader.DevTools.Runtime;
using Reloader.Inventory;
using Reloader.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.DevTools.Tests.PlayMode
{
    public sealed class DevGiveItemCommandPlayModeTests
    {
        [Test]
        public void GetSuggestions_GiveItem_PreservesCommandPrefixInApplyText()
        {
            var root = new GameObject("DevGiveItemSuggestionsRoot");
            var inputSource = root.AddComponent<StubInputSource>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            inventoryController.Configure(inputSource, null, new PlayerInventoryRuntime());

            var ammo = ScriptableObject.CreateInstance<ItemDefinition>();
            ammo.SetValuesForTests(
                "ammo-308",
                ItemCategory.Consumable,
                "308 Winchester FMJ",
                ItemStackPolicy.StackByDefinition,
                maxStack: 999);
            SetPrivateField(
                typeof(PlayerInventoryController),
                inventoryController,
                "_itemDefinitionRegistry",
                new List<ItemDefinition> { ammo });

            var runtime = new DevToolsRuntime();
            runtime.Context.InventoryController = inventoryController;

            var suggestions = runtime.GetSuggestions("give item 308", 0).ToArray();

            Assert.That(suggestions, Is.Not.Empty);
            Assert.That(suggestions[0].Token, Is.EqualTo("ammo-308"));
            Assert.That(suggestions[0].ApplyText, Is.EqualTo("give item ammo-308"));

            Object.DestroyImmediate(ammo);
            Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator GiveItemCommand_StoresResolvedItemInInventory()
        {
            var root = new GameObject("DevGiveItemInventoryRoot");
            var inputSource = root.AddComponent<StubInputSource>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var inventoryRuntime = new PlayerInventoryRuntime();
            inventoryController.Configure(inputSource, null, inventoryRuntime);

            var ammo = ScriptableObject.CreateInstance<ItemDefinition>();
            ammo.SetValuesForTests(
                "ammo-308",
                ItemCategory.Consumable,
                "308 Winchester FMJ",
                ItemStackPolicy.StackByDefinition,
                maxStack: 999);
            SetPrivateField(
                typeof(PlayerInventoryController),
                inventoryController,
                "_itemDefinitionRegistry",
                new List<ItemDefinition> { ammo });

            var runtime = new DevToolsRuntime();
            runtime.Context.InventoryController = inventoryController;

            yield return null;

            var executed = runtime.TryExecute("give item 308 Winchester FMJ 50", out var resultMessage);

            Assert.That(executed, Is.True);
            Assert.That(resultMessage, Does.Contain("ammo-308"));
            Assert.That(inventoryRuntime.GetItemQuantity("ammo-308"), Is.EqualTo(50));

            Object.DestroyImmediate(ammo);
            Object.DestroyImmediate(root);
        }

        private static void SetPrivateField(System.Type ownerType, object instance, string fieldName, object value)
        {
            var field = ownerType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {ownerType.Name}.");
            field.SetValue(instance, value);
        }

        private sealed class StubInputSource : MonoBehaviour, IPlayerInputSource
        {
            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public bool ConsumePickupPressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;
            public bool ConsumeDevConsoleTogglePressed() => false;
            public bool ConsumeAutocompletePressed() => false;
            public int ConsumeSuggestionDelta() => 0;
        }
    }
}

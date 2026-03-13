using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Items;
using Reloader.DevTools.Runtime;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.DevTools.Tests.PlayMode
{
    public sealed class DevGiveItemCommandPlayModeTests
    {
        private const string StarterAmmoItemId = "ammo-factory-308-147-fmj";

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

            UnityEngine.Object.DestroyImmediate(ammo);
            UnityEngine.Object.DestroyImmediate(root);
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

            UnityEngine.Object.DestroyImmediate(ammo);
            UnityEngine.Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator GiveTest_GrantsStarterKitAndEquipsLoadedScopedKar98k()
        {
#if UNITY_EDITOR
            var root = new GameObject("DevGiveTestRoot");
            var inputSource = root.AddComponent<StubInputSource>();
            var inventoryRuntime = new PlayerInventoryRuntime();
            inventoryRuntime.SetBackpackCapacity(8);
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            inventoryController.Configure(inputSource, null, inventoryRuntime);

            var starterWeapon = ScriptableObject.CreateInstance<ItemDefinition>();
            starterWeapon.SetValuesForTests(
                "weapon-kar98k",
                ItemCategory.Weapon,
                "Kar98k (.308)",
                ItemStackPolicy.NonStackable,
                maxStack: 1);

            var starterAmmo = ScriptableObject.CreateInstance<ItemDefinition>();
            starterAmmo.SetValuesForTests(
                StarterAmmoItemId,
                ItemCategory.Consumable,
                ".308 Winchester FMJ",
                ItemStackPolicy.StackByDefinition,
                maxStack: 999);

            SetPrivateField(
                typeof(PlayerInventoryController),
                inventoryController,
                "_itemDefinitionRegistry",
                new List<ItemDefinition> { starterWeapon, starterAmmo });

            var weaponRegistry = CreateWeaponRegistry();
            var weaponController = root.AddComponent(ResolveRequiredType("Reloader.Weapons.Controllers.PlayerWeaponController"));
            SetPrivateField(weaponController.GetType(), weaponController, "_weaponRegistry", weaponRegistry);
            var worldCameraGo = ConfigureScopedKar98kViewRuntime(weaponController);

            var runtime = new DevToolsRuntime();
            runtime.Context.InventoryController = inventoryController;
            runtime.Context.WeaponController = weaponController as MonoBehaviour;

            yield return null;

            var executed = runtime.TryExecute("give test", out var resultMessage);

            yield return null;

            Assert.That(executed, Is.True, resultMessage);
            Assert.That(inventoryRuntime.GetItemQuantity("weapon-kar98k"), Is.EqualTo(1));
            Assert.That(inventoryRuntime.GetItemQuantity("att-kar98k-scope-remote-a"), Is.EqualTo(0));
            Assert.That(inventoryRuntime.GetItemQuantity(StarterAmmoItemId), Is.EqualTo(500));
            Assert.That(inventoryController.TryGetSelectedInventoryItemId(out var selectedItemId), Is.True);
            Assert.That(selectedItemId, Is.EqualTo("weapon-kar98k"));
            Assert.That(GetPropertyValue<string>(weaponController, "EquippedItemId"), Is.EqualTo("weapon-kar98k"));

            var state = GetWeaponRuntimeState(weaponController, "weapon-kar98k");
            Assert.That(state, Is.Not.Null);
            Assert.That(GetPropertyValue<bool>(state, "ChamberLoaded"), Is.True);
            Assert.That(GetPropertyValue<int>(state, "MagazineCount"), Is.EqualTo(4));
            Assert.That(GetPropertyValue<int>(state, "ReserveCount"), Is.EqualTo(500));
            Assert.That(GetEquippedAttachmentItemId(state, "Scope"), Is.EqualTo("att-kar98k-scope-remote-a"));
            Assert.That(GetPropertyValue<bool>(weaponController, "HasActiveScopedAdsAlignment"), Is.True,
                "give test should mount the scoped runtime view from state instead of only mutating the saved attachment snapshot.");

            inputSource.AimHeldValue = true;
            var adsBridge = GetPrivateFieldValue<Component>(weaponController, "_adsStateRuntimeBridge");
            Assert.That(adsBridge, Is.Not.Null);

            var frames = 0;
            while (GetPropertyValue<float>(adsBridge, "AdsT") < 0.999f && frames < 90)
            {
                frames++;
                yield return null;
                if (!Application.isBatchMode)
                {
                    yield return new WaitForEndOfFrame();
                }
            }

            yield return null;
            if (!Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame();
            }

            var equippedView = GetPropertyValue<Transform>(weaponController, "EquippedWeaponViewTransform");
            Assert.That(equippedView, Is.Not.Null);
            var attachmentManagerType = ResolveRequiredType("Reloader.Game.Weapons.AttachmentManager");
            var manager = equippedView.GetComponent(attachmentManagerType);
            Assert.That(manager, Is.Not.Null);

            var getActiveSightAnchor = attachmentManagerType.GetMethod("GetActiveSightAnchor", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(getActiveSightAnchor, Is.Not.Null);
            var activeSightAnchor = getActiveSightAnchor!.Invoke(manager, null) as Transform;
            Assert.That(activeSightAnchor, Is.Not.Null);

            var worldCamera = worldCameraGo.GetComponent<Camera>();
            var scopedAlignmentDistance = Vector3.Distance(activeSightAnchor.position, worldCamera.transform.position);
            Assert.That(
                scopedAlignmentDistance,
                Is.LessThan(0.04f),
                "give test should leave the real Kar98k optic aligned to the camera at full ADS instead of burying the scope anchor inside the stock.");

            UnityEngine.Object.DestroyImmediate(starterWeapon);
            UnityEngine.Object.DestroyImmediate(starterAmmo);
            UnityEngine.Object.DestroyImmediate(worldCameraGo);
            UnityEngine.Object.DestroyImmediate(weaponRegistry.gameObject);
            UnityEngine.Object.DestroyImmediate(root);
#else
            Assert.Ignore("Requires UnityEditor AssetDatabase.");
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator GiveTest_LoadsCanonicalStarterKitDefinitionsWhenInventoryRegistryIsMissingStarterKit()
        {
#if UNITY_EDITOR
            var root = new GameObject("DevGiveTestFallbackRoot");
            var inputSource = root.AddComponent<StubInputSource>();
            var inventoryRuntime = new PlayerInventoryRuntime();
            inventoryRuntime.SetBackpackCapacity(8);
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            inventoryController.Configure(inputSource, null, inventoryRuntime);

            SetPrivateField(
                typeof(PlayerInventoryController),
                inventoryController,
                "_itemDefinitionRegistry",
                new List<ItemDefinition>());

            var weaponRegistry = CreateWeaponRegistry();
            var weaponController = root.AddComponent(ResolveRequiredType("Reloader.Weapons.Controllers.PlayerWeaponController"));
            SetPrivateField(weaponController.GetType(), weaponController, "_weaponRegistry", weaponRegistry);
            var worldCameraGo = ConfigureScopedKar98kViewRuntime(weaponController);

            var runtime = new DevToolsRuntime();
            runtime.Context.InventoryController = inventoryController;
            runtime.Context.WeaponController = weaponController as MonoBehaviour;

            yield return null;

            var executed = runtime.TryExecute("give test", out var resultMessage);

            yield return null;

            Assert.That(executed, Is.True, resultMessage);
            Assert.That(inventoryRuntime.GetItemQuantity("weapon-kar98k"), Is.EqualTo(1));
            Assert.That(inventoryRuntime.GetItemQuantity("att-kar98k-scope-remote-a"), Is.EqualTo(0));
            Assert.That(inventoryRuntime.GetItemQuantity(StarterAmmoItemId), Is.EqualTo(500));
            Assert.That(GetPropertyValue<bool>(weaponController, "HasActiveScopedAdsAlignment"), Is.True,
                "Canonical give test fallback should still mount the live scoped ADS bridge after seeding runtime state.");

            UnityEngine.Object.DestroyImmediate(worldCameraGo);
            UnityEngine.Object.DestroyImmediate(weaponRegistry.gameObject);
            UnityEngine.Object.DestroyImmediate(root);
#else
            Assert.Ignore("Requires UnityEditor AssetDatabase.");
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator GiveTest_WhenStarterWeaponCannotReachBelt_RollsBackGrantedItems()
        {
            var root = new GameObject("DevGiveTestAtomicityRoot");
            var inputSource = root.AddComponent<StubInputSource>();
            var inventoryRuntime = new PlayerInventoryRuntime();
            inventoryRuntime.SetBackpackCapacity(8);
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            inventoryController.Configure(inputSource, null, inventoryRuntime);

            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount; i++)
            {
                var fillerItemId = $"belt-filler-{i}";
                inventoryRuntime.SetItemMaxStack(fillerItemId, 1);
                var stored = inventoryRuntime.TryAddStackItem(fillerItemId, 1, out _, out _, out _);
                Assert.That(stored, Is.True);
            }

            inventoryRuntime.SelectBeltSlot(0);

            var starterWeapon = ScriptableObject.CreateInstance<ItemDefinition>();
            starterWeapon.SetValuesForTests(
                "weapon-kar98k",
                ItemCategory.Weapon,
                "Kar98k (.308)",
                ItemStackPolicy.NonStackable,
                maxStack: 1);

            var starterScope = ScriptableObject.CreateInstance<ItemDefinition>();
            starterScope.SetValuesForTests(
                "att-kar98k-scope-remote-a",
                ItemCategory.Misc,
                "Kar98k Scope Remote A",
                ItemStackPolicy.NonStackable,
                maxStack: 1);

            var starterAmmo = ScriptableObject.CreateInstance<ItemDefinition>();
            starterAmmo.SetValuesForTests(
                StarterAmmoItemId,
                ItemCategory.Consumable,
                ".308 Winchester FMJ",
                ItemStackPolicy.StackByDefinition,
                maxStack: 999);

            SetPrivateField(
                typeof(PlayerInventoryController),
                inventoryController,
                "_itemDefinitionRegistry",
                new List<ItemDefinition> { starterWeapon, starterScope, starterAmmo });

            var runtime = new DevToolsRuntime();
            runtime.Context.InventoryController = inventoryController;
            runtime.Context.WeaponController = root.AddComponent<StubWeaponControllerMarker>();

            yield return null;

            var executed = runtime.TryExecute("give test", out var resultMessage);

            Assert.That(executed, Is.False);
            Assert.That(resultMessage, Is.EqualTo("Unable to locate 'weapon-kar98k' on the player belt."));
            Assert.That(inventoryRuntime.GetItemQuantity("weapon-kar98k"), Is.EqualTo(0), "Starter weapon grant should be rolled back when equip preconditions fail.");
            Assert.That(inventoryRuntime.GetItemQuantity("att-kar98k-scope-remote-a"), Is.EqualTo(0), "Starter scope grant should not leak on failure.");
            Assert.That(inventoryRuntime.GetItemQuantity(StarterAmmoItemId), Is.EqualTo(0), "Starter ammo grant should be rolled back when equip preconditions fail.");
            Assert.That(inventoryRuntime.SelectedBeltIndex, Is.EqualTo(0), "Atomic rollback should preserve the previously selected belt slot.");
            Assert.That(inventoryRuntime.SelectedBeltItemId, Is.EqualTo("belt-filler-0"));
            CollectionAssert.AreEqual(
                new[]
                {
                    "belt-filler-0",
                    "belt-filler-1",
                    "belt-filler-2",
                    "belt-filler-3",
                    "belt-filler-4"
                },
                inventoryRuntime.BeltSlotItemIds,
                "Atomic rollback should leave the existing belt layout untouched.");

            UnityEngine.Object.DestroyImmediate(starterWeapon);
            UnityEngine.Object.DestroyImmediate(starterScope);
            UnityEngine.Object.DestroyImmediate(starterAmmo);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void SetPrivateField(System.Type ownerType, object instance, string fieldName, object value)
        {
            var field = ownerType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {ownerType.Name}.");
            field.SetValue(instance, value);
        }

        private static Component CreateWeaponRegistry()
        {
            var registryType = ResolveRequiredType("Reloader.Weapons.Runtime.WeaponRegistry");
            var definitionType = ResolveRequiredType("Reloader.Weapons.Data.WeaponDefinition");
            var compatibilityType = ResolveRequiredType("Reloader.Weapons.Data.WeaponAttachmentCompatibility");
            var slotType = ResolveRequiredType("Reloader.Weapons.Data.WeaponAttachmentSlotType");

            var registryRoot = new GameObject("WeaponRegistry");
            var registry = registryRoot.AddComponent(registryType);
            var definition = ScriptableObject.CreateInstance(definitionType);

            var setRuntimeValues = definitionType.GetMethod("SetRuntimeValuesForTests", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(setRuntimeValues, Is.Not.Null);
            setRuntimeValues!.Invoke(definition, new object[]
            {
                "weapon-kar98k",
                "Kar98k",
                5,
                0.05f,
                80f,
                0f,
                20f,
                120f,
                0,
                0,
                false,
                0.7f,
                null,
                null,
                StarterAmmoItemId
            });

            var scopeSlot = Enum.Parse(slotType, "Scope");
            var createCompatibility = compatibilityType.GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
            Assert.That(createCompatibility, Is.Not.Null);
            var scopeCompatibility = createCompatibility!.Invoke(null, new object[] { scopeSlot, new[] { "att-kar98k-scope-remote-a" } });
            var compatibilityArray = Array.CreateInstance(compatibilityType, 1);
            compatibilityArray.SetValue(scopeCompatibility, 0);

            var setCompatibilities = definitionType.GetMethod("SetAttachmentCompatibilitiesForTests", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(setCompatibilities, Is.Not.Null);
            setCompatibilities!.Invoke(definition, new object[] { compatibilityArray });

            var definitionsArray = Array.CreateInstance(definitionType, 1);
            definitionsArray.SetValue(definition, 0);
            var setDefinitions = registryType.GetMethod("SetDefinitionsForTests", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(setDefinitions, Is.Not.Null);
            setDefinitions!.Invoke(registry, new object[] { definitionsArray });

            return registry;
        }

        private static GameObject ConfigureScopedKar98kViewRuntime(Component weaponController)
        {
            Assert.That(weaponController, Is.Not.Null);

            var rifleViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Weapons/Prefabs/RifleView.prefab");
            var opticDefinition = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                "Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteA.asset");
            Assert.That(rifleViewPrefab, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null);

            var worldCameraGo = new GameObject("WorldCam");
            var worldCamera = worldCameraGo.AddComponent<Camera>();
            worldCamera.tag = "MainCamera";
            var viewmodelCameraGo = new GameObject("ViewmodelCamera");
            viewmodelCameraGo.transform.SetParent(worldCamera.transform, false);
            viewmodelCameraGo.AddComponent<Camera>();

            SetPrivateField(weaponController.GetType(), weaponController, "_adsCamera", worldCamera);

            var metadataType = ResolveRequiredType("Reloader.Weapons.Runtime.WeaponAttachmentItemMetadata");
            var slotType = ResolveRequiredType("Reloader.Weapons.Data.WeaponAttachmentSlotType");
            var createForTests = metadataType.GetMethod("CreateForTests", BindingFlags.Public | BindingFlags.Static);
            Assert.That(createForTests, Is.Not.Null);
            var scopeSlot = Enum.Parse(slotType, "Scope");
            var metadata = createForTests!.Invoke(null, new object[]
            {
                "att-kar98k-scope-remote-a",
                scopeSlot,
                opticDefinition
            });

            var metadataArray = Array.CreateInstance(metadataType, 1);
            metadataArray.SetValue(metadata, 0);
            SetPrivateField(weaponController.GetType(), weaponController, "_attachmentItemMetadata", metadataArray);
            SetControllerWeaponViewBinding(weaponController, "weapon-kar98k", rifleViewPrefab);

            var poseHelper = weaponController.gameObject.AddComponent<WeaponViewPoseTuningHelper>();
            SetPrivateField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_weaponController", weaponController);
            SetPrivateField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_targetWeaponItemId", "weapon-kar98k");
            SetPrivateField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_hipLocalPosition", new Vector3(0.015f, 0.15f, 0.005f));
            SetPrivateField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_adsLocalPosition", new Vector3(0f, 0.2f, 0.077f));
            SetPrivateField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_hipLocalEuler", Vector3.zero);
            SetPrivateField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_adsLocalEuler", Vector3.zero);
            SetPrivateField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_rifleLocalEulerOffset", new Vector3(90f, 0f, 0f));
            SetPrivateField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_blendSpeed", 24f);

            var overrideType = typeof(WeaponViewPoseTuningHelper).GetNestedType("AttachmentPoseOverride", BindingFlags.NonPublic);
            Assert.That(overrideType, Is.Not.Null);
            var overrides = Array.CreateInstance(overrideType!, 1);
            var entry = Activator.CreateInstance(overrideType!);
            SetPrivateField(overrideType!, entry, "_slotType", scopeSlot);
            SetPrivateField(overrideType!, entry, "_attachmentItemId", "att-kar98k-scope-remote-a");
            SetPrivateField(overrideType!, entry, "_hipLocalPosition", new Vector3(0.015f, 0.15f, 0.005f));
            SetPrivateField(overrideType!, entry, "_hipLocalEuler", Vector3.zero);
            SetPrivateField(overrideType!, entry, "_adsLocalPosition", new Vector3(0f, 0.2f, 0.05f));
            SetPrivateField(overrideType!, entry, "_adsLocalEuler", Vector3.zero);
            SetPrivateField(overrideType!, entry, "_rifleLocalEulerOffset", new Vector3(90f, 0f, 0f));
            SetPrivateField(overrideType!, entry, "_blendSpeed", 24f);
            SetPrivateField(overrideType!, entry, "_scopedAdsEyeReliefBackOffset", 0f);
            overrides.SetValue(entry, 0);
            SetPrivateField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_attachmentPoseOverrides", overrides);

            return worldCameraGo;
        }

        private static void SetControllerWeaponViewBinding(Component weaponController, string itemId, GameObject viewPrefab)
        {
            Assert.That(weaponController, Is.Not.Null);
            Assert.That(viewPrefab, Is.Not.Null);

            var controllerType = weaponController.GetType();
            var weaponViewParentField = controllerType.GetField("_weaponViewParent", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(weaponViewParentField, Is.Not.Null, "Expected _weaponViewParent field on PlayerWeaponController.");
            if (weaponViewParentField!.GetValue(weaponController) == null)
            {
                weaponViewParentField.SetValue(weaponController, weaponController.transform);
            }

            var bindingType = ResolveRequiredType("Reloader.Weapons.Controllers.WeaponViewPrefabBinding");
            var binding = Activator.CreateInstance(bindingType);
            var itemField = bindingType.GetField("_itemId", BindingFlags.Instance | BindingFlags.NonPublic);
            var prefabField = bindingType.GetField("_viewPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(itemField, Is.Not.Null);
            Assert.That(prefabField, Is.Not.Null);
            itemField!.SetValue(binding, itemId);
            prefabField!.SetValue(binding, viewPrefab);

            var bindings = Array.CreateInstance(bindingType, 1);
            bindings.SetValue(binding, 0);
            SetPrivateField(controllerType, weaponController, "_weaponViewPrefabs", bindings);

            var updateEquip = controllerType.GetMethod("UpdateEquipFromSelection", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(updateEquip, Is.Not.Null, "Expected UpdateEquipFromSelection on PlayerWeaponController.");
            updateEquip!.Invoke(weaponController, null);
        }

        private static object GetWeaponRuntimeState(Component weaponController, string itemId)
        {
            var method = weaponController.GetType().GetMethod("TryGetRuntimeState", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null);

            var args = new object[] { itemId, null };
            var resolved = method!.Invoke(weaponController, args);
            Assert.That(resolved, Is.EqualTo(true));
            return args[1];
        }

        private static string GetEquippedAttachmentItemId(object weaponRuntimeState, string slotName)
        {
            var slotType = ResolveRequiredType("Reloader.Weapons.Data.WeaponAttachmentSlotType");
            var slotValue = Enum.Parse(slotType, slotName);
            var method = weaponRuntimeState.GetType().GetMethod("GetEquippedAttachmentItemId", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null);
            return method!.Invoke(weaponRuntimeState, new[] { slotValue }) as string;
        }

        private static T GetPropertyValue<T>(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}' on {instance.GetType().Name}.");
            return (T)property!.GetValue(instance);
        }

        private static T GetPrivateFieldValue<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {instance.GetType().Name}.");
            return (T)field!.GetValue(instance);
        }

        private static Type ResolveRequiredType(string fullName)
        {
            var type = Type.GetType(fullName);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            Assert.Fail($"Expected runtime type '{fullName}'.");
            return null;
        }

        private sealed class StubInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool AimHeldValue { get; set; }

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => AimHeldValue;
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

        private sealed class StubWeaponControllerMarker : MonoBehaviour
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Reloader.Core.Events;
using Reloader.Core.Items;
using Reloader.Inventory;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.DevTools.Runtime
{
    public sealed class DevGiveItemCommand
    {
        private const string StarterKitToken = "test";
        private const string StarterWeaponId = "weapon-kar98k";
        private const string StarterScopeId = "att-kar98k-scope-remote-a";
        private const string StarterAmmoId = "ammo-factory-308-147-fmj";
        private const int StarterAmmoQuantity = 500;
        private const int StarterMagazineCount = 4;
        private const string StarterWeaponDefinitionPath = "Assets/_Project/Inventory/Data/Items/Rifle_308_Starter.asset";
        private const string StarterScopeDefinitionPath = "Assets/_Project/Inventory/Data/Items/Kar98k_Scope_Remote_A.asset";
        private const string StarterAmmoDefinitionPath = "Assets/_Project/Inventory/Data/Items/Cartridge_308_147_FMJ_PMC_Bronze.asset";

        private readonly struct StarterRuntimeRollbackState
        {
            public StarterRuntimeRollbackState(bool hasSnapshot, object snapshot)
            {
                HasSnapshot = hasSnapshot;
                Snapshot = snapshot;
            }

            public bool HasSnapshot { get; }
            public object Snapshot { get; }
        }

        private readonly struct StarterInventoryRollbackState
        {
            public StarterInventoryRollbackState(
                int backpackCapacity,
                int selectedBeltIndex,
                string[] beltSlotItemIds,
                List<string> backpackItemIds,
                Dictionary<string, int> itemQuantities,
                Dictionary<string, int> itemMaxStacks,
                Dictionary<string, object> slotStackStates)
            {
                BackpackCapacity = backpackCapacity;
                SelectedBeltIndex = selectedBeltIndex;
                BeltSlotItemIds = beltSlotItemIds;
                BackpackItemIds = backpackItemIds;
                ItemQuantities = itemQuantities;
                ItemMaxStacks = itemMaxStacks;
                SlotStackStates = slotStackStates;
            }

            public int BackpackCapacity { get; }
            public int SelectedBeltIndex { get; }
            public string[] BeltSlotItemIds { get; }
            public List<string> BackpackItemIds { get; }
            public Dictionary<string, int> ItemQuantities { get; }
            public Dictionary<string, int> ItemMaxStacks { get; }
            public Dictionary<string, object> SlotStackStates { get; }
        }

        public bool TryExecute(DevCommandContext context, DevCommandParseResult parseResult, out string resultMessage)
        {
            var inventoryController = context?.ResolveInventoryController();
            if (inventoryController == null)
            {
                resultMessage = "No player inventory controller is available.";
                return false;
            }

            if (IsGiveTestCommand(parseResult))
            {
                return TryExecuteStarterKit(context, inventoryController, out resultMessage);
            }

            if (!TryParseGiveItemArguments(parseResult, out var rawItemToken, out var quantity))
            {
                resultMessage = "Usage: give item <item-id-or-name> [quantity] | give test";
                return false;
            }

            var lookup = new DevItemLookupService(inventoryController.GetItemDefinitionRegistrySnapshot());
            if (!lookup.TryResolve(rawItemToken, out var definition) || definition == null)
            {
                resultMessage = $"Unknown item '{rawItemToken}'.";
                return false;
            }

            if (!inventoryController.TryGrantItemForDevTools(definition, quantity, out var rejectReason))
            {
                resultMessage = $"Unable to grant '{definition.DefinitionId}': {rejectReason}.";
                return false;
            }

            resultMessage = $"Granted {quantity} x {definition.DefinitionId}.";
            return true;
        }

        public IReadOnlyList<DevConsoleSuggestion> GetSuggestions(
            DevCommandContext context,
            string input,
            DevCommandParseResult parseResult)
        {
            if (parseResult.Arguments.Length == 0)
            {
                return new[]
                {
                    new DevConsoleSuggestion("item", "item", applyText: "give item"),
                    new DevConsoleSuggestion(StarterKitToken, StarterKitToken, "Grant the scoped Kar98k starter kit.", "give test")
                };
            }

            var firstArgument = parseResult.Arguments[0];
            if (StarterKitToken.StartsWith(firstArgument, StringComparison.OrdinalIgnoreCase))
            {
                return new[]
                {
                    new DevConsoleSuggestion(StarterKitToken, StarterKitToken, "Grant the scoped Kar98k starter kit.", "give test")
                };
            }

            if (!"item".StartsWith(firstArgument, StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<DevConsoleSuggestion>();
            }

            if (!string.Equals(firstArgument, "item", StringComparison.OrdinalIgnoreCase))
            {
                return new[]
                {
                    new DevConsoleSuggestion("item", "item", applyText: "give item")
                };
            }

            if (IsEditingQuantity(parseResult, input))
            {
                return Array.Empty<DevConsoleSuggestion>();
            }

            var inventoryController = context?.ResolveInventoryController();
            if (inventoryController == null)
            {
                return Array.Empty<DevConsoleSuggestion>();
            }

            var lookup = new DevItemLookupService(inventoryController.GetItemDefinitionRegistrySnapshot());
            var itemToken = ExtractItemTokenForSuggestions(parseResult, input);
            var suggestions = lookup.GetSuggestions(itemToken);
            return suggestions
                .Select(static suggestion => new DevConsoleSuggestion(
                    suggestion.Token,
                    suggestion.Label,
                    suggestion.Description,
                    $"give item {suggestion.Token}"))
                .ToArray();
        }

        private static bool TryParseGiveItemArguments(
            DevCommandParseResult parseResult,
            out string rawItemToken,
            out int quantity)
        {
            rawItemToken = string.Empty;
            quantity = 1;
            if (parseResult.Arguments.Length < 2
                || !string.Equals(parseResult.Arguments[0], "item", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var trailingArgument = parseResult.Arguments[^1];
            if (parseResult.Arguments.Length >= 3
                && int.TryParse(trailingArgument, out var parsedQuantity)
                && parsedQuantity > 0)
            {
                quantity = parsedQuantity;
                rawItemToken = string.Join(" ", parseResult.Arguments.Skip(1).Take(parseResult.Arguments.Length - 2));
                return !string.IsNullOrWhiteSpace(rawItemToken);
            }

            rawItemToken = string.Join(" ", parseResult.Arguments.Skip(1));
            return !string.IsNullOrWhiteSpace(rawItemToken);
        }

        private static bool IsEditingQuantity(DevCommandParseResult parseResult, string input)
        {
            if (parseResult.Arguments.Length < 3)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(input) || char.IsWhiteSpace(input[^1]))
            {
                return false;
            }

            return int.TryParse(parseResult.Arguments[^1], out _);
        }

        private static string ExtractItemTokenForSuggestions(DevCommandParseResult parseResult, string input)
        {
            if (parseResult.Arguments.Length < 2)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(input) || char.IsWhiteSpace(input[^1]))
            {
                return string.Empty;
            }

            return string.Join(" ", parseResult.Arguments.Skip(1));
        }

        private static bool IsGiveTestCommand(DevCommandParseResult parseResult)
        {
            return parseResult.Arguments.Length == 1
                   && string.Equals(parseResult.Arguments[0], StarterKitToken, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryExecuteStarterKit(
            DevCommandContext context,
            PlayerInventoryController inventoryController,
            out string resultMessage)
        {
            if (inventoryController.Runtime == null)
            {
                resultMessage = "Player inventory runtime is unavailable.";
                return false;
            }

            var weaponController = context?.ResolveWeaponController();
            if (weaponController == null)
            {
                resultMessage = "No player weapon controller is available.";
                return false;
            }

            var lookup = new DevItemLookupService(BuildStarterKitDefinitionSource(inventoryController));
            if (!TryResolveStarterDefinition(lookup, StarterWeaponId, out var weaponDefinition)
                || !TryResolveStarterDefinition(lookup, StarterScopeId, out var scopeDefinition)
                || !TryResolveStarterDefinition(lookup, StarterAmmoId, out var ammoDefinition))
            {
                resultMessage = "Starter-kit definitions are unavailable.";
                return false;
            }

            if (!CanEquipStarterWeaponFromInventory(inventoryController.Runtime))
            {
                resultMessage = $"Unable to locate '{StarterWeaponId}' on the player belt.";
                return false;
            }

            var inventoryRollbackState = CaptureStarterInventoryRollbackState(inventoryController.Runtime);
            var runtimeRollbackState = CaptureStarterRuntimeRollbackState(weaponController);
            if (!TryGrantStarterDefinition(inventoryController, weaponDefinition, 1, out resultMessage))
            {
                return false;
            }

            if (!TryGrantStarterDefinition(inventoryController, ammoDefinition, StarterAmmoQuantity, out resultMessage))
            {
                RestoreStarterInventoryRollbackState(inventoryController.Runtime, inventoryRollbackState);
                return false;
            }

            var beltIndex = FindBeltSlotIndex(inventoryController.Runtime, StarterWeaponId);
            if (beltIndex < 0)
            {
                RestoreStarterInventoryRollbackState(inventoryController.Runtime, inventoryRollbackState);
                resultMessage = $"Unable to locate '{StarterWeaponId}' on the player belt.";
                return false;
            }

            inventoryController.Runtime.SelectBeltSlot(beltIndex);
            SyncStarterWeaponEquipFromSelection(weaponController);
            if (!TryApplyStarterWeaponState(weaponController, inventoryController, scopeDefinition, out resultMessage))
            {
                RestoreStarterInventoryRollbackState(inventoryController.Runtime, inventoryRollbackState);
                RestoreStarterRuntimeRollbackState(weaponController, runtimeRollbackState);
                return false;
            }

            resultMessage = $"Granted starter kit: {StarterWeaponId}, {StarterAmmoId} x{StarterAmmoQuantity}.";
            return true;
        }

        private static void SyncStarterWeaponEquipFromSelection(MonoBehaviour weaponController)
        {
            if (weaponController == null)
            {
                return;
            }

            var controllerType = weaponController.GetType();
            var updateEquipFromSelection = controllerType.GetMethod("UpdateEquipFromSelection", BindingFlags.Instance | BindingFlags.NonPublic);
            if (updateEquipFromSelection == null)
            {
                return;
            }

            updateEquipFromSelection.Invoke(weaponController, null);
        }

        private static bool TryResolveStarterDefinition(
            DevItemLookupService lookup,
            string definitionId,
            out ItemDefinition definition)
        {
            definition = null;
            return lookup != null
                   && lookup.TryResolve(definitionId, out definition)
                   && definition != null;
        }

        private static bool TryGrantStarterDefinition(
            PlayerInventoryController inventoryController,
            ItemDefinition definition,
            int quantity,
            out string resultMessage)
        {
            resultMessage = string.Empty;
            if (definition == null)
            {
                resultMessage = "Starter-kit item definition is missing.";
                return false;
            }

            if (inventoryController.TryGrantItemForDevTools(definition, quantity, out var rejectReason))
            {
                return true;
            }

            resultMessage = $"Unable to grant '{definition.DefinitionId}': {rejectReason}.";
            return false;
        }

        private static IReadOnlyList<ItemDefinition> BuildStarterKitDefinitionSource(PlayerInventoryController inventoryController)
        {
            var combined = new List<ItemDefinition>();
            var seenDefinitionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AppendDefinitions(combined, seenDefinitionIds, inventoryController?.GetItemDefinitionRegistrySnapshot());
            AppendDefinitions(combined, seenDefinitionIds, Resources.FindObjectsOfTypeAll<ItemDefinition>());
            AppendStarterKitEditorDefinitions(combined, seenDefinitionIds);

            return combined;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void AppendStarterKitEditorDefinitions(
            ICollection<ItemDefinition> target,
            ISet<string> seenDefinitionIds)
        {
#if UNITY_EDITOR
            AppendEditorDefinition(target, seenDefinitionIds, StarterWeaponDefinitionPath);
            AppendEditorDefinition(target, seenDefinitionIds, StarterScopeDefinitionPath);
            AppendEditorDefinition(target, seenDefinitionIds, StarterAmmoDefinitionPath);
#endif
        }

#if UNITY_EDITOR
        private static void AppendEditorDefinition(
            ICollection<ItemDefinition> target,
            ISet<string> seenDefinitionIds,
            string assetPath)
        {
            if (target == null || seenDefinitionIds == null || string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            var definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
            if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId))
            {
                return;
            }

            if (!seenDefinitionIds.Add(definition.DefinitionId))
            {
                return;
            }

            target.Add(definition);
        }
#endif

        private static void AppendDefinitions(
            ICollection<ItemDefinition> target,
            ISet<string> seenDefinitionIds,
            IReadOnlyList<ItemDefinition> definitions)
        {
            if (target == null || seenDefinitionIds == null || definitions == null)
            {
                return;
            }

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId))
                {
                    continue;
                }

                if (!seenDefinitionIds.Add(definition.DefinitionId))
                {
                    continue;
                }

                target.Add(definition);
            }
        }

        private static int FindBeltSlotIndex(PlayerInventoryRuntime runtime, string itemId)
        {
            if (runtime == null || string.IsNullOrWhiteSpace(itemId))
            {
                return -1;
            }

            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount; i++)
            {
                if (string.Equals(runtime.BeltSlotItemIds[i], itemId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool CanEquipStarterWeaponFromInventory(PlayerInventoryRuntime runtime)
        {
            if (runtime == null)
            {
                return false;
            }

            if (FindBeltSlotIndex(runtime, StarterWeaponId) >= 0)
            {
                return true;
            }

            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount; i++)
            {
                if (string.IsNullOrWhiteSpace(runtime.BeltSlotItemIds[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static StarterInventoryRollbackState CaptureStarterInventoryRollbackState(PlayerInventoryRuntime runtime)
        {
            if (runtime == null)
            {
                return default;
            }

            return new StarterInventoryRollbackState(
                runtime.BackpackCapacity,
                runtime.SelectedBeltIndex,
                (string[])runtime.BeltSlotItemIds.Clone(),
                new List<string>(runtime.BackpackItemIds),
                CloneStringIntDictionary(runtime, "_itemQuantities"),
                CloneStringIntDictionary(runtime, "_itemMaxStacks"),
                CloneSlotStateDictionary(runtime));
        }

        private static void RestoreStarterInventoryRollbackState(PlayerInventoryRuntime runtime, StarterInventoryRollbackState rollbackState)
        {
            if (runtime == null)
            {
                return;
            }

            runtime.ClearCarriedItems();
            SetPrivateAutoProperty(runtime, nameof(PlayerInventoryRuntime.BackpackCapacity), rollbackState.BackpackCapacity);

            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount && rollbackState.BeltSlotItemIds != null; i++)
            {
                runtime.BeltSlotItemIds[i] = rollbackState.BeltSlotItemIds[i];
            }

            runtime.BackpackItemIds.Clear();
            if (rollbackState.BackpackItemIds != null)
            {
                runtime.BackpackItemIds.AddRange(rollbackState.BackpackItemIds);
            }

            RestoreStringIntDictionary(runtime, "_itemQuantities", rollbackState.ItemQuantities);
            RestoreStringIntDictionary(runtime, "_itemMaxStacks", rollbackState.ItemMaxStacks);
            RestoreSlotStateDictionary(runtime, rollbackState.SlotStackStates);
            SetPrivateAutoProperty(runtime, nameof(PlayerInventoryRuntime.SelectedBeltIndex), rollbackState.SelectedBeltIndex);
        }

        private static StarterRuntimeRollbackState CaptureStarterRuntimeRollbackState(MonoBehaviour weaponController)
        {
            if (weaponController == null)
            {
                return default;
            }

            var getSnapshots = weaponController.GetType().GetMethod("GetRuntimeStateSnapshots", BindingFlags.Instance | BindingFlags.Public);
            if (getSnapshots == null)
            {
                return default;
            }

            if (getSnapshots.Invoke(weaponController, null) is not System.Collections.IEnumerable snapshots)
            {
                return default;
            }

            foreach (var snapshot in snapshots)
            {
                if (snapshot == null)
                {
                    continue;
                }

                var itemId = snapshot.GetType().GetProperty("ItemId", BindingFlags.Instance | BindingFlags.Public)?.GetValue(snapshot) as string;
                if (string.Equals(itemId, StarterWeaponId, StringComparison.Ordinal))
                {
                    return new StarterRuntimeRollbackState(true, snapshot);
                }
            }

            return default;
        }

        private static void RestoreStarterRuntimeRollbackState(MonoBehaviour weaponController, StarterRuntimeRollbackState rollbackState)
        {
            if (weaponController == null)
            {
                return;
            }

            var controllerType = weaponController.GetType();
            var applyRuntimeState = controllerType.GetMethod("ApplyRuntimeState", BindingFlags.Instance | BindingFlags.Public);
            var applyRuntimeBallistics = controllerType.GetMethod("ApplyRuntimeBallistics", BindingFlags.Instance | BindingFlags.Public);
            var applyRuntimeAttachments = controllerType.GetMethod("ApplyRuntimeAttachments", BindingFlags.Instance | BindingFlags.Public);
            if (applyRuntimeState == null || applyRuntimeBallistics == null || applyRuntimeAttachments == null)
            {
                return;
            }

            if (!rollbackState.HasSnapshot || rollbackState.Snapshot == null)
            {
                var emptyAttachments = CreateEmptyAttachmentMap(applyRuntimeAttachments);
                var emptyMagazineRounds = CreateEmptyMagazineRounds(applyRuntimeBallistics);
                applyRuntimeState.Invoke(weaponController, new object[] { StarterWeaponId, 0, 0, false });
                applyRuntimeBallistics.Invoke(weaponController, new object[] { StarterWeaponId, null, emptyMagazineRounds });
                applyRuntimeAttachments.Invoke(weaponController, new object[] { StarterWeaponId, emptyAttachments });
                SyncStarterWeaponEquipFromSelection(weaponController);
                return;
            }

            var snapshotType = rollbackState.Snapshot.GetType();
            var chamberLoadedValue = snapshotType.GetProperty("ChamberLoaded", BindingFlags.Instance | BindingFlags.Public)?.GetValue(rollbackState.Snapshot);
            var magCountValue = snapshotType.GetProperty("MagCount", BindingFlags.Instance | BindingFlags.Public)?.GetValue(rollbackState.Snapshot);
            var reserveCountValue = snapshotType.GetProperty("ReserveCount", BindingFlags.Instance | BindingFlags.Public)?.GetValue(rollbackState.Snapshot);
            var chamberRound = snapshotType.GetProperty("ChamberRound", BindingFlags.Instance | BindingFlags.Public)?.GetValue(rollbackState.Snapshot);
            var magazineRounds = snapshotType.GetProperty("MagazineRounds", BindingFlags.Instance | BindingFlags.Public)?.GetValue(rollbackState.Snapshot);
            var attachments = snapshotType.GetProperty("EquippedAttachmentItemIdsBySlot", BindingFlags.Instance | BindingFlags.Public)?.GetValue(rollbackState.Snapshot);

            var chamberLoaded = chamberLoadedValue is bool boolValue && boolValue;
            var magCount = magCountValue is int magCountInt ? magCountInt : 0;
            var reserveCount = reserveCountValue is int reserveCountInt ? reserveCountInt : 0;
            applyRuntimeState.Invoke(weaponController, new object[] { StarterWeaponId, magCount, reserveCount, chamberLoaded });
            applyRuntimeBallistics.Invoke(weaponController, new[] { (object)StarterWeaponId, chamberRound, magazineRounds });
            applyRuntimeAttachments.Invoke(weaponController, new[] { (object)StarterWeaponId, attachments });
            SyncStarterWeaponEquipFromSelection(weaponController);
        }

        private static object CreateEmptyAttachmentMap(MethodInfo applyRuntimeAttachmentsMethod)
        {
            var parameterType = applyRuntimeAttachmentsMethod.GetParameters()[1].ParameterType;
            var slotType = ResolveRuntimeType("Reloader.Weapons.Data.WeaponAttachmentSlotType");
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(slotType ?? typeof(object), typeof(string));
            if (parameterType.IsAssignableFrom(dictionaryType))
            {
                return Activator.CreateInstance(dictionaryType);
            }

            return null;
        }

        private static object CreateEmptyMagazineRounds(MethodInfo applyRuntimeBallisticsMethod)
        {
            var parameters = applyRuntimeBallisticsMethod.GetParameters();
            if (parameters.Length < 3)
            {
                return Array.Empty<object>();
            }

            var listType = parameters[2].ParameterType;
            if (!listType.IsGenericType)
            {
                return Array.Empty<object>();
            }

            var itemType = listType.GetGenericArguments()[0];
            return Array.CreateInstance(itemType, 0);
        }

        private static Dictionary<string, int> CloneStringIntDictionary(PlayerInventoryRuntime runtime, string fieldName)
        {
            var field = typeof(PlayerInventoryRuntime).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(runtime) is not Dictionary<string, int> source)
            {
                return new Dictionary<string, int>(StringComparer.Ordinal);
            }

            return new Dictionary<string, int>(source, StringComparer.Ordinal);
        }

        private static Dictionary<string, object> CloneSlotStateDictionary(PlayerInventoryRuntime runtime)
        {
            var field = typeof(PlayerInventoryRuntime).GetField("_slotStackStates", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(runtime) is not System.Collections.IDictionary source)
            {
                return new Dictionary<string, object>(StringComparer.Ordinal);
            }

            var cloned = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (System.Collections.DictionaryEntry entry in source)
            {
                if (entry.Key is not string key)
                {
                    continue;
                }

                cloned[key] = CloneSlotStackState(entry.Value);
            }

            return cloned;
        }

        private static object CloneSlotStackState(object slotState)
        {
            if (slotState == null)
            {
                return null;
            }

            var cloneMethod = slotState.GetType().GetMethod("Clone", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return cloneMethod != null ? cloneMethod.Invoke(slotState, null) : slotState;
        }

        private static void RestoreStringIntDictionary(PlayerInventoryRuntime runtime, string fieldName, Dictionary<string, int> snapshot)
        {
            var field = typeof(PlayerInventoryRuntime).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(runtime) is not Dictionary<string, int> target)
            {
                return;
            }

            target.Clear();
            if (snapshot == null)
            {
                return;
            }

            foreach (var pair in snapshot)
            {
                target[pair.Key] = pair.Value;
            }
        }

        private static void RestoreSlotStateDictionary(PlayerInventoryRuntime runtime, Dictionary<string, object> snapshot)
        {
            var field = typeof(PlayerInventoryRuntime).GetField("_slotStackStates", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(runtime) is not System.Collections.IDictionary target)
            {
                return;
            }

            target.Clear();
            if (snapshot == null)
            {
                return;
            }

            foreach (var pair in snapshot)
            {
                target[pair.Key] = CloneSlotStackState(pair.Value);
            }
        }

        private static void SetPrivateAutoProperty(object instance, string propertyName, object value)
        {
            if (instance == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            property?.SetValue(instance, value);
        }

        private static bool TryApplyStarterWeaponState(
            MonoBehaviour weaponController,
            PlayerInventoryController inventoryController,
            ItemDefinition scopeDefinition,
            out string resultMessage)
        {
            resultMessage = string.Empty;
            var slotType = ResolveRuntimeType("Reloader.Weapons.Data.WeaponAttachmentSlotType");
            if (slotType == null)
            {
                resultMessage = "Unable to resolve weapon attachment slot type.";
                return false;
            }

            var applyRuntimeBallistics = weaponController.GetType().GetMethod("ApplyRuntimeBallistics", BindingFlags.Instance | BindingFlags.Public);
            var applyRuntimeState = weaponController.GetType().GetMethod("ApplyRuntimeState", BindingFlags.Instance | BindingFlags.Public);
            var trySwapAttachment = weaponController.GetType().GetMethod("TrySwapEquippedWeaponAttachment", BindingFlags.Instance | BindingFlags.Public);
            if (applyRuntimeBallistics == null || applyRuntimeState == null || trySwapAttachment == null)
            {
                resultMessage = "Player weapon controller is missing required runtime APIs.";
                return false;
            }

            var ammoDefaultsType = ResolveRuntimeType("Reloader.Weapons.Runtime.WeaponAmmoDefaults");
            var ammoSnapshotType = ResolveRuntimeType("Reloader.Weapons.Ballistics.AmmoBallisticSnapshot");
            var buildFactoryRound = ammoDefaultsType?.GetMethod("BuildFactoryRound", BindingFlags.Static | BindingFlags.Public);
            if (ammoSnapshotType == null || buildFactoryRound == null)
            {
                resultMessage = "Unable to resolve weapon ammo runtime APIs.";
                return false;
            }

            var chamberRound = buildFactoryRound.Invoke(null, new object[] { StarterAmmoId });
            var nullableAmmoSnapshotType = typeof(Nullable<>).MakeGenericType(ammoSnapshotType);
            var boxedChamberRound = Activator.CreateInstance(nullableAmmoSnapshotType, chamberRound);
            var magazineRounds = Array.CreateInstance(ammoSnapshotType, StarterMagazineCount);
            for (var i = 0; i < StarterMagazineCount; i++)
            {
                magazineRounds.SetValue(buildFactoryRound.Invoke(null, new object[] { StarterAmmoId }), i);
            }

            var stateApplied = applyRuntimeState.Invoke(weaponController, new object[] { StarterWeaponId, StarterMagazineCount, StarterAmmoQuantity, true });
            if (stateApplied is not bool stateResult || !stateResult)
            {
                resultMessage = $"Unable to seed loaded runtime state for '{StarterWeaponId}'.";
                return false;
            }

            var ballisticsApplied = applyRuntimeBallistics.Invoke(weaponController, new object[] { StarterWeaponId, boxedChamberRound, magazineRounds });
            if (ballisticsApplied is not bool ballisticResult || !ballisticResult)
            {
                resultMessage = $"Unable to seed ammo loadout for '{StarterWeaponId}'.";
                return false;
            }

            if (!TryGrantStarterDefinition(inventoryController, scopeDefinition, 1, out resultMessage))
            {
                return false;
            }

            var scopeSlot = Enum.Parse(slotType, "Scope");
            var attachmentSwapApplied = trySwapAttachment.Invoke(weaponController, new[] { scopeSlot, (object)StarterScopeId });
            if (attachmentSwapApplied is not bool attachmentResult || !attachmentResult)
            {
                inventoryController?.Runtime?.TryRemoveStackItem(StarterScopeId, 1);
                resultMessage = $"Unable to apply '{StarterScopeId}' to '{StarterWeaponId}'.";
                return false;
            }

            return true;
        }

        private static Type ResolveRuntimeType(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return null;
            }

            var direct = Type.GetType(fullName);
            if (direct != null)
            {
                return direct;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var resolved = assemblies[i].GetType(fullName);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            return null;
        }
    }
}

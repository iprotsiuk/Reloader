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

            if (!TryGrantStarterDefinition(inventoryController, weaponDefinition, 1, out resultMessage)
                || !TryGrantStarterDefinition(inventoryController, ammoDefinition, StarterAmmoQuantity, out resultMessage))
            {
                return false;
            }

            var beltIndex = FindBeltSlotIndex(inventoryController.Runtime, StarterWeaponId);
            if (beltIndex < 0)
            {
                resultMessage = $"Unable to locate '{StarterWeaponId}' on the player belt.";
                return false;
            }

            inventoryController.Runtime.SelectBeltSlot(beltIndex);
            SyncStarterWeaponEquipFromSelection(weaponController);
            if (!TryApplyStarterWeaponState(weaponController, inventoryController, scopeDefinition, out resultMessage))
            {
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

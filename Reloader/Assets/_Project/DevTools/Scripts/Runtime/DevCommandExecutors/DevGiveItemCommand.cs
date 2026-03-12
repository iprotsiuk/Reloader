using System;
using System.Collections.Generic;
using System.Linq;
using Reloader.Core.Events;
using Reloader.Core.Items;
using Reloader.Inventory;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevGiveItemCommand
    {
        public bool TryExecute(DevCommandContext context, DevCommandParseResult parseResult, out string resultMessage)
        {
            var inventoryController = context?.ResolveInventoryController();
            if (inventoryController == null)
            {
                resultMessage = "No player inventory controller is available.";
                return false;
            }

            if (!TryParseGiveItemArguments(parseResult, out var rawItemToken, out var quantity))
            {
                resultMessage = "Usage: give item <item-id-or-name> [quantity]";
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
                    new DevConsoleSuggestion("item", "item", applyText: "give item")
                };
            }

            var firstArgument = parseResult.Arguments[0];
            if (!"item".StartsWith(firstArgument, StringComparison.OrdinalIgnoreCase))
                return Array.Empty<DevConsoleSuggestion>();

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
    }
}

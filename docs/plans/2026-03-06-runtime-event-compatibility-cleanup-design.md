# Runtime Event Compatibility Cleanup Design

## Scope

Remove obsolete non-save runtime-event compatibility for the legacy static `GameEvents` surface and the legacy string-based shop trade result bridge. Keep only the typed runtime hub and typed `ShopTradeResultPayload` contract. Limit code changes to the owned files plus directly affected Player/Economy/NPCs/UI tests that still emulate the deleted legacy API.

## Current State

- `GameEventsRuntimeBridgeTests` already expects `Reloader.Core.Events.GameEvents` to be absent.
- `InventoryEventContractsTests` already expects legacy shop-trade runtime members to be absent.
- Production code still ships both legacy surfaces:
  - `Core/Scripts/Events/GameEvents.cs`
  - `IShopEvents.OnShopTradeResult`
  - `IShopEvents.RaiseShopTradeResult(string, int, bool, bool, string)`
  - `DefaultRuntimeEvents` legacy event/method bridge
  - `ShopTradeResultPayload.ParseLegacyFailureReason`
- Direct runtime consumers already use `OnShopTradeResultReceived` and `RaiseShopTradeResult(ShopTradeResultPayload)`.
- Several PlayMode test doubles in Player/Economy/NPCs/UI still model the deleted legacy API.

## Options

### Option 1: Keep obsolete shells

Pros:
- Lowest short-term compile risk for hidden callers.

Cons:
- Conflicts with existing tests that already assert the legacy surface is gone.
- Preserves duplicate contract shapes and parsing paths.
- Leaves more cleanup debt in test doubles.

### Option 2: Remove only `GameEvents`, keep legacy shop trade bridge

Pros:
- Smaller change than full cleanup.

Cons:
- Still conflicts with `InventoryEventContractsTests`.
- Leaves the shop runtime contract ambiguous.

### Option 3: Remove all non-save runtime-event legacy compatibility

Pros:
- Matches current architectural intent and existing tests.
- Leaves a single typed shop-trade contract.
- Shrinks maintenance surface in production and test doubles.

Cons:
- Requires updating any remaining fake/test `IShopEvents` implementations in affected domains.

## Recommendation

Choose Option 3. Delete `GameEvents.cs`, remove all legacy shop-trade compatibility members and parsing helpers, keep only the typed runtime hub contract, and update affected tests/fakes to compile against the typed interface only.

## Design

### Runtime contract

- Delete `Core/Scripts/Events/GameEvents.cs`.
- Keep `ShopTradeFailureReason` and `ShopTradeResultPayload`.
- Remove `ParseLegacyFailureReason` from `ShopTradeResultPayload`.
- Remove legacy event/method declarations from `IShopEvents`.
- Remove legacy bridging behavior from `DefaultRuntimeEvents`.

### Test updates

- Keep the existing contract assertions that legacy members are absent.
- Update direct test doubles under Player/Economy/NPCs/UI to implement only the typed `IShopEvents` surface.
- Keep runtime behavior tests focused on typed payload delivery and UI/menu state behavior.

### Verification

- Run scoped Unity EditMode tests for:
  - `Reloader.Core.Tests.EditMode.RuntimeEventHubBehaviorTests`
  - `Reloader.Core.Tests.EditMode.InventoryEventContractsTests`
- Run scoped Unity PlayMode tests for directly affected domains:
  - `Reloader.Economy.Tests.PlayMode.EconomyControllerCheckoutPlayModeTests`
  - `Reloader.UI.Tests.PlayMode.TradeUiToolkitPlayModeTests`
  - `Reloader.NPCs.Tests.PlayMode.ShopVendorInteractionPlayModeTests`
  - `Reloader.Player.Tests.PlayMode.PlayerControllerPlayModeTests`
- If Unity compilation is blocked by unrelated branch errors, report that blocker with the exact files/errors after running the commands.

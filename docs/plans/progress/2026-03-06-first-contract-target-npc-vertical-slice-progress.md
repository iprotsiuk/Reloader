# First Contract Target NPC Vertical Slice Progress

## Status

- [x] Design locked
- [x] Implementation plan written
- [x] Non-draft PR opened to `main`
- [x] `@codex` tagged for review

## Execution Checklist

- [x] Contract runtime controller landed
- [x] TAB `Contracts` section replaces `Quests`
- [x] Contract UI adapter wired through runtime bridge
- [x] Target NPC runtime identity landed
- [x] Contract target kill resolution landed
- [x] Police-heat payout gate landed
- [x] `MainTown` authored target slice landed
- [x] EditMode contract tests green
- [x] Contract-specific PlayMode bridge/target tests landed
- [x] Scene smoke tests green

## Notes

- Scope is intentionally narrow: one contract, one target NPC, one `MainTown` loop.
- Handler NPC intake, portraits, confiscation/respawn, and broad civilian spawning are deferred.
- Contracts intake is implemented inside the existing TAB shell by repointing the old `Quests` section to a runtime-rendered `Contracts` panel.
- The UI bridge now resolves a narrow contract snapshot/provider seam so the UI assembly does not depend directly on contract asset types.
- Contract runtime and target resolution no longer scene-scan for unrelated payout or elimination sinks; `MainTown` is authored with explicit references.
- The UI runtime bridge now prefers local device/target-selection controllers over scene-global fallbacks to keep contract/device flows deterministic.
- Review fix: `ContractEscapeResolutionRuntime` now requires a real target-elimination resolution before `Advance()` can complete the active contract.
- Review fix: `TabInventoryViewBinder` now rebinds the reused `inventory__contracts-accept` button to the current binder instance, so `UiToolkitScreenRuntimeBridge` teardown/rebind cycles do not strand the Contracts accept action.
- Review fix: `StaticContractRuntimeProvider` now snapshots and restores its live `ContractEscapeResolutionRuntime` state when `RuntimeKernelBootstrapper.EventsReconfigured` swaps event hubs, and `OnEnable` now refreshes that runtime binding on re-enable so disabled providers still publish police-heat changes into the current hub.
- Added `MainTownContractSlicePlayModeTests` as the authored world-scene smoke for the first contract loop; it boots `MainTown`, accepts the authored contract, eliminates the authored target, and verifies payout stays gated until the search timer clears.
- PR: `#25` `feat: first contract target NPC vertical slice`

## Verification

- Full EditMode suite: `307/307` passed
- Targeted regression: `Reloader.Core.Tests.EditMode.ContractEscapeResolutionRuntimeTests` `4/4` passed
- Targeted PlayMode regression: `Reloader.Core.Tests.PlayMode.StaticContractRuntimeProviderPlayModeTests` `3/3` passed
- Targeted PlayMode class: `Reloader.UI.Tests.PlayMode.TabInventoryContractsSectionPlayModeTests` `3/3` passed, including `Initialize_WhenContractsControlsAlreadyExist_RebindsAcceptButtonToCurrentBinder`
- Targeted PlayMode smoke: `Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests` `1/1` passed
- Full PlayMode suite remains red in unrelated preexisting subsystems (`Audio`, `Economy`, `Player`, `EscMenu`, broad `UI` flows)
- Latest full PlayMode failure list does not include:
  - `Reloader.UI.Tests.PlayMode.TabInventoryContractsBridgePlayModeTests.RuntimeBridge_BindTabInventory_AcceptsAvailableContractThroughContractsTab`
  - `Reloader.Core.Tests.PlayMode.StaticContractRuntimeProviderPlayModeTests.RuntimeKernelReconfigure_AfterAccept_PreservesActiveContractAndConsumedOffer`
  - `Reloader.Core.Tests.PlayMode.StaticContractRuntimeProviderPlayModeTests.RuntimeKernelReconfigure_DuringSearchClearWait_PreservesPendingPayoutProgress`
  - `Reloader.Core.Tests.PlayMode.StaticContractRuntimeProviderPlayModeTests.OnEnable_AfterHubSwapWhileDisabled_RebindsLawEnforcementEvents`
  - `Reloader.UI.Tests.PlayMode.TabInventoryContractsSectionPlayModeTests.Initialize_WhenContractsControlsAlreadyExist_RebindsAcceptButtonToCurrentBinder`
  - `Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests.MainTownContractSlice_AcceptsTargetEliminationAndAwardsPayoutAfterSearchClears`
  - `Reloader.Weapons.Tests.PlayMode.ContractTargetDamageablePlayModeTests`
  - `Reloader.UI.Tests.PlayMode.TabInventoryDeviceSectionPlayModeTests.Acceptance_DeviceFullLoop_ChooseTargetFireSaveClearReopenTab_PreservesSavedSessionAndClearsMarkers`
  - `Reloader.UI.Tests.PlayMode.UiRuntimeCutoverPlayModeTests`

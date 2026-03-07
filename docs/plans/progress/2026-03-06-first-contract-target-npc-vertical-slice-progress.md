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
- [ ] Scene smoke tests green

## Notes

- Scope is intentionally narrow: one contract, one target NPC, one `MainTown` loop.
- Handler NPC intake, portraits, confiscation/respawn, and broad civilian spawning are deferred.
- Contracts intake is implemented inside the existing TAB shell by repointing the old `Quests` section to a runtime-rendered `Contracts` panel.
- The UI bridge now resolves a narrow contract snapshot/provider seam so the UI assembly does not depend directly on contract asset types.
- Contract runtime and target resolution no longer scene-scan for unrelated payout or elimination sinks; `MainTown` is authored with explicit references.
- The UI runtime bridge now prefers local device/target-selection controllers over scene-global fallbacks to keep contract/device flows deterministic.
- PR: `#25` `feat: first contract target NPC vertical slice`

## Verification

- Full EditMode suite: `307/307` passed
- Full PlayMode suite remains red in unrelated preexisting subsystems (`Audio`, `Economy`, `Player`, `EscMenu`, broad `UI` flows)
- Latest full PlayMode failure list does not include:
  - `Reloader.UI.Tests.PlayMode.TabInventoryContractsBridgePlayModeTests.RuntimeBridge_BindTabInventory_AcceptsAvailableContractThroughContractsTab`
  - `Reloader.Weapons.Tests.PlayMode.ContractTargetDamageablePlayModeTests`
  - `Reloader.UI.Tests.PlayMode.TabInventoryDeviceSectionPlayModeTests.Acceptance_DeviceFullLoop_ChooseTargetFireSaveClearReopenTab_PreservesSavedSessionAndClearsMarkers`
  - `Reloader.UI.Tests.PlayMode.UiRuntimeCutoverPlayModeTests`

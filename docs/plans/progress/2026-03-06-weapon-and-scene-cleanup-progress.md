# Weapon And Scene Cleanup Progress

## Scope

- Supported authored weapons: `Kar98k (.308)` and `Canik TP9 (9mm)`
- Strict registry/runtime behavior: no hidden fallback lookup
- Scene parity: `MainTown` and `IndoorRange`
- MainTown acquisition authority: vendor catalogs + `StorageChest`, no seeded starter floor spawns
- Dropped-item visuals: no grey cube fallback in live runtime
- Active PR review feedback: `PoliceHeatController.ReportLineOfSightLost()` idempotence

## Status

- [x] Design approved
- [x] Implementation plan written
- [x] Docs committed before code changes
- [x] PR review thread rechecked
- [x] Police heat LOS-loss review comment fixed
- [x] WeaponRegistry strict-resolution cleanup complete
- [x] Tab inventory strict-resolution cleanup complete
- [x] PlayerWeaponController strict-resolution cleanup complete
- [x] Canik TP9 naming/content cleanup complete
- [x] Unsupported authored weapon content pruned
- [x] MainTown / IndoorRange parity cleanup complete
- [x] MainTown starter floor-spawn removal complete
- [x] StorageChest one-time grandpa kit seeding complete
- [x] Dropped-item visual cleanup complete
- [x] Sniper scope subsystem docs aligned to assassination-contract roadmap
- [x] Repo docs/ignore cleanup complete
- [x] Targeted verification complete
- [x] PR updated with progress/evidence

## Active PR Feedback

- `PR #24` police heat inline thread was replied to and resolved after `dc0a9d90`.

## Evidence So Far

- Docs commit: `b5154463` (`docs: add weapon and scene cleanup plan`)
- Live PR thread check confirmed one unresolved inline review comment on `PoliceHeatController.cs`
- Additional fallback audit confirmed `PlayerWeaponController` also rescues weapon definitions scene-wide and is now included in scope
- Police heat review fix commit: `dc0a9d90` (`fix: preserve police heat search countdown`)
- Police heat verification:
  - `PoliceHeatController_RepeatedLineOfSightLostWhileSearching_DoesNotRefreshCountdown`: red `0/1`, green `1/1`
  - `PoliceHeatControllerTests`: green `4/4`
- Registry/tab strict-resolution commit: `11127316` (`test: enforce strict weapon registry resolution`)
- Registry verification:
  - `WeaponRegistryFallbackResolutionTests`: red `1/2`, green `2/2`
- Tab inventory verification:
  - `TabInventoryAttachmentsPlayModeTests`: red `1/4`, green `4/4`
- Player weapon strict-resolution commit: `0e21d264` (`strict controller-bound weapon resolution`)
- Player weapon verification:
  - `MultipleRegistries_WhenAssignedRegistryMissesSelectedItem_DoesNotRescueFromOtherSceneRegistry`: red `0/1`, green `1/1`
  - targeted follow-up batch with `MissingLocalInputSource_StillEquips_FromSceneInputProvider`: green `2/2`
- Dropped-item runtime cleanup:
  - `PlayerInventoryControllerPlayModeTests`: red `26/31`, green `31/31`
  - live drop path now rejects missing item definitions and missing icon prefabs instead of spawning fallback cubes
- Supported weapon authority cleanup:
  - `WorldSceneContractValidatorEditModeTests.SupportedWeaponAuthority_UsesKar98kAndCanikTp9Only`: green `1/1`
  - `WorldSceneContractValidatorEditModeTests.ActivityInstanceScaffold_SeedsSupportedWeaponAuthoritySet`: green `1/1`
  - `RoundTripTravelPlayModeTests.MainTownAndIndoorRange_ShareSupportedWeaponIdsAndViewMappings`: green `1/1`
- MainTown acquisition cleanup:
  - `MainTownCombatWiringEditModeTests.MainTownScene_RemovesStarterFloorPickups_InFavorOfVendorAndChestAuthority`: green `1/1`
  - `WorldStorageContainerSeedLoadoutPlayModeTests`: green `2/2`
  - `StorageTransferEngineTests`: green `2/2`
- Fresh detached-worktree verification (`1c39c47f`):
  - `git diff --check`: passed
  - `storage-world-edit.xml`: green `10/10`
  - `storage-seed-play.xml`: green `2/2`
- Repo contract verification:
  - `bash scripts/verify-docs-and-context.sh`: passed
  - `bash scripts/verify-extensible-development-contracts.sh`: passed
  - `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`: passed
- PR update evidence:
  - progress comment: `https://github.com/iprotsiuk/Reloader/pull/24#issuecomment-4011173096`
  - inline review reply: `https://github.com/iprotsiuk/Reloader/pull/24#discussion_r2895291135`
  - police heat review thread resolved via GitHub GraphQL mutation on `2026-03-06`

## Notes

- Do not invent fake pistol attachment content in this pass.
- Do not broaden this into the later contract/law/NPC/world architecture rewrite.
- Starter access is now vendor/storage-driven; scene-floor starter pickups are intentionally removed.
- Keep this progress doc updated with commits, verification, and deferred follow-ups.

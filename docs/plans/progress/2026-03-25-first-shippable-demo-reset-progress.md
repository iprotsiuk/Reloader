# First Shippable Demo Reset Progress

## Scope

- Plan: `docs/plans/2026-03-25-first-shippable-demo-reset-implementation-plan.md`
- Design: `docs/plans/2026-03-25-first-shippable-demo-reset-design.md`
- Current slice: Tasks 1-5 integration hardening
- Status: focused demo verification, broader world/travel verification, touched world edit filters, and the menu-toggle asset contract check are green

## Task Checklist

- [x] Review the plan, design, and v0.1 milestone/status docs
- [x] Create a progress note for the first shippable demo reset
- [x] Keep the `MainTown` demo focus lock explicit in the v0.1 status board
- [x] Run the requested doc guardrails
- [x] Record validation results, blockers, and touched files
- [ ] Commit is intentionally deferred in this pass per task ownership instructions

## What This Task Locks In

- The demo loop being hardened: accept -> prep -> shoot -> escape -> claim payout
- One authored assassination demo path in `MainTown`
- A stable target exposure pattern with multiple plausible firing lanes/sightlines and at least one known-good reference setup for deterministic tests
- Existing contract-prep-escape seams as the demo spine
- `Contracts` tab intake, `PlayerDevice` validation, `PoliceHeatRuntime` payout gating, scoped `.308` rifle prep, and reloading coverage as the near-term working set
- Freeze on extra world expansion, floating-origin follow-on work, and broader sim breadth until the demo path is reproducible without editor intervention

## Task Files

- `docs/plans/progress/2026-03-25-first-shippable-demo-reset-progress.md`
- `docs/design/v0.1-demo-status-and-milestones.md`
- `Reloader/Assets/_Project/Core/Scripts/Runtime/ContractEscapeResolutionRuntime.cs`
- `Reloader/Assets/_Project/Core/Scripts/Runtime/StaticContractRuntimeProvider.cs`
- `Reloader/Assets/_Project/Core/Tests/EditMode/ContractEscapeResolutionRuntimeTests.cs`
- `Reloader/Assets/_Project/Core/Tests/PlayMode/StaticContractRuntimeProviderPlayModeTests.cs`
- `Reloader/Assets/_Project/Player/InputSystem_Actions.inputactions`
- `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs`
- `Reloader/Assets/_Project/PlayerDevice/Scripts/World/PlayerDeviceController.cs`
- `Reloader/Assets/_Project/PlayerDevice/Scripts/World/PlayerDeviceTargetSelectionController.cs`
- `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsBridgePlayModeTests.cs`
- `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryDeviceSectionPlayModeTests.cs`
- `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ContractTargetDamageablePlayModeTests.cs`
- `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/WorldTravelCoordinator.cs`
- `Reloader/Assets/_Project/World/Terrain/MainTown/MainTownTerrainData.asset`
- `Reloader/Assets/_Project/World/Terrain/MainTown/MainTown_Ocean.mat`
- `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`
- `Reloader/Assets/_Project/World/Tests/EditMode/WorldTravelCoordinatorEditModeTests.cs`
- `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`
- `Reloader/Assets/_Project/World/Tests/PlayMode/RoundTripTravelPlayModeTests.cs`

## Verification Log

- Focused demo edit/play verification is green: 17/17 edit, 41/42 play, 1 ignored.
- Broad world/travel rerun is green: `tmp/demo-reset-task4-broad-final.xml` -> 31/31 play, 0 failed.
- Touched world edit filters are green: `tmp/demo-reset-world-edit-final.xml` -> 36/36 edit, 0 failed.
- Direct player input-asset contract check is green: `tmp/menu-toggle-asset-contract-final.xml` -> 1/1 play, 0 failed.
- Final touched-domain checkpoint rerun is green:
  - `tmp/reset-checkpoint-edit.xml` -> 52/52 edit, 0 failed
  - `tmp/reset-checkpoint-play.xml` -> 71/72 play, 0 failed, 1 skipped
- Root-cause investigations isolated and fixed four seams:
  - stale MainTown contract-slice death assertions
  - broken `MainTownTerrainData.asset` import/regeneration
  - missing `MenuToggle` action in the player input asset
  - travel player-root handoff publishing success before canonical-root reposition was guaranteed
- Final world-side test cleanup also corrected one stale travel test simulation so it asserts the post-travel owned seam instead of synthetic keyboard delivery in that fixture.
- `bash scripts/verify-docs-and-context.sh`
  - PASS: scene persistence script placement check passed; all docs/context guardrails passed.
- `bash scripts/verify-extensible-development-contracts.sh`
  - PASS: all extensible development contract checks passed.
- `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`
  - PASS: repository guardrails, extensible development guardrails, local markdown links, protected router overlap, and ignore hygiene all passed.

## Blockers / Risks

- No known functional blockers remain in the targeted reset slice verification set.
- Remaining risk is broader-than-targeted regression outside the exercised reset filters; that has not been widened further in this pass.
- Checkpoint commit is the next integration step for this branch state.

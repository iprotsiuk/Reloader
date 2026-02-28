# World Object Persistence (Unified Store) Implementation Plan

> Status Pointer (2026-02-28): This is a planning/execution artifact. For live implemented-vs-planned status, use `docs/design/v0.1-demo-status-and-milestones.md`.


> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement a unified, policy-driven world-object persistence system with per-scene behavior (`Persistent` and `DailyReset`) using scene-path keys, including reclaim/lost-and-found for daily cleanup.

**Architecture:** Introduce stable object identity + one canonical runtime world-state service + one save module (`WorldObjectState`). Apply persisted state on scene load/travel, capture mutations at source (pickup/transform/destroy hooks), and enforce scene policy at day boundary. Keep Core save module payload-only and runtime scene scanning in persistence bridge layer.

**Tech Stack:** Unity C#, existing SaveEnvelope/SaveCoordinator pipeline, Json.NET, runtime scene events, existing inventory/weapon/world runtime systems.

---

## Status Update (2026-02-28)

This plan is an execution artifact for a slice that has been implemented.

Implemented outcomes:
- Unified world-object state module and migration support.
- Runtime apply bridge and pickup mutation capture.
- Daily reset cleanup with reclaim storage.
- Travel workaround removal in favor of unified persistence apply.
- Scene policy validation + docs sync.

Use `docs/design/v0.1-demo-status-and-milestones.md` and `docs/design/save-and-progression.md` as current status/contract sources.

---

### Task 1: Finalize contracts and policy types

**Files:**
- Create: `Reloader/Assets/_Project/Core/Scripts/Persistence/WorldObjectPersistenceMode.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Persistence/WorldScenePersistencePolicy.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Persistence/WorldObjectStateRecord.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Persistence/WorldObjectStateStore.cs`

**Step 1: Write failing tests**
- Add EditMode tests validating default policy behavior, store upsert semantics, and scene-path keying.

**Step 2: Run tests to verify failures**
- Run targeted EditMode tests; confirm red.

**Step 3: Implement minimal contracts/types**
- Add enums/classes/records only needed for failing tests.

**Step 4: Re-run tests and verify green**
- Ensure all new contract tests pass.

**Step 5: Commit**
- `feat(persistence): add world object policy and state contracts`

### Task 2: Add stable world-object identity component

**Files:**
- Create: `Reloader/Assets/_Project/Core/Scripts/Persistence/WorldObjectIdentity.cs`
- Modify: `Reloader/Assets/_Project/Inventory/Scripts/World/DefinitionPickupTarget.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/World/WeaponPickupTarget.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/World/AmmoStackPickupTarget.cs`
- Test: new PlayMode/EditMode identity tests

**Step 1: Write failing tests**
- Assert each pickup type exposes stable identity (`objectId` non-empty and stable across enable/disable).

**Step 2: Run tests to verify red**

**Step 3: Implement identity component and wire pickup targets**
- Require/resolve identity on relevant targets.

**Step 4: Re-run tests and verify green**

**Step 5: Commit**
- `feat(persistence): add stable world object identity for pickups`

### Task 3: Implement world-object save module

**Files:**
- Create: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/WorldObjectStateModule.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SaveBootstrapper.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/Migrations/*` (new `v1->v2` migration)
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SaveCoordinator.cs` (if schema/version wiring needed)
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/*WorldObjectState*Tests.cs`

**Step 1: Write failing tests**
- Save/load includes required new module.
- Migration injects empty module for v1 saves.
- Module restore tolerates empty payload internals.

**Step 2: Run tests to verify failures**

**Step 3: Implement module + migration + bootstrap registration**

**Step 4: Re-run tests and verify green**

**Step 5: Commit**
- `feat(save): add world object state module with v1->v2 migration`

### Task 4: Build runtime persistence bridge and scene apply

**Files:**
- Create: `Reloader/Assets/_Project/Core/Scripts/Persistence/WorldObjectPersistenceRuntimeBridge.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Persistence/WorldScenePolicyRegistry.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Persistence/WorldObjectStateApplyService.cs`
- Modify: scene bootstrap wiring (where runtime bridge is initialized)
- Test: PlayMode scene load/apply tests

**Step 1: Write failing tests**
- Given saved consumed/destroyed/transform states, entering scene applies exact expected runtime object state.

**Step 2: Run tests to verify failures**

**Step 3: Implement bridge + apply logic**
- Use scene path as canonical key.

**Step 4: Re-run tests and verify green**

**Step 5: Commit**
- `feat(persistence): apply world object state on scene load`

### Task 5: Capture mutations at source (pickup and future hooks)

**Files:**
- Modify: `Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs`
- Modify: pickup targets as needed
- Create/modify: mutation event helpers under `Core/Scripts/Persistence/**`
- Test: PlayMode pickup persistence tests

**Step 1: Write failing tests**
- Successful pickup marks object consumed in state store and persists across travel.

**Step 2: Run tests to verify failures**

**Step 3: Implement mutation capture for pickup flow**
- Mark consumed using `scenePath + objectId` before/at deactivation.

**Step 4: Re-run tests and verify green**

**Step 5: Commit**
- `feat(persistence): capture pickup consumption into world object state`

### Task 6: Add day-boundary cleanup + reclaim storage for DailyReset

**Files:**
- Create: `Reloader/Assets/_Project/Core/Scripts/Persistence/ReclaimStorageService.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Persistence/WorldCleanupService.cs`
- Modify: day-advance/core-world integration points
- Modify: world object save module payload if reclaim block required
- Test: PlayMode daily reset/retain tests

**Step 1: Write failing tests**
- `DailyReset` scene retains same-day changes.
- On day change, scene state is cleaned.
- Cleaned items appear in reclaim storage.
- `Persistent` scenes unaffected by day change.

**Step 2: Run tests to verify failures**

**Step 3: Implement cleanup + reclaim flow**

**Step 4: Re-run tests and verify green**

**Step 5: Commit**
- `feat(persistence): add daily reset cleanup with reclaim storage`

### Task 7: Remove temporary travel workaround and align travel behavior

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/WorldTravelCoordinator.cs`
- Test: `Reloader/Assets/_Project/World/Tests/PlayMode/RoundTripTravelPlayModeTests.cs`

**Step 1: Write failing tests**
- Ensure no itemId ownership heuristic is required for respawn correctness.

**Step 2: Run tests to verify failures**

**Step 3: Remove workaround and rely on unified store apply**

**Step 4: Re-run tests and verify green**

**Step 5: Commit**
- `refactor(travel): remove owned-item pickup hide workaround`

### Task 8: Policy authoring and validation tooling

**Files:**
- Create: `Reloader/Assets/_Project/World/Editor/WorldScenePersistencePolicyValidator.cs`
- Create/update: policy assets for active scenes (`MainTown`, `IndoorRangeInstance`, others)
- Test: EditMode validator tests

**Step 1: Write failing tests**
- Validator fails when scene lacks policy.
- Validator fails on duplicate scenePath policies.

**Step 2: Run tests to verify failures**

**Step 3: Implement validator + baseline policy assets**

**Step 4: Re-run tests and verify green**

**Step 5: Commit**
- `feat(world): add scene persistence policy validation`

### Task 9: Verification sweep and docs sync

**Files:**
- Modify: `docs/design/save-and-progression.md`
- Modify: `docs/design/save-contract-quick-reference.md`
- Modify: `docs/design/world-scene-contracts.md`
- Add/update: any migration notes/changelog docs

**Step 1: Run full targeted test suites**
- Core Save EditMode tests
- World PlayMode travel/persistence tests
- Inventory/Weapon tests impacted by pickup flow

**Step 2: Fix regressions found in verification**

**Step 3: Update design docs to reflect new canonical architecture**

**Step 4: Re-run verification and ensure green**

**Step 5: Commit**
- `docs: update persistence and save contracts for unified world object state`

## Verification Checklist
1. Picked item in `Persistent` scene never respawns unless explicitly reintroduced.
2. `DailyReset` scenes preserve same-day modifications and clean at next day.
3. Cleaned items are reclaimable (not silently lost).
4. Save/load roundtrip preserves unified object states.
5. Older schema saves migrate safely.
6. Travel works without ownership-based hacks.

## Risks / Watch Items
1. Save schema compatibility (module registration strictness).
2. Assembly boundaries between Core save code and scene-specific runtime objects.
3. Identity assignment for existing scene content.
4. Cleanup ordering at day boundary vs. scene load timing.

## Suggested Execution Order (Historical)
1. Tasks 1-3 (core contracts + identity + save module)
2. Tasks 4-5 (runtime apply + pickup capture)
3. Tasks 6-7 (daily reset + cleanup + remove workaround)
4. Tasks 8-9 (validation tooling + docs + verification)

This section is preserved as historical execution sequencing context.

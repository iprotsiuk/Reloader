# Kar98k Generic Attachments Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Ship a generic inventory-backed attachment framework and wire `weapon-rifle-01` to `WWII_Recon_A_PreSet` with functional scope/muzzle swapping via TAB attachments UI.

**Architecture:** Build a project-owned generic attachment layer in `_Project/Weapons` (data + runtime + UI) and bridge selected attachments into existing `Assets/Game/Weapons` attachment/ADS runtime. Keep camera-authoritative ADS unchanged and avoid per-weapon special-case logic.

**Tech Stack:** Unity C#, ScriptableObjects, existing `_Project` inventory/weapon runtime, existing `Assets/Game/Weapons` ADS/attachment runtime, Unity PlayMode tests.

---

### Task 1: Add generic attachment domain contracts (data + runtime state)

**Files:**
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Data/WeaponAttachmentSlotType.cs`
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Data/WeaponAttachmentCompatibility.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Data/WeaponDefinition.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponRuntimeState.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponRuntimeStatePlayModeTests.cs`

**Step 1: Write the failing test**
- Add PlayMode tests for:
  - retrieving compatible ids per slot from weapon definition,
  - storing/getting equipped attachment item id per slot in runtime state,
  - deterministic defaults (empty when not configured).

**Step 2: Run test to verify it fails**
- Run: `Unity PlayMode tests for WeaponRuntimeStatePlayModeTests`
- Expected: compile/test failure for missing contracts/APIs.

**Step 3: Write minimal implementation**
- Add slot enum/type + compatibility container.
- Extend `WeaponDefinition` serialized fields + accessors.
- Extend runtime state with slot->equipped item id map.

**Step 4: Run test to verify it passes**
- Run same PlayMode test target.
- Expected: new tests pass.

**Step 5: Commit**
- `feat(weapons): add generic attachment slot compatibility contracts`

### Task 2: Add attachment inventory metadata + swap transaction service

**Files:**
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponAttachmentItemMetadata.cs`
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponAttachmentSwapService.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`

**Step 1: Write the failing test**
- Add PlayMode tests for atomic swap:
  - rejects unknown/incompatible/unowned attachment,
  - removes new item from inventory,
  - returns previous attachment item to inventory,
  - updates runtime equipped attachment for slot.

**Step 2: Run test to verify it fails**
- Run targeted PlayMode tests for `PlayerWeaponControllerPlayModeTests`.
- Expected: failing assertions and/or missing APIs.

**Step 3: Write minimal implementation**
- Implement swap service with validation and atomic mutation.
- Add controller method used by UI to request swap on equipped weapon.

**Step 4: Run test to verify it passes**
- Run same targeted tests.
- Expected: tests pass.

**Step 5: Commit**
- `feat(weapons): add inventory-backed attachment swap transaction`

### Task 3: Bridge equipped attachments to runtime view/ADS

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/AttachmentManager.cs` (only if API extension needed and backwards-compatible)
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/MuzzleAttachmentRuntimePlayModeTests.cs`

**Step 1: Write the failing test**
- Add tests that selected scope/muzzle item ids resolve into runtime definition equip calls and hot-swap behavior.
- Verify scope swap updates active sight anchor/mask policy without resetting camera-authoritative ADS.

**Step 2: Run test to verify it fails**
- Run targeted PlayMode tests above.
- Expected: failures in equip/hot-swap assertions.

**Step 3: Write minimal implementation**
- Map equipped attachment item ids to runtime attachment definitions.
- Apply to view on equip and on swap.
- Keep deterministic selection; no random fallback.

**Step 4: Run test to verify it passes**
- Run targeted tests again.
- Expected: tests pass.

**Step 5: Commit**
- `feat(weapons): bridge generic attachment state into ADS and muzzle runtime`

### Task 4: Implement TAB context action and attachments window UI

**Files:**
- Create: `Reloader/Assets/_Project/UI/Scripts/Inventory/WeaponAttachmentsPanelController.cs` (or project-equivalent UI path)
- Modify: TAB inventory context-menu scripts under `Reloader/Assets/_Project/UI/Scripts/**`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/*Attachments*.cs` (new/updated)

**Step 1: Write the failing test**
- Add UI tests for:
  - `Attachments` action visible only for weapon items,
  - panel opens for selected weapon item,
  - slot dropdown entries filtered by compatibility + ownership.

**Step 2: Run test to verify it fails**
- Run targeted UI PlayMode tests.
- Expected: missing action/panel/filter behavior failures.

**Step 3: Write minimal implementation**
- Add context-menu action.
- Add reusable attachments panel with dynamic slot rows and immediate swap callback.

**Step 4: Run test to verify it passes**
- Run same targeted tests.
- Expected: tests pass.

**Step 5: Commit**
- `feat(ui): add weapon attachments panel in tab inventory`

### Task 5: Wire Kar98k demo rifle content + compatibility data

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Data/Weapons/StarterRifle.asset`
- Create/Modify: project-owned attachment metadata assets under `Reloader/Assets/_Project/Weapons/Data/**`
- Modify: `Reloader/Assets/_Project/Weapons/Editor/WeaponsSceneWiring.cs` (if needed)
- Modify: `Reloader/Assets/_Project/Weapons/Prefabs/**` wrapper/runtime prefab assets (project-owned only)

**Step 1: Write the failing test**
- Add integration PlayMode test for `weapon-rifle-01`:
  - view resolves to `WWII_Recon_A_PreSet` wrapper path,
  - compatible scope/muzzle list exists,
  - swap updates equipped view anchor/muzzle visual.

**Step 2: Run test to verify it fails**
- Run targeted rifle integration test.
- Expected: missing/incorrect content wiring failures.

**Step 3: Write minimal implementation**
- Wire demo rifle view/content.
- Configure compatibility lists for scope and muzzle.
- Keep ammo id `.308` path unchanged.

**Step 4: Run test to verify it passes**
- Run same targeted integration tests.
- Expected: pass.

**Step 5: Commit**
- `feat(content): wire kar98k demo rifle with scope and muzzle compatibility`

### Task 6: Documentation + validation log updates

**Files:**
- Modify: `docs/plans/2026-03-04-combat-audio-and-attachments-validation.md`
- Create: `docs/plans/2026-03-04-kar98k-generic-attachments-validation.md`

**Step 1: Write failing docs check (manual)**
- Define required sections and expected verification records.

**Step 2: Run verification**
- Execute targeted test commands used above; capture exact pass/fail output summaries.

**Step 3: Write docs**
- Record implemented behavior, commands run, outcomes, and known limitations/risks.

**Step 4: Re-verify references**
- Confirm docs match current asset/script paths.

**Step 5: Commit**
- `docs: add kar98k generic attachments validation notes`


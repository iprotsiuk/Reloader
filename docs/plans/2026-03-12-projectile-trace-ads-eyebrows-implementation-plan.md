# Projectile Trace, Scoped ADS Resync, and NPC Eyebrows Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make dev traces render the exact simulated projectile path, make scoped weapons restore/equip into live scoped ADS correctly, and add STYLE eyebrow support as first-class NPC appearance data.

**Architecture:** Keep exact path ownership inside `WeaponProjectile`, centralize runtime attachment-to-view projection in `PlayerWeaponController`, and add a dedicated eyebrow field to the civilian appearance model instead of overloading `OutfitBottomId`.

**Tech Stack:** Unity C#, NUnit, Unity PlayMode/EditMode tests, runtime save modules, UI/debug runtime.

---

### Task 1: Exact Projectile Trace

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/WeaponProjectile.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevTraceRuntime.cs`
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevTraceSegmentView.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponProjectilePlayModeTests.cs`
- Test: `Reloader/Assets/_Project/DevTools/Tests/PlayMode/DevTraceRuntimePlayModeTests.cs`

**Step 1: Write the failing tests**

Add focused tests proving:
- projectile path reporting contains multiple exact segments for a curved shot
- projectile expiry without hit reports its real terminal point
- dev trace runtime renders a polyline path instead of a 2-point straight fallback

**Step 2: Run the focused trace tests to verify RED**

Run the projectile and dev trace slices and confirm failure because the current runtime only knows fire origin plus hit point.

**Step 3: Write the minimal implementation**

Introduce a narrow projectile-path observer seam, emit exact segments and terminal events from `WeaponProjectile`, pass the observer from `PlayerWeaponController` when dev tracing is active, and let `DevTraceRuntime` render one polyline per projectile id.

**Step 4: Run the focused trace tests to verify GREEN**

Re-run the projectile + dev trace slice and confirm the exact-path assertions pass.

### Task 2: Scoped ADS Runtime Resync

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandExecutors/DevGiveItemCommand.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/World/Tests/PlayMode/RoundTripTravelPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/DevTools/Tests/PlayMode/DevGiveItemCommandPlayModeTests.cs`

**Step 1: Write the failing tests**

Add tests proving:
- applying scoped runtime attachment state to an equipped Kar98k produces a live active optic before any manual swap
- scoped state survives travel to `IndoorRangeInstance` and still drives scoped ADS
- `give test` grants rifle + ammo only and still equips a scoped, loaded rifle

**Step 2: Run the focused scoped-ADS tests to verify RED**

Run the narrow weapon/travel/devtools slice and confirm failure on missing live optic / duplicate scope expectations.

**Step 3: Write the minimal implementation**

Add one authoritative sync seam in `PlayerWeaponController` that rebuilds the equipped view and ADS bridges from current runtime attachment state. Route restore/seed/equip paths through it. Update `give test` to grant only the rifle and ammo and seed the rifle from authored default optic state.

**Step 4: Run the focused scoped-ADS tests to verify GREEN**

Re-run the narrow weapon/travel/devtools slice and confirm live scoped ADS works on first RMB after restore/equip.

### Task 3: NPC Eyebrows as First-Class Appearance Data

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/CivilianPopulationRecord.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/CivilianPopulationModule.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Generation/CivilianAppearanceLibrary.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Generation/CivilianAppearanceGenerator.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Generation/MainTownCuratedAppearanceRules.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/MainTownNpcAppearanceApplicator.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianAppearanceGeneratorTests.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/MainTownNpcAppearanceApplicatorEditModeTests.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/CivilianPopulationSaveModuleTests.cs`

**Step 1: Write the failing tests**

Add tests proving:
- generated civilian records carry an eyebrow id
- save-module round-trips preserve it
- the applicator activates the correct `brous*` child on male and female STYLE roots

**Step 2: Run the focused eyebrow tests to verify RED**

Run the editmode generator/applicator/save slice and confirm failure because there is no eyebrow field yet.

**Step 3: Write the minimal implementation**

Add a dedicated eyebrow field through the civilian appearance data model, populate it in generation and seeded authoring appearance, propagate it through save/runtime bridge copies, and activate the correct STYLE eyebrow objects in `MainTownNpcAppearanceApplicator`.

**Step 4: Run the focused eyebrow tests to verify GREEN**

Re-run the focused NPC editmode slice and confirm eyebrows are now part of the authored/runtime appearance contract.

### Task 4: Narrow Regression Verification

**Files:**
- Verify only

**Step 1: Run the combined regression slices**

Run the relevant projectile/devtools, weapon/travel, and NPC editmode slices together, or as narrow grouped batches if the Unity runner is busy, and confirm all targeted regressions are green.

**Step 2: Run repo guardrails relevant to docs/contracts**

Run:

```bash
scripts/verify-docs-and-context.sh
scripts/verify-extensible-development-contracts.sh
.agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

Expected: PASS.

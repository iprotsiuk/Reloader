# Specialty Ammo Light Cover Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add the first .308 specialty-ammo proof point: AP/light-cover ammunition can penetrate explicitly marked light cover while default factory FMJ cannot.

**Architecture:** Extend the existing ammo snapshot and projectile impact payload with one penetration scalar. Keep cover behavior explicit through a lightweight marker component instead of a material database. Reuse the existing contract briefing text path for player-facing intel.

**Tech Stack:** Unity 6.3 C#, ScriptableObject inventory assets, NUnit EditMode/PlayMode tests, `scripts/run-unity-tests.sh`.

---

## Task 1: Thread Cover Penetration Through Ammo State

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/AmmoBallisticSnapshot.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/CartridgeBallisticSpec.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/CartridgeBallisticSpecBuilder.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/ProjectileImpactPayload.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/WeaponsModule.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponsRuntimeSaveBridge.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/WeaponsRuntimeSaveBridgeTests.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/EditMode/ProjectileImpactPayloadEditModeTests.cs`

**Step 1: Write failing tests**

Add tests proving:

- factory snapshots default `CoverPenetrationPower` to `0`
- a custom snapshot/spec/payload carries `CoverPenetrationPower = 1`
- `WeaponsRuntimeSaveBridge` saves and restores `CoverPenetrationPower`

**Step 2: Run focused failing tests**

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Weapons.Tests.EditMode.ProjectileImpactPayloadEditModeTests|Reloader.Core.Tests.EditMode.WeaponsRuntimeSaveBridgeTests" "$(pwd)/tmp/specialty-ammo-state-editmode.xml" "$(pwd)/tmp/specialty-ammo-state-editmode.log"
```

Expected: fail before implementation because the scalar does not exist.

**Step 3: Implement the scalar**

Add `CoverPenetrationPower` as a non-negative float or int-like float. Keep constructor overloads backward-compatible by defaulting to `0`.

**Step 4: Re-run focused tests**

Expected: pass.

## Task 2: Add Factory FMJ And AP Ammo Defaults

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponAmmoDefaults.cs`
- Modify/Create: `Reloader/Assets/_Project/Inventory/Data/Items/Cartridge_308_150_AP.asset`
- Modify/Create meta as required by Unity asset creation
- Test: `Reloader/Assets/_Project/Weapons/Tests/EditMode/ProjectileImpactPayloadEditModeTests.cs`
- Test: nearest inventory item definition edit-mode test if one exists

**Step 1: Write failing tests**

Add tests proving:

- `ammo-factory-308-147-fmj` has `CoverPenetrationPower = 0`
- `ammo-specialty-308-150-ap` resolves to `150 gr`, roughly factory `.308` velocity/BC, and `CoverPenetrationPower = 1`

**Step 2: Implement defaults and content**

Add one AP item id:

```text
ammo-specialty-308-150-ap
```

Keep the content minimal: item id, display name, category/type matching existing ammunition assets, and enough catalog compatibility for tests/runtime lookup.

**Step 3: Run focused tests**

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Weapons.Tests.EditMode.ProjectileImpactPayloadEditModeTests" "$(pwd)/tmp/specialty-ammo-defaults-editmode.xml" "$(pwd)/tmp/specialty-ammo-defaults-editmode.log"
```

Expected: pass.

## Task 3: Implement Explicit Light Cover Penetration

**Files:**
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/LightCoverPenetrable.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/WeaponProjectile.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponProjectilePlayModeTests.cs`

**Step 1: Write failing PlayMode tests**

Add two focused tests:

- FMJ projectile hits a `LightCoverPenetrable` box and never damages the target behind it.
- AP projectile hits the same cover, continues through once, and damages the target with lower delivered energy than an unobstructed hit.

**Step 2: Run tests and verify failure**

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.Weapons.Tests.PlayMode.WeaponProjectilePlayModeTests" "$(pwd)/tmp/specialty-ammo-cover-playmode.xml" "$(pwd)/tmp/specialty-ammo-cover-playmode.log"
```

Expected: fail before implementation.

**Step 3: Implement minimal pass-through**

In `WeaponProjectile`, when the first hit has `LightCoverPenetrable`:

- if `CoverPenetrationPower < requiredPenetrationPower`, keep current behavior and stop
- otherwise reduce speed/energy by `energyRetentionMultiplier`
- move the next ray origin beyond the cover by `exitOffsetMeters`
- allow only one cover penetration in this first slice

Do not build generic material lookup.

**Step 4: Re-run PlayMode tests**

Expected: pass.

## Task 4: Add Contract Briefing Proof Point

**Files:**
- Modify: `Reloader/Assets/_Project/Contracts/Data/MainTown_FirstContract.asset`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsSectionPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsBridgePlayModeTests.cs`

**Step 1: Write/update UI text test**

Add or update a test asserting a contract briefing containing a light-cover/AP hint flows into the existing contracts tab briefing/intel surfaces.

**Step 2: Update authored briefing**

Keep the note short and actionable. Example:

```text
Target may stay behind office glass. Standard ball can lose the shot; AP .308 is the clean solution.
```

**Step 3: Run targeted UI tests**

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.UI.Tests.PlayMode.TabInventoryContractsSectionPlayModeTests|Reloader.UI.Tests.PlayMode.TabInventoryContractsBridgePlayModeTests" "$(pwd)/tmp/specialty-ammo-contract-ui-playmode.xml" "$(pwd)/tmp/specialty-ammo-contract-ui-playmode.log"
```

Expected: pass.

## Task 5: Docs, Review, And Integration

**Files:**
- Modify: `docs/design/weapons-and-ballistics.md`
- Modify: `docs/design/assassination-contracts.md`
- Modify: `docs/design/v0.1-demo-status-and-milestones.md`

**Step 1: Update docs**

Document:

- one AP/light-cover specialty ammo exists
- cover penetration is explicit marker-based
- no generic material database exists yet
- the first contract hint uses existing briefing surfaces

**Step 2: Run guardrails**

```bash
git diff --check
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
```

Expected: pass.

**Step 3: Run final focused verification**

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Weapons.Tests.EditMode.ProjectileImpactPayloadEditModeTests|Reloader.Core.Tests.EditMode.WeaponsRuntimeSaveBridgeTests" "$(pwd)/tmp/specialty-ammo-final-editmode.xml" "$(pwd)/tmp/specialty-ammo-final-editmode.log"
bash scripts/run-unity-tests.sh playmode "Reloader.Weapons.Tests.PlayMode.WeaponProjectilePlayModeTests|Reloader.UI.Tests.PlayMode.TabInventoryContractsSectionPlayModeTests|Reloader.UI.Tests.PlayMode.TabInventoryContractsBridgePlayModeTests" "$(pwd)/tmp/specialty-ammo-final-playmode.xml" "$(pwd)/tmp/specialty-ammo-final-playmode.log"
```

Expected: pass.

**Step 4: Commit and push**

```bash
git add <changed files>
git commit -m "feat: add specialty ammo light cover penetration"
git push origin main
```

Expected: `main` is clean and pushed.

# Weapon Animation Contract Design v1

> Status Pointer (2026-02-28): This is a planning/execution artifact. For live implemented-vs-planned status, use `docs/design/v0.1-demo-status-and-milestones.md`.

> Migration Update (2026-03-04): FPS ADS/optic alignment is implemented via `Reloader/Assets/Game/Weapons/**` (code-driven camera-authoritative model). Keep this document as historical contract-design context and use `docs/design/ads-optics-framework.md` for active ADS/scope implementation behavior.


**Date:** 2026-02-25  
**Scope:** Reusable FPS-viewmodel weapon animation architecture that scales to multiple weapons now and world-character adapters later.

## Goals

- Define a stable animation contract independent of specific weapon rigs.
- Keep gameplay authoritative and animation event-driven.
- Support interruptible reload and ADS movement slowdown in v1.
- Enable per-weapon and per-character overrides without controller explosion.

## Non-Goals (v1)

- Implement world-character (NPC/police/range) animation adapters now.
- Ship staged per-shell reload choreography for all weapon classes.
- Couple animation timing as hard gameplay authority (animation remains presentation-driven).

## Final Direction

Use a hybrid contract-based architecture:

1. Shared base animation contract and animator schema for all weapons.
2. Profile-driven overrides for weapon families and character archetypes.
3. Optional custom rig modules per weapon as an extension path.

## Architecture

### 1. Contract Layers

- **Gameplay contract (authoritative):** weapon and movement systems emit lifecycle events.
- **Animation contract (stable API):** adapters map gameplay events to animator parameters/triggers.
- **Rig contract (binding points):** required transforms and IK targets validated at equip time.

### 2. Runtime Components

- `ViewmodelAnimationAdapter`
  - Subscribes to weapon/movement events.
  - Sets animator parameters/triggers only.
  - No direct fire/reload authority.
- `ViewmodelRigAdapter`
  - Applies IK/constraint values from resolved profiles.
  - Handles left-hand lock and ADS alignment offsets.
- `ViewmodelProfileResolver`
  - Resolves effective profile stack: weapon override -> family default -> global default.
- `ViewmodelBindingResolver`
  - Resolves and caches required transforms.
  - Emits one warning per missing optional bind point; hard-fails only required points.
- `ViewmodelRecoilDriver`
  - Applies additive recoil channels from fire events and profile curves.

### 3. Data Assets

- `AnimationContractProfile` (global)
  - Contract version (`major.minor`).
  - Canonical parameter/trigger names.
  - Required and optional event names.
- `WeaponAnimationProfile` (per weapon family or weapon)
  - Clip set references for base states.
  - ADS timings and movement multiplier.
  - Recoil and IK offset settings.
  - Optional custom rig module reference.
- `CharacterViewmodelProfile` (per archetype)
  - Arm proportion offsets, additive pose offsets, IK default weights.
  - Per-weapon-family offset overrides.

## Gameplay Event Contract v1

Extend event surface with explicit lifecycle events:

- `OnWeaponEquipStarted(string itemId)`
- `OnWeaponEquipped(string itemId)` (already present)
- `OnWeaponUnequipStarted(string itemId)`
- `OnWeaponFired(string itemId, Vector3 origin, Vector3 direction)` (already present)
- `OnWeaponReloadStarted(string itemId)`
- `OnWeaponReloadCancelled(string itemId, WeaponReloadCancelReason reason)`
- `OnWeaponReloaded(string itemId, int magazineCount, int reserveCount)` (already present)
- `OnWeaponAimChanged(string itemId, bool isAiming)`

`WeaponReloadCancelReason` v1:

- `Sprint`
- `Unequip`
- `DryStateInvalidated`
- `InterruptedByAction`

## State Priority Rules v1

Priority order:

1. `Unequip`
2. `Sprint`
3. `Reload`
4. `Aim`
5. `Fire`
6. `Move`

Rules:

- Sprint during reload cancels reload and raises `ReloadCancelled(Sprint)`.
- Equip/unequip interrupts ADS and reload.
- Fire is blocked by reload/sprint/unequip states.
- Animation mirrors authoritative gameplay state; it does not infer hidden states from local input.

## Animator Contract v1

### Required Parameters

- `MoveSpeed01` (float, normalized `0..1`)
- `AimWeight` (float, normalized `0..1`)
- `IsAiming` (bool)
- `IsReloading` (bool)
- `IsSprinting` (bool)
- `Fire` (trigger)
- `Reload` (trigger)
- `Equip` (trigger)
- `Unequip` (trigger)

### Optional Extension Parameters

- `Inspect` (trigger)
- `ReloadStage` (int)
- `RecoilAdditive` (float)

## Movement Signal Spec v1

To avoid profile portability issues, define normalization explicitly:

- `MoveSpeed01 = clamp(horizontalSpeed / referenceMaxSpeed, 0, 1)`
- `referenceMaxSpeed = max(WalkSpeed, SprintSpeed)` from movement settings.

ADS effect in gameplay (authoritative):

- `effectiveMoveSpeed = baseMoveSpeed * (isAiming ? adsSpeedMultiplier : 1f)`
- `adsSpeedMultiplier` sourced from `WeaponAnimationProfile`, default `0.7`.

## Rig Binding Contract v1

### Required Weapon Bind Points

- `Muzzle`
- `RightHandGrip`
- `LeftHandIKTarget`
- `AimReference`

### Optional Weapon Bind Points

- `EjectPort`
- `MagazineAttach`

### Required Viewmodel Rig Bind Points

- `RightHandBone`
- `LeftHandBone`
- `RightHandHint`
- `LeftHandHint`
- `WeaponAnchor`

## Validation Strategy

Add `AnimationContractValidator` editor utility with severity levels:

- **Error:** missing required animator parameter, missing required bind point, contract major-version mismatch.
- **Warning:** missing optional bind point, missing optional extension parameter/event.
- **Info:** fallback profile applied.

Validation runs:

- Manual menu action for all relevant prefabs/assets.
- Auto-run in content build tooling for weapon prefab generation.

## Migration Plan From Current Runtime

1. Keep `FpsViewmodelAnimatorDriver` initially; migrate output to `MoveSpeed01` normalized contract.
2. Introduce `ViewmodelAnimationAdapter` subscribed to `IWeaponEvents` (via `RuntimeKernelBootstrapper.WeaponEvents`).
3. Add new weapon lifecycle events in `IWeaponEvents` and publish them from weapon controller flow.
4. Introduce `AnimationContractProfile` and move hardcoded parameter names there.
5. Add first `WeaponAnimationProfile` as family default (`Rifle_Generic`) to avoid one-off logic.
6. Add contract validator before scaling content packs.

## Test Strategy

### EditMode

- Contract profile validates required parameter/event names.
- Profile resolver fallback chain returns deterministic result.
- Binding resolver emits expected severities for required vs optional points.
- Event adapter maps event payloads to animator hashes correctly.

### PlayMode

- Equip triggers equip animation state for selected weapon item.
- Reload start sets `IsReloading`; sprint cancels reload and sets cancelled flow.
- ADS toggles `IsAiming` and reduces movement speed by profile multiplier.
- Fire events trigger recoil and fire animation only when state allows.
- Multiple weapon profiles swap without controller replacement.

## Compatibility Note (Long-Term)

`WorldCharacterAnimationAdapter` is a planned adapter using the same gameplay event contract and profile schema. This avoids refactoring weapon logic when police/range/NPC armed actors are introduced.

## Success Criteria

- New weapons can reuse shared controller and contract without code changes.
- ADS slowdown and interruptible reload work with deterministic event flow.
- Missing content bindings fail predictably through validation, not runtime surprises.
- v1 FPS implementation remains compatible with future world-character adapters.

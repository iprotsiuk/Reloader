# Weapon Animation Contract v1 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement a reusable FPS viewmodel animation contract that supports interruptible reload and ADS movement slowdown across all current and future weapons.

**Architecture:** Extend gameplay-side weapon/movement contracts first (events + state flow), then add animation-side adapters/profiles that consume only those contracts. Keep one shared animator schema and bind-point contract, with profile-based overrides and deterministic fallback/validation so content scales without controller rewrites.

**Tech Stack:** Unity 6.3, C#, Unity Animator (Mecanim), Animation Rigging-ready bind contracts, NUnit EditMode/PlayMode tests, existing `GameEvents`, `PlayerMover`, `PlayerWeaponController`.

---

### Task 1: Add Weapon Animation Lifecycle Event Contracts

**Files:**
- Create: `Reloader/Assets/_Project/Core/Scripts/Events/WeaponEventsTypes.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Events/GameEvents.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/InventoryEventContractsTests.cs`

**Step 1: Write the failing test**

- Add EditMode tests for new event contracts:
  - `OnWeaponEquipStarted`
  - `OnWeaponUnequipStarted`
  - `OnWeaponReloadStarted`
  - `OnWeaponReloadCancelled`
  - `OnWeaponAimChanged`
- Add payload assertions including `WeaponReloadCancelReason`.

**Step 2: Run test to verify it fails**

Run:
```bash
UNITY_EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity}"
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform editmode -testResults "$(pwd)/.tmp/weapon-anim-task1.xml" -testFilter "Reloader.Core.Tests.EditMode.InventoryEventContractsTests" -quit
```
Expected: FAIL due to missing event members/types.

**Step 3: Write minimal implementation**

- Add enum:
```csharp
public enum WeaponReloadCancelReason
{
    Sprint = 0,
    Unequip = 1,
    DryStateInvalidated = 2,
    InterruptedByAction = 3
}
```
- Add event declarations + `Raise*` methods in `GameEvents`.

**Step 4: Run test to verify it passes**

Run the same command from Step 2.  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Events/WeaponEventsTypes.cs \
  Reloader/Assets/_Project/Core/Scripts/Events/GameEvents.cs \
  Reloader/Assets/_Project/Core/Tests/EditMode/InventoryEventContractsTests.cs
git commit -m "feat: add weapon animation lifecycle event contracts"
```

### Task 2: Add ADS Input Contract and Movement Slowdown

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Scripts/IPlayerInputSource.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerInputReader.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerMover.cs`
- Modify: `Reloader/Assets/_Project/Player/InputSystem_Actions.inputactions`
- Test: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerInventoryControllerPlayModeTests.cs`

**Step 1: Write the failing test**

- Add PlayMode tests asserting:
  - input exposes `AimHeld`,
  - mover applies ADS multiplier when aiming,
  - sprint speed still wins when not aiming.

**Step 2: Run test to verify it fails**

Run:
```bash
UNITY_EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity}"
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform playmode -testResults "$(pwd)/.tmp/weapon-anim-task2.xml" -testFilter "Reloader.Player.Tests.PlayMode.PlayerControllerPlayModeTests" -quit
```
Expected: FAIL because aim input contract/path does not exist.

**Step 3: Write minimal implementation**

- Add `bool AimHeld { get; }` to `IPlayerInputSource`.
- Add `Aim` input action mapping in `PlayerInputReader`.
- Add ADS multiplier usage in `PlayerMover`:
```csharp
var baseSpeed = _inputSource.SprintHeld ? _settings.SprintSpeed : _settings.WalkSpeed;
var targetSpeed = _inputSource.AimHeld ? baseSpeed * adsSpeedMultiplier : baseSpeed;
```
- Keep multiplier source injectable (temporary serialized field in mover until weapon profile wiring is added in Task 4).

**Step 4: Run test to verify it passes**

Run the same command from Step 2 plus:
```bash
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform playmode -testResults "$(pwd)/.tmp/weapon-anim-task2b.xml" -testFilter "Reloader.Player.Tests.PlayMode.PlayerInventoryControllerPlayModeTests" -quit
```
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Player/Scripts/IPlayerInputSource.cs \
  Reloader/Assets/_Project/Player/Scripts/PlayerInputReader.cs \
  Reloader/Assets/_Project/Player/Scripts/PlayerMover.cs \
  Reloader/Assets/_Project/Player/InputSystem_Actions.inputactions \
  Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs \
  Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerInventoryControllerPlayModeTests.cs
git commit -m "feat: add ads input and movement slowdown contract"
```

### Task 3: Add Weapon Animation Profile Data Contracts

**Files:**
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Data/WeaponAnimationProfile.cs`
- Create: `Reloader/Assets/_Project/Player/Scripts/Viewmodel/AnimationContractProfile.cs`
- Create: `Reloader/Assets/_Project/Player/Scripts/Viewmodel/CharacterViewmodelProfile.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/WeaponRuntimeStateTests.cs` (new profile-focused test section or split into dedicated test file)

**Step 1: Write the failing test**

- Add EditMode tests for profile defaults:
  - `adsSpeedMultiplier` defaults to `0.7f`,
  - missing optional fields do not throw,
  - contract profile provides canonical parameter names.

**Step 2: Run test to verify it fails**

Run targeted EditMode tests for the added profile tests.  
Expected: FAIL due to missing profile types/fields.

**Step 3: Write minimal implementation**

- Add serializable SO assets for three profiles.
- Include version fields in `AnimationContractProfile` (`major`, `minor`).
- Include required parameter name fields with sane defaults (`MoveSpeed01`, `AimWeight`, etc.).

**Step 4: Run test to verify it passes**

Run targeted EditMode tests again.  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Weapons/Scripts/Data/WeaponAnimationProfile.cs \
  Reloader/Assets/_Project/Player/Scripts/Viewmodel/AnimationContractProfile.cs \
  Reloader/Assets/_Project/Player/Scripts/Viewmodel/CharacterViewmodelProfile.cs \
  Reloader/Assets/_Project/Core/Tests/EditMode/WeaponRuntimeStateTests.cs
git commit -m "feat: add animation contract and profile assets"
```

### Task 4: Integrate Interruptible Reload + Aim Events in Weapon Controller

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponRuntimeState.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Data/WeaponDefinition.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/WeaponRuntimeStateTests.cs`

**Step 1: Write the failing test**

- Add PlayMode tests for:
  - reload started event is raised,
  - sprint while reloading raises reload cancelled (`Sprint`),
  - aim state raises `OnWeaponAimChanged`,
  - unequip while reloading raises reload cancelled (`Unequip`).
- Add EditMode tests if new runtime flags/methods are added to `WeaponRuntimeState`.

**Step 2: Run test to verify it fails**

Run targeted weapon controller PlayMode tests.  
Expected: FAIL on missing lifecycle behavior/events.

**Step 3: Write minimal implementation**

- Extend controller loop:
  - publish equip/unequip start events,
  - publish reload start/cancel events,
  - handle cancellation before applying ammo mutation,
  - publish aim change event on state transitions only.
- Add ADS multiplier field to `WeaponDefinition` and expose read path for movement system.

**Step 4: Run test to verify it passes**

Run targeted weapon/controller + runtime tests.  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs \
  Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponRuntimeState.cs \
  Reloader/Assets/_Project/Weapons/Scripts/Data/WeaponDefinition.cs \
  Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs \
  Reloader/Assets/_Project/Core/Tests/EditMode/WeaponRuntimeStateTests.cs
git commit -m "feat: add interruptible reload and aim event flow"
```

### Task 5: Normalize Movement Signal for Animation Contract

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Scripts/FpsViewmodelAnimatorDriver.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerMovementSettings.cs`
- Test: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs`

**Step 1: Write the failing test**

- Add PlayMode tests asserting `MoveSpeed01` is clamped [0..1] and normalized against `max(WalkSpeed, SprintSpeed)`.

**Step 2: Run test to verify it fails**

Run targeted Player PlayMode tests.  
Expected: FAIL because current driver emits raw horizontal speed.

**Step 3: Write minimal implementation**

- Update viewmodel animator driver normalization formula:
```csharp
var referenceMaxSpeed = Mathf.Max(_settings.WalkSpeed, _settings.SprintSpeed);
var normalized = referenceMaxSpeed > 0f ? horizontalSpeed / referenceMaxSpeed : 0f;
_animator.SetFloat(speedHash, Mathf.Clamp01(normalized));
```
- Pass required movement settings reference into driver.

**Step 4: Run test to verify it passes**

Run targeted Player PlayMode tests again.  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Player/Scripts/FpsViewmodelAnimatorDriver.cs \
  Reloader/Assets/_Project/Player/Scripts/PlayerMovementSettings.cs \
  Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs
git commit -m "feat: normalize viewmodel movement signal to contract"
```

### Task 6: Implement Viewmodel Animation Adapter (Event-Driven)

**Files:**
- Create: `Reloader/Assets/_Project/Player/Scripts/Viewmodel/ViewmodelAnimationAdapter.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/FpsViewmodelAnimatorDriver.cs` (only if shared hashing/utilities are extracted)
- Test: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs`

**Step 1: Write the failing test**

- Add PlayMode tests for adapter behavior:
  - weapon fire event sets `Fire` trigger,
  - reload start toggles `IsReloading`,
  - reload cancel clears `IsReloading`,
  - aim changed sets `IsAiming` + `AimWeight`.

**Step 2: Run test to verify it fails**

Run targeted PlayMode tests with adapter filter.  
Expected: FAIL due to missing adapter.

**Step 3: Write minimal implementation**

- Subscribe/unsubscribe to `GameEvents` in `OnEnable`/`OnDisable`.
- Map events to animator params via cached hashes.
- Read parameter names from `AnimationContractProfile` first, fallback to hardcoded defaults.

**Step 4: Run test to verify it passes**

Run targeted PlayMode tests.  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Player/Scripts/Viewmodel/ViewmodelAnimationAdapter.cs \
  Reloader/Assets/_Project/Player/Scripts/FpsViewmodelAnimatorDriver.cs \
  Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs
git commit -m "feat: add event-driven viewmodel animation adapter"
```

### Task 7: Implement Binding Resolver + Profile Resolver Fallback

**Files:**
- Create: `Reloader/Assets/_Project/Player/Scripts/Viewmodel/ViewmodelBindingResolver.cs`
- Create: `Reloader/Assets/_Project/Player/Scripts/Viewmodel/ViewmodelProfileResolver.cs`
- Test: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs`

**Step 1: Write the failing test**

- Add PlayMode tests asserting:
  - missing required bind point returns invalid result,
  - missing optional bind points emit warning-only path,
  - profile fallback order resolves deterministically.

**Step 2: Run test to verify it fails**

Run targeted PlayMode tests for resolver scenarios.  
Expected: FAIL due to missing resolvers.

**Step 3: Write minimal implementation**

- Implement required/optional bind point lookup (`Muzzle`, `RightHandGrip`, `LeftHandIKTarget`, `AimReference`).
- Implement fallback chain:
  - weapon-specific profile
  - weapon-family profile
  - global default.

**Step 4: Run test to verify it passes**

Run targeted PlayMode tests again.  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Player/Scripts/Viewmodel/ViewmodelBindingResolver.cs \
  Reloader/Assets/_Project/Player/Scripts/Viewmodel/ViewmodelProfileResolver.cs \
  Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs
git commit -m "feat: add viewmodel binding and profile fallback resolvers"
```

### Task 8: Add Editor Validation for Animation Contract Compliance

**Files:**
- Create: `Reloader/Assets/_Project/Player/Scripts/Editor/AnimationContractValidator.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/Editor/PlayerRigMenu.cs` (optional menu integration)
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/InventoryEventContractsTests.cs` (or create dedicated validator tests)

**Step 1: Write the failing test**

- Add EditMode tests for validator severity output:
  - required bind missing => error,
  - optional bind missing => warning,
  - contract major version mismatch => error.

**Step 2: Run test to verify it fails**

Run targeted EditMode validation tests.  
Expected: FAIL due to missing validator.

**Step 3: Write minimal implementation**

- Implement validator output model with severity (`Error`, `Warning`, `Info`).
- Add menu entry `Reloader/Player/Validate Weapon Animation Contract In Active Scene`.

**Step 4: Run test to verify it passes**

Run targeted EditMode tests again.  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Player/Scripts/Editor/AnimationContractValidator.cs \
  Reloader/Assets/_Project/Player/Scripts/Editor/PlayerRigMenu.cs \
  Reloader/Assets/_Project/Core/Tests/EditMode/InventoryEventContractsTests.cs
git commit -m "feat: add animation contract validator tooling"
```

### Task 9: Wire Scene Defaults + Regression Tests + Docs Sync

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Editor/WeaponsSceneWiring.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/Editor/PlayerRigMenu.cs`
- Modify: `docs/design/weapons-and-ballistics.md`
- Modify: `docs/design/core-architecture.md` (event list update if needed)
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs`

**Step 1: Write the failing test**

- Add PlayMode regression test ensuring default wired player rig + weapon flow:
  - equip,
  - aim,
  - reload cancel on sprint,
  - fire after cancel.

**Step 2: Run test to verify it fails**

Run targeted player/weapon PlayMode tests.  
Expected: FAIL due to missing default wiring.

**Step 3: Write minimal implementation**

- Ensure scene wiring tools add required components/references for adapter + profiles.
- Update design docs with finalized v1 event surface and ADS behavior note.

**Step 4: Run test to verify it passes**

Run:
```bash
UNITY_EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity}"
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform editmode -testResults "$(pwd)/.tmp/weapon-anim-task9-edit.xml" -testFilter "Reloader.Core.Tests.EditMode.InventoryEventContractsTests" -quit
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform playmode -testResults "$(pwd)/.tmp/weapon-anim-task9-play.xml" -testFilter "Reloader.Weapons.Tests.PlayMode.PlayerWeaponControllerPlayModeTests|Reloader.Player.Tests.PlayMode.PlayerControllerPlayModeTests" -quit
```
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Weapons/Editor/WeaponsSceneWiring.cs \
  Reloader/Assets/_Project/Player/Scripts/Editor/PlayerRigMenu.cs \
  docs/design/weapons-and-ballistics.md \
  docs/design/core-architecture.md \
  Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs \
  Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs
git commit -m "feat: wire animation contract defaults and update docs"
```

## Final Verification Gate

Run before merge:

```bash
UNITY_EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity}"
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform editmode -testResults "$(pwd)/.tmp/weapon-anim-final-edit.xml" -quit
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform playmode -testResults "$(pwd)/.tmp/weapon-anim-final-play.xml" -quit
```

Expected:
- EditMode: all pass.
- PlayMode: all pass.
- No new warnings from animation contract validation in wired scenes.


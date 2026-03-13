# ADS Shot Cam Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add an ADS-only live shot camera that triggers immediately for predicted long shots beyond `100m`, slows global time to `0.10`, accelerates to `0.25` while `Space` is held, and can be canceled with `Esc` without canceling the projectile.

**Architecture:** Keep fire-time qualification in `PlayerWeaponController`, keep live projectile authority in `WeaponProjectile`, and add a dedicated camera/time-control runtime under `_Project/Weapons` to avoid a new `Player -> Weapons` assembly dependency cycle. Use a temporary `Cinemachine` projectile-follow camera and a presentation-only cinematic projectile visibility mode.

**Tech Stack:** Unity 6 C#, `Unity.Cinemachine`, existing `PlayerWeaponController` / `WeaponProjectile` runtime, NUnit PlayMode tests, existing player input and cursor-lock seams.

---

### Task 1: Add long-shot qualification and shot-cam runtime registration

**Files:**
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Cinematics/ShotCameraSettings.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`

**Step 1: Write the failing test**
- Add coverage proving:
  - ADS fire with predicted first hit beyond `100m` requests shot cam immediately
  - ADS fire with predicted first hit at or under `100m` does not request shot cam
  - hip-fire never requests shot cam even if the predicted hit is long

**Step 2: Run test to verify it fails**
Run: Unity PlayMode test filtered to the new `PlayerWeaponControllerPlayModeTests` shot-cam qualification tests.
Expected: FAIL because there is no shot-cam qualification or runtime registration seam yet.

**Step 3: Write minimal implementation**
- Add a small serializable settings model for:
  - enable/disable
  - qualifying distance threshold
  - default slow-mo scale
  - hold-to-speed-up scale
- Add a fire-time qualification seam in `PlayerWeaponController` that:
  - requires active ADS
  - resolves the authoritative aim camera
  - estimates the first meaningful hit distance from the current aim solution
  - registers the newly spawned live projectile with the shot-cam runtime only when the prediction is `> threshold`

**Step 4: Run test to verify it passes**
Run: the same filtered PlayMode tests.
Expected: PASS

**Step 5: Commit**
```bash
git add docs/plans/2026-03-13-ads-shot-cam-design.md docs/plans/2026-03-13-ads-shot-cam-implementation-plan.md Reloader/Assets/_Project/Weapons/Scripts/Cinematics/ShotCameraSettings.cs Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs
git commit -m "feat: add shot cam qualification seam"
```

### Task 2: Add the live shot-cam runtime and input-driven time control

**Files:**
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Cinematics/ShotCameraRuntime.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/IPlayerInputSource.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerInputReader.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerCursorLockController.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`

**Step 1: Write the failing test**
- Add coverage proving:
  - qualifying fire activates a temporary projectile-follow cinematic mode
  - global time scale becomes `0.10` on entry
  - holding `Space` raises time scale to `0.25`
  - releasing `Space` returns time scale to `0.10`
  - pressing `Esc` exits shot cam and restores `Time.timeScale = 1.0`

**Step 2: Run test to verify it fails**
Run: Unity PlayMode test filtered to the new shot-cam control tests in `PlayerWeaponControllerPlayModeTests`.
Expected: FAIL because the runtime, extra input seam, and cleanup logic do not exist yet.

**Step 3: Write minimal implementation**
- Add a dedicated `ShotCameraRuntime` under `_Project/Weapons` that owns:
  - the temporary `CinemachineCamera`
  - current active projectile target
  - global time-scale changes
  - `Esc` cancel and `Space` hold behavior
  - restore of camera/time on exit
- Extend player input with non-invasive read access for:
  - shot-cam cancel this frame
  - shot-cam speed-up held state
- Ensure `Esc` used by shot cam is marked consumed so cursor-unlock handling does not also fire in the same frame.

**Step 4: Run test to verify it passes**
Run: the same filtered PlayMode tests.
Expected: PASS

**Step 5: Commit**
```bash
git add Reloader/Assets/_Project/Weapons/Scripts/Cinematics/ShotCameraRuntime.cs Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs Reloader/Assets/_Project/Player/Scripts/IPlayerInputSource.cs Reloader/Assets/_Project/Player/Scripts/PlayerInputReader.cs Reloader/Assets/_Project/Player/Scripts/PlayerCursorLockController.cs Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs
git commit -m "feat: add live shot cam runtime controls"
```

### Task 3: Make the live projectile followable and visible in shot cam

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/WeaponProjectile.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Prefabs/WeaponProjectile.prefab`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponProjectilePlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`

**Step 1: Write the failing test**
- Add coverage proving:
  - shot cam follows the live projectile instead of a fake replay object
  - the projectile enters a cinematic-visible state while followed
  - impact and miss/despawn both clear the cinematic-visible state and exit shot cam
  - `Esc` exits the camera but does not destroy or stop the projectile

**Step 2: Run test to verify it fails**
Run: Unity PlayMode tests filtered to the new `WeaponProjectilePlayModeTests` and shot-cam lifecycle tests in `PlayerWeaponControllerPlayModeTests`.
Expected: FAIL because the projectile has no shot-cam visibility/follow contract yet.

**Step 3: Write minimal implementation**
- Extend `WeaponProjectile` with a narrow live-follow / lifecycle seam that lets `ShotCameraRuntime`:
  - follow the actual projectile transform and velocity direction
  - detect impact and despawn termination
  - toggle a presentation-only cinematic visibility mode
- Update the projectile prefab only as needed so the visibility uplift works without changing damage or ballistics.

**Step 4: Run test to verify it passes**
Run: the same filtered PlayMode tests.
Expected: PASS

**Step 5: Commit**
```bash
git add Reloader/Assets/_Project/Weapons/Scripts/Ballistics/WeaponProjectile.cs Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs Reloader/Assets/_Project/Weapons/Prefabs/WeaponProjectile.prefab Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponProjectilePlayModeTests.cs Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs
git commit -m "feat: add projectile follow and shot cam visibility"
```

### Task 4: Focused verification and cleanup

**Files:**
- Modify: any touched implementation/tests/docs from previous tasks as needed

**Step 1: Run focused verification**
- Run the new shot-cam qualification/control/lifecycle PlayMode tests.
- Re-run existing projectile and fire-loop PlayMode tests that overlap `PlayerWeaponController`, `WeaponProjectile`, and cursor/input handling.

**Step 2: Fix any regressions**
- Keep fixes minimal and localized.

**Step 3: Run docs/context validation**
Run:
```bash
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```
Expected: PASS

**Step 4: Commit**
```bash
git add docs/plans Reloader/Assets/_Project/Weapons Reloader/Assets/_Project/Player
git commit -m "test: verify ads shot cam workflow"
```

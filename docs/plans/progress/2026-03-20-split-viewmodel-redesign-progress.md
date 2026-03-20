# Split Viewmodel Redesign Progress

## Scope

- Reset first-person weapon presentation around explicit split-viewmodel ownership.
- Keep `WeaponPresentationRoot` and `PlayerArms` as authored sibling roots under `CameraPivot`.
- Preserve muzzle-origin firing and fail closed when authored roots are missing.

## Status

- [x] Accepted MainTown explicit-contract slice landed at `56309613929181b7ddc555ae6d1503a7924cf2ad`.
- [x] `MainTownCombatWiring` now resolves `CameraPivot`, `PlayerArms`, `PlayerArmsAnimator`, `CameraLookTarget`, `WeaponPresentationRoot`, and the authored main camera from explicit `PlayerCameraDefaults` ownership.
- [x] Active `MainTownCombatWiring` path fails closed when the explicit contract is incomplete instead of recovering from `Camera.main` or hierarchy-name lookup.

## Shipped Slice

- MainTown authored-root enforcement is now explicit:
  - `PlayerCameraDefaults` owns the live render-tree roots for the MainTown path.
  - `MainTownCombatWiring` consumes that contract directly.
  - explicit-root failure is now a hard stop for the wiring menu path.

## Verification Snapshot

- Targeted editmode verification for `Reloader.World.Tests.EditMode.MainTownCombatWiringEditModeTests` passed on the accepted slice.
- The slice keeps verification focused on the explicit-contract seam and does not require PlayMode evidence.

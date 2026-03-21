# Split Viewmodel Redesign Progress

## Scope

- Reset first-person weapon presentation around a split-viewmodel ownership model.
- Keep `WeaponPresentationRoot` and `PlayerArms` as sibling roots under `CameraPivot`.
- Move HIP/ADS presentation authority onto weapon/optic authoring plus one explicit scoped alignment seam.
- Preserve weapon-authored muzzle-origin firing.
- Support reuse across irons, non-PiP optics, and PiP optics.
- Delete legacy helper/fallback paths rather than carrying them forward behind compatibility shims.

## Status

- [x] Architecture reset plan written.
- [x] Progress tracker created.
- [x] Audited runtime anti-revival blocker list cleared at current `HEAD`.
- [x] MainTown authored-root enforcement landed with explicit `PlayerCameraDefaults` roots and fail-closed scene wiring.
- [x] Active `PlayerRigMenu` create-flow backfill cleanup completed for `WeaponPresentationRoot`.
- [x] Bootstrap front-door contract slice landed separately: explicit `New Game`/`Continue` startup flow, no automatic MainTown entry, and latest-save discovery seam.
- [ ] Final ownership-contract tests rewritten around the new model.
- [ ] Runtime root-cutover slice landed for `CameraPivot/WeaponPresentationRoot` before broader scene/prefab split work.
- [ ] Shared camera and weapon-root ownership locked to the sibling-root contract.
- [ ] Weapon/optic authoring owns coarse HIP/ADS presentation.
- [ ] Scoped ADS reduced to one runtime alignment seam.
- [ ] Hand rig verified as hand-follow only.
- [ ] PiP rendering/reticle contract stabilized under the new model.
- [ ] Transitional helpers and old fallback tests removed.

## Shipped / Landed Slices

- Documentation reset landed for the split-viewmodel redesign.
- Architecture direction is now explicit:
  - weapon view and arms stay split under `CameraPivot`
  - first runtime implementation target is the weapon-root cutover, not immediate broad scene/helper deletion
  - broader split-scene/prefab cleanup follows only after the live runtime mount path is stable
- Bootstrap startup/front-door work is now split into its own explicit contract slice:
  - `Bootstrap` remains the first build scene
  - MainTown is no longer entered automatically on load
  - `New Game` and `Continue` are routed explicitly from a menu host in `Bootstrap`
  - latest-save discovery lives behind a thin repository seam
- Narrow runtime root-cutover sub-slice landed in `5b4246ba8ce508c911888d885a8be7e6006fab3e`:
  - runtime now requires an explicit hand target root
  - `WeaponHandRigTargets` is no longer revived from `CameraPivot` as a fallback
  - this is treated as a focused anti-revival cut, not the full completion of the broader root-cutover slice
- Narrow runtime anti-revival sub-slice landed in `25462b84`:
  - removed the `PlayerCameraDefaults` pivot-viewmodel-parent fallback loop
  - camera/pivot rescue no longer revives the old parent chain through that path
  - together with `5b4246ba`, this clears the currently audited active runtime blocker list at `HEAD`
- Verified authored-root enforcement landed in `7c8340d9a27a3483aa395803931b94e14aa7c64b`:
  - MainTown prefab/scene now serialize explicit `PlayerCameraDefaults` roots
  - `MainTownCombatWiring` now fail-closes instead of silently backfilling old authored-root assumptions
  - this moves the branch from runtime anti-revival only into explicit authored-root enforcement for the active MainTown content
- Follow-up meta-only commit landed in `6b79d16801b39723491cade0e02cad0c04a2471c`:
  - preserves/imports the asset metadata needed for the authored-root enforcement slice
- Verified partial `PlayerRigMenu` repair-path reduction landed in `a40bd688c2ef87b72541bc21fd7994c04e88daed`:
  - reduces legacy repair/backfill behavior in the rig-authoring path
  - staged the final cleanup by narrowing the remaining backfill path to `CreateFpsRig`
- Verified `PlayerRigMenu` create-flow backfill cleanup completed in `9333066da4c0c8a6296ea2ab9b08dae3a8e99d3e`:
  - active create/repair/configure menu paths now fail-close for `WeaponPresentationRoot`
  - those paths no longer recreate or backfill `WeaponPresentationRoot`
- Verified narrow hand-anchor ownership tightening landed in `f39bca2ca264e923669daa4122791e12778968de`:
  - removed nested child hand-anchor recovery
  - did not touch muzzle/firing fallback, which remains the next authored-weapon contract gap
- Verified narrow active firing-path authored-muzzle tightening landed in `7e002925fa5adde208a8a7928baa34fb3db91ad0`:
  - removed the live equipped-gun firing-path fallback to the controller transform
  - made authored muzzle presence mandatory in the active `PlayerWeaponController` firing path
  - verified in `Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs` and `Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`
- Accepted MainTown explicit-contract follow-up at local commit `56309613929181b7ddc555ae6d1503a7924cf2ad`:
  - active MainTown wiring no longer uses `Camera.main` or render-tree `Transform.Find(...)` recovery for main camera, camera pivot, player arms root, camera look target, or weapon presentation root resolution
  - `PlayerCameraDefaults` explicit ownership is now required for those bindings, otherwise MainTown wiring fails closed
  - targeted artifact `tmp/main-town-explicit-contract.xml` proves the three new fail-closed tests passed
- Parked local review item `b3c38ef221eddc2b5aa6bb2aa20188b426f0ba7c`:
  - reviewed as coherent for explicit-only scoped optic anchor/display cleanup
  - not landed or pushed because automated verification never reached test execution
  - blocked first by EasyRoads editor initialize-on-load crash, then by Burst startup crash in isolated-workspace batchmode
- Earlier branch work provided useful evidence, but it is not treated as the final redesign contract:
  - scoped root-pose helper overlap was a real bug
  - PiP reticle composite jitter was a real bug
  - shared look-path micro-step filtering needed MainTown tuning
- No runtime slice is counted as final for this redesign until it matches the ownership model in the reset plan.

## Verification Snapshot

- Current evidence guiding the redesign:
  - pose-independent rifle/housing micro-steps implicate the shared yaw/camera subtree rather than only scoped pose helpers
  - prior scoped issues showed duplicate transform writers in stable ADS
  - PiP reticle drift evidence showed render-path issues can coexist with transform-ownership issues
  - the look-smoothing boolean is already enabled on the active MainTown player config, so future shared-yaw work must target real behavior knobs or upstream normalization, not a no-op flag flip
- Constraints pulled forward from the reset/docs work:
  - keep PiP sharp and exclude viewmodel/peripheral blur contamination from the PiP image
  - preserve weapon-authored muzzle-origin firing
  - keep the redesign reusable across irons, non-PiP optics, and PiP optics
  - delete fallback/helper compatibility paths once the replacement seam is verified instead of carrying both
- Remaining active runtime revival points still capable of reintroducing legacy ownership assumptions:
  - none from the currently audited runtime blocker list at current `HEAD`
- Runtime anti-revival progress this turn:
  - `WeaponHandRigTargets` revival from `CameraPivot` has been removed by requiring an explicit hand target root
  - the `PlayerCameraDefaults` pivot-viewmodel-parent fallback loop has been removed
  - the previously audited active runtime blocker list is now cleared at current `HEAD`
- Authored-root progress this turn:
  - MainTown prefab/scene now carry explicit `PlayerCameraDefaults` root serialization rather than relying on implicit recovery
  - `MainTownCombatWiring` fail-closes when those authored roots are missing instead of reviving the old path
- MainTown explicit-contract follow-up progress this turn:
  - active seam resolution no longer falls back to `Camera.main`
  - active seam resolution no longer recovers camera/pivot/arms/look-target/presentation-root via render-tree `Transform.Find(...)`
  - MainTown wiring now stays on explicit `PlayerCameraDefaults` ownership or fails closed
- Repair-path reduction progress this turn:
  - `PlayerRigMenu` active create/repair/configure paths now fail-close for `WeaponPresentationRoot`
  - active menu flows no longer recreate or backfill `WeaponPresentationRoot`
- Hand-anchor ownership progress this turn:
  - nested child hand-anchor recovery has been removed from the active path
  - the later firing-path slice now closes the live controller-transform fallback on the active path
- Firing-path ownership progress this turn:
  - active live equipped-gun firing no longer falls back to the controller transform
  - authored muzzle is now mandatory for the active `PlayerWeaponController` firing path
- Documentation gate for this slice:
  - reset plan exists
  - matching progress tracker exists
  - effort is explicitly separated from the older helper-removal thread
  - next step is to continue tightening any remaining non-active-path authored-weapon fallbacks before broader split-scene work
  - `b3c38ef221eddc2b5aa6bb2aa20188b426f0ba7c` remains parked until a trustworthy automated verification path runs past pre-test editor startup

## Open Risks

- active branch state still contains older helper-era assumptions and tests that may conflict with the new target model
- authored prefab data may still sit on transforms that runtime code resets on equip
- even with the audited runtime blocker list cleared, authored content and active-path repair logic can still revive the old model indirectly until the broader root-cutover is finished
- scoped hold state and PiP repair logic may still be keyed to deleted/transitional helpers
- scene/prefab cleanup after helper deletion can create missing-script noise if not planned as an explicit slice
- active-path repair logic still needs reduction even after MainTown authored-root enforcement
- authored-weapon contract follow-through still needs review outside the active firing path, even though the live controller-transform firing fallback is now removed
- MainTown explicit-contract coverage is now better locked, but broader non-MainTown authored ownership still needs the same fail-closed treatment
- local scoped-optic explicit-binding cleanup candidate `b3c38ef221eddc2b5aa6bb2aa20188b426f0ba7c` remains unverified due pre-test batchmode crashes
- reuse claims are not credible until one irons weapon, one non-PiP optic weapon, and one PiP optic weapon all pass focused checks

## Commit Log

- `5b4246ba8ce508c911888d885a8be7e6006fab3e`
  - narrow runtime root-cutover sub-slice
  - explicit hand target root required
  - removed `CameraPivot`-based `WeaponHandRigTargets` revival
- `25462b84`
  - narrow runtime anti-revival sub-slice
  - removed `PlayerCameraDefaults` pivot-viewmodel-parent fallback loop
  - helps clear the audited active runtime blocker list at current `HEAD`
- `7c8340d9a27a3483aa395803931b94e14aa7c64b`
  - verified authored-root enforcement slice
  - MainTown prefab/scene serialize explicit `PlayerCameraDefaults` roots
  - `MainTownCombatWiring` fail-closes instead of backfilling the old path
- `6b79d16801b39723491cade0e02cad0c04a2471c`
  - meta-only follow-up for the authored-root enforcement slice
- `a40bd688c2ef87b72541bc21fd7994c04e88daed`
  - verified partial `PlayerRigMenu` repair-path reduction
  - narrowed the remaining backfill to the active create-flow
- `9333066da4c0c8a6296ea2ab9b08dae3a8e99d3e`
  - completed active `PlayerRigMenu` create-flow backfill cleanup
  - create/repair/configure menu paths now fail-close for `WeaponPresentationRoot`
- `f39bca2ca264e923669daa4122791e12778968de`
  - verified narrow hand-anchor ownership tightening
  - removed nested child hand-anchor recovery
  - did not touch muzzle/firing fallback
- `7e002925fa5adde208a8a7928baa34fb3db91ad0`
  - verified narrow active firing-path authored-muzzle tightening
  - removed live equipped-gun firing fallback to controller transform
  - made authored muzzle mandatory in active `PlayerWeaponController` firing path
- `56309613929181b7ddc555ae6d1503a7924cf2ad`
  - accepted MainTown explicit-contract follow-up
  - active MainTown wiring no longer uses `Camera.main` or render-tree `Transform.Find(...)` recovery for camera/pivot/arms/look-target/presentation-root resolution
  - `tmp/main-town-explicit-contract.xml` verifies the three new fail-closed tests passed
- This tracker should record each landed slice with:
  - commit SHA
  - short description
  - focused verification evidence
  - any deferred follow-up risk

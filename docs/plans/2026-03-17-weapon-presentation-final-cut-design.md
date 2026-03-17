# Weapon Presentation Final Cut Design

**Problem**
The current scoped runtime still allows residual rifle motion because weapon presentation is only half-detached: the rifle no longer hangs directly from `ik_hand_gun`, but `WeaponPresentationRoot` still lives under `PlayerArms`, so animation/state seams can still leak into precision scoped ADS.

**Approved direction**
Make weapon presentation fully independent from the arms branch. Under `PlayerRoot/CameraPivot`, author two sibling roots:
- `WeaponPresentationRoot`
- `PlayerArms`

The equipped weapon view always mounts under `WeaponPresentationRoot`. `PlayerArms` remains visual-only. No weapon runtime path may fall back to `ik_hand_gun`, `Armature`, or any bone-derived parent. Scoped ADS stabilization and PiP stay on the one runtime path only.

**Architecture**
1. `PlayerWeaponController` resolves/creates `CameraPivot/WeaponPresentationRoot` as the only valid weapon parent.
2. `PlayerRigMenu`, `WeaponsSceneWiring`, scene/prefab authoring align to that hierarchy.
3. `FpsViewmodelAnimatorDriver` continues to own arms-root normalization only for `PlayerArms`.
4. Legacy parent discovery and migration logic that seeds from `ik_hand_gun` is removed.
5. Local tests cover the new parent contract and scoped stabilization decisions.

**Cleanup policy**
- Remove legacy animated-parent fallback behavior instead of preserving compatibility.
- Keep one runtime spawn path.
- Do not reintroduce generic fallback attachment/view discovery.

**Verification target**
- Equipped rifle spawns only under `CameraPivot/WeaponPresentationRoot`.
- No runtime path mounts under `PlayerArms`, `ik_hand_gun`, `ik_hand_root`, or `Armature`.
- Scoped ADS keeps rifle and PiP moving as one camera-owned presentation.

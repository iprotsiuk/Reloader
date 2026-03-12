# Kar98k Reticle And PiP Adjustment Design

## Goal

Make the Kar98k scoped optic behave like a real dialed scope:
- use an EBR-7C style transparent reticle with only the red markings visible
- treat the reticle as FFP
- keep the reticle fixed in the optic while windage/elevation adjustment shifts the PiP scene under it

## Current Context

- The scoped PiP path already separates scene rendering from reticle rendering:
  - `RenderTextureScopeController` owns the scope camera and render texture
  - `ScopeReticleController` owns the in-optic reticle sprite and FFP/SFP scaling
  - `OpticDefinition` already points at a `ScopeReticleDefinition`
- The Kar98k optic already has authored reticle assets:
  - `Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteA.asset`
  - `Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteAReticle.asset`
- Scope adjustment input is already flowing into `ScopeAdjustmentController`, but the visual PiP path does not yet consume those values.

## Design

### Reticle Asset

- Replace the current Kar98k reticle sprite with a transparent sprite containing only the EBR-7C markings.
- Strip the white background and black ring/posts so the optic body and lens presentation remain responsible for the scope frame.
- Set the Kar98k `ScopeReticleDefinition` to `Ffp`.
- Keep the existing asset wiring path so the optic continues to bind the reticle through `OpticDefinition -> ScopeReticleDefinition -> ScopeReticleController`.

### Adjustment Visualization

- Keep the reticle centered and visually stable in the scope.
- Apply windage/elevation visually by offsetting the PiP scope camera projection under the reticle.
- Use scope-adjustment values from `AttachmentManager.ActiveScopeAdjustmentController`.
- Convert click counts into a small normalized screen-space offset and clamp it so the projected image cannot slide into obviously broken lens edges.

### Why PiP Offset Instead Of Moving The Reticle

- Moving the overlay reticle lets large dial values drift the reticle out of the scope tube, which looks wrong fast.
- Offsetting the PiP keeps the reticle authored as part of the optic while still making dialing visible to the player.
- This approach matches the repo’s existing split between reticle rendering and scope-camera rendering.

## Testing

- Add a focused PlayMode regression proving scope adjustment changes the PiP visual alignment, not just tooltip/controller state.
- Add a focused asset/wiring regression proving the real Kar98k optic uses an FFP reticle definition and binds the authored reticle sprite.
- Re-run the existing scoped PiP/reticle integration tests to make sure the new behavior does not break prior FFP/SFP expectations.

## Non-Goals

- No ballistic or bullet-impact changes in this slice.
- No full lens-edge distortion simulation.
- No new generic reticle authoring pipeline beyond the Kar98k asset update.

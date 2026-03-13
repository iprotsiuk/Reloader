# HUD Compass Navigation Assist Design

> Status Pointer (2026-03-13): This is a planning/execution artifact. For live implemented-vs-planned status, use `docs/design/v0.1-demo-status-and-milestones.md`.

**Date:** 2026-03-13  
**Status:** Approved

## Goal
Add a Fallout-style top-center compass HUD that always shows cardinal directions based on true world north and, when a contract is active, shows a horizontal target marker pointing toward the live contract target.

## Scope
- Add a reusable UI Toolkit HUD screen for a compass strip centered at the top of the screen.
- Render a continuously scrolling horizontal compass lane driven by player yaw.
- Show cardinal directions using true north (`+Z`) and true east (`+X`).
- Show an active-contract marker on the same lane when the accepted contract target can be resolved in the world.
- Compute marker direction on the XZ plane only.
- Keep the solution extensible for future POI markers.

Out of scope:
- Vertical guidance or elevation markers
- Distance text in the compass HUD
- Multiple POI categories beyond the active contract target
- Minimap or world-space waypoint widgets

## Behavior Contract
- The compass is anchored at the top-center of the screen and remains visible during normal gameplay HUD states.
- The center of the compass strip represents the player's forward heading.
- `N`, `E`, `S`, and `W` labels repeat across the strip so rotation feels continuous instead of snapping between four static anchors.
- Heading uses world-space truth north:
  - `N` = world `+Z`
  - `E` = world `+X`
  - `S` = world `-Z`
  - `W` = world `-X`
- The player heading and target marker both ignore vertical offset and are computed from the XZ plane.
- If there is no active contract, no target marker is rendered.
- If there is an active contract but the live target cannot be resolved, the compass still renders cardinal directions and suppresses the target marker.
- The rendering/data model must support future additional POI markers without changing the heading math contract.

## Architecture
- Add a dedicated UI Toolkit screen to the runtime HUD composition, parallel to `BeltHud` and `AmmoHud`.
- Use the existing `UiToolkitRuntimeInstaller` and `UiToolkitScreenRuntimeBridge` composition path rather than scene-local hand wiring.
- Implement the compass as the standard controller/view-binder split already used across runtime HUD screens:
  - controller reads scene/runtime dependencies and produces a render state
  - view binder renders the compass track and marker visuals into the UXML tree
- Introduce a compact compass math/model layer so future POI markers reuse the same heading-to-screen-position conversion instead of duplicating calculations inside the view binder.

Data sources:
- Viewer transform: existing runtime `PlayerInventoryController.transform` seam already used by the contracts tab tracking adapter.
- Active contract: `IContractRuntimeProvider.TryGetContractSnapshot(...)`
- Live target world transform: `CivilianPopulationRuntimeBridge.TryResolveSpawnedCivilian(snapshot.TargetId, out spawnedCivilian)`

## UI Structure
- New screen id in `UiRuntimeCompositionIds` for the compass HUD.
- New `CompassHud.uxml` and `CompassHud.uss`.
- Layout:
  - outer root pinned top-center
  - masked ruler lane with repeated cardinal labels
  - center tick / frame
  - optional contract marker layered on the same lane
- The visual style should feel diegetic and utilitarian, closer to the current muted HUD language than a bright arcade overlay.

## Rendering Model
- Convert world heading into a normalized compass heading in degrees.
- Build a repeated ordered set of cardinal anchors so the visible lane can always render labels around the current heading window.
- Convert each compass anchor and marker bearing into a signed delta from player heading using shortest-angle math.
- Map signed delta to horizontal pixel offset inside a configurable visible heading span.
- Hide entries outside the visible span plus a small overscan margin.

Future POI extension contract:
- Marker rendering should consume a list of marker descriptors (`kind`, `bearing`, `label/visual state`) so new POIs can be appended without changing the compass math or layout contract.

## Error Handling
- If the player/viewer transform is unavailable, render a neutral compass fallback with hidden dynamic entries instead of spamming null errors.
- If contract provider or population bridge is unavailable, render only cardinal directions.
- If the active target despawns transiently, suppress the marker until the target is resolvable again.

## Testing Strategy
- Unit/EditMode tests for compass math:
  - world heading to cardinal label placement
  - shortest-angle delta mapping across `0/360`
  - XZ-only bearing calculation
- PlayMode/UI tests for:
  - runtime bridge binding of the new compass screen
  - cardinal labels rendering around the heading center
  - active contract marker visibility when the target is resolvable
  - marker suppression when no active contract or no live fix exists
- Scene/runtime verification through the existing UI Toolkit runtime installer path used in `MainTown` and `IndoorRangeInstance`.

## Risks and Mitigations
- Risk: HUD updates become frame-by-frame scene lookups.
  - Mitigation: resolve provider/bridge through existing bridge/controller seams and only recompute lightweight math each refresh tick.

- Risk: marker jitter when the target is nearly aligned with the player heading.
  - Mitigation: use stable signed-angle math and a single normalized conversion path for all entries.

- Risk: future POI support forces a refactor.
  - Mitigation: define the render state around generic marker descriptors from the start, even if only one contract marker exists initially.

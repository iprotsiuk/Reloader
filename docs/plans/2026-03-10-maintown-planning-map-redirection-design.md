# MainTown Planning-Map Redirection Design

**Context:** `MainTown` currently has a large world shell, preserved runtime roots, and a readable town core. A later terrain pass added more natural slopes and ramps, but that direction is not what is needed for the current review. The user wants a clearer planning-map layout pass with obvious district identity, simple grayboxed masses, and easier zoomed-out editor readability.

**Problem:** The map is harder to evaluate as a planning artifact when terrain shaping dominates the silhouette. The next pass should emphasize district readability, scale, and coverage over terrain fidelity.

## Approved Direction

Use a planning-map mode pass.

- Keep the current town core roughly where it is.
- Remove or disable the newly added slope/ramp landforms from the second terrain pass.
- Expand the shell to roughly `2000m x 2000m`.
- Move the outer ridge masses outward so the playable basin feels larger.
- Add more landmarks and district masses as simple gray cubes/boxes with proper scale.
- Keep or increase forest tree density around the outskirts.
- If practical, add large readable overhead district labels or obvious plan-view marker shapes so the layout is easy to read from a zoomed-out editor camera.

## Spatial Direction

- `Town Core` remains the anchor in roughly the same area.
- `Outer Ridges` become more distant context walls rather than the focus of traversal design.
- `Quarry` remains a distinct district, but it can read as a simple grayboxed zone rather than a sculpted basin.
- New districts should be pulled farther out to make better use of the larger `2km x 2km` footprint.

## District Readability

The map should read clearly from above.

- New districts should have large simple footprints and distinct placement.
- Landmarks should use graybox silhouettes that immediately communicate scale and purpose.
- Good candidates for added districts:
  - industrial yard
  - trailer park
  - rail/service depot
  - utility yard / water treatment
  - school / municipal campus
  - truck stop / highway pull-off

## Visual Language

- Prefer graybox cubes / blocks for new masses.
- Avoid more slope/ramp detailing for this pass.
- Forest can stay natural enough to communicate wooded areas, but district planning geometry should be the visual priority.
- Roads can remain surfaced and simple if they help readability.

## Label / Marker Direction

- Preferred: visible in-world district labels that can be read from above in the editor.
- Fallback: large marker pads / plaques and very explicit naming hierarchy if label rendering is not reliable through current tooling.

## Success Criteria

- `MainTown` reads like a clear planning map when zoomed out.
- The map shell is closer to `2km x 2km`.
- The added districts are legible and distinct.
- The recent slope/ramp additions are no longer part of the scene presentation.
- Runtime-critical scene contracts remain intact.

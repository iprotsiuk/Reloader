# MainTown Forest Density Expansion Design

## Summary

Push `MainTown` into a much denser planning-map forest pass without rewriting district layout. The goal is to make the `2km x 2km` shell feel screened, rural, and less empty from the editor and from ground level, while keeping the named districts, road links, and major sniper-landmark read clarity intact.

## Goals

- Add a large new wave of tree anchors around the outskirts and water edges.
- Keep the town core and district labels readable from a zoomed-out editor view.
- Preserve visibility around the church hill, water tower, quarry interior, and key road junctions.
- Use existing tree variants already present in `MainTown` so the pass stays visually coherent and quick to iterate.

## Placement Direction

### Dense Bands

- West and south-west edge should read as a heavy wooded belt.
- South-east and north-east outskirts should gain enough trees to stop looking like open pads in empty space.
- River and reservoir edges should get tree framing so the water features feel embedded in the landscape.

### Controlled Openings

- Keep openings around:
  - `TownCore`
  - `ChurchHill`
  - `UtilityLandmarks`
  - quarry work area / quarry interior
  - primary road intersections

## Asset Direction

- Reuse existing `ForestTree_*` anchors as duplication sources.
- Favor multiple variants instead of a single repeated tree so the added density stays readable even in graybox form.

## Contract Impact

- Extend the focused `MainTown` layout test with a small set of new stable tree anchors from this pass.
- Record the density expansion in the progress log so later art passes know these are intentional coverage anchors, not accidental duplicates.

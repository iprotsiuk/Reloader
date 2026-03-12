# MainTown Mass Forest Layering Design

## Summary

Push `MainTown` from a dense edge-screening pass into a genuinely wooded planning map by adding a few hundred more trees. The fastest stable way to do that is to duplicate the existing `District_ForestBelt` tree field into several offset density layers, producing mixed extra coverage around the perimeter, between districts, along roads, and around the river / reservoir without manually placing hundreds of one-off tree anchors.

## Goals

- Add roughly `200-300` more trees to `MainTown`.
- Keep the result mixed, not just an outer wall.
- Preserve the current district labels, road reads, church hill, utility landmarks, and quarry interior openings.
- Keep the new pass easy to iterate by using stable root-level density-layer names instead of hundreds of new contract names.

## Placement Direction

### Density Layers

- Keep the original `District_ForestBelt` as the canonical anchor root.
- Add several offset forest-belt clones under the world shell:
  - one biased toward west / south-west thickening
  - one biased toward south / south-east and road-edge fill
  - one biased toward north / north-east and reservoir framing

### Mixed Feel

- Offset the new layers so they thicken:
  - inter-district gaps
  - river / reservoir edges
  - secondary road approaches
  - existing sparse voids between the current tree bands

### Controlled Openings

- Do not close the central planning-map readability window around:
  - `TownCore`
  - `ChurchHill`
  - `UtilityLandmarks`
  - quarry bowl / quarry work area
  - major road intersections
  - overhead district labels

## Contract Impact

- Extend the focused `MainTown` layout test with stable density-layer root names rather than hundreds of extra individual tree names.
- Extend the road/forest contract note with the density-layer roots.
- Record the layering pass in the progress log so later art passes know the extra density is intentional.

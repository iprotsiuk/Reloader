# MainTown Dramatic Terrain Redesign Design

## Summary

Replace the flat paint-ready `MainTown` terrain presentation with a dramatic landform pass that supports the assassination-sandbox fantasy: a central town basin surrounded by heavier mountain edges, carved valleys, a visible river corridor, a deeper quarry depression, and clear forest regions.

This pass should stop the map from reading as a flat planning board while preserving the authored district layout, labels, roads, and runtime-critical scene roots.

## Why This Pass

- The current terrain bootstrap solved the technical blocker for Terrain Tools, but the world still reads as too flat.
- The project fantasy is not a neutral planning map. It is a dirty criminal-sniper sandbox with prep, positioning, line-of-sight control, and escape routes.
- The terrain needs stronger shape before any serious texture-paint pass, because the final textures should follow slope, basin, ridges, riverbanks, and quarry cut lines.

## Approved Approach

- Preserve:
  - `MainTownWorldShell`
  - district roots
  - labels
  - marker pads
  - runtime-critical scene roots
  - current road topology
- Keep the existing `MainTownTerrain` object and terrain-layer assets.
- Remove the current broad road/dirt/quarry paint presentation so the terrain returns to a neutral base.
- Sculpt the terrain into a dramatic basin:
  - flatter central town floor
  - raised mountain edge around the map
  - several stronger sniper ridges and overlook spurs
  - at least one carved river valley
  - a reservoir/lake pocket
  - a deeper quarry depression below town grade
- Restore broad forest presence as terrain-driven regional woods rather than as random whole-map clutter.
- Keep water simple in this pass. The goal is landform readability, not final shader polish.

## Terrain Language

- `Central Basin`
  - town districts remain on the more readable floor of the map
  - enough grade variation to avoid perfect flatness, but not enough to make the district markup unreadable
- `Outer Mountain Ring`
  - clearly higher than the basin
  - forms the visual boundary of the map
  - includes a few intentionally useful high-ground shooting perches
- `Valley Cuts`
  - break the ring so the world does not feel like a perfect bowl
  - support road/river/escape route logic
- `River Corridor`
  - reads as a real terrain cut, not just a blue stripe
  - includes a wider water pocket for a reservoir/lake read
- `Quarry Basin`
  - lower than the town floor
  - sharper excavation feel than the rest of the terrain
- `Forest Regions`
  - concentrated on outer slopes, ridge shoulders, and river-edge/valley bands
  - not packed into the town center or into critical landmark readability windows

## Forest Direction

- Use broad low-poly forest masses with a mix of conifer-heavy outer slopes and a lighter fill near the valley corridor.
- The first forest pass should prioritize region readability:
  - west / south-west mountain woods
  - north and north-east ridge woods
  - riverbank and reservoir framing woods
  - selective woods near the outer districts
- Preserve openings around:
  - `District_TownCore`
  - `District_ChurchHill`
  - `District_UtilityLandmarks`
  - `District_QuarryBasin`
  - main district labels
  - major road corridors

## Scene Contract

- `MainTownTerrain` remains the authoritative terrain surface.
- `BasinFloor` may remain inactive as a planning reference object.
- The scene should gain simple water presentation roots again for the river / reservoir read.
- Forest presence can be authored via terrain tree instances or grouped prefab placement, but the result must be stable enough for focused tests to validate.

## Validation

- Extend the focused `MainTown` EditMode suite so the terrain can no longer remain effectively flat.
- Require evidence of:
  - meaningful terrain height range
  - higher outer terrain than the center basin
  - a lower quarry area than the town center
  - water presentation roots for river / reservoir
  - meaningful forest population in the scene or terrain data
- Re-run the focused Unity EditMode suite and the repo doc/rule guardrails.

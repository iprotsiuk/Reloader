# MainTown District Expansion And Water Spine Design

**Prerequisites:** Read [../design/core-architecture.md](../design/core-architecture.md) first.

`MainTown` is currently in a readable graybox planning-map mode with a `2km x 2km` shell, preserved town core, several outer districts, forest presence, and overhead labels. The next pass should make the map feel more distributed and less dominated by a few oversized outer districts.

## Goal

Expand the planning map into a clearer ring of smaller districts while adding one simple water system that improves spatial separation.

The scene should still read like a planning artifact first:
- graybox districts
- obvious labels
- simple terrain language
- strong separation between town, industry, roadside, and wooded zones

## Approved Direction

- Keep the town core roughly where it is.
- Make the current outer districts smaller.
- Add more districts around the map so the shell reads fuller and more town-like.
- Add a simple `river with a small reservoir/lake`.
- Keep forest presence and use it to frame the river/lake edges.
- Stay in planning-map mode rather than returning to slope-heavy terrain work.

## Spatial Layout

The map should read as a set of destinations around a central town anchor.

- `Town Core` stays central.
- `Church Hill`, `Player Compound`, `Motel Strip`, and `Quarry Basin` remain recognizable anchors.
- Existing large outer districts should shrink so they stop dominating their quadrants.
- New districts should fill the empty perimeter space and make the shell feel broader.

Recommended placement:

- `Northwest`
  - `District_SchoolCampus`
  - new `District_MunicipalBlock`
- `North`
  - forest and a small reservoir
- `Northeast`
  - `District_ServiceDepot`
  - `District_WaterTreatment`
- `East`
  - `District_IndustrialYard`
  - quarry-side service access
- `Southeast`
  - `District_QuarryBasin`
  - new radio/utility landmark zone
- `South`
  - `District_TrailerPark`
  - new roadside market / diner strip
- `Southwest`
  - `District_TruckStop`
  - new freight or rail yard zone
- `West`
  - `District_PlayerCompound`
  - `District_ChurchHill`
  - forest and a river crossing back toward town

## Water Direction

Use one readable `water spine` rather than many scattered water features.

- Add a simple river corridor crossing part of the map.
- Add a small reservoir/lake attached to the river rather than a giant standalone lake.
- Use the water to separate districts and create visual rhythm in the zoomed-out view.
- Keep the shapes simple and legible, not naturalistic.

Recommended water pieces:

- `Water_RiverWest`
- `Water_RiverCentral`
- `Water_ReservoirNorth`
- `Landmark_RiverBridge`

## New District Candidates

Add a few compact districts with clear silhouettes and labels.

- `District_MunicipalBlock`
  - town hall / sheriff annex / parking pad shape
- `District_FreightYard`
  - rail or loading-yard silhouettes
- `District_RoadsideMarket`
  - diner / lot / shed row
- `District_RadioTower`
  - utility or radio-service pad with a tall marker mass

These should be smaller than the current outer districts and spaced to improve overall map balance.

## Visual Language

- Keep graybox cubes / pads for districts.
- Keep roads simple and readable.
- Keep forest tree masses and add more only where the new layout would otherwise feel empty.
- Use large overhead labels again because they proved useful in the last pass.
- Avoid bringing back slope-heavy terrain shaping for this iteration.

## Success Criteria

- The current oversized outer districts are visibly reduced.
- New districts fill more of the `2km x 2km` shell.
- The river and reservoir improve layout readability.
- The scene remains easy to understand from a zoomed-out editor view.
- Existing runtime-critical roots and current layout tests remain intact.

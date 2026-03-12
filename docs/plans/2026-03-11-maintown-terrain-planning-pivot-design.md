# MainTown Terrain-Planning Pivot Design

## Summary

Pivot `MainTown` away from the current landscape-heavy planning map and back to a cleaner terrain-planning shell. Remove the added trees, rivers, and mountain/hill presentation masses, while preserving the town/district blockout, road layout, district marker pads, and readable overhead labels.

## Why This Pivot

- The current scene has too much presentation-layer geometry for the next terrain-paint workflow.
- The next intended workflow is terrain painting after Unity terrain tools are installed.
- A cleaner planning shell makes it easier to paint:
  - road corridors
  - dirt/quarry ground
  - grass/open ground
  - district boundaries

## Approved Approach

- Keep:
  - `MainTownWorldShell`
  - the district roots
  - the district marker pads
  - the current road layout
  - the current district labels
  - the core town blockout / landmark blockout needed to read the town
- Remove:
  - all added forest tree objects
  - forest density layer roots
  - forest gap cluster roots
  - river / reservoir water objects
  - river bridge landmark
  - mountain rim / ridge masses
  - hill / slope / approach / ramp terrain-presentation masses
- Keep the scene flat enough that later terrain painting becomes the primary landscape-authoring pass.

## Scene Contract

- Preserve district roots such as:
  - `District_TownCore`
  - `District_PlayerCompound`
  - `District_ChurchHill`
  - `District_QuarryBasin`
  - outer planning districts
- Preserve label roots such as:
  - `Label_IndustrialYard`
  - `Label_TrailerPark`
  - `Label_ServiceDepot`
  - `Label_TruckStop`
  - `Label_WaterTreatment`
  - `Label_StorageYard`
  - `Label_MunicipalBlock`
  - `Label_FreightYard`
  - `Label_RoadsideMarket`
  - `Label_RadioTower`
- Preserve road roots:
  - `PerimeterLoopRoad`
  - `MainStreetSpine`
  - `ServiceRoads`

## Validation

- Update the focused `MainTown` layout tests to reflect planning mode:
  - district roots still exist
  - marker pads and labels still exist
  - water roots are gone
  - mountain / ridge / hill presentation roots are gone
  - forest presentation roots are gone
  - road surface contract remains intact

# MainTown Road Network And Forest Fill Design

## Summary

Connect the planning-map districts in `MainTown` with a readable road network and add substantially more trees so the shell stops reading as disconnected pads in open space. Roads should be mostly paved between districts, with dirt/gravel only for quarry, forest, and rough service access.

## Goals

- Make the outer districts feel intentionally connected.
- Preserve `TownCore` as the hub.
- Keep district labels readable from the editor.
- Increase tree density enough to create separation, concealment, and edge screening.
- Avoid blocking the main landmark sightlines around church hill, water tower, quarry, and motel approaches.

## Road Direction

### Paved Routes

- West trunk:
  - `TownCore -> MotelStrip -> TruckStop -> TrailerPark`
- East trunk:
  - `TownCore -> UtilityLandmarks -> QuarryBasin -> IndustrialYard`
- North-east trunk:
  - `TownCore -> ChurchHill -> ServiceDepot`
- North-west connector:
  - `PlayerCompound / ForestBelt -> TruckStop`
- Northern civic connector:
  - tie `StorageYard` and `MunicipalBlock` back toward the north-west / north road structure
- Cross-links:
  - give `RoadsideMarket`, `FreightYard`, `RadioTower`, and `WaterTreatment` obvious frontage or spur connections so they do not read as isolated markers

### Dirt / Gravel Routes

- `TrailerPark -> MotelStrip -> QuarryBasin` back-road route
- quarry work approach / service access
- forest access spurs
- optional ridge-edge maintenance / radio-tower approach if needed

## Asset Direction

- Prefer existing modular road pieces already in the project for this pass.
- Do not rely on EasyRoads3D for this graybox connection pass unless the editor path becomes clearly simpler than modular placement.

## Forest Fill Direction

- Add heavier tree clusters in the empty west, south-west, north-east, and south-east gaps.
- Use trees to separate districts rather than forming a uniform blanket.
- Keep clear windows near:
  - `ChurchHill`
  - `UtilityLandmarks`
  - quarry rims / quarry interior
  - motel frontage
  - key road junctions

## Contract Impact

- The focused `MainTown` layout test should continue to prove the world-shell and district structure.
- Add assertions for extra road connectors and additional tree anchors only where the naming contract needs to stay stable for future passes.

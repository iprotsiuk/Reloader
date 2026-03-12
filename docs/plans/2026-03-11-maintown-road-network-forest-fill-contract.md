# MainTown Road-Network And Planning-Shell Contract

## Scope

This is the stable naming contract for the current `MainTown` planning-shell pass. It records the actual connector and planning-marker names used by the scene so downstream tests and later terrain-paint passes can target them consistently.

## Road Connectors

Under `MainTownWorldShell/ServiceRoads`:

- `Road_WestTownToMotel`
- `Road_WestMotelToTruckStop`
- `Road_WestTruckStopToTrailer`
- `Road_EastTownToQuarry`
- `Road_QuarryToIndustrial`
- `Road_ChurchToServiceDepot`
- `Road_MunicipalToTown`
- `Road_StorageToMunicipal`
- `Road_FreightYardSpur`
- `Road_RoadsideMarketSpur`
- `Road_RadioTowerSpur`
- `Road_WaterTreatmentSpur`
- `Road_ForestBypass`
- `Road_ForestPlayerLink`
- `Road_Dirt_TrailerToMotel`
- `Road_Dirt_MotelToQuarry`
- `Road_Dirt_ForestAccess`
- `Road_Dirt_QuarryService`

## District Labels

Scene-root planning labels:

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

## Marker Pads

Under `MainTownWorldShell`:

- `MarkerPad_IndustrialYard`
- `MarkerPad_TrailerPark`
- `MarkerPad_ServiceDepot`
- `MarkerPad_TruckStop`
- `MarkerPad_WaterTreatment`
- `MarkerPad_StorageYard`
- `MarkerPad_MunicipalBlock`
- `MarkerPad_FreightYard`
- `MarkerPad_RoadsideMarket`
- `MarkerPad_RadioTower`

## Removed Presentation Layer

The current planning-shell scene intentionally no longer keeps these earlier presentation roots:

- `Water_*`
- `ForestTree_*`
- `ForestDensityLayer_*`
- `ForestGapCluster_*`
- `MountainRim`
- `Landmark_RiverBridge`

## Terrain Bootstrap

Under `MainTownWorldShell`:

- `MainTownTerrain`

Terrain assets live under:

- `Assets/_Project/World/Terrain/MainTown/MainTownTerrainData.asset`
- `Assets/_Project/World/Terrain/MainTown/MainTown_Grass.terrainlayer`
- `Assets/_Project/World/Terrain/MainTown/MainTown_Dirt.terrainlayer`
- `Assets/_Project/World/Terrain/MainTown/MainTown_Road.terrainlayer`
- `Assets/_Project/World/Terrain/MainTown/MainTown_Stone.terrainlayer`

Starter terrain intent:

- terrain footprint should cover the roughly `2km x 2km` planning shell
- terrain is no longer meant to stay flat; the current target is a dramatic basin/mountain/river shell
- broad route-specific starter paint has been cleared back to a neutral terrain base so later texturing can follow terrain shape
- `BasinFloor` remains in the scene as a planning reference object but should be inactive after bootstrap

Current dramatic terrain anchors:

- central basin around the authored town core
- higher outer ring / mountain edge
- lower quarry basin near `District_QuarryBasin`
- `Water_RiverChannel`
- `Water_ReservoirBasin`
- terrain-tree forest regions on the outer slopes and river-side bands

## Intent

These names are meant to be stable planning anchors, not a final dressing prescription. Later terrain-paint or environment passes can refine exact ground texturing and landscape detail as long as these connector, marker, and label objects remain addressable.

## Current Road Surface Rendering

Current scene intent for the visible planning-map road strips:

- paved routes should render with:
  - `Assets/EasyRoads3D/Resources/Materials/roads/road material.mat`
- dirt service spurs should render with:
  - `Assets/EasyRoads3D/Resources/Materials/roads/dirt material.mat`
- representative visual road strips should no longer use:
  - `Assets/ThirdParty/SimplePoly - Town Pack/Materials/sptp_Main_Mat.mat`
  - built-in `Cube` mesh blockout rendering

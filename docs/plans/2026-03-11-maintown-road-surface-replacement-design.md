# MainTown Road Surface Replacement Design

## Summary

Replace the current `MainTown` road-strip presentation with stable textured road surfaces without using `EasyRoads3D` automation. Preserve the existing named road objects and route layout, but convert the visible road strips from atlas-textured cube blockouts into flat road-surface meshes with dedicated paved or dirt materials.

## Why This Direction

- The installed `EasyRoads3D` package blocks scripted road creation in this project, so MCP/editor automation cannot build the network through its API.
- The visible road problem is primarily a presentation issue:
  - current strips are mostly `Cube` meshes
  - they use `sptp_Main_Mat`
  - that material samples a texture atlas, which causes the banner-collage look
- Keeping the current road object names and transforms minimizes churn in `MainTown` while still fixing the visual issue.

## Approved Approach

- Keep the current road topology and named route objects.
- Replace visible paved road strips with:
  - a flat road-capable mesh from the existing asset packs
  - a dedicated asphalt road material instead of the atlas material
- Replace visible dirt/gravel spurs with:
  - the same flat road-capable mesh
  - a dirt/gravel material
- Preserve current route naming so layout tests and future mission/world wiring can continue to target the same paths.

## Asset Direction

- Base road mesh source:
  - `Assets/ThirdParty/SimplePoly - Town Pack/Prefabs/RoadSegments/sptp_asphalt_01.prefab`
- Paved surface material:
  - `Assets/EasyRoads3D/Resources/Materials/roads/road material.mat`
  - or equivalent dedicated asphalt material if scene validation shows a better result
- Dirt surface material:
  - `Assets/EasyRoads3D/Resources/Materials/roads/dirt material.mat`

## Scene Contract

- Preserve these roots:
  - `PerimeterLoopRoad`
  - `MainStreetSpine`
  - `ServiceRoads`
- Preserve existing child route names under those roots.
- Do not move districts or landmarks in this pass.

## Validation

- Add a focused layout assertion that representative road objects:
  - no longer use `sptp_Main_Mat`
  - no longer render as `Cube` mesh strips
- Re-run:
  - `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`
  - docs/context guardrails

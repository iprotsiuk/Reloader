# MainTown Terrain Bootstrap Design

## Summary

Add a real Unity `Terrain` under the cleaned `MainTown` planning shell so the map can move into terrain-paint workflows. Preserve the town blockout, district labels, marker pads, roads, and runtime-critical roots, while replacing the flat planning floor with a paintable terrain surface seeded with a few broad ground layers.

## Why This Pass

- `Terrain Tools` and `Splines` are now installed.
- `MainTown` has already been cleaned back to a readable planning shell.
- The next useful step is not more graybox geometry. It is a real terrain surface that can carry:
  - grass ground
  - dirt/gravel routes
  - asphalt road paint
  - quarry/stone ground

## Approved Approach

- Add one `Terrain` object under `MainTownWorldShell`.
- Size it to the current shell footprint:
  - about `2000m x 2000m`
- Keep it mostly flat for now.
- Seed at least four `TerrainLayer` assets using existing project textures/materials:
  - grass
  - dirt
  - asphalt
  - stone / quarry
- Apply a broad starter paint pass:
  - grass as the default fill
  - paint paved road corridors from the current road-strip layout
  - paint dirt routes from the current dirt-spur layout
  - paint a quarry/stone patch in the quarry district
- Leave the scene in a “paint-ready” state rather than trying to fully finish terrain art in one pass.

## Asset Direction

- Grass source:
  - `Assets/ThirdParty/Polygon-Mega Survival Forest/Textures/TerrainTextures/Grass.PNG`
- Stone/quarry source:
  - `Assets/ThirdParty/Polygon-Mega Survival Forest/Textures/TerrainTextures/Stones.PNG`
- Dirt source:
  - `Assets/EasyRoads3D/textures/roads/dirtRoad_A.tga`
- Asphalt source:
  - `Assets/EasyRoads3D/textures/roads/road2Lane_A.tga`

Starter TerrainLayer assets should live under:

- `Assets/_Project/World/Terrain/MainTown/`

## Scene Contract

- Preserve:
  - `MainTownWorldShell`
  - district roots
  - marker pads
  - scene-root labels
  - roads
  - runtime-critical scene roots such as `PlayerRoot`, `MainTownEntry_Spawn`, `MainTownEntry_Return`, `MainTownContractRuntime`, `MainTownPopulationRuntime`, `WeaponRegistry`, `StorageChest`, `ReloadingWorkbench`
- Add:
  - `MainTownTerrain`

## Validation

- Add a focused `MainTown` layout assertion for:
  - `MainTownTerrain` exists
  - terrain size roughly matches the planning shell
  - terrain has at least four layers
- Re-run the focused `MainTown` EditMode suite and repo guardrails.

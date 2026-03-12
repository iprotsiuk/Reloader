# MainTown Literal-Mile Rebuild Progress

## Goal

Rebuild `MainTown` into a literal-mile Appalachian sniper-sandbox shell with authored districts, landmark sightlines, and preserved runtime scene contracts.

## Checkpoints

- [x] Defined approved map direction:
  - literal `1 x 1 mile`
  - Appalachian basin
  - terrain-first first pass
  - balanced sightlines
  - motel-strip extra landmark
- [x] Added initial layout-contract test scaffold:
  - `MainTownLayoutEditModeTests`
- [x] Saved implementation plan doc
- [x] Verified layout test fails red before scene work
- [x] Authored `MainTownWorldShell` literal-mile footprint
- [x] Added district and landmark roots
- [x] Added road skeleton and terrain masses
- [x] Re-ran dedicated EditMode layout validation after the scale-contract expansion
- [ ] Re-ran targeted PlayMode validation
- [ ] Captured MCP read-back and screenshot verification

## Activity Log

### 2026-03-10 20:00 PT

- Pulled project world/design docs plus local `.cursor` guidance before scene mutation.
- Confirmed active scene is `Assets/_Project/World/Scenes/MainTown.unity`.
- Confirmed current `MainTown` is still the compact prototype scene with small `TownGround`.
- Confirmed available tooling/assets for the rebuild:
  - `EasyRoads3D`
  - existing road prefabs under `Assets/ThirdParty/**`
  - existing environment/terrain-capable project setup
- Locked approved design direction with the user:
  - literal-mile footprint
  - terrain-first implementation
  - Appalachian tone
  - balanced sniper/urban sightlines
  - motel-strip signature area

### 2026-03-10 20:05 PT

- Added `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`.
- Paused further scene authoring to document the plan and progress log before proceeding.

### 2026-03-10 20:15 PT

- Saved implementation plan:
  - `docs/plans/2026-03-10-maintown-literal-mile-rebuild-implementation-plan.md`
- Verified red test through Unity MCP because the live editor already had the project open:
  - `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`
  - status: `failed`
  - failure: `MainTownWorldShell` missing from `MainTown`

### 2026-03-10 20:25 PT

- Began scene authoring through batched Unity MCP requests.
- Added `MainTownWorldShell` and disabled legacy `TownGround`.
- Added required layout-contract roots:
  - `BasinFloor`
  - `MountainRim`
  - all approved district roots
  - all approved road roots
  - all approved landmark roots
- Added first terrain-first blockout masses:
  - perimeter ridges
  - ring-road / main-street / service-road ribbons
  - church hill blockout
  - player compound blockout
  - quarry pit and wall masses
  - town-core service buildings
  - water-tower structure
  - motel strip blockout
  - first forest trees and quarry rocks
- Noted an MCP quirk:
  - `batch_execute` scene creation works well for structure
  - transform placement is reliable only through direct `manage_gameobject` follow-up writes by instance id
- Verified the new layout test green through Unity MCP:
  - `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`
  - summary: `1 passed / 0 failed`

### 2026-03-10 20:40 PT

- Re-ran the dedicated layout-contract test after the first world-shell authoring pass:
  - `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`
  - summary: `1 passed / 0 failed`
- Recorded additional world-scale constraints from the user for all follow-up blockout work:
  - player character height: about `1.8m`
  - reliable jump height: about `1m`
  - one-story structures should generally read around `5m` tall
  - retail/storefront masses must be sized like drive-up rural commercial buildings, not sheds
  - pines should generally read in the `15m` to `45m` range
  - hills, quarry walls, and ridge lines must feel regionally tall enough to create real sniper elevation and not toy-scale
- Next scene pass is focused on bringing the existing world-shell masses and preserved runtime cluster into a coherent, scale-correct layout.

### 2026-03-10 21:05 PT

- Audited the live test and scene contract dependencies before continuing scene edits.
- Confirmed the rebuild still has to preserve:
  - `PlayerRoot` name, `Player` tag, and its expected child paths
  - `MainTownEntry_Spawn`
  - `MainTownEntry_Return`
  - `MainTown_SmokeToIndoor_Trigger`
  - `ReloadingWorkbench`
  - `StorageChest`
  - `WeaponRegistry`
  - `MainTownContractRuntime`
  - `MainTownPopulationRuntime` and its direct anchor children
- Confirmed an additional compatibility name requirement from playmode coverage:
  - exact `PlayerHouse`
- Expanded `MainTownLayoutEditModeTests` locally to cover the scale envelope requested by the user:
  - exact `PlayerHouse` presence
  - one-story shell heights
  - civic building massing
  - church tower silhouette
  - water tower stem/tank size
  - motel pad and mountain rim scale
- The Unity editor stayed running, but the MCP bridge became intermittent during this pass.
- Switched to direct YAML scene edits for the active blockout pass instead of waiting on the bridge.
- Resized or repositioned named blockout masses toward a more believable human scale:
  - player house compound
  - gun store
  - reloading supply
  - police station
  - hospital
  - church nave and tower
  - water tower stem and tank
  - motel block and office
- Added the missing exact `PlayerHouse` object under `Landmark_PlayerHouse`.
- Re-ran the expanded dedicated layout-contract test through Unity MCP:
  - `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`
  - summary: `2 passed / 0 failed`

### 2026-03-10 21:20 PT

- Added another environment readability pass around the validated shell:
  - player-overlook hill
  - church slope mass
  - water-tower hill
  - quarry south rim
  - additional forest trees for longer rural concealment lanes
- Unity-side save/test tooling became unreliable late in the pass:
  - Unity MCP scene/test commands started failing intermittently
  - command-line Unity test fallback is blocked while the project is already open in the live editor
- Verified the saved `MainTown.unity` YAML contains the later hill and forest transforms after applying the final persistence patch directly to the scene file.

### 2026-03-10 21:30 PT

- Re-ran repo documentation and design-context guardrails successfully:
  - `bash scripts/verify-docs-and-context.sh`
  - `bash scripts/verify-extensible-development-contracts.sh`
  - `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`
- All three guardrail passes completed cleanly against the documented rebuild plan and progress log.
- Attempted a fresh live-editor Unity MCP rerun of:
  - `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`
- The Unity MCP bridge recovered after a forced refresh, but the direct test request still timed out before returning a result.
- Current best evidence remains:
  - previously observed `2 passed / 0 failed` on the dedicated layout EditMode test during the scale pass
  - current scene YAML and test file remain aligned with that validated scale contract
  - broader Unity-side regression and screenshot capture are still blocked on bridge stability

### 2026-03-10 21:45 PT

- Documented and approved a second-pass `Hybrid` terrain direction for `MainTown`.
- Saved supporting docs:
  - `docs/plans/2026-03-10-maintown-terrain-second-pass-design.md`
  - `docs/plans/2026-03-10-maintown-terrain-second-pass-implementation-plan.md`
- Locked the second-pass terrain contract:
  - all named hills should become climbable through sloped approaches
  - the outer ridge should be mostly continuous and climbable to an interior shelf
  - the map-edge-facing backface should stay steeper so players do not naturally reach the boundary lip
  - the quarry should read as the deepest basin / valley in the map
  - forest density should increase substantially across the outskirts
  - roads should receive cohesive surfaced geometry or road assets rather than remaining as bare raised strips
- Confirmed reusable asset families for the pass:
  - textured road prefabs under `SimplePoly - Town Pack/Prefabs/RoadSegments`
  - additional road objects under `Polygon-Town City/Prefabs/Road_Objects`
  - denser conifer and rock assets under `Polygon-Mega Survival Forest/Prefabs`
- Next scene-authoring pass is focused on terrain readability and traversal, not town-core replacement.

### 2026-03-10 22:10 PT

- Applied the second-pass terrain readability sweep directly in `MainTown.unity`.
- Reworked the existing outer landform masses so the basin no longer reads as flat floor plus vertical wall blocks:
  - expanded / shifted `Ridge_North`, `Ridge_East`, `Ridge_South`, `Ridge_West`, `Ridge_NE`, and `Ridge_SW`
  - widened and re-angled `PlayerOverlookHill` and `WaterTowerHill`
- Added new slope and valley support masses so players have clearer climbable approaches:
  - `RidgeNorth_InnerSlope`
  - `RidgeSouth_InnerSlope`
  - `RidgeEast_InnerSlope`
  - `RidgeWest_InnerSlope`
  - `NorthValleyApproach`
  - `EastValleyApproach`
  - `PlayerOverlookApproach`
  - `WaterTowerApproach`
- Deepened the quarry into a more obvious basin:
  - lowered `QuarryPitFloor`
  - enlarged `QuarryWall_North`, `QuarryWall_East`, and `QuarryRimSouth`
  - resized / repositioned `Quarry_TerraceA`, `Quarry_TerraceB`, and `Quarry_TerraceC`
  - added `QuarrySouthApproach` and `QuarryWestRamp`
- Increased forest density with another wave of saved scene-side conifers:
  - `ForestTree_06` through `ForestTree_11`
- Surfaced the visible road strips by swapping their renderer materials to the textured `SimplePoly - Town Pack` road material:
  - `Road_MainStreet`
  - `MainStreet_EastWest`
  - `MainStreet_NorthSouth`
  - `Loop_North`
  - `Loop_South`
  - `Loop_East`
  - `Loop_West`
  - `Road_ToChurch`
  - `Road_ToQuarry`
  - `Road_ToWaterTower`
  - `Road_ToMotel`
- Re-ran repo docs/context guardrails successfully after the scene patch:
  - `bash scripts/verify-docs-and-context.sh`
- Performed a saved-scene YAML integrity sanity check:
  - no new missing `m_Father` references introduced by the terrain pass
  - the file still contains some older pre-existing missing child references unrelated to this pass

### 2026-03-10 22:20 PT

- Recovered a healthy enough Unity MCP session to run the dedicated layout suite in the live editor.
- Re-ran:
  - `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`
- Result:
  - `2 passed / 0 failed`
- The live passing test covered both:
  - literal-mile world-shell / landmark-root contract presence
  - human-scale landmark, ridge, road-width, quarry-wall, and tree-height checks after the second terrain pass
- Re-ran the full docs/context audit successfully:
  - `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`
- Remaining gap is unchanged:
  - broader `MainTown` PlayMode regression and fresh screenshot/read-back verification are still not completed in this pass

### 2026-03-10 22:35 PT

- User redirected the environment pass away from slope-heavy terrain shaping and toward a clearer planning-map layout.
- Saved the redirected docs:
  - `docs/plans/2026-03-10-maintown-planning-map-redirection-design.md`
  - `docs/plans/2026-03-10-maintown-planning-map-redirection-implementation-plan.md`
- Locked the new pass requirements:
  - keep the existing town core roughly where it is
  - remove or disable the recent slope/ramp helper masses
  - enlarge the map shell to about `2km x 2km`
  - move the outer ridges outward accordingly
  - add more districts and landmarks as proper-size graybox masses
  - keep forest presence
  - prefer overhead-readable markers and labels so the layout is easy to read from a zoomed-out editor view

### 2026-03-10 22:55 PT

- Applied the planning-map redirection directly in `MainTown.unity` instead of extending the slope-heavy terrain pass.
- Kept the original town core / contract-sensitive shell intact while shifting the planning view outward:
  - `BasinFloor` remains expanded to a `2000 x 2000` shell
  - outer ridges stay pushed outward around the larger footprint
  - second-pass slope helpers remain disabled rather than used as the active presentation layer
- Added two more obvious graybox districts so the map reads better from a zoomed-out editor view:
  - `District_WaterTreatment`
  - `District_SchoolCampus`
- Each new district uses large marker pads plus a few simple silhouette masses instead of terrain detail:
  - `PumpHouse`, `ClarifierTank_A`, `ClarifierTank_B`
  - `SchoolMain`, `Gymnasium`, `AdminBlock`
- Kept filling the enlarged outskirts with another small forest pass:
  - `ForestTree_12`
  - `ForestTree_13`
  - `ForestTree_14`
  - `ForestTree_15`
- Tightened the dedicated layout test to match the redirected planning-map contract:
  - require a roughly `2km` shell instead of only literal-mile minimum
  - assert the new district roots exist
  - assert the new district masses and additional tree anchors stay within the expected human / landscape scale envelope
- Added overhead-readable in-world planning labels through Unity MCP after the static YAML pass:
  - `Label_IndustrialYard`
  - `Label_TrailerPark`
  - `Label_ServiceDepot`
  - `Label_TruckStop`
  - `Label_WaterTreatment`
  - `Label_SchoolCampus`
- Unity MCP label authoring quirk discovered and worked around:
  - parented `create` calls zeroed transforms in the current bridge build
  - final labels were authored as root-level `TextMesh` objects at district world coordinates instead

### 2026-03-10 23:05 PT

- Saved the live-edited `MainTown` scene after the planning-map district and label pass.
- Re-ran the dedicated Unity EditMode layout suite against the saved scene:
  - `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`
  - result: `2 passed / 0 failed`
- Re-ran repo verification successfully after the redirected pass:
  - `bash scripts/verify-docs-and-context.sh`
  - `bash scripts/verify-extensible-development-contracts.sh`
  - `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`
- Current validated planning-map additions now include:
  - enlarged `2km` shell expectation
  - the extra graybox districts beyond town core
  - extra forest anchors for the wider outskirts
  - overhead-readable `TextMesh` district labels saved into the scene YAML

### 2026-03-10 23:20 PT

- Approved and documented a further planning-map expansion pass for `MainTown`.
- Locked the new pass direction:
  - shrink the current outer districts
  - add more smaller districts around the shell
  - add a simple river plus small reservoir
  - keep planning-map readability primary
  - keep town core roughly where it is
- Saved supporting docs:
  - `docs/plans/2026-03-10-maintown-district-expansion-water-spine-design.md`
  - `docs/plans/2026-03-10-maintown-district-expansion-water-spine-implementation-plan.md`
- Next scene pass is focused on better shell distribution and water-based separation, not natural terrain detail.

### 2026-03-10 23:38 PT

- Finished the planning-map expansion pass directly in `MainTown.unity`.
- Made the outer planning districts smaller and redistributed them more evenly around the `2km` shell:
  - `District_IndustrialYard`
  - `District_TrailerPark`
  - `District_ServiceDepot`
  - `District_TruckStop`
  - `District_WaterTreatment`
  - `District_SchoolCampus`
- Added four more readable shell districts as compact graybox destinations:
  - `District_MunicipalBlock`
  - `District_FreightYard`
  - `District_RoadsideMarket`
  - `District_RadioTower`
- Added a simple water spine so the shell no longer reads as only dry pads:
  - `Water_RiverWest`
  - `Water_RiverCentral`
  - `Water_ReservoirNorth`
  - `Landmark_RiverBridge`
- Expanded the forest belt anchors again with:
  - `ForestTree_16`
  - `ForestTree_17`
  - `ForestTree_18`
  - `ForestTree_19`
- Refreshed the saved planning labels so the redistributed districts are readable from a zoomed-out editor view:
  - moved the existing label roots for the resized districts
  - added new root labels for `Municipal Block`, `Freight Yard`, `Roadside Market`, and `Radio Tower`
- Verified the pass with a fresh Unity EditMode run:
  - `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`
  - result: `2 passed / 0 failed`

### 2026-03-11 00:15 PT

- Removed the `School Campus` district from `MainTown` because it did not fit the game's assassin / sniper tone.
- Replaced it in-place with a more appropriate `Storage Yard` planning district while keeping the same general shell footprint:
  - `District_StorageYard`
  - `MarkerPad_StorageYard`
  - `StorageOffice`
  - `LockerRow_A`
  - `LockerRow_B`
- Replaced the overhead planning label:
  - `Label_SchoolCampus` -> `Label_StorageYard`
  - `SCHOOL CAMPUS` -> `STORAGE YARD`
- Updated the dedicated `MainTown` layout EditMode test to require the storage-yard contract instead of school-specific names.
- Verified the district swap with a fresh Unity EditMode run:
  - `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`
  - result: `2 passed / 0 failed`

### 2026-03-11 00:58 PT

- Started the dedicated `MainTown` road-network and forest-fill pass at the contract/doc level only.
- Added a conservative upcoming-pass contract for a small set of named road connectors and forest anchors:
  - `RoadConnector_PlayerCompoundToMainStreet`
  - `RoadConnector_ChurchHillSwitchback`
  - `RoadConnector_QuarryTruckRoute`
  - `RoadConnector_MotelFrontage`
  - `ForestAnchor_NorthwestApproach`
  - `ForestAnchor_WestTreeline`
  - `ForestAnchor_ReservoirNorthEdge`
  - `ForestAnchor_QuarrySouthEdge`
- Saved the contract note:
  - `docs/plans/2026-03-11-maintown-road-network-forest-fill-contract.md`
- Updated the EditMode layout contract so the upcoming scene pass has stable names to target.
- Verification has not been claimed for this pass yet:
  - no fresh Unity test run recorded in this log entry
  - no scene mutation claimed in this log entry

### 2026-03-11 01:10 PT

- Implemented the `MainTown` road-network and forest-fill pass in the saved scene.
- Added a mostly paved inter-district connection layer under `ServiceRoads`, including:
  - west-side chain from town toward motel / truck stop / trailer park
  - east-side chain from town toward quarry / industrial yard
  - church-to-depot and municipal / storage connectors
  - frontage or access spurs for freight yard, roadside market, radio tower, and water treatment
- Added dirt / gravel-style service routes for:
  - trailer park to motel back route
  - motel to quarry back route
  - forest access
  - quarry service access
- Added another heavy forest-fill wave in the wider empty shell with `ForestTree_21` through `ForestTree_36`.
- Reconciled the new contract files to the actual live scene names:
  - updated the focused `MainTown` layout EditMode suite to require the current road and forest anchor names
  - updated `docs/plans/2026-03-11-maintown-road-network-forest-fill-contract.md`
- Verified the saved scene with a fresh Unity EditMode run:
  - `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`
  - result: `3 passed / 0 failed`

### 2026-03-11 01:42 PT

- Added a much heavier `MainTown` forest-density expansion wave in the saved scene with `ForestTree_37` through `ForestTree_70`.
- Focused the new tree coverage on the map shell and water framing instead of crowding the town core:
  - west and south-west perimeter
  - south and south-east edge
  - north and north-east reservoir edge
  - river / mid-shell filler bands
- Preserved the current planning-map readability by keeping the density pass out of the central label cluster and major landmark cores.
- Extended the focused forest-anchor contract with a small representative subset from the new wave:
  - `ForestTree_37`
  - `ForestTree_40`
  - `ForestTree_44`
  - `ForestTree_48`
  - `ForestTree_52`
  - `ForestTree_56`
- Saved the updated density-expansion design and implementation docs:
  - `docs/plans/2026-03-11-maintown-forest-density-expansion-design.md`
  - `docs/plans/2026-03-11-maintown-forest-density-expansion-implementation-plan.md`
- Verified the expanded forest pass with a fresh Unity EditMode run:
  - `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`
  - result: `3 passed / 0 failed`
- Re-ran the repo doc/rule guardrails successfully:
  - `bash scripts/verify-docs-and-context.sh`
  - `bash scripts/verify-extensible-development-contracts.sh`
  - `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`

### 2026-03-11 02:12 PT

- Added a mass-forest layering pass to `MainTown` by duplicating the saved `District_ForestBelt` tree field into three new world-shell density roots:
  - `ForestDensityLayer_West`
  - `ForestDensityLayer_SouthEast`
  - `ForestDensityLayer_NorthEast`
- Used the layer offsets and slight rotation changes to add a few hundred more trees without serially hand-placing hundreds of one-off anchors.
- Coverage intent by layer:
  - `ForestDensityLayer_West` thickens the west and south-west perimeter
  - `ForestDensityLayer_SouthEast` thickens the south-east shell and quarry / industrial approaches
  - `ForestDensityLayer_NorthEast` thickens the north-east shell plus river / reservoir framing
- Preserved readability windows around:
  - `TownCore`
  - `ChurchHill`
  - `UtilityLandmarks`
  - quarry interior / work bowl
  - major road junctions and district labels
- Saved the supporting design and implementation docs:
  - `docs/plans/2026-03-11-maintown-mass-forest-layering-design.md`
  - `docs/plans/2026-03-11-maintown-mass-forest-layering-implementation-plan.md`
- Read-back evidence after scene save:
  - `MainTown.unity` now contains `300` named `ForestTree_*` entries
- Red/green verification:
  - before the scene mutation, refreshed Unity EditMode failed on missing `ForestDensityLayer_West`
  - after the scene mutation and scene save, `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests` passed with `3 passed / 0 failed`
- Re-ran the repo doc/rule guardrails successfully:
  - `bash scripts/verify-docs-and-context.sh`
  - `bash scripts/verify-extensible-development-contracts.sh`
  - `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`

### 2026-03-11 02:47 PT

- Added a targeted outer-gap pocket-woods pass instead of another map-wide tree blanket.
- Created three new world-shell gap-cluster roots:
  - `ForestGapCluster_West`
  - `ForestGapCluster_East`
  - `ForestGapCluster_North`
- Populated each root with a compact duplicated tree group so the empty outer-district gaps read as local woods:
  - west cluster separates motel / truck stop side approaches
  - east cluster thickens the quarry / industrial / roadside-market side gap
  - north cluster thickens the storage / municipal / service-depot side gap
- Preserved readability for:
  - `TownCore`
  - `ChurchHill`
  - `UtilityLandmarks`
  - quarry interior / bowl
  - major road junctions and district labels
- Saved the supporting design and implementation docs:
  - `docs/plans/2026-03-11-maintown-outer-gap-pocket-woods-design.md`
  - `docs/plans/2026-03-11-maintown-outer-gap-pocket-woods-implementation-plan.md`
- Red/green verification:
  - before the scene mutation, refreshed Unity EditMode failed on missing `ForestGapCluster_West`
  - after the scene mutation and scene save, `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests` passed with `3 passed / 0 failed`
- Re-ran the repo doc/rule guardrails successfully:
  - `bash scripts/verify-docs-and-context.sh`
  - `bash scripts/verify-extensible-development-contracts.sh`
  - `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`

### 2026-03-11 03:25 PT

- Investigated the broken road-texture report from the live `MainTown` scene and confirmed the root cause:
  - visible roads were still stretched `Cube` meshes
  - those strips were using `sptp_Main_Mat`
  - the atlas-driven material produced the banner-collage look on long road ribbons
- Attempted the requested `EasyRoads3D` rebuild path and documented the real blocker from the Unity editor log:
  - the installed package build warns `The free version does not support API calls`
  - scripted `CreateRoad(...)` returned `null` for every route
- Saved the approved fallback docs:
  - `docs/plans/2026-03-11-maintown-road-surface-replacement-design.md`
  - `docs/plans/2026-03-11-maintown-road-surface-replacement-implementation-plan.md`
- Added a focused road-surface contract to `MainTownLayoutEditModeTests`:
  - representative paved roads must use the paved road material
  - representative dirt spurs must use the dirt road material
  - representative roads must no longer use `Cube` mesh rendering
  - verified the red step through Unity EditMode:
    - failure: `MainStreet_EastWest` still used `Cube`
- Added `MainTownRoadSurfaceRebuilder` under `Assets/_Project/World/Editor`.
- Rebuilt the saved `MainTown` road presentation in one editor-side pass:
  - updated `58` visual road strips
  - preserved route names and topology
  - swapped visible road meshes off the cube blockout surface
  - assigned paved or dirt material by route type
- Verified green through Unity EditMode:
  - `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`
  - result: `3 passed / 0 failed`

### 2026-03-11 03:55 PT

- Approved and documented the `MainTown` terrain-planning pivot:
  - `docs/plans/2026-03-11-maintown-terrain-planning-pivot-design.md`
  - `docs/plans/2026-03-11-maintown-terrain-planning-pivot-implementation-plan.md`
- Replaced the focused `MainTown` layout suite with a planning-shell contract:
  - district roots, marker pads, labels, and roads must remain
  - water, forest-density roots, forest-tree presentation, and mountain/hill presentation must be gone
  - road surface contract remains in place
- Verified the red step through Unity EditMode before scene cleanup:
  - failure: `Water_RiverWest` still existed under `MainTownWorldShell`
- Added `MainTownTerrainPlanningCleanup` under `Assets/_Project/World/Editor`.
- Ran the saved-scene cleanup pass and stripped the landscape presentation layer from `MainTown`:
  - removed `352` objects
  - removed water roots
  - removed forest tree / density / gap roots
  - removed `MountainRim`
  - removed hill / slope / approach masses such as `PlayerOverlookHill`, `WaterTowerHill`, and `ChurchSlope`
  - preserved district roots, marker pads, labels, roads, and the town blockout
- Verified green through Unity EditMode after the cleanup:
  - `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`
  - result: `3 passed / 0 failed`

### 2026-03-11 04:28 PT

- Confirmed the package pivot for paintable terrain authoring:
  - `Packages/manifest.json` now includes `com.unity.terrain-tools`
- Approved and documented the terrain-bootstrap pass:
  - `docs/plans/2026-03-11-maintown-terrain-bootstrap-design.md`
  - `docs/plans/2026-03-11-maintown-terrain-bootstrap-implementation-plan.md`
- Extended the focused `MainTown` EditMode contract with a dedicated terrain-bootstrap test:
  - `MainTownTerrain` must exist under `MainTownWorldShell`
  - it must carry `Terrain` and `TerrainCollider`
  - terrain size must cover the planning shell
  - terrain must have at least four starter terrain layers
- Verified the red step through Unity EditMode before scene mutation:
  - failure: `MainTownTerrain` was missing under `MainTownWorldShell`
- Added `Assets/_Project/World/Editor/MainTownTerrainBootstrap.cs`.
- Bootstrapped a real Unity terrain into the saved `MainTown` scene:
  - created `MainTownTerrain` under `MainTownWorldShell`
  - created `Assets/_Project/World/Terrain/MainTown/MainTownTerrainData.asset`
  - created starter `TerrainLayer` assets:
    - `MainTown_Grass`
    - `MainTown_Dirt`
    - `MainTown_Road`
    - `MainTown_Stone`
  - used raw project textures for TerrainLayer sources rather than scene materials
  - set terrain size to `2000 x 120 x 2000`
  - flattened the bootstrap terrain so the scene is paint-ready, not pre-sculpted
  - seeded broad starter paint:
    - grass default fill
    - paved paint under the current paved road layout
    - dirt paint under `Road_Dirt_*` spurs
    - stone/quarry paint in `District_QuarryBasin`
  - deactivated `BasinFloor` so the terrain becomes the visible planning surface
- Editor-bridge note:
  - the live Unity MCP menu call timed out due bridge churn
  - Unity `Editor.log` still recorded successful execution of `MainTownTerrainBootstrap.BootstrapMainTownTerrain`
- Verified green through Unity EditMode after bootstrap:
  - `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`
  - result: `4 passed / 0 failed`

### 2026-03-11 04:59 PT

- Approved and documented the dramatic terrain redesign pivot:
  - `docs/plans/2026-03-11-maintown-dramatic-terrain-redesign-design.md`
  - `docs/plans/2026-03-11-maintown-dramatic-terrain-redesign-implementation-plan.md`
- Kept the authored district shell, labels, roads, and runtime roots, but redirected the terrain pass away from flat starter paint and toward world-shape first.
- Extended the focused `MainTown` EditMode contract with a dramatic-terrain regression:
  - terrain must have meaningful height range
  - outer terrain samples must sit above the basin center
  - quarry terrain must sit below the town center
  - simple water presentation roots must exist
  - forest coverage must exist through terrain tree instances
- Verified the red step through Unity EditMode before scene mutation:
  - failure: `MainTown terrain` still read as a flat shell with `0.0f` effective height range
- Extended `Assets/_Project/World/Editor/MainTownTerrainBootstrap.cs` with a dramatic terrain redesign menu path.
- Re-authored the saved `MainTown` terrain presentation:
  - cleared the broad road/dirt/quarry starter paint back to a neutral terrain base
  - sculpted a higher outer ring / mountain edge
  - carved a central basin
  - cut a river corridor
  - added a reservoir pocket
  - sank the quarry below town grade
  - added stronger overlook zones including the outer radio-tower side ridge
  - restored simple water presentation roots:
    - `Water_RiverChannel`
    - `Water_ReservoirBasin`
  - repopulated forests through terrain tree instances instead of one-off tree GameObjects
  - final saved terrain-tree count after the pass: `1070`
- Tree-authoring refinement:
  - the first mixed-forest tree-prototype set emitted Unity terrain-tree warnings
  - narrowed the terrain forest prototype set to `SM_Pine.prefab` for a cleaner terrain-tree regression surface
- Editor-bridge note:
  - live Unity MCP menu calls still timed out during reload churn
  - Unity `Editor.log` recorded repeated successful execution of `ApplyMainTownDramaticTerrainRedesign`
- Verified green through Unity EditMode after the redesign:
  - `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`
  - result: `5 passed / 0 failed`

## Open Risks

- Scene-authoring mutations must preserve all existing runtime-critical roots used by travel, population, contract, and inventory flows.
- The installed `EasyRoads3D` build in this project does not support scripted API road creation, so future automated road-authoring passes must continue using direct scene/prefab surface replacement unless the package/tooling changes.
- The current planning-shell scene is intentionally sparse until the later terrain-paint pass, so it will look unfinished until terrain layers and painted ground are added.
- The new terrain shell is dramatically shaped, but it still uses placeholder-neutral terrain painting. Road shoulders, slope-aware texturing, better water presentation, and more varied forest species are still follow-up work.
- The larger shell exists around the old runtime cluster, so follow-up passes may still want to relocate spawn, vendors, and local utility objects deeper into the authored town layout once broader gameplay tests are green.
- Broader existing world regression runs and fresh screenshot capture remain pending until the Unity editor bridge is healthy again.

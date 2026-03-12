# MainTown Literal-Mile Rebuild Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Rebuild `MainTown` into a literal-mile Appalachian sniper-sandbox shell with authored districts, long sightlines, and preserved runtime scene contracts.

**Architecture:** Keep all current runtime-critical roots in `MainTown`, then layer a new `MainTownWorldShell` hierarchy around them. Use batched Unity MCP scene authoring for the large world rebuild, keep landmark naming deterministic for future contract/population integration, and validate the scene with a dedicated layout EditMode test plus existing world PlayMode coverage.

**Tech Stack:** Unity 6.3, Unity MCP batched scene/probuilder commands, EasyRoads3D-compatible road layout where practical, existing third-party environment prefabs, NUnit EditMode/PlayMode scene tests.

## Scale Envelope

- Character reference:
  - standing height should read around `1.8m`
  - reliable jump / climb threshold should read around `1m`
- One-story masses:
  - player house, reloading shop, gun store, and generic low commercial shells should usually land around `4.5m` to `7.5m` tall
  - door-height assumptions should stay compatible with `2m` clear entries and `3m` to `4m` facade rhythms
- Larger civic / landmark masses:
  - police station should read like a compact two-story municipal shell in the `8m` to `14m` range
  - hospital block should read as a larger civic shell in the `10m` to `18m` range
  - church body should stay near `10m` to `18m`, while the church tower should read as a true town-overwatch landmark in the `30m` to `60m` range
  - water tower tank should sit clearly above roofline, with the tank center at least `25m` above grade
- Natural landmarks:
  - pines should generally render in the `15m` to `45m` range
  - quarry walls should read around `35m` to `90m`
  - surrounding ridges should read around `120m` to `260m` so the basin feels regionally large and supports long-range overwatch
- Roads and lots:
  - main public roads should generally read around `14m` to `20m` wide including shoulders
  - parking pads and yards should be large enough for vehicles, ambush spacing, and long storefront sightlines rather than tiny prop plazas

---

### Task 1: Add a failing scene-layout contract test

**Files:**
- Create: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`
- Verify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`

**Step 1: Write the failing test**

- Assert `MainTown` contains a `MainTownWorldShell` root.
- Assert `BasinFloor` spans at least `1600` units in both `X` and `Z`.
- Assert world-shell children exist for:
  - `MountainRim`
  - `District_TownCore`
  - `District_PlayerCompound`
  - `District_ChurchHill`
  - `District_QuarryBasin`
  - `District_ForestBelt`
  - `District_UtilityLandmarks`
  - `District_MotelStrip`
  - `PerimeterLoopRoad`
  - `MainStreetSpine`
  - `ServiceRoads`
  - `Landmark_Church`
  - `Landmark_WaterTower`
  - `Landmark_QuarryTerraces`
  - `Landmark_PoliceStation`
  - `Landmark_Hospital`
  - `Landmark_GunStore`
  - `Landmark_ReloadingSupply`
  - `Landmark_PlayerHouse`
  - `Landmark_Motel`

**Step 2: Run test to verify it fails**

Run: `bash scripts/run-unity-tests.sh editmode Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests tmp/maintown-layout-red.xml tmp/maintown-layout-red.log`

Expected: FAIL because `MainTownWorldShell` and its district roots do not exist in the current scene.

### Task 2: Author the world-shell hierarchy and literal-mile footprint

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Update: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Create the world-shell root and footprint**

- Add `MainTownWorldShell` as a new top-level scene root.
- Add `BasinFloor` under it with a scale of at least `1609 x 1 x 1609`.
- Add a `MountainRim` hierarchy framing the playable basin on the north, east, south, and west edges.

**Step 2: Create authored district roots**

- Add district children under `MainTownWorldShell`:
  - `District_TownCore`
  - `District_PlayerCompound`
  - `District_ChurchHill`
  - `District_QuarryBasin`
  - `District_ForestBelt`
  - `District_UtilityLandmarks`
  - `District_MotelStrip`

**Step 3: Preserve runtime-critical roots**

- Do not delete or rename existing scene-contract/runtime roots:
  - `MainTownEntry_Spawn`
  - `MainTownEntry_Return`
  - `MainTown_SmokeToIndoor_Trigger`
  - `PlayerRoot`
  - `ReloadingWorkbench`
  - `StorageChest`
  - `MainTownContractRuntime`
  - `MainTownPopulationRuntime`
  - `CoreWorldController`
  - existing vendors and economy/controller roots

**Step 4: Update progress doc**

- Record world-shell hierarchy creation and any preserved runtime-root decisions.

### Task 3: Build terrain-first landmark masses and road skeleton

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Update: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Add landmark roots**

- Under `MainTownWorldShell`, create:
  - `Landmark_Church`
  - `Landmark_WaterTower`
  - `Landmark_QuarryTerraces`
  - `Landmark_PoliceStation`
  - `Landmark_Hospital`
  - `Landmark_GunStore`
  - `Landmark_ReloadingSupply`
  - `Landmark_PlayerHouse`
  - `Landmark_Motel`

**Step 2: Add road roots**

- Create:
  - `PerimeterLoopRoad`
  - `MainStreetSpine`
  - `ServiceRoads`
- Author a first road skeleton connecting town core, church hill, quarry basin, utility hill, motel strip, and player compound.

**Step 3: Author terrain-first masses**

- Add large-scale mountain/ridge masses, quarry terraces, forest blockers, and district-blockout buildings with batch-oriented MCP operations.
- Keep the blockout inside the scale envelope above so one-story buildings, civic landmarks, roads, and trees all read correctly against a `1.8m` character.
- Prioritize sightlines and map readability over interior completeness.

**Step 4: Update progress doc**

- Record completed districts, landmarks, and any compromises in asset/tool usage.

### Task 4: Make the layout test pass and verify scene integrity

**Files:**
- Verify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`
- Verify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Update: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Run the new layout test**

Run: `bash scripts/run-unity-tests.sh editmode Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests tmp/maintown-layout-green.xml tmp/maintown-layout-green.log`

Expected: PASS

**Step 2: Run existing targeted world tests**

Run: `bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.MainTownCombatWiringEditModeTests|Reloader.World.Tests.EditMode.WorldSceneContractValidatorEditModeTests" tmp/maintown-world-edit.xml tmp/maintown-world-edit.log`

Run: `bash scripts/run-unity-tests.sh playmode "Reloader.World.Tests.PlayMode.RoundTripTravelPlayModeTests|Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests|Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests" tmp/maintown-world-play.xml tmp/maintown-world-play.log`

Expected: PASS, or failures documented precisely with blockers and logs.

**Step 3: MCP read-back verification**

- Read back the rebuilt scene hierarchy.
- Confirm the world-shell roots, landmark roots, and preserved runtime roots exist simultaneously.
- Capture a fresh scene screenshot for manual review.

**Step 4: Update progress doc**

- Record test commands, pass/fail counts, and read-back verification summary.

# MainTown Terrain Second Pass Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Upgrade `MainTown` from a scale-correct shell into a more natural, climbable sniper landscape with sloped ridges, valleys, a deeper quarry, denser forest cover, and surfaced roads while preserving existing scene contracts.

**Architecture:** Keep the existing `MainTownWorldShell` and town/district layout, then perform a second-pass environment authoring sweep over the outskirts and circulation network. Use the existing named ridge, hill, quarry, forest, and road roots so the new work layers onto the current scene contract rather than replacing it wholesale.

**Tech Stack:** Unity 6.3 scene authoring, Unity MCP batched operations where reliable, direct `.unity` YAML edits where MCP save/transform operations remain unstable, EasyRoads3D fallback-compatible road dressing, existing third-party road/forest prefabs, NUnit EditMode tests.

---

### Task 1: Document the approved terrain-pass direction

**Files:**
- Create: `docs/plans/2026-03-10-maintown-terrain-second-pass-design.md`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Save the approved design**

- Record the approved `Hybrid` terrain pass.
- Capture the required traversal contract:
  - all interior hills climbable
  - perimeter ridge climbable to a controlled shelf
  - steep non-playable outer backface
  - quarry as the deepest basin
  - denser forest
  - surfaced roads

**Step 2: Update the progress log before scene mutation**

- Add an activity-log entry for the new second-pass scope and the user-approved terrain direction.

### Task 2: Audit reusable road and forest assets

**Files:**
- Verify: `Reloader/Assets/ThirdParty/SimplePoly - Town Pack/Prefabs/RoadSegments/**`
- Verify: `Reloader/Assets/ThirdParty/Polygon-Town City/Prefabs/Road_Objects/**`
- Verify: `Reloader/Assets/ThirdParty/Polygon-Mega Survival Forest/Prefabs/**`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Confirm road-surface fallback assets**

- Verify textured road-prefab candidates for:
  - town main roads
  - rural side roads
  - sidewalk or shoulder transitions if needed

**Step 2: Confirm forest/rock density assets**

- Verify conifer and rock prefabs suitable for:
  - ridge tree lines
  - valley concealment
  - quarry rim breakup

**Step 3: Record asset choices in the progress log**

- Document which asset families were selected for roads and forest density.

### Task 3: Reshape climbable hills and perimeter ridges

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Verify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Preserve the scene contract**

- Do not rename or delete:
  - `MainTownWorldShell`
  - district roots
  - landmark roots
  - runtime-critical roots and spawn/travel objects

**Step 2: Convert named hills into sloped, traversable masses**

- Rework:
  - `PlayerOverlookHill`
  - `WaterTowerHill`
  - the church-hill support masses
- Ensure approach slopes feel walkable rather than pedestal-like.

**Step 3: Rework the perimeter into a climbable ridge ring**

- Reshape:
  - `Ridge_North`
  - `Ridge_East`
  - `Ridge_South`
  - `Ridge_West`
  - `Ridge_NE`
  - `Ridge_SW`
- Add or reposition transitional slope masses so players can reach selected ridge shelves.
- Keep a steeper outer backface beyond the playable shelf.

**Step 4: Cut valleys and lower approach channels**

- Add or reshape valley masses to break the ridge continuity at selected points.
- Ensure the world no longer reads as flat basin plus walls.

**Step 5: Update the progress log**

- Record which ridge/hill/valley objects were reshaped and any MCP-to-YAML fallback used.

### Task 4: Deepen the quarry basin and improve basin readability

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Deepen quarry grade**

- Push `QuarryPitFloor` lower relative to town grade.

**Step 2: Strengthen enclosure**

- Rework:
  - `QuarryWall_North`
  - `QuarryWall_East`
  - `QuarryRimSouth`
  - `Quarry_TerraceA`
  - `Quarry_TerraceB`
  - `Quarry_TerraceC`
- Make the quarry read as a real cut into the terrain, not shallow blockout.

**Step 3: Keep traversal intentional**

- Preserve at least one readable descent route via terraces or broad sloped access.

**Step 4: Update the progress log**

- Record quarry depth and traversal decisions.

### Task 5: Densify forest and landscape clutter

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Add conifer coverage**

- Increase tree count substantially under `District_ForestBelt` and along ridge/valley approaches.
- Favor clustered placement over even spacing.

**Step 2: Add secondary clutter**

- Add rocks, bushes, and similar landscape breakup where useful for concealment and silhouette variety.

**Step 3: Preserve tactical readability**

- Keep some sightline corridors and avoid filling every lane with dense foliage.

**Step 4: Update the progress log**

- Record the density pass and any asset choices.

### Task 6: Surface the roads with cohesive road assets

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Attempt the preferred road-authoring path**

- If EasyRoads3D is stable through current tooling, use it for the connected surfaced road network.

**Step 2: Apply fallback if needed**

- If EasyRoads3D is not reliable in the live editor workflow, skin or replace the existing named road objects with textured road prefabs under:
  - `PerimeterLoopRoad`
  - `MainStreetSpine`
  - `ServiceRoads`

**Step 3: Keep road identity differentiated**

- Main town roads should read more paved and coherent.
- Rural/service/quarry spurs can read narrower or rougher.

**Step 4: Update the progress log**

- Record whether EasyRoads3D or prefab-based road surfacing was used.

### Task 7: Re-verify scene contract and document blockers

**Files:**
- Verify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`
- Verify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Re-run the dedicated layout EditMode test if Unity MCP is healthy**

Run: Unity MCP `run_tests` for `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`

Expected: PASS, or precise timeout/blocker recorded.

**Step 2: Re-run repo guardrails**

Run:
- `bash scripts/verify-docs-and-context.sh`
- `bash scripts/verify-extensible-development-contracts.sh`
- `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`

Expected: PASS

**Step 3: Record final verification state truthfully**

- If Unity MCP remains unstable, document that the scene file and tests were updated but broader live-editor verification remains blocked.

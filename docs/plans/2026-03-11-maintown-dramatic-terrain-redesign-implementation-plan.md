# MainTown Dramatic Terrain Redesign Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the flat `MainTown` terrain presentation with a dramatic basin/river/mountain/forest pass while preserving district readability and scene runtime contracts.

**Architecture:** Tighten the focused `MainTown` EditMode tests first so they reject a flat terrain shell, then extend the existing terrain bootstrap tooling into a terrain-sculpting/world-presentation utility that clears the broad starter paint, sculpts the terrain heightfield, restores simple river/reservoir water surfaces, authors regional forest coverage, and saves the scene.

**Tech Stack:** Unity Terrain system, Terrain tree instances and/or prefab placement, Unity EditMode tests, editor automation, `MainTown.unity`

---

### Task 1: Lock the Dramatic Terrain Contract

**Files:**
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`

**Step 1: Write the failing test**

- Require:
  - `MainTownTerrain` still exists and remains the terrain authority
  - terrain height range is meaningfully above flat-shell level
  - outer ring sample points are higher than the town-center sample
  - quarry sample point is lower than town-center sample
  - simple river / reservoir water roots exist again
  - forest population exists in a stable measurable form

**Step 2: Run test to verify it fails**

Run:

```bash
Unity EditMode test: Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests
```

Expected:
- fail because the current terrain is still broad and flat and the water / forest presentation has not been restored yet

### Task 2: Implement the Terrain Sculpting Pass

**Files:**
- Modify: `Reloader/Assets/_Project/World/Editor/MainTownTerrainBootstrap.cs`
- Modify if cleaner than extending bootstrap: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`

**Step 1: Reset starter texture presentation**

- Remove the broad road/dirt/quarry alphamap presentation
- Leave the terrain layers available, but return the visible terrain to a neutral base so shape is the main output

**Step 2: Sculpt the terrain**

- Keep the terrain footprint around `2000 x 120 x 2000`
- Create:
  - central basin floor
  - outer mountain ring
  - valley cuts
  - quarry depression
  - river corridor
  - reservoir/lake pocket
  - a few stronger overlook ridges/spurs

**Step 3: Add simple water presentation**

- Create or reuse stable scene roots for:
  - river surface
  - reservoir surface
- Keep the presentation simple and blockout-safe

**Step 4: Add regional forests**

- Use terrain-tree or stable prefab-group authoring to add forest regions around:
  - outer slopes
  - ridge shoulders
  - river / reservoir framing
  - selected outer-district gaps
- Preserve clear windows around town core, quarry bowl, labels, and primary landmark reads

**Step 5: Save the scene**

- Mark dirty
- Save `MainTown.unity`

### Task 3: Verify and Document

**Files:**
- Modify: `docs/plans/2026-03-11-maintown-road-network-forest-fill-contract.md`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Re-run verification**

Run:

```bash
Unity EditMode test: Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

**Step 2: Update progress log**

- Record:
  - design redirection from flat terrain bootstrap to dramatic terrain shaping
  - terrain heightfield sculpting
  - water presentation restoration
  - forest-region authoring
  - verification evidence

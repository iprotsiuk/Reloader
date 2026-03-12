# MainTown Terrain Bootstrap Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a real paint-ready Unity terrain to `MainTown` while preserving the current planning shell and runtime scene contracts.

**Architecture:** Extend the focused `MainTown` layout tests to require a real terrain root first, then add an editor bootstrap utility that creates the terrain asset/data, creates terrain layers from existing texture/material assets, seeds broad alphamap painting from the named road and quarry layout, and saves the scene.

**Tech Stack:** Unity 6 Terrain system, TerrainLayer assets, Unity EditMode tests, editor automation, `MainTown.unity`

---

### Task 1: Lock the Terrain Contract

**Files:**
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`

**Step 1: Write the failing test**

- Require:
  - `MainTownTerrain` under `MainTownWorldShell`
  - `Terrain` and `TerrainCollider` components
  - terrain data size around `2000m x 2000m`
  - at least four terrain layers

**Step 2: Run test to verify it fails**

Run:

```bash
Unity EditMode test: Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests
```

Expected:
- fail because `MainTownTerrain` does not exist yet

### Task 2: Add the Terrain Bootstrap Utility

**Files:**
- Create: `Reloader/Assets/_Project/World/Editor/MainTownTerrainBootstrap.cs`

**Step 1: Create terrain assets**

- Create or reuse:
  - `TerrainData`
  - `TerrainLayer` assets for grass, dirt, asphalt, and stone
- Store them under a stable world folder, for example:
  - `Assets/_Project/World/Terrain/MainTown/`
- Use raw texture assets as the TerrainLayer sources rather than relying on scene materials.

**Step 2: Create or reuse scene terrain**

- Create `MainTownTerrain` under `MainTownWorldShell`
- Set size to roughly `2000 x 120 x 2000`
- Position it so the current town layout stays centered on the terrain

**Step 3: Seed starter paint**

- Fill with grass
- Paint paved strips under paved road routes
- Paint dirt strips under `Road_Dirt_*`
- Paint a stone/quarry patch in `District_QuarryBasin`
- Disable `BasinFloor` so the new terrain becomes the visible planning surface.

**Step 4: Save the scene**

- Mark dirty
- save `MainTown.unity`

### Task 3: Verify and Document

**Files:**
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`
- Modify if needed: `docs/plans/2026-03-11-maintown-road-network-forest-fill-contract.md`

**Step 1: Execute terrain bootstrap**

- Run the menu utility in Unity

**Step 2: Re-run verification**

Run:

```bash
Unity EditMode test: Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

**Step 3: Update progress log**

- Record:
  - Terrain Tools package presence
  - terrain root creation
  - terrain layer setup
  - starter paint pass
  - verification evidence

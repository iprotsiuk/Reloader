# MainTown Terrain-Planning Pivot Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Strip `MainTown` back to a cleaner terrain-planning shell that preserves district/town markup while removing trees, rivers, and hill/mountain presentation masses.

**Architecture:** Update the focused `MainTown` layout tests to the new planning-mode contract first, then run a dedicated editor cleanup utility that deletes the terrain-presentation layer from the saved scene while leaving district roots, labels, marker pads, roads, and core town blockout intact.

**Tech Stack:** Unity 6, Unity EditMode tests, scene cleanup editor automation, `MainTown.unity`

---

### Task 1: Lock the Planning-Mode Contract

**Files:**
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`

**Step 1: Write the failing test**

- Update the focused layout contract to require:
  - preserved district roots
  - preserved marker pads
  - preserved district labels
  - no water roots
  - no forest density / gap roots
  - no mountain rim / ridge / hill presentation roots
- Keep the current road-surface contract in place.

**Step 2: Run test to verify it fails**

Run:

```bash
Unity EditMode test: Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests
```

Expected:
- fail because the current scene still contains water / forest / hill presentation objects

### Task 2: Add the Scene Cleanup Utility

**Files:**
- Create: `Reloader/Assets/_Project/World/Editor/MainTownTerrainPlanningCleanup.cs`

**Step 1: Implement a menu utility**

- Load `MainTown`
- Find `MainTownWorldShell`
- Delete or disable the terrain-presentation layer by exact names / prefixes:
  - `Water_*`
  - `ForestTree_*`
  - `ForestDensityLayer_*`
  - `ForestGapCluster_*`
  - `MountainRim`
  - ridge / hill / slope / approach / ramp masses
  - `Landmark_RiverBridge`

**Step 2: Preserve the planning shell**

- Keep:
  - district roots
  - labels
  - marker pads
  - roads
  - core town blockout roots

**Step 3: Save the scene**

- Mark dirty
- save `MainTown.unity`

### Task 3: Verify and Document

**Files:**
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`
- Modify if needed: `docs/plans/2026-03-11-maintown-road-network-forest-fill-contract.md`

**Step 1: Execute cleanup**

- Run the cleanup menu item in Unity

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
  - the approved terrain-planning pivot
  - the removed presentation layers
  - the preserved planning markup
  - verification evidence

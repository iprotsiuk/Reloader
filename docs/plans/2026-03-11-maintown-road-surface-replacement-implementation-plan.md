# MainTown Road Surface Replacement Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace `MainTown` atlas-textured road cubes with stable textured paved and dirt road surfaces while preserving the existing route layout.

**Architecture:** Keep the current named road objects in `MainTown`, add a small editor-side rebuild utility that swaps their meshes and materials based on route type, and persist the scene after the conversion. Validate the new contract with a focused EditMode layout test that rejects the old cube-plus-atlas road setup.

**Tech Stack:** Unity 6, Unity EditMode tests, editor automation under `Assets/_Project/World/Editor`, scene persistence in `MainTown.unity`

---

### Task 1: Lock the Road-Surface Contract

**Files:**
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`

**Step 1: Write the failing test**

- Add a focused assertion covering representative paved and dirt road objects.
- Assert they do not use `sptp_Main_Mat`.
- Assert their mesh is not `Cube`.

**Step 2: Run test to verify it fails**

Run:

```bash
Unity EditMode test: Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests
```

Expected:
- fail on one or more road objects still using the atlas material and/or `Cube`

### Task 2: Add the Road Replacement Utility

**Files:**
- Create: `Reloader/Assets/_Project/World/Editor/MainTownRoadSurfaceRebuilder.cs`

**Step 1: Implement a menu utility**

- Load `MainTown`
- Find `MainTownWorldShell`
- Find representative road-strip objects by current naming conventions:
  - `MainStreet*`
  - `Road_*`
  - `PerimeterLoopRoad`
- Exclude non-rendered grouping roots like `ServiceRoads`

**Step 2: Swap road presentation**

- Load one flat road mesh from:
  - `Assets/ThirdParty/SimplePoly - Town Pack/Prefabs/RoadSegments/sptp_asphalt_01.prefab`
- Load paved material from:
  - `Assets/EasyRoads3D/Resources/Materials/roads/road material.mat`
- Load dirt material from:
  - `Assets/EasyRoads3D/Resources/Materials/roads/dirt material.mat`
- For each visual road strip:
  - preserve transform and object name
  - assign the flat road mesh
  - assign paved or dirt material by route name
  - keep collider behavior intact unless scene validation requires a cleanup

**Step 3: Save the scene**

- Mark dirty
- save `MainTown.unity`

### Task 3: Execute and Verify

**Files:**
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`
- Modify if needed: `docs/plans/2026-03-11-maintown-road-network-forest-fill-contract.md`

**Step 1: Run the rebuild utility**

- Execute the new menu item in Unity

**Step 2: Run focused verification**

Run:

```bash
Unity EditMode test: Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

Expected:
- layout suite passes
- docs/context guardrails pass

**Step 3: Update progress log**

- Record:
  - EasyRoads API block
  - approved road-surface fallback
  - mesh/material conversion result
  - verification evidence

# MainTown District Expansion And Water Spine Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Expand the `MainTown` planning map by shrinking oversized outer districts, adding more smaller districts, and introducing a simple river plus reservoir separator while preserving scene contracts.

**Architecture:** Keep the current `MainTownWorldShell`, town core, runtime roots, and existing planning-map district hierarchy. Rebalance the outer shell by resizing current district marker pads and building masses, then add a small set of new compact district roots and simple water landmarks as graybox/readability objects with matching labels.

**Tech Stack:** Unity 6.3 scene authoring, direct `.unity` YAML edits, Unity MCP for scene save/labels/tests, existing `MainTownLayoutEditModeTests`, repo docs/context guardrails.

---

### Task 1: Document the expansion-water pass

**Files:**
- Create: `docs/plans/2026-03-10-maintown-district-expansion-water-spine-design.md`
- Create: `docs/plans/2026-03-10-maintown-district-expansion-water-spine-implementation-plan.md`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Save the approved design**

- Record:
  - smaller outer districts
  - more districts around the shell
  - river plus small reservoir
  - planning-map readability still primary

**Step 2: Update the progress log before scene mutation**

- Add a new entry for the expansion-water pass.

### Task 2: Rebalance the current outer districts

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Shrink the current district marker pads**

- Reduce planning pads and main masses for:
  - `District_IndustrialYard`
  - `District_TrailerPark`
  - `District_ServiceDepot`
  - `District_TruckStop`
  - `District_WaterTreatment`
  - `District_SchoolCampus`

**Step 2: Keep named tested masses believable**

- Maintain current human-scale expectations for the building shells.
- Avoid making new duplicate names that would confuse the existing name-based tests.

**Step 3: Update the progress log**

- Record which districts were reduced.

### Task 3: Add more compact districts around the shell

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Add new district roots**

- Add compact graybox districts such as:
  - `District_MunicipalBlock`
  - `District_FreightYard`
  - `District_RoadsideMarket`
  - `District_RadioTower`

**Step 2: Add marker pads and simple silhouettes**

- Keep each district to a few obvious blocks.
- Place them around the map edge to improve shell distribution.

**Step 3: Add labels if practical**

- Mirror the previous overhead label pattern using root-level `TextMesh` objects if parented label placement remains unreliable.

**Step 4: Update the progress log**

- Record the new districts and positions.

### Task 4: Add the river and reservoir separator

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Add simple water objects**

- Add:
  - `Water_RiverWest`
  - `Water_RiverCentral`
  - `Water_ReservoirNorth`

**Step 2: Add one simple crossing landmark**

- Add:
  - `Landmark_RiverBridge`

**Step 3: Keep geometry planning-map simple**

- Use broad readable shapes instead of naturalistic sculpting.

**Step 4: Update the progress log**

- Record the water layout and bridge placement.

### Task 5: Preserve forest presence and re-verify

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Add forest fill only where the new shell feels empty**

- Prefer outskirts and water-edge framing.

**Step 2: Re-run the dedicated layout test**

Run:
`Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests`

Expected:
- `2 passed / 0 failed`

**Step 3: Re-run repo guardrails**

Run:
- `bash scripts/verify-docs-and-context.sh`
- `bash scripts/verify-extensible-development-contracts.sh`
- `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`

**Step 4: Record the final truth**

- Document:
  - what districts were resized
  - what districts were added
  - where the river/reservoir landed
  - what verification passed

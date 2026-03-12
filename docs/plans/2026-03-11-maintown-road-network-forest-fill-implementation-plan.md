# MainTown Road Network And Forest Fill Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Connect `MainTown` districts with mostly paved roads plus a few dirt/gravel service spurs, then add heavy tree fill around the shell without collapsing landmark readability.

**Architecture:** Keep the current planning-map district layout intact and extend the existing road language with additional graybox road segments and stable names. Add new forest anchors in deliberate clusters rather than random scattering, then verify the updated layout through the focused Unity EditMode suite.

**Tech Stack:** Unity scene YAML, Unity MCP verification, NUnit EditMode tests, markdown progress docs

---

### Task 1: Lock The Road And Tree Contract

**Files:**
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`

**Step 1: Add expectations for new stable road connectors**

- Require a minimal set of new named road connectors that prove the districts are linked.

**Step 2: Add expectations for additional forest anchors**

- Require a defined set of new `ForestTree_*` anchors for the expanded fill pass.

**Step 3: Run the focused Unity EditMode test**

- Confirm it fails before the scene is updated.

### Task 2: Implement The Road Network

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`

**Step 1: Add paved connector roads**

- Build stable named road objects for:
  - west trunk links
  - east trunk links
  - north-east links
  - north-west connector
  - frontage / short spurs to secondary districts

**Step 2: Add dirt / gravel service spurs**

- Add rougher routes for quarry and forest service access.

**Step 3: Keep naming explicit**

- Use names that make future layout edits readable, for example by district-to-district or destination route naming.

### Task 3: Add Forest Fill

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`

**Step 1: Add clustered tree anchors in the major empty gaps**

- Focus on west, south-west, north-east, south-east, and center-west gaps.

**Step 2: Preserve landmark readability**

- Keep the trees away from key sniper landmarks and major road junctions.

### Task 4: Verification And Progress Update

**Files:**
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Re-run the focused Unity EditMode test**

- Verify the updated `MainTown` layout suite passes.

**Step 2: Update the progress log**

- Record the new road network, dirt spurs, forest-fill pass, and verification evidence.

**Step 3: Re-run repo guardrails**

Run:

- `bash scripts/verify-docs-and-context.sh`
- `bash scripts/verify-extensible-development-contracts.sh`
- `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`

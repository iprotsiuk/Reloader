# MainTown Forest Density Expansion Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a much larger forest coverage wave to `MainTown` so the outskirts, river, and reservoir read as intentionally wooded while preserving district readability and major landmark sightlines.

**Architecture:** Keep the current planning-map layout and road network intact, then duplicate existing tree anchors into new clustered positions around the map shell and water features. Lock a small set of the new anchors into the focused `MainTown` EditMode layout suite and record the pass in the progress docs.

**Tech Stack:** Unity scene YAML, Unity MCP scene editing, NUnit EditMode tests, markdown design/progress docs

---

### Task 1: Lock The Stable Anchor Contract

**Files:**
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`

**Step 1: Add expectations for a representative set of new tree anchors**

- Require a minimal set of names from the new `ForestTree_37+` wave.

**Step 2: Keep assertions scoped**

- Prove the new pass exists without hard-coding every added tree anchor.

### Task 2: Implement The Density Pass

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`

**Step 1: Add west and south-west tree clusters**

- Fill the large empty edge bands without blocking `TownCore` readability.

**Step 2: Add south-east and north-east tree clusters**

- Screen the outer districts and water edges.

**Step 3: Add river / reservoir framing trees**

- Make the water break-up read intentional from above.

### Task 3: Update Progress Docs

**Files:**
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Record the new forest-density wave**

- Summarize the new anchor range and the intent of the denser pass.

### Task 4: Verify

**Step 1: Run the focused Unity EditMode layout suite**

- Verify the updated `MainTown` layout test passes.

**Step 2: Re-run repo guardrails**

Run:

- `bash scripts/verify-docs-and-context.sh`
- `bash scripts/verify-extensible-development-contracts.sh`
- `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`

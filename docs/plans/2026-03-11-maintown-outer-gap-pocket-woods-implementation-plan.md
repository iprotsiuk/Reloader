# MainTown Outer Gap Pocket Woods Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add targeted pocket-woods clusters between `MainTown` outer districts so the map feels less open without losing district readability.

**Architecture:** Keep the existing forest belt and density layers intact, then add a small number of new gap-cluster roots under `MainTownWorldShell`. Populate those roots with duplicated existing tree anchors placed as compact wooded pockets in the major outer-district voids, and lock only the cluster-root names into the focused layout test.

**Tech Stack:** Unity scene YAML, Unity MCP scene editing, NUnit EditMode tests, markdown design/progress docs

---

### Task 1: Lock The Gap-Cluster Contract

**Files:**
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`

**Step 1: Add expectations for the new gap-cluster root names**

- Require the new stable roots under `MainTownWorldShell`.

**Step 2: Run the focused Unity EditMode suite and confirm the new root assertions fail before scene mutation**

### Task 2: Implement The Pocket Woods Clusters

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`

**Step 1: Create the new gap-cluster roots**

- One for west, one for east, one for north.

**Step 2: Populate each root with compact tree groups**

- Reuse existing `ForestTree_*` anchors as duplication sources.
- Keep the pockets dense locally but limited in footprint.

**Step 3: Save the updated scene**

### Task 3: Sync Docs

**Files:**
- Modify: `docs/plans/2026-03-11-maintown-road-network-forest-fill-contract.md`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Add the stable gap-cluster roots to the contract note**

**Step 2: Add a progress entry for the targeted outer-gap pocket-woods pass**

### Task 4: Verify

**Step 1: Re-run the focused Unity EditMode layout suite**

- Confirm the new gap-cluster contract passes.

**Step 2: Re-run repo guardrails**

Run:

- `bash scripts/verify-docs-and-context.sh`
- `bash scripts/verify-extensible-development-contracts.sh`
- `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`

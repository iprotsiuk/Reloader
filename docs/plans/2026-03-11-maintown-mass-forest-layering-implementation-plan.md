# MainTown Mass Forest Layering Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a few hundred more trees to `MainTown` by duplicating the existing forest belt into several offset density layers while preserving landmark readability.

**Architecture:** Keep `District_ForestBelt` as the canonical named anchor set, then add a small number of new root-level forest density layers under the world shell. Each layer is a duplicated tree field with a different offset so the map gains mixed additional woods without requiring hundreds of new stable tree contracts.

**Tech Stack:** Unity scene YAML, Unity MCP scene editing, NUnit EditMode tests, markdown design/progress docs

---

### Task 1: Lock The Density-Layer Contract

**Files:**
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`

**Step 1: Add expectations for the new forest density layer roots**

- Require the new root names under `MainTownWorldShell`.

**Step 2: Run the focused test and confirm the new layer-root assertions fail before the scene mutation**

- Use the targeted Unity EditMode suite only.

### Task 2: Implement The Layered Forest Pass

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`

**Step 1: Duplicate `District_ForestBelt` into several new density layers**

- Create stable root names for the added layers.

**Step 2: Offset the layers for mixed added coverage**

- Bias the duplicates toward different shell / water / road regions.

**Step 3: Save the updated scene**

- Persist the live-editor mutation after read-back checks.

### Task 3: Sync Docs

**Files:**
- Modify: `docs/plans/2026-03-11-maintown-road-network-forest-fill-contract.md`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Record the new density-layer root names in the road/forest contract note**

**Step 2: Add a progress entry for the few-hundred-tree layering pass**

### Task 4: Verify

**Step 1: Re-run the focused Unity EditMode layout suite**

- Confirm the density-layer contract passes.

**Step 2: Re-run repo guardrails**

Run:

- `bash scripts/verify-docs-and-context.sh`
- `bash scripts/verify-extensible-development-contracts.sh`
- `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`

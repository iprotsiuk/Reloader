# MainTown School Removal Storage Yard Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the `School Campus` planning district in `MainTown` with an on-theme `Storage Yard` district while keeping the shell layout stable.

**Architecture:** Update the dedicated layout EditMode test first so the new district contract is explicit, then replace the old school graybox objects and overhead label in the scene YAML with a storage-yard set using the same footprint. Finish by rerunning the focused Unity test and updating the progress log.

**Tech Stack:** Unity scene YAML, NUnit EditMode tests, Unity MCP test runner, markdown planning docs

---

### Task 1: Red Test For Storage Yard Contract

**Files:**
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`

**Step 1: Replace school district assertions with storage-yard assertions**

- Remove the `District_SchoolCampus` lookup and its related scale checks.
- Require:
  - `District_StorageYard`
  - `MarkerPad_StorageYard`
  - `StorageOffice`
  - `LockerRow_A`
  - `LockerRow_B`

**Step 2: Run the focused Unity EditMode test**

Run the dedicated `MainTown` layout suite and confirm it fails because the scene still contains the old school district.

### Task 2: Replace The Scene District

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`

**Step 1: Rename the district root and child objects**

- `District_SchoolCampus` -> `District_StorageYard`
- `MarkerPad_SchoolCampus` -> `MarkerPad_StorageYard`
- `SchoolMain` -> `StorageOffice`
- `Gymnasium` -> `LockerRow_A`
- `AdminBlock` -> `LockerRow_B`

**Step 2: Keep the footprint stable**

- Preserve the district root position.
- Keep the marker pad in roughly the same size range.
- Keep the existing masses but use names and dimensions that read as storage-yard structures.

**Step 3: Replace the overhead label**

- `Label_SchoolCampus` -> `Label_StorageYard`
- `SCHOOL CAMPUS` -> `STORAGE YARD`

### Task 3: Verification And Progress Update

**Files:**
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Re-run the focused Unity EditMode test**

- Verify the updated `MainTown` layout suite passes.

**Step 2: Append a short progress entry**

- Record the school removal, storage-yard replacement, and verification result.

**Step 3: Re-run repo guardrails**

Run:

- `bash scripts/verify-docs-and-context.sh`
- `bash scripts/verify-extensible-development-contracts.sh`
- `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`

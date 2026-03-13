# NPC Mandatory Brows Pants Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Ensure STYLE/MainTown NPCs always render with eyebrows and pants, even when legacy/demo records contain invalid or empty appearance ids.

**Architecture:** Add canonical normalization helpers in curated appearance rules, then route applicator and clone/save seams through them so bad data self-heals instead of propagating. Keep the change narrow to STYLE/MainTown appearance behavior and cover it with focused editmode regressions.

**Tech Stack:** Unity C#, NUnit EditMode tests, existing MainTown NPC appearance/runtime/save contracts

---

### Task 1: Add Red Tests For Invalid Appearance Healing

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/MainTownNpcAppearanceApplicatorEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/CivilianPopulationSaveModuleTests.cs`

**Step 1: Write the failing tests**

Add tests that prove:
- a STYLE record with blank/invalid `EyebrowId` still activates a valid eyebrow mesh
- a STYLE record with blank/invalid `OutfitBottomId` still activates `pants1`
- save-module clone normalization rewrites invalid eyebrow/bottom ids to approved defaults

**Step 2: Run tests to verify they fail**

Run focused editmode tests for the new cases and confirm the failures are about missing normalization, not test mistakes.

**Step 3: Commit**

Commit only after the tests are red and clearly reproducing the regression.

### Task 2: Normalize Brows And Pants At Shared Seams

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Generation/MainTownCuratedAppearanceRules.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/MainTownNpcAppearanceApplicator.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/CivilianPopulationModule.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`

**Step 1: Write minimal implementation**

- add canonical eyebrow normalization helper in curated rules
- tighten bottom normalization helper
- use those helpers in applicator and clone/save seams
- keep the old bottom-to-eyebrow migration path only as input fallback

**Step 2: Run focused tests to verify green**

Run the new applicator/save tests plus nearby NPC appearance regressions.

**Step 3: Refactor only if needed**

Keep the logic centralized in rules helpers and avoid duplicating fallback constants.

**Step 4: Commit**

Commit the production code plus the focused tests once green.

### Task 3: Verify Focused Regressions And Guardrails

**Files:**
- Verify only

**Step 1: Run focused editmode suites**

Run:
- `MainTownNpcAppearanceApplicatorEditModeTests`
- relevant `CivilianAppearanceGeneratorTests`
- relevant `CivilianPopulationSaveModuleTests`

**Step 2: Run repository guardrails**

Run:
- `bash scripts/verify-docs-and-context.sh`
- `bash scripts/verify-extensible-development-contracts.sh`
- `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`

**Step 3: Confirm clean diffs**

Check the touched files with `git diff --check`.

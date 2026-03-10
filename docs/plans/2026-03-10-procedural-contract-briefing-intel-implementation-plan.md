# Procedural Contract Briefing Intel Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the generic procedural contract briefing with stable role/location intel derived from the live civilian record while keeping the visual clue summary unchanged.

**Architecture:** Extend `CivilianPopulationRuntimeBridge` so procedural offers build `BriefingText` from existing `PoolId` and `AreaTag` data using small normalization helpers. Cover the behavior with a focused EditMode test that proves the briefing text follows the current occupant record and not stale placeholder copy.

**Tech Stack:** Unity, C#, NUnit EditMode tests, existing procedural contract runtime bridge.

---

### Task 1: Document the approved approach

**Files:**
- Create: `docs/plans/2026-03-10-procedural-contract-briefing-intel-design.md`
- Create: `docs/plans/2026-03-10-procedural-contract-briefing-intel-implementation-plan.md`

**Step 1: Write the design doc**

- Capture the approved split between `TargetDescription` and `BriefingText`.

**Step 2: Save the implementation plan**

- Record the exact runtime file and test file to edit.

**Step 3: Commit**

```bash
git add docs/plans/2026-03-10-procedural-contract-briefing-intel-design.md docs/plans/2026-03-10-procedural-contract-briefing-intel-implementation-plan.md
git commit -m "docs: plan procedural contract briefing intel"
```

### Task 2: Add the failing runtime test

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`

**Step 1: Write the failing test**

- Add a test that seeds a live civilian with:
  - a known `PoolId`
  - a known `AreaTag`
  - appearance fields that already produce a valid `TargetDescription`
- Assert that the published snapshot uses:
  - the existing appearance clue string for `TargetDescription`
  - a generated role/location sentence for `BriefingText`

**Step 2: Run the test to verify it fails**

Run:

```bash
dotnet test Reloader.sln --filter "FullyQualifiedName~CivilianPopulationRuntimeBridgeTests.RebuildScenePopulation_WhenProceduralOfferIsPublished_DerivesBriefingTextFromPoolAndArea"
```

Expected:

- FAIL because the runtime still emits the generic procedural briefing placeholder.

### Task 3: Implement the minimal runtime change

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`

**Step 1: Replace the placeholder briefing text**

- Build `BriefingText` from the target record inside procedural offer publishing.

**Step 2: Add minimal helper methods**

- Add helpers to:
  - normalize `PoolId` into a readable role label
  - normalize `AreaTag` into a readable location phrase
  - format the final contractor-note sentence

**Step 3: Keep scope tight**

- Do not change save schema or UI contracts.
- Do not alter the existing `TargetDescription` clue builder except where tests require no regression.

### Task 4: Verify green

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`

**Step 1: Run the focused test**

Run:

```bash
dotnet test Reloader.sln --filter "FullyQualifiedName~CivilianPopulationRuntimeBridgeTests.RebuildScenePopulation_WhenProceduralOfferIsPublished_DerivesBriefingTextFromPoolAndArea"
```

Expected:

- PASS

**Step 2: Run the relevant suite**

Run:

```bash
dotnet test Reloader.sln --filter "FullyQualifiedName~CivilianPopulationRuntimeBridgeTests"
```

Expected:

- PASS

### Task 5: Unity verification and commit

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`

**Step 1: Run Unity EditMode verification**

Run the NPC EditMode assembly through Unity MCP.

Expected:

- `Reloader.NPCs.Tests.EditMode` passes.

**Step 2: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs
git commit -m "feat: add procedural contract briefing intel"
```

**Step 3: Push and request review**

```bash
git push
```


# Procedural Contract Target Descriptions Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build clearer procedural contract target descriptions from persisted civilian appearance data so contracts describe the current live occupant with stable, readable clues.

**Architecture:** Keep the change localized to `CivilianPopulationRuntimeBridge`, where procedural contracts are published. Add deterministic appearance-to-text mapping helpers there, keep existing saved appearance fields as the source of truth, and preserve old `GeneratedDescriptionTags` only as fallback data.

**Tech Stack:** Unity C#, NUnit EditMode/PlayMode tests, existing civilian save/runtime contracts

---

### Task 1: Add runtime tests for derived contract descriptions

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`

**Step 1: Write the failing test**

Add a test that publishes a procedural contract for a live civilian with persisted fields like `female.body`, `hoody`, `hair.long`, `hair.brown`, and asserts the contract snapshot description contains stable clues such as `female`, `hoodie`, and `long brown hair`.

**Step 2: Run test to verify it fails**

Run: Unity EditMode assembly `Reloader.NPCs.Tests.EditMode`
Expected: the new test fails because the runtime still returns old joined tags or area/pool fallback text.

**Step 3: Write minimal implementation**

Add description-builder helpers in `CivilianPopulationRuntimeBridge` that derive clue fragments from persisted appearance fields and return a terse comma-separated clue line.

**Step 4: Run test to verify it passes**

Run: Unity EditMode assembly `Reloader.NPCs.Tests.EditMode`
Expected: PASS

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs \
  Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs
git commit -m "feat: derive procedural contract target clues"
```

### Task 2: Add replacement coverage for occupant-matching descriptions

**Files:**
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownPopulationInfrastructurePlayModeTests.cs`
- Test: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownPopulationInfrastructurePlayModeTests.cs`

**Step 1: Write the failing test**

Add or extend a Monday-replacement test so the refreshed contract/description matches the replacement occupant’s saved appearance fields rather than the retired civilian’s old description.

**Step 2: Run test to verify it fails**

Run: Unity PlayMode tests for `MainTownPopulationInfrastructurePlayModeTests`
Expected: FAIL if the old description source survives.

**Step 3: Write minimal implementation**

Adjust the runtime description publication path only if needed so refreshed contracts rebuild from the current live occupant record every time.

**Step 4: Run test to verify it passes**

Run: focused PlayMode test or the relevant assembly
Expected: PASS

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/World/Tests/PlayMode/MainTownPopulationInfrastructurePlayModeTests.cs \
  Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs
git commit -m "test: cover replacement occupant contract clues"
```

### Task 3: Verify fallback behavior and regression coverage

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`

**Step 1: Write the failing test**

Add a sparse-record fallback test proving that when appearance-derived clues cannot be built, the runtime still returns old tags or area/pool fallback instead of an empty description.

**Step 2: Run test to verify it fails**

Run: Unity EditMode/PlayMode affected suites
Expected: FAIL until the fallback chain is explicit.

**Step 3: Write minimal implementation**

Finish the fallback chain in the runtime description builder.

**Step 4: Run test to verify it passes**

Run:
- `Reloader.Core.Tests.EditMode`
- `Reloader.NPCs.Tests.EditMode`
- `Reloader.UI.Tests.PlayMode`
- affected `Reloader.World.Tests.PlayMode` coverage

Expected:
- Core green
- NPCs green
- UI green
- World: affected population/contract tests green, with any unrelated known travel failures called out explicitly

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs \
  Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs \
  Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs \
  Reloader/Assets/_Project/World/Tests/PlayMode/MainTownPopulationInfrastructurePlayModeTests.cs
git commit -m "test: harden procedural contract target descriptions"
```

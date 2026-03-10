# Procedural Civilian Dialogue Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Give every spawned procedural civilian a lightweight runtime-generated dialogue that confirms the civilian's public name.

**Architecture:** Extend the civilian spawn path so it ensures `DialogueCapability` alongside `NpcAgent` and `AmbientCitizenCapability`. Generate a single-node in-memory `DialogueDefinition` from the `CivilianPopulationRecord` and bind it before capability initialization so the existing dialogue runtime handles the rest.

**Tech Stack:** Unity, C#, NUnit EditMode tests, existing NPC dialogue runtime (`DialogueCapability`, `DialogueDefinition`, `DialogueOrchestrator`).

---

### Task 1: Document the approved design

**Files:**
- Create: `docs/plans/2026-03-10-procedural-civilian-dialogue-design.md`
- Create: `docs/plans/2026-03-10-procedural-civilian-dialogue-implementation-plan.md`

**Step 1: Save the design doc**

- Capture the runtime-generated shared-dialogue approach.

**Step 2: Save the implementation plan**

- Record the runtime bridge and test seams to edit.

**Step 3: Commit**

```bash
git add docs/plans/2026-03-10-procedural-civilian-dialogue-design.md docs/plans/2026-03-10-procedural-civilian-dialogue-implementation-plan.md
git commit -m "docs: plan procedural civilian dialogue"
```

### Task 2: Add the failing regression test

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`

**Step 1: Write the failing test**

- Spawn a civilian through `CivilianPopulationRuntimeBridge`.
- Assert the spawned actor has `DialogueCapability`.
- Assert the capability definition is valid.
- Assert the entry node text includes the civilian's current public display name.

**Step 2: Run the test to verify it fails**

Run the focused NPC EditMode test through Unity MCP.

Expected:

- FAIL because procedural civilians do not yet receive a dialogue capability/definition.

### Task 3: Implement the minimal runtime wiring

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`

**Step 1: Ensure dialogue capability on spawned civilians**

- Extend actor component setup to add `DialogueCapability` if missing.

**Step 2: Generate and assign a runtime definition**

- Add a small helper that creates a one-node `DialogueDefinition` using the civilian's public name.
- Bind it to the `DialogueCapability` before capability initialization.

**Step 3: Keep scope tight**

- No save-schema changes.
- No authored dialogue assets.
- No branching beyond the minimal single-node exchange.

### Task 4: Verify green

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`

**Step 1: Run the focused test**

- Expect PASS.

**Step 2: Run the full runtime-bridge EditMode class**

- Expect PASS.

### Task 5: Broader verification and checkpoint commit

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`

**Step 1: Run relevant NPC EditMode coverage**

- `Reloader.NPCs.Tests.EditMode`

**Step 2: Run a relevant scene/UI slice if the runtime change touches interaction wiring**

- Keep scope focused on tests that exercise civilian spawn/interact seams.

**Step 3: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs
git commit -m "feat: add procedural civilian dialogue"
```


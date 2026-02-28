# NPC Capability Foundation Implementation Plan

> Status Pointer (2026-02-28): This is a planning/execution artifact. For live implemented-vs-planned status, use `docs/design/v0.1-demo-status-and-milestones.md`.


> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Establish a reusable NPC foundation using capability composition, with an explicit decision-provider seam for future behavior tree adoption.

**Architecture:** Introduce data-driven NPC role presets and a runtime `NpcAgent` orchestrator that composes capabilities. Capabilities expose actions for interaction/UI and can use a pluggable decision provider interface. Keep role behavior modular and avoid role-specific monolithic classes.

**Tech Stack:** Unity 6.3, C#, ScriptableObject data assets, MonoBehaviour composition, NUnit EditMode tests.

---

### Task 1: Add Core NPC Contracts

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcRoleKind.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcCapabilityKind.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcActionDefinition.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/INpcCapability.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/INpcActionProvider.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/INpcDecisionProvider.cs`

**Step 1: Write failing tests**
- Add tests referencing these types so compile fails before implementation.

**Step 2: Run test to verify failure**
Run: `dotnet test` (or Unity test runner equivalent)
Expected: missing-type compile failure.

**Step 3: Write minimal implementation**
- Implement enum/interface/DTO contracts only.

**Step 4: Run test to verify pass**
Run: `dotnet test` (or Unity test runner equivalent)
Expected: compile succeeds, tests continue.

**Step 5: Commit**
`git commit -m "feat(npcs): add npc capability core contracts"`

### Task 2: Add Data-Driven NPC Definition Layer

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Data/NpcCapabilityConfig.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Data/NpcRolePreset.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Data/NpcDefinition.cs`

**Step 1: Write failing tests**
- Add tests asserting role presets expose capability lists and definitions expose role binding.

**Step 2: Run test to verify failure**
Run: `dotnet test`
Expected: missing-type/member failure.

**Step 3: Write minimal implementation**
- Implement SO classes with serialized fields and read-only properties.

**Step 4: Run test to verify pass**
Run: `dotnet test`
Expected: tests pass.

**Step 5: Commit**
`git commit -m "feat(npcs): add npc definition and role preset data model"`

### Task 3: Add NpcAgent Orchestrator and Capability Action Aggregation

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcAgent.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcCapabilityBase.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcActionCollection.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/NpcAgentEditModeTests.cs`

**Step 1: Write failing tests**
- Test capability initialization lifecycle.
- Test action collection returns combined actions from installed providers.

**Step 2: Run test to verify failure**
Run: `dotnet test`
Expected: tests fail due to missing behavior.

**Step 3: Write minimal implementation**
- `NpcAgent` discovers local capabilities, initializes them once, and exposes action collection.

**Step 4: Run test to verify pass**
Run: `dotnet test`
Expected: tests pass.

**Step 5: Commit**
`git commit -m "feat(npcs): add npc agent capability orchestration"`

### Task 4: Add Decision Provider Seam for Future BT Integration

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcDecisionContext.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcDecision.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/RuleBasedDecisionProvider.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcAiController.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/NpcDecisionProviderEditModeTests.cs`

**Step 1: Write failing tests**
- Test AI controller delegates to `INpcDecisionProvider`.
- Test fallback provider behavior is deterministic.

**Step 2: Run test to verify failure**
Run: `dotnet test`
Expected: failing tests.

**Step 3: Write minimal implementation**
- Add provider interface usage and default rule-based provider.

**Step 4: Run test to verify pass**
Run: `dotnet test`
Expected: tests pass.

**Step 5: Commit**
`git commit -m "feat(npcs): add decision provider seam for npc ai"`

### Task 5: Wire Basic Vendor Capability Through New Foundation (Non-breaking)

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Capabilities/VendorTradeCapability.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/World/ShopVendorTarget.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/NpcVendorCapabilityEditModeTests.cs`

**Step 1: Write failing tests**
- Verify vendor action is surfaced through capability action collection.

**Step 2: Run test to verify failure**
Run: `dotnet test`
Expected: fail until capability exists.

**Step 3: Write minimal implementation**
- Keep existing world interaction behavior while exposing vendor action in new system.

**Step 4: Run test to verify pass**
Run: `dotnet test`
Expected: tests pass.

**Step 5: Commit**
`git commit -m "feat(npcs): expose vendor behavior through capability system"`

### Task 6: Verify and Open PR

**Files:**
- Modify: `docs/plans/2026-02-26-npc-capability-foundation-design.md` (if needed for as-built notes)

**Step 1: Run verification commands**
- Run targeted NPC EditMode tests.
- Run existing NPC PlayMode vendor test file.

**Step 2: Confirm outputs and fix issues**
- Resolve failures before completion claims.

**Step 3: Push feature branch**
`git push -u origin feat/npc-capability-foundation`

**Step 4: Create PR to main**
- Title: `feat: npc capability foundation with decision-provider seam`
- Include summary + test evidence.

**Step 5: Commit final adjustments**
`git commit -m "chore(npcs): finalize npc foundation verification"` (only if needed)

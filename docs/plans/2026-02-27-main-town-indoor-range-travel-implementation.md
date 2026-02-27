# Main Town + Indoor Range Travel Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Establish a production-ready world foundation with a persistent player character that can travel from `MainTown` to an `IndoorRange` instance and back, while preserving player state and applying hybrid time progression.

**Architecture:** Use a persistent bootstrap root for global services and player lifetime, with additive scene travel between `MainTown` and reusable activity scenes. Travel is orchestrated through a single world travel pipeline that carries a compact `TravelContext` payload and validates access through data-driven unlock rules. Clock/calendar and selected lightweight systems advance on travel; no background NPC simulation runs while away.

**Tech Stack:** Unity 6.3, C#, ScriptableObject data assets, additive scene loading, existing `SaveCoordinator` pipeline, Unity EditMode/PlayMode tests.

---

## Scope Guardrails (v1)

- In scope:
  - `MainTown` as player hub.
  - `IndoorRange` reusable activity instance.
  - Persistent player object preserved across travel.
  - Data-driven unlock evaluation for travel entry.
  - Hybrid time progression hooks on travel start/return.
- Out of scope for this slice:
  - Outdoor range and competitions/hunting travel targets.
  - Background NPC simulation while in instances.
  - Full fast-travel map UX and cinematic transitions.

---

### Task 1: Baseline Scene Topology and Build Settings

**Files:**
- Create: `Reloader/Assets/Scenes/Bootstrap.unity`
- Create: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Create: `Reloader/Assets/_Project/World/Scenes/IndoorRangeInstance.unity`
- Modify: `ProjectSettings/EditorBuildSettings.asset`
- Modify: `docs/design/world-and-scenes.md` (create if missing)

**Step 1: Create failing verification check**
- Add/prepare a minimal PlayMode smoke test that asserts required scenes exist in build settings.

**Step 2: Run test to verify failure**
Run: `Unity Test Runner (PlayMode): scene topology smoke test`
Expected: fails until scenes and build settings are configured.

**Step 3: Implement minimal scene topology**
- Create scenes and register build order: `Bootstrap` first, `MainTown` second, `IndoorRangeInstance` third.
- Keep legacy `MainWorld` scene unchanged for rollback/testing.

**Step 4: Run test to verify pass**
Run: `Unity Test Runner (PlayMode): scene topology smoke test`
Expected: pass.

**Step 5: Commit**
`git commit -m "feat(world): add bootstrap maintown and indoor range scene topology"`

---

### Task 2: Persistent Player Root and Bootstrap Lifetime

**Files:**
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/PersistentPlayerRoot.cs`
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/BootstrapWorldRoot.cs`
- Modify: `Reloader/Assets/_Project/Player/Prefabs/` (player prefab wiring as needed)
- Test: `Reloader/Assets/_Project/World/Tests/EditMode/PersistentPlayerRootEditModeTests.cs`

**Step 1: Write failing tests**
- Assert only one `PersistentPlayerRoot` survives duplicate initialization.
- Assert player object is marked `DontDestroyOnLoad` and remains addressable after scene swap.

**Step 2: Run test to verify failure**
Run: `Unity Test Runner (EditMode): PersistentPlayerRootEditModeTests`
Expected: compile/runtime failure before implementation.

**Step 3: Write minimal implementation**
- Implement singleton-style guard for persistent player root.
- Bootstrap initializes persistent services and player root once.

**Step 4: Run test to verify pass**
Run: `Unity Test Runner (EditMode): PersistentPlayerRootEditModeTests`
Expected: pass.

**Step 5: Commit**
`git commit -m "feat(world): add persistent player bootstrap lifetime"`

---

### Task 3: Travel Contracts (Context, Entry/Return Points, Activity Type)

**Files:**
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/TravelContext.cs`
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/TravelActivityType.cs`
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/TravelTimeAdvancePolicy.cs`
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/SceneEntryPoint.cs`
- Test: `Reloader/Assets/_Project/World/Tests/EditMode/TravelContextEditModeTests.cs`

**Step 1: Write failing tests**
- Verify required `TravelContext` fields are present and validated.
- Verify entry point lookup behavior for missing/invalid marker IDs.

**Step 2: Run test to verify failure**
Run: `Unity Test Runner (EditMode): TravelContextEditModeTests`
Expected: fail until contracts exist.

**Step 3: Write minimal implementation**
- Add serializable travel contracts with strict null/empty validation.
- Add `SceneEntryPoint` marker component with stable ID field.

**Step 4: Run test to verify pass**
Run: `Unity Test Runner (EditMode): TravelContextEditModeTests`
Expected: pass.

**Step 5: Commit**
`git commit -m "feat(world): add travel context and scene entry contracts"`

---

### Task 4: Travel Orchestration with Additive Scene Loading

**Files:**
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/WorldTravelManager.cs`
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/SceneTravelRequest.cs`
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/SceneTravelResult.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/RuntimeKernelBootstrapper.cs` (registration seam if needed)
- Test: `Reloader/Assets/_Project/World/Tests/PlayMode/WorldTravelManagerPlayModeTests.cs`

**Step 1: Write failing tests**
- Test travel `MainTown -> IndoorRangeInstance -> MainTown` preserves same player instance ID.
- Test player is repositioned to destination marker on enter and return marker on exit.

**Step 2: Run test to verify failure**
Run: `Unity Test Runner (PlayMode): WorldTravelManagerPlayModeTests`
Expected: failing travel behavior.

**Step 3: Write minimal implementation**
- Implement additive load/unload flow with deterministic order:
  - validate access
  - capture current scene/return marker
  - load destination scene additively
  - move persistent player to destination entry marker
  - unload previous activity scene on return
- Emit travel lifecycle events via existing runtime event hub if available.

**Step 4: Run test to verify pass**
Run: `Unity Test Runner (PlayMode): WorldTravelManagerPlayModeTests`
Expected: pass.

**Step 5: Commit**
`git commit -m "feat(world): add additive world travel manager with persistent player teleport"`

---

### Task 5: Data-Driven Unlock Rules and Access Evaluation

**Files:**
- Create: `Reloader/Assets/_Project/World/Scripts/Data/UnlockRuleDefinition.cs`
- Create: `Reloader/Assets/_Project/World/Scripts/Data/TravelAccessProfile.cs`
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/TravelAccessEvaluator.cs`
- Create: `Reloader/Assets/_Project/World/Data/TravelAccess/IndoorRangeAccess.asset`
- Test: `Reloader/Assets/_Project/World/Tests/EditMode/TravelAccessEvaluatorEditModeTests.cs`

**Step 1: Write failing tests**
- Verify access denied when required flag/item/pass is missing.
- Verify access granted when requirements are met.

**Step 2: Run test to verify failure**
Run: `Unity Test Runner (EditMode): TravelAccessEvaluatorEditModeTests`
Expected: fail until evaluator and assets exist.

**Step 3: Write minimal implementation**
- Implement evaluator that reads `TravelAccessProfile` + `UnlockRuleDefinition` list.
- Keep rule model simple in v1: boolean progression flags and required inventory token IDs.

**Step 4: Run test to verify pass**
Run: `Unity Test Runner (EditMode): TravelAccessEvaluatorEditModeTests`
Expected: pass.

**Step 5: Commit**
`git commit -m "feat(world): add data-driven travel unlock evaluation"`

---

### Task 6: Hybrid Time Advancement Hooks on Travel

**Files:**
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Time/TravelTimeAdvanceService.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/CoreWorldModule.cs` (persist needed world-time fields)
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SaveEnvelope.cs` (module payload fields if required)
- Test: `Reloader/Assets/_Project/World/Tests/EditMode/TravelTimeAdvanceServiceEditModeTests.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/CoreWorldModuleTimePersistenceTests.cs`

**Step 1: Write failing tests**
- Verify travel applies configured time delta policy.
- Verify world time persists through save/load roundtrip.

**Step 2: Run test to verify failure**
Run: `Unity Test Runner (EditMode): TravelTimeAdvanceServiceEditModeTests`
Run: `Unity Test Runner (EditMode): CoreWorldModuleTimePersistenceTests`
Expected: failures until service + persistence wiring is added.

**Step 3: Write minimal implementation**
- Advance only lightweight systems (clock/date + schedule windows).
- Do not simulate NPC movement/AI while away.

**Step 4: Run test to verify pass**
Run: same tests above.
Expected: pass.

**Step 5: Commit**
`git commit -m "feat(world): add hybrid travel time advancement with persistence"`

---

### Task 7: MainTown Interaction Wiring for Indoor Range Entry/Exit

**Files:**
- Create: `Reloader/Assets/_Project/World/Scripts/World/TravelTriggerTarget.cs`
- Create: `Reloader/Assets/_Project/World/Scripts/World/PlayerTravelTriggerController.cs`
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `Reloader/Assets/_Project/World/Scenes/IndoorRangeInstance.unity`
- Test: `Reloader/Assets/_Project/World/Tests/PlayMode/IndoorRangeTravelFlowPlayModeTests.cs`

**Step 1: Write failing tests**
- Verify interaction with town trigger enters indoor range only if access is granted.
- Verify interaction with range return trigger sends player back to town return marker.

**Step 2: Run test to verify failure**
Run: `Unity Test Runner (PlayMode): IndoorRangeTravelFlowPlayModeTests`
Expected: fail until triggers are wired.

**Step 3: Write minimal implementation**
- Add trigger targets and resolver/controller wiring using existing interaction pattern conventions.
- Keep UX minimal: interaction prompt + transition lock.

**Step 4: Run test to verify pass**
Run: `Unity Test Runner (PlayMode): IndoorRangeTravelFlowPlayModeTests`
Expected: pass.

**Step 5: Commit**
`git commit -m "feat(world): wire town and indoor range travel triggers"`

---

### Task 8: MCP Authoring Workflow and Verification Gates

**Files:**
- Create: `docs/plans/2026-02-27-main-town-indoor-range-mcp-authoring-checklist.md`
- Modify: `.cursor/rules/world-vehicles-context.mdc` (only if routing needs world-travel pattern mention)

**Step 1: Add MCP execution checklist**
- Document instance binding, scene/object mutation order, and mandatory read-back verification.
- Include preferred MCP commands for this feature (`manage_scene`, `manage_gameobject`, `manage_components`, `read_console`, `run_tests`).

**Step 2: Validate checklist against local skill contracts**
- Ensure consistency with `.agent/skills/using-unity-mcp/SKILL.md` and `.cursor/agents.md`.

**Step 3: Save and review**
- Ensure checklist is short, concrete, and usable during implementation sessions.

**Step 4: Commit**
`git commit -m "docs(world): add mcp authoring checklist for world travel slice"`

---

### Task 9: End-to-End Verification and Integration Readiness

**Files:**
- Modify: `docs/plans/2026-02-27-main-town-indoor-range-travel-implementation.md` (as-built notes)

**Step 1: Run targeted test suite**
- EditMode:
  - `PersistentPlayerRootEditModeTests`
  - `TravelContextEditModeTests`
  - `TravelAccessEvaluatorEditModeTests`
  - `TravelTimeAdvanceServiceEditModeTests`
  - `CoreWorldModuleTimePersistenceTests`
- PlayMode:
  - `WorldTravelManagerPlayModeTests`
  - `IndoorRangeTravelFlowPlayModeTests`

**Step 2: Run Unity console validation**
- Check for scene load exceptions, missing reference errors, and save serialization warnings.

**Step 3: MCP read-back verification**
- Verify scene hierarchy and key component wiring after authoring actions.

**Step 4: Branch hygiene + review prep**
- Prepare diff summary and test evidence.
- Request code review before merge.

**Step 5: Final commit (if needed)**
`git commit -m "chore(world): finalize maintown indoor range travel verification"`

---

## MCP-First Execution Notes for This Plan

- Use MCP-first for scene/object/component/prefab work and read-back verification.
- Use file-first for C# and docs edits.
- Before any MCP mutation:
  - Bind target Unity instance.
  - Confirm project info.
  - Ensure editor is not in Play Mode unless test step explicitly requires it.
- For repeated object setup operations, prefer MCP `batch_execute`.
- Do not claim completion for any task until:
  - changes are read back from Unity state, and
  - targeted tests are run with clear pass/fail evidence.

## Sequencing Recommendation

- Execute Tasks 1-4 first to establish stable travel backbone.
- Execute Tasks 5-7 next for access control, time policy, and interaction.
- Execute Tasks 8-9 last for MCP workflow hardening and verification evidence.

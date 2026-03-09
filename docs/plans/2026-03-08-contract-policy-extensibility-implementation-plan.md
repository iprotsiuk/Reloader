# Contract Policy Extensibility Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add structured contract failure/objective policies so ordinary procedural contracts stay permissive, strict contracts can opt into wrong-target failure, failed contracts can be cleared at any time, and Contracts-tab restriction text is sourced from contract policy data.

**Architecture:** Extend `AssassinationContractDefinition` with explicit policy data, make `ContractEscapeResolutionRuntime` consult that data instead of using a global wrong-target failure rule, and surface policy-derived restriction/failure text through the Contracts tab UI. Keep law-enforcement consequences independent from contract policy and preserve the current failed-snapshot UI path.

**Tech Stack:** Unity 6.3, C#, ScriptableObject-backed runtime definitions, NUnit EditMode/PlayMode tests, GitHub PR workflow with frequent commits.

---

### Task 1: Document and guard the current wrong-target contract behavior

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/ContractEscapeResolutionRuntimeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`

**Step 1: Write the failing edit-mode tests**

Add tests that express:
- contracts without a `WrongTargetKill` rule stay active after a wrong-target elimination
- contracts with a `WrongTargetKill` rule still fail on wrong-target elimination

**Step 2: Run the targeted edit-mode tests to verify they fail**

Run:

```bash
<unity test runner for ContractEscapeResolutionRuntimeTests>
```

Expected:
- the relaxed-policy test fails because current runtime always fails the contract

**Step 3: Write the failing play-mode seam test**

Add or update a `MainTownContractSlice` test so the procedural civilian contract remains active after killing an unrelated authored contract target.

**Step 4: Run the targeted play-mode test to verify it fails**

Run:

```bash
<unity test runner for MainTownContractSlice wrong-target scenario>
```

Expected:
- failure because the contract still enters failed state

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Tests/EditMode/ContractEscapeResolutionRuntimeTests.cs \
        Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs
git commit -m "test: lock relaxed wrong-target contract behavior"
```

### Task 2: Add policy data to assassination contracts

**Files:**
- Modify: `Reloader/Assets/_Project/Contracts/Scripts/Runtime/AssassinationContractDefinition.cs`
- Create or Modify: `Reloader/Assets/_Project/Contracts/Scripts/Runtime/*Contract*Policy*.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/ContractEscapeResolutionRuntimeTests.cs`

**Step 1: Write the failing definition/policy tests**

Add tests that require:
- definitions can declare failure rules
- the default procedural policy contains no strict rules

**Step 2: Run the targeted tests to verify they fail**

Run the smallest edit-mode command that exercises the new tests.

**Step 3: Write the minimal implementation**

Add:
- `ContractFailurePolicy`
- `ContractObjectivePolicy`
- `AssassinationContractFailureRuleType`
- `AssassinationContractFailureRule`

Keep the first implementation minimal and serializable.

**Step 4: Run the targeted tests to verify they pass**

Run the same targeted edit-mode command and confirm green.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Contracts/Scripts/Runtime \
        Reloader/Assets/_Project/Core/Tests/EditMode/ContractEscapeResolutionRuntimeTests.cs
git commit -m "feat: add structured assassination contract policies"
```

### Task 3: Make runtime wrong-target failure opt-in

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/ContractEscapeResolutionRuntime.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/ContractOfferSnapshot.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/ContractEscapeResolutionRuntimeTests.cs`
- Test: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`

**Step 1: Run the failing tests from Task 1**

Use the same targeted commands and confirm the relaxed-path failures are still red.

**Step 2: Write the minimal runtime implementation**

Update runtime so:
- wrong-target kills still report crime/heat
- contract failure happens only when the active definition’s failure policy includes `WrongTargetKill`
- failure reason/status text continues to work for strict contracts

**Step 3: Run targeted tests to verify they pass**

Run the edit-mode and play-mode commands from Task 1 and confirm both strict and relaxed scenarios are green.

**Step 4: Refactor only if needed**

Extract tiny helpers if policy checks are duplicated. Do not introduce rule engines beyond current needs.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Runtime/ContractEscapeResolutionRuntime.cs \
        Reloader/Assets/_Project/Core/Scripts/Runtime/ContractOfferSnapshot.cs \
        Reloader/Assets/_Project/Core/Tests/EditMode/ContractEscapeResolutionRuntimeTests.cs \
        Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs
git commit -m "feat: make wrong-target contract failure opt-in"
```

### Task 4: Add clear/cancel for failed contracts

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/ContractEscapeResolutionRuntime.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryUiState.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryContractStatus.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsSectionPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/ContractEscapeResolutionRuntimeTests.cs`

**Step 1: Write the failing tests**

Add tests that require:
- failed contracts expose a clear/cancel action
- clearing the failed contract removes the mission snapshot
- clearing a failed contract does not reset police-search state

**Step 2: Run the targeted tests to verify they fail**

Run focused edit-mode and play-mode commands for the new coverage.

**Step 3: Write the minimal implementation**

Add a clear/cancel flow that:
- is available any time after failure
- clears contract state only
- leaves law-enforcement state intact

**Step 4: Run the targeted tests to verify they pass**

Re-run the focused commands and confirm green.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Runtime/ContractEscapeResolutionRuntime.cs \
        Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs \
        Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryController.cs \
        Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryUiState.cs \
        Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryContractStatus.cs \
        Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsSectionPlayModeTests.cs \
        Reloader/Assets/_Project/Core/Tests/EditMode/ContractEscapeResolutionRuntimeTests.cs
git commit -m "feat: allow failed contracts to be cleared"
```

### Task 5: Render restrictions in the Contracts tab from policy data

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryViewBinder.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryUiState.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryContractStatus.cs`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/USS/TabInventory.uss`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsSectionPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/EditMode/TabInventoryUxmlCopyEditModeTests.cs`

**Step 1: Write the failing UI tests**

Require:
- strict contracts show a readable restriction line in the Contracts tab
- relaxed contracts omit that line or show no special restrictions
- failed strict contracts continue showing the same restriction context

**Step 2: Run the targeted tests to verify they fail**

Run focused UI edit-mode/play-mode commands.

**Step 3: Write the minimal implementation**

Add policy-derived restriction text to the Contracts tab without leaking contract messaging back into the Device tab.

**Step 4: Run the targeted tests to verify they pass**

Re-run the focused UI commands and confirm green.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs \
        Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryViewBinder.cs \
        Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryUiState.cs \
        Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryContractStatus.cs \
        Reloader/Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml \
        Reloader/Assets/_Project/UI/Toolkit/USS/TabInventory.uss \
        Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsSectionPlayModeTests.cs \
        Reloader/Assets/_Project/UI/Tests/EditMode/TabInventoryUxmlCopyEditModeTests.cs
git commit -m "feat: show contract restrictions in contracts tab"
```

### Task 6: Wire procedural defaults and verify full slice

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`
- Modify: any contract-definition creation helpers touched by the runtime
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/ContractEscapeResolutionRuntimeTests.cs`
- Test: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsSectionPlayModeTests.cs`

**Step 1: Write the failing procedural-default test**

Require procedural MainTown contracts to publish without strict failure rules by default.

**Step 2: Run the focused test to verify it fails**

Use the smallest edit-mode command for the population bridge tests.

**Step 3: Write the minimal implementation**

Make procedural contract generation emit relaxed policy data by default.

**Step 4: Run the full targeted verification set**

Run:
- NPC edit-mode tests
- core edit-mode contract tests
- targeted MainTown play-mode contract tests
- targeted Contracts-tab UI play-mode tests
- `git diff --check`

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs \
        Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs \
        Reloader/Assets/_Project/Core/Tests/EditMode/ContractEscapeResolutionRuntimeTests.cs \
        Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs \
        Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsSectionPlayModeTests.cs
git commit -m "feat: default procedural contracts to relaxed policy"
```

### Task 7: Open the new PR

**Files:**
- Modify: progress note under `docs/plans/progress/` if needed

**Step 1: Re-run final verification**

Run the same targeted suite from Task 6 plus any additional focused tests added during implementation.

**Step 2: Push the branch**

```bash
git push -u origin <new-branch>
```

**Step 3: Create a non-draft PR**

Use `gh pr create` with:
- clear summary bullets
- explicit test plan
- note that this is the follow-up slice after PR 28

**Step 4: Request review / leave branch ready for bot review**

Ensure the PR is open and non-draft so `@codex` review can happen on the commit stream.

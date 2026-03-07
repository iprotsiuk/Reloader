# TAB Overlay Contracts Redesign Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Redesign the immersive `TAB` overlay into a denser three-region shell, ship the new `Contracts` posted-feed and active-mission views, and make contract completion use an explicit claim flow instead of collapsing directly into empty state.

**Architecture:** Keep the current UI Toolkit runtime bridge and controller stack, but refactor the tab shell into a stable left-rail / center-workspace / right-detail layout. Implement the visual shell first, then upgrade the contracts data flow and states to support `Cancel Contract` and `Ready to Claim`, validating each stage with Unity MCP screenshots and targeted tests.

**Tech Stack:** Unity, UI Toolkit, existing `TabInventoryController` / `TabInventoryViewBinder` / `UiToolkitScreenRuntimeBridge`, Unity MCP screenshots/tests, GitHub PR workflow.

---

### Task 1: Create the redesign branch scaffold and docs checkpoint

**Files:**
- Create: `docs/plans/2026-03-07-tab-overlay-contracts-redesign-design.md`
- Create: `docs/plans/2026-03-07-tab-overlay-contracts-redesign.md`
- Create: `docs/plans/progress/2026-03-07-tab-overlay-contracts-redesign-progress.md`

**Step 1: Write the progress tracker**

- add status, execution checklist, notes, and verification sections
- include explicit screenshot-validation checkpoints

**Step 2: Run docs guardrail verification**

Run: `bash scripts/verify-docs-and-context.sh`
Expected: PASS

**Step 3: Commit docs scaffold**

```bash
git add docs/plans/2026-03-07-tab-overlay-contracts-redesign-design.md \
        docs/plans/2026-03-07-tab-overlay-contracts-redesign.md \
        docs/plans/progress/2026-03-07-tab-overlay-contracts-redesign-progress.md
git commit -m "docs: add tab overlay contracts redesign plan"
```

---

### Task 2: Open the non-draft PR immediately

**Files:**
- Modify: none

**Step 1: Push the branch**

Run: `git push -u origin feature/tab-overlay-contracts-redesign`
Expected: branch published

**Step 2: Open the PR**

Create a non-draft PR to `main` with:

- summary of redesign goals
- note that this PR will commit in narrow UI checkpoints
- initial test plan referencing targeted PlayMode verification + screenshot review

**Step 3: Tag `@codex` for review**

- post review request after PR creation

---

### Task 3: Gather visual references and icon candidates

**Files:**
- Inspect: `/Users/ivanprotsiuk/Documents/assets/LOWPOLY/**`
- Modify: `docs/plans/progress/2026-03-07-tab-overlay-contracts-redesign-progress.md`

**Step 1: Inspect icon sources**

- search the `LOWPOLY` packs for inventory/device/journal/calendar-compatible icons
- note whether the assets fit the current visual language

**Step 2: Capture current baseline screenshots**

- open current `TAB` overlay in editor
- capture screenshots for `Inventory`, `Contracts`, and `Device`

**Step 3: Record visual findings**

- update progress doc with:
  - accepted icon source
  - baseline screenshot observations
  - density problems to eliminate

**Step 4: Commit findings**

```bash
git add docs/plans/progress/2026-03-07-tab-overlay-contracts-redesign-progress.md
git commit -m "docs: record tab redesign baseline findings"
```

---

### Task 4: Lock the left rail shell with a failing UI test first

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/UiRuntimeCutoverPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryViewBinder.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryUiState.cs`

**Step 1: Write the failing test**

- add a PlayMode test that asserts the `TAB` shell renders:
  - narrow left rail
  - center pane
  - right detail pane
- verify icon-only nav structure instead of large text-tab button stack

**Step 2: Run the test to confirm failure**

Run targeted Unity MCP PlayMode test for the new test case
Expected: FAIL because the old shell still renders

**Step 3: Implement the minimal shell changes**

- add left rail containers / center pane / right pane to the view binder
- preserve existing intent wiring
- keep existing sections functional while changing layout structure

**Step 4: Re-run targeted tests**

Run targeted Unity MCP PlayMode test
Expected: PASS

**Step 5: Capture screenshot**

- take a screenshot of the new shell in editor
- compare against the approved sketch

**Step 6: Commit**

```bash
git add Reloader/Assets/_Project/UI/Tests/PlayMode/UiRuntimeCutoverPlayModeTests.cs \
        Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryViewBinder.cs \
        Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryUiState.cs \
        docs/plans/progress/2026-03-07-tab-overlay-contracts-redesign-progress.md
git commit -m "feat: add dense tab overlay shell"
```

---

### Task 5: Implement icon-first left rail and denser spacing

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryViewBinder.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryUiState.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsSectionPlayModeTests.cs`

**Step 1: Write failing tests**

- assert active section highlight still works with icon-first rail
- assert old text-tab button footprint is gone / reduced

**Step 2: Run tests to verify failure**

Run targeted PlayMode tests
Expected: FAIL

**Step 3: Implement minimal rail + spacing changes**

- narrow the rail
- switch to icon-first buttons / placeholders
- reduce padding and text scale consistently

**Step 4: Re-run tests**

Expected: PASS

**Step 5: Capture screenshots**

- `Contracts`
- `Inventory`
- `Device`

**Step 6: Commit**

```bash
git add ...
git commit -m "feat: convert tab navigation to icon rail"
```

---

### Task 6: Convert posted contracts into a scrollable dense feed

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryViewBinder.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryContractStatus.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsSectionPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsBridgePlayModeTests.cs`

**Step 1: Write failing tests**

- posted contract renders as a row with:
  - left image slot
  - title
  - summary clamp
  - payout
  - accept button
- feed container is scrollable

**Step 2: Run tests to confirm failure**

Expected: FAIL on old card layout

**Step 3: Implement minimal posted-feed UI**

- replace the current contract panel stack with a scroll feed region
- keep data model small for now: single posted contract still renders through list infrastructure

**Step 4: Re-run tests**

Expected: PASS

**Step 5: Capture screenshot and review density**

- verify row height and spacing against sketch

**Step 6: Commit**

```bash
git add ...
git commit -m "feat: redesign posted contracts as dense feed"
```

---

### Task 7: Add active-contract workspace mode

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryUiState.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryViewBinder.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsSectionPlayModeTests.cs`

**Step 1: Write failing tests**

- active state shows:
  - mission status at top
  - payout in header
  - target name below status
  - briefing block
  - intel block
  - `Cancel Contract` action

**Step 2: Run targeted tests**

Expected: FAIL

**Step 3: Implement minimal active workspace**

- render active mission workspace in center pane
- wire state ordering to match approved design

**Step 4: Re-run tests**

Expected: PASS

**Step 5: Capture screenshot**

- compare with approved active-contract sketch

**Step 6: Commit**

```bash
git add ...
git commit -m "feat: add active contract workspace view"
```

---

### Task 8: Add explicit cancel and ready-to-claim states with TDD

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/ContractEscapeResolutionRuntimeTests.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/ContractEscapeResolutionRuntime.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/ContractOfferSnapshot.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryContractStatus.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsBridgePlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`

**Step 1: Write failing runtime tests**

- active contract can be cancelled before kill
- successful contract after search clear enters `ReadyToClaim`
- reward is not awarded until `ClaimReward`

**Step 2: Run tests to verify failure**

Expected: FAIL

**Step 3: Implement minimal runtime changes**

- extend runtime state to distinguish:
  - active
  - awaiting search clear
  - ready to claim
- add explicit cancel / claim API surface

**Step 4: Run runtime tests**

Expected: PASS

**Step 5: Extend UI bridge / controller tests**

- add failing tests for `Cancel Contract` and `Claim Reward`
- run them to verify failure

**Step 6: Implement UI wiring**

- surface the new actions through adapter/controller/view

**Step 7: Re-run targeted EditMode and PlayMode tests**

Expected: PASS

**Step 8: Capture screenshot**

- `Ready to Claim` state screenshot

**Step 9: Commit**

```bash
git add ...
git commit -m "feat: add contract cancel and claim flow"
```

---

### Task 9: Rework right pane into mission terms and reward logic

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryUiState.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryViewBinder.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryContractStatus.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsSectionPlayModeTests.cs`

**Step 1: Write failing tests**

- right pane shows:
  - base payout
  - bonus list placeholder
  - restrictions placeholder
  - failure conditions
  - current reward / claim state

**Step 2: Run tests to verify failure**

Expected: FAIL

**Step 3: Implement minimal right-pane terms block**

- use placeholders where the runtime does not yet provide real modifier data
- keep structure ready for future bonus conditions

**Step 4: Re-run tests**

Expected: PASS

**Step 5: Capture screenshot**

- verify right pane reads as rules / reward logic, not weak metadata

**Step 6: Commit**

```bash
git add ...
git commit -m "feat: redesign contract detail pane as mission terms"
```

---

### Task 10: Validate with screenshots and tighten density

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryViewBinder.cs`
- Modify: `docs/plans/progress/2026-03-07-tab-overlay-contracts-redesign-progress.md`

**Step 1: Review screenshot set**

- posted list
- active contract
- ready to claim
- inventory under new shell

**Step 2: Apply minimal density-only adjustments**

- font sizes
- padding
- divider spacing
- row height

**Step 3: Re-run affected UI tests**

Expected: PASS

**Step 4: Capture final screenshot set**

**Step 5: Update progress doc with before/after notes**

**Step 6: Commit**

```bash
git add ...
git commit -m "style: tighten tab overlay density"
```

---

### Task 11: Final verification and review handoff

**Files:**
- Modify: `docs/plans/progress/2026-03-07-tab-overlay-contracts-redesign-progress.md`

**Step 1: Run targeted verification**

- `bash scripts/verify-docs-and-context.sh`
- targeted UI PlayMode suites
- targeted contract runtime EditMode suites
- `git diff --check`

**Step 2: Capture final Unity console state**

- verify no unexpected new console errors

**Step 3: Update progress doc with exact verification results**

**Step 4: Push latest branch**

**Step 5: Request / respond to PR review**

- tag `@codex review`
- resolve comments in-thread with frequent small commits

---

Plan complete and saved to `docs/plans/2026-03-07-tab-overlay-contracts-redesign.md`. Two execution options:

**1. Subagent-Driven (this session)** - I dispatch fresh subagent per task, review between tasks, fast iteration

**2. Parallel Session (separate)** - Open new session with executing-plans, batch execution with checkpoints

**Which approach?**

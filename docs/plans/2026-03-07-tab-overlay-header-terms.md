# TAB Overlay Header And Terms Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a live game-world date/time plus player balance header to the `TAB` overlay, and turn the right pane into a meaningful contracts-only terms pane.

**Architecture:** Introduce a lightweight `CoreWorld` runtime/controller for `dayCount` and `timeOfDay`, surface it to the TAB UI through the existing runtime bridge/controller path, and keep the right pane contracts-specific for this slice. Drive the implementation test-first so the header formatting and terms-pane behavior are locked before the UI markup changes.

**Tech Stack:** Unity, UI Toolkit, existing `UiToolkitScreenRuntimeBridge`, `TabInventoryController`, `TabInventoryViewBinder`, `EconomyController`, targeted Unity EditMode/PlayMode tests, GitHub PR workflow.

---

### Task 1: Document the slice and progress checkpoint

**Files:**
- Create: `docs/plans/2026-03-07-tab-overlay-header-terms-design.md`
- Create: `docs/plans/2026-03-07-tab-overlay-header-terms.md`
- Modify: `docs/plans/progress/2026-03-07-tab-overlay-contracts-redesign-progress.md`

**Step 1: Write the design doc**

- capture the approved header format: `Monday • 18:40 • $2,450`
- capture the lightweight live `CoreWorld` runtime decision
- capture the contracts-only right-pane scope

**Step 2: Write this implementation plan**

- keep tasks bite-sized
- include exact files and targeted tests

**Step 3: Update the redesign progress tracker**

- add the new header/terms-pane slice as the next checkpoint

**Step 4: Run docs verification**

Run: `bash scripts/verify-docs-and-context.sh`
Expected: PASS

**Step 5: Commit**

```bash
git add docs/plans/2026-03-07-tab-overlay-header-terms-design.md \
        docs/plans/2026-03-07-tab-overlay-header-terms.md \
        docs/plans/progress/2026-03-07-tab-overlay-contracts-redesign-progress.md
git commit -m "docs: add tab header and terms plan"
```

---

### Task 2: Add failing tests for world-clock formatting

**Files:**
- Create or modify: `Reloader/Assets/_Project/Core/Tests/EditMode/CoreWorldRuntimeTests.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/EditMode/TabInventory...` test file if a UI formatting test belongs there

**Step 1: Write the failing tests**

Cover:

- `dayCount = 0` maps to `Monday`
- fractional `timeOfDay` formats to military `HH:mm`
- a small snapshot/helper can produce `Monday • 18:40`

**Step 2: Run the test to confirm it fails**

Run targeted Unity EditMode tests for the new file/class
Expected: FAIL because the runtime/formatter does not exist yet

**Step 3: Write minimal runtime/formatter code**

- add the smallest `CoreWorld` runtime/value object needed to satisfy the tests

**Step 4: Re-run the tests**

Expected: PASS

**Step 5: Commit**

```bash
git add ...
git commit -m "feat: add core world clock runtime"
```

---

### Task 3: Add a lightweight world-clock controller/runtime seam

**Files:**
- Create: `Reloader/Assets/_Project/Core/Scripts/Runtime/CoreWorldRuntime.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Runtime/CoreWorldController.cs`
- Modify if needed: save/runtime contracts or bridge files that will resolve the controller
- Test: `Reloader/Assets/_Project/Core/Tests/PlayMode/...` if a controller PlayMode test is needed

**Step 1: Write the failing controller test**

Cover:

- controller exposes a runtime with authored starting `dayCount` and `timeOfDay`
- runtime values are readable without requiring full gameplay time progression

**Step 2: Run the targeted test and confirm failure**

Expected: FAIL

**Step 3: Implement the minimal controller**

- initialize runtime in `Awake`
- expose runtime publicly
- keep scope read-only/minimal for this slice

**Step 4: Re-run the targeted test**

Expected: PASS

**Step 5: Commit**

```bash
git add ...
git commit -m "feat: add lightweight core world controller"
```

---

### Task 4: Add failing TAB header tests

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventory...` tests
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/UiRuntimeCutoverPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsBridgePlayModeTests.cs` if bridge coverage is needed

**Step 1: Write the failing tests**

Cover:

- TAB header shows `dayOfWeek • timeOfDay • balance`
- header updates from live economy/world values
- formatting uses full weekday + military time + full currency

**Step 2: Run targeted tests to confirm failure**

Expected: FAIL because the header bindings do not exist yet

**Step 3: Implement the minimal state surface**

- extend TAB UI state for header text or structured header fields
- extend the runtime bridge/controller resolution to read:
  - `CoreWorldController`
  - `EconomyController`

**Step 4: Re-run tests**

Expected: PASS

**Step 5: Commit**

```bash
git add ...
git commit -m "feat: add tab header world time and balance"
```

---

### Task 5: Add failing terms-pane tests

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Tests/EditMode/TabInventoryUxmlCopyEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsSectionPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsBridgePlayModeTests.cs`

**Step 1: Write the failing tests**

Cover:

- right pane contains structured terms fields in the `Contracts` section
- terms pane reflects:
  - base payout
  - restrictions
  - failure conditions
  - reward state
- non-Contracts sections do not show contract-terms content

**Step 2: Run targeted tests to verify failure**

Expected: FAIL

**Step 3: Implement the minimal state and binding**

- extend contract UI state with right-pane terms fields
- keep data simple for the current slice
- derive values from current contract/runtime state where possible

**Step 4: Re-run tests**

Expected: PASS

**Step 5: Commit**

```bash
git add ...
git commit -m "feat: add contracts terms pane"
```

---

### Task 6: Update UXML/USS for the header and terms-pane layout

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/USS/TabInventory.uss`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryViewBinder.cs`

**Step 1: Write the failing UXML structure test**

Cover:

- header metadata label/container exists in the top-right header row
- right pane contains authored terms placeholders/containers

**Step 2: Run targeted EditMode test**

Expected: FAIL

**Step 3: Implement minimal markup/styling**

- add top-right metadata container
- add terms-pane sections/labels
- preserve compact density established in the current redesign

**Step 4: Re-run targeted tests**

Expected: PASS

**Step 5: Run non-Unity verification**

Run:
- `xmllint --noout Reloader/Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml`
- `git diff --check`

Expected: PASS

**Step 6: Commit**

```bash
git add ...
git commit -m "feat: author tab header and contract terms pane"
```

---

### Task 7: Run the full targeted verification chain

**Files:**
- Modify: `docs/plans/progress/2026-03-07-tab-overlay-contracts-redesign-progress.md`

**Step 1: Run targeted Unity tests**

Run:

- `Reloader.UI.Tests.EditMode.TabInventoryUxmlCopyEditModeTests`
- `Reloader.UI.Tests.EditMode.TabInventoryResponsiveLayoutEditModeTests`
- relevant new `CoreWorld` EditMode tests
- `Reloader.UI.Tests.PlayMode.TabInventoryContractsSectionPlayModeTests`
- `Reloader.UI.Tests.PlayMode.TabInventoryContractsBridgePlayModeTests`
- `Reloader.UI.Tests.PlayMode.UiRuntimeCutoverPlayModeTests`
- `Reloader.UI.Tests.PlayMode.TabInventoryAttachmentsPlayModeTests`

**Step 2: Run supporting verification**

Run:

- `bash scripts/verify-docs-and-context.sh`
- `xmllint --noout Reloader/Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml`
- `git diff --check`

**Step 3: Capture screenshot checkpoint**

- use Unity MCP screenshot capture on the updated TAB overlay
- verify the header and terms pane against the approved design

**Step 4: Update progress doc**

- record what landed
- record exact verification results

**Step 5: Commit**

```bash
git add docs/plans/progress/2026-03-07-tab-overlay-contracts-redesign-progress.md
git commit -m "docs: record tab header and terms verification"
```

---

### Task 8: Push and request review

**Files:**
- Modify: none

**Step 1: Push branch**

Run: `git push origin feature/tab-overlay-contracts-redesign`

**Step 2: Post PR checkpoint comment**

- summarize the new header/terms-pane slice
- tag `@codex` for review

**Step 3: Check review threads**

- inspect unresolved review threads on PR `#26`
- reply and resolve any valid addressed comments

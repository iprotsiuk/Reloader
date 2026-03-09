# STYLE Demo Crowd Review Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Rebuild the imported STYLE demo scene into a 100-NPC review playground with 50 plausible archetypes and 50 stress-test combinations.

**Architecture:** Keep the source of truth in the disposable demo scene, but drive the large crowd build from a small editor utility plus explicit batch definitions. Use pure C# planning logic for counts, naming, and part-selection contracts, then let the editor utility instantiate and configure the scene from the approved spec.

**Tech Stack:** Unity editor scripts, EditMode tests, imported STYLE model assets, Unity scene serialization, Unity MCP verification

---

### Task 1: Lock the crowd spec in code

**Files:**
- Create: `Reloader/Assets/_Project/World/Editor/StyleCrowdReviewSpec.cs`
- Create: `Reloader/Assets/_Project/World/Editor/StyleCrowdReviewPlausibleBatch.cs`
- Create: `Reloader/Assets/_Project/World/Editor/StyleCrowdReviewStressBatch.cs`
- Test: `Reloader/Assets/_Project/World/Tests/EditMode/StyleCrowdReviewSpecEditModeTests.cs`

**Step 1: Write the failing test**

- Add EditMode tests that assert:
  - total spec count is `100`
  - `PlausibleBatch` count is `50`
  - `StressBatch` count is `50`
  - names are unique
  - the expected role prefixes exist across the plausible batch

**Step 2: Run test to verify it fails**

Run a focused EditMode test for the new spec file and confirm failure because the spec types do not exist yet.

**Step 3: Write minimal implementation**

- Define the review-spec data model.
- Add one static file for plausible entries and one static file for stress entries.
- Keep the entries explicit rather than over-engineered.

**Step 4: Run test to verify it passes**

Run the focused EditMode spec tests and confirm green.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/World/Editor/StyleCrowdReviewSpec.cs
git add Reloader/Assets/_Project/World/Editor/StyleCrowdReviewPlausibleBatch.cs
git add Reloader/Assets/_Project/World/Editor/StyleCrowdReviewStressBatch.cs
git add Reloader/Assets/_Project/World/Tests/EditMode/StyleCrowdReviewSpecEditModeTests.cs
git commit -m "test: add style crowd review spec contract"
```

### Task 2: Build the editor-side crowd rebuilder

**Files:**
- Create: `Reloader/Assets/_Project/World/Editor/StyleCrowdReviewBuilder.cs`
- Test: `Reloader/Assets/_Project/World/Tests/EditMode/StyleCrowdReviewBuilderEditModeTests.cs`

**Step 1: Write the failing test**

- Add EditMode tests for the pure helper logic used by the builder:
  - valid part selection per gender
  - exactly one active top/bottom/hair selection per configured NPC
  - stable batch-to-grid positioning helpers

**Step 2: Run test to verify it fails**

Run the focused builder test and confirm it fails before the helper code exists.

**Step 3: Write minimal implementation**

- Add a menu-driven builder for the active STYLE demo scene.
- Load the imported male/female rig assets.
- Delete prior review roots and old demo character roots.
- Instantiate `100` NPCs from the two batch specs.
- Rename and place roots into `PlausibleBatch` and `StressBatch`.
- Toggle child part GameObjects according to the selected spec.

**Step 4: Run test to verify it passes**

Run the focused builder helper tests and confirm green.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/World/Editor/StyleCrowdReviewBuilder.cs
git add Reloader/Assets/_Project/World/Tests/EditMode/StyleCrowdReviewBuilderEditModeTests.cs
git commit -m "feat: add style crowd review scene builder"
```

### Task 3: Rebuild the demo scene from the approved batches

**Files:**
- Modify: `Reloader/Assets/STYLE - Character Customization Kit/Scene/Demo.unity`

**Step 1: Run the builder against the demo scene**

- Open the STYLE demo scene.
- Execute the menu item that rebuilds the crowd from the approved spec.

**Step 2: Verify the hierarchy shape**

- Confirm the scene now has batch roots and `100` named NPCs.
- Confirm the old six-character lineup is no longer the primary content.

**Step 3: Adjust only what is needed for readability**

- Tweak spacing or root transforms if the initial grid is too dense for manual review.

**Step 4: Save the scene**

- Save `Demo.unity` after the rebuilt crowd is in place.

**Step 5: Commit**

```bash
git add "Reloader/Assets/STYLE - Character Customization Kit/Scene/Demo.unity"
git commit -m "feat: rebuild style demo scene as crowd review playground"
```

### Task 4: Verify the scene visually and record follow-up notes

**Files:**
- Modify: `docs/plans/progress/2026-03-07-style-character-preview-scene-progress.md`

**Step 1: Run focused verification**

- Capture a Unity scene screenshot.
- Read the Unity console and confirm no obvious material/import errors remain.

**Step 2: Record limitations**

- Note which archetypes are only approximate because the pack lacks dedicated police/EMS uniforms.
- Note any obvious clipping or weak combinations discovered during the first automated pass.

**Step 3: Update progress**

- Record that the demo scene is now the working crowd-review playground and summarize the two-batch setup.

**Step 4: Commit**

```bash
git add docs/plans/progress/2026-03-07-style-character-preview-scene-progress.md
git commit -m "docs: update style crowd review playground progress"
```

# STYLE Character Preview Scene Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a project-owned preview scene with several STYLE-based NPC variants so character quality and variation can be evaluated before MainTown population work.

**Architecture:** Keep the preview scene under `_Project/World/Scenes`, use imported STYLE character assets for visuals, and wrap preview characters in the existing NPC foundation/prefab pipeline so the content can be reused later. Keep the slice static and inspection-focused, with targeted validation rather than broad gameplay wiring.

**Tech Stack:** Unity scenes/prefabs, imported FBX character assets, existing NPC foundation prefabs, EditMode/PlayMode tests, docs progress tracking

---

### Task 1: Inspect STYLE kit entry points and record the viable visual pipeline

**Files:**
- Modify: `docs/plans/progress/2026-03-07-style-character-preview-scene-progress.md`

**Step 1: Inspect the imported STYLE demo scene and model assets**

- Open the imported demo scene and model assets through Unity/editor tooling.
- Identify which FBXs/materials are viable for quick civilian variants.

**Step 2: Record the chosen visual path**

- Note whether the slice will use full-rig male/female bases, material swaps, and/or mesh swaps.

**Step 3: Commit**

```bash
git add docs/plans/progress/2026-03-07-style-character-preview-scene-progress.md
git commit -m "docs: record style preview scene asset inspection"
```

### Task 2: Add validation coverage for the preview scene contract

**Files:**
- Create: `Reloader/Assets/_Project/World/Tests/EditMode/StyleCharacterPreviewSceneEditModeTests.cs`
- Test: `Reloader/Assets/_Project/World/Tests/EditMode/StyleCharacterPreviewSceneEditModeTests.cs`

**Step 1: Write the failing test**

- Add an EditMode test that asserts the preview scene exists and contains the expected preview root/lineup objects.

**Step 2: Run test to verify it fails**

- Run the focused EditMode test and confirm failure before authoring the scene.

**Step 3: Write minimal implementation**

- Author the scene structure needed for the test to pass.

**Step 4: Run test to verify it passes**

- Re-run the focused EditMode test and confirm green.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/World/Tests/EditMode/StyleCharacterPreviewSceneEditModeTests.cs
git commit -m "test: add style character preview scene contract"
```

### Task 3: Author the preview scene shell

**Files:**
- Create: `Reloader/Assets/_Project/World/Scenes/StyleCharacterPreview.unity`
- Modify: `Reloader/Assets/_Project/World/Data/SceneContracts/*.asset` if needed

**Step 1: Create the scene**

- Add a simple ground, camera/spawn, and neutral daylight setup for inspection.

**Step 2: Add lineup structure**

- Create a root object and evenly spaced lineup anchors for `4-6` preview characters.

**Step 3: Keep the scene intentionally minimal**

- Do not add gameplay-specific noise beyond what is needed for inspection.

**Step 4: Verify scene opens cleanly**

- Open the scene in Unity and confirm no import/wiring errors.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/World/Scenes/StyleCharacterPreview.unity
git commit -m "feat: add style character preview scene shell"
```

### Task 4: Build preview NPC variants

**Files:**
- Create or modify under: `Reloader/Assets/_Project/NPCs/Prefabs/Preview/`
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/NpcFoundation.prefab` only if strictly necessary

**Step 1: Create the first failing expectation**

- Verify the preview scene contract fails until the expected number of preview NPCs are present.

**Step 2: Build minimal variants**

- Create several `NpcFoundation`-based preview NPC variants using STYLE visuals.
- Keep them static and inspection-focused.

**Step 3: Place the variants into the preview scene**

- Assign lineup positions and ensure each variant is visually distinct enough for evaluation.

**Step 4: Verify in editor**

- Open the scene and inspect proportions, materials, and readability.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Prefabs/Preview
git add Reloader/Assets/_Project/World/Scenes/StyleCharacterPreview.unity
git commit -m "feat: add style preview npc lineup"
```

### Task 5: Run focused verification and update progress

**Files:**
- Modify: `docs/plans/progress/2026-03-07-style-character-preview-scene-progress.md`

**Step 1: Run focused verification**

- Run the preview-scene EditMode test and any relevant world/NPC validation.

**Step 2: Capture verification notes**

- Record exactly what was checked and any known limitations.

**Step 3: Update progress doc**

- Summarize the resulting scene, chosen asset path, and next follow-up work.

**Step 4: Commit**

```bash
git add docs/plans/progress/2026-03-07-style-character-preview-scene-progress.md
git commit -m "docs: update style character preview scene progress"
```

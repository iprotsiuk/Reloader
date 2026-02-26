# Belt HUD (Reusable Prefab) Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a reusable 5-slot belt HUD prefab that reflects runtime inventory occupancy and selected slot state using the Post-apocalyptic Survival UI assets.

**Architecture:** Add a UI presenter that subscribes to inventory events and reads `PlayerInventoryController.Runtime` for authoritative slot/selection state. Instantiate via a lightweight bootstrap component so scenes can opt in without hardcoded references. Keep visuals belt-only and independent from future TAB menu systems.

**Tech Stack:** Unity 6 C#, uGUI (`Canvas`, `Image`, `TMP_Text`), runtime event ports/hub (`IGameEventsRuntimeHub`/`IInventoryEvents`), existing `PlayerInventoryController`/`PlayerInventoryRuntime`, imported PNG sprites.

---

### Task 1: Import external sprites into project UI folder

**Files:**
- Create: `Reloader/Assets/_Project/UI/Sprites/BeltHUD/SlotFrame_Component12.png`
- Create: `Reloader/Assets/_Project/UI/Sprites/BeltHUD/ItemPlaceholder_Icon01.png`
- Create: corresponding `.meta` import files (Unity-generated)

**Step 1: Gather source assets**
- Copy from external package mapping discovered in `pathname` metadata:
  - `Component_12.png` (slot frame)
  - `Icon_01.png` (placeholder)

**Step 2: Place in project UI sprite folder**
- Keep names explicit for belt HUD usage.

**Step 3: Verify import settings in Unity**
- Texture type: Sprite (2D and UI)
- Compression suitable for UI

**Step 4: Commit**
```bash
git add Reloader/Assets/_Project/UI/Sprites/BeltHUD
git commit -m "feat: import belt hud sprites"
```

### Task 2: Add HUD presenter and bootstrap scripts

**Files:**
- Create: `Reloader/Assets/_Project/UI/Scripts/BeltHudPresenter.cs`
- Create: `Reloader/Assets/_Project/UI/Scripts/BeltHudBootstrap.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts` asmdef (create if missing) to reference `Reloader.Core` and `Reloader.Inventory`

**Step 1: Write failing test scaffolds (EditMode where practical)**
- Test selected-index styling behavior (scale/tint state mapping)
- Test occupancy visibility mapping

**Step 2: Implement presenter**
- Serialized list of 5 slot view refs (frame image, item image, label)
- Bind to `PlayerInventoryController`
- Subscribe/unsubscribe runtime inventory port events (`OnInventoryChanged` + `OnBeltSelectionChanged`)
- Refresh all slots each update event

**Step 3: Implement bootstrap**
- Resolve `PlayerInventoryController` in scene
- Ensure presenter has runtime source
- Warn once if missing source

**Step 4: Commit**
```bash
git add Reloader/Assets/_Project/UI/Scripts
git commit -m "feat: add belt hud presenter and bootstrap"
```

### Task 3: Create reusable BeltHud prefab

**Files:**
- Create: `Reloader/Assets/_Project/UI/Prefabs/BeltHud.prefab`
- Optional create: helper prefab for slot view if useful

**Step 1: Build prefab structure**
- Root under screen-space canvas-friendly container
- Horizontal row with 5 slot children
- Slot child includes frame image, placeholder image, TMP label `1..5`

**Step 2: Wire presenter references**
- Assign 5 slot refs in `BeltHudPresenter`
- Assign normal/selected tint + selected scale

**Step 3: Add bootstrap to a persistent scene object or prefab root**
- Ensure it can instantiate HUD once at runtime

**Step 4: Commit**
```bash
git add Reloader/Assets/_Project/UI/Prefabs
git commit -m "feat: add reusable belt hud prefab"
```

### Task 4: Add focused playmode verification tests

**Files:**
- Create: `Reloader/Assets/_Project/UI/Tests/PlayMode/BeltHudPresenterPlayModeTests.cs`
- Create/modify: `Reloader/Assets/_Project/UI/Tests/PlayMode` asmdef

**Step 1: Write tests**
- Selected slot applies selected style (scale/tint)
- Empty slot selected still styled selected
- Occupied slot shows placeholder icon
- Empty slot hides placeholder icon

**Step 2: Run tests in Unity test runner**
- PlayMode filter to belt HUD tests

**Step 3: Commit**
```bash
git add Reloader/Assets/_Project/UI/Tests/PlayMode
git commit -m "test: add belt hud presenter playmode tests"
```

### Task 5: Docs alignment and verification

**Files:**
- Modify: `docs/design/inventory-and-economy.md` (belt HUD note)
- Modify: `docs/plans/2026-02-24-belt-hud-design.md` if implementation details differ

**Step 1: Update docs**
- Note reusable prefab path and style contract.

**Step 2: Run docs/context verifier**
```bash
./scripts/verify-docs-and-context.sh
```

**Step 3: Run available Unity validation commands**
- If CLI test results remain unavailable, record limitation and rely on in-editor play test checklist.

**Step 4: Final commit**
```bash
git add docs/design/inventory-and-economy.md docs/plans/2026-02-24-belt-hud-design.md
git commit -m "docs: document belt hud integration"
```

# Persistent Civilian Population Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Generate a persistent, map-wide `MainTown` civilian roster on save creation, respawn those civilians from save data on load, and retire/queue replacements when civilians die.

**Architecture:** Add a data-first civilian roster module that stores generated appearance records instead of scene object state. Use a curated appearance-part library plus free random slot selection to build initial civilians, then spawn runtime NPC instances from those persisted records when `MainTown` loads. The roster should evolve toward stable `populationSlotId` + pool ownership so future replacements inherit the same role slot. Death updates only the civilian roster state in this slice; Monday refresh execution and contract targeting land later.

**Tech Stack:** Unity ScriptableObjects, runtime save modules, NPC foundation prefabs/runtime, world scene load hooks, EditMode/PlayMode tests, docs progress tracking

---

### Task 1: Document the current contracts and roster scope

**Files:**
- Modify: `docs/plans/progress/2026-03-07-persistent-civilian-population-progress.md`

**Step 1: Record the approved scope**

- Capture the approved lifecycle: generate on new save, persist across save/load, retire on death, queue Monday `08:00` replacement.

**Step 2: Record explicit non-goals**

- Note that professions, dialogue, wandering zones, voices, contract targeting, and refresh execution stay out of scope for this slice.

**Step 3: Commit**

```bash
git add docs/plans/progress/2026-03-07-persistent-civilian-population-progress.md
git commit -m "docs: record persistent civilian population scope"
```

### Task 2: Add the failing save-data contract tests

**Files:**
- Create: `Reloader/Assets/_Project/Core/Tests/EditMode/CivilianPopulationSaveModuleTests.cs`
- Create: `Reloader/Assets/_Project/World/Tests/EditMode/CivilianAppearanceGeneratorTests.cs`

**Step 1: Write the failing appearance-record test**

- Add an EditMode test that asserts generated civilian records contain required appearance slots, stable IDs, and contract-eligibility flags.

**Step 2: Write the failing save-module test**

- Add an EditMode test that asserts the civilian roster module serializes/deserializes generated records, death state, and queued replacements.

**Step 3: Run the focused tests to verify they fail**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.World.Tests.EditMode.CivilianAppearanceGeneratorTests tmp/civilian-appearance-edit.xml tmp/civilian-appearance-edit.log
bash scripts/run-unity-tests.sh editmode Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests tmp/civilian-save-edit.xml tmp/civilian-save-edit.log
```

Expected: failing assertions because the generator and save module contracts do not exist yet.

**Step 4: Commit**

```bash
git add Reloader/Assets/_Project/Core/Tests/EditMode/CivilianPopulationSaveModuleTests.cs
git add Reloader/Assets/_Project/World/Tests/EditMode/CivilianAppearanceGeneratorTests.cs
git commit -m "test: add civilian population save contracts"
```

### Task 3: Implement the civilian appearance library and generator

**Files:**
- Create under: `Reloader/Assets/_Project/NPCs/Data/Civilians/`
- Create under: `Reloader/Assets/_Project/NPCs/Scripts/Generation/`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcAgent.cs` only if required for minimal runtime wiring

**Step 1: Add the generator input contracts**

- Create the minimal data assets/contracts for allowed bodies, hair, beard, outfit, and material/color slots.

**Step 2: Implement the generator**

- Build a generator that selects free random compatible parts and produces a persistent civilian appearance record.

**Step 3: Re-run the generator tests**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.World.Tests.EditMode.CivilianAppearanceGeneratorTests tmp/civilian-appearance-edit.xml tmp/civilian-appearance-edit.log
```

Expected: passing generator tests.

**Step 4: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Data/Civilians
git add Reloader/Assets/_Project/NPCs/Scripts/Generation
git commit -m "feat: add civilian appearance generator"
```

### Task 4: Implement civilian population save-module wiring

**Files:**
- Create under: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/`
- Modify: save registration/bootstrap files under `Reloader/Assets/_Project/Core/Scripts/Save/`
- Modify: `docs/design/save-and-progression.md` only if the new module contract changes the shared save matrix

**Step 1: Add the civilian population payload**

- Define the persisted civilian roster payload, including living/dead state and queued replacement entries.

**Step 2: Register the module**

- Hook the civilian roster module into the existing save coordinator registration path without disturbing existing module order beyond what the new dependency requires.

**Step 3: Re-run the save-module tests**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests tmp/civilian-save-edit.xml tmp/civilian-save-edit.log
```

Expected: passing roster save/load tests.

**Step 4: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Save
git add docs/design/save-and-progression.md
git commit -m "feat: add civilian population save module"
```

### Task 5: Spawn civilians from saved records when MainTown loads

**Files:**
- Create or modify under: `Reloader/Assets/_Project/World/Scripts/Runtime/`
- Create or modify under: `Reloader/Assets/_Project/NPCs/Prefabs/Civilians/`
- Create: `Reloader/Assets/_Project/World/Tests/PlayMode/PersistentCivilianPopulationPlayModeTests.cs`

**Step 1: Write the failing scene-spawn test**

- Add a PlayMode test that boots `MainTown`, injects a small generated civilian roster, and asserts the expected civilians spawn from those saved records.

**Step 2: Run the focused PlayMode test to verify it fails**

Run:

```bash
bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.PersistentCivilianPopulationPlayModeTests tmp/civilian-pop-play.xml tmp/civilian-pop-play.log
```

Expected: failing scene-spawn assertions because no loader exists yet.

**Step 3: Implement the loader/spawner**

- Add the minimal world/runtime bridge that instantiates civilians from the saved roster when `MainTown` loads.

**Step 4: Re-run the focused PlayMode test**

Run:

```bash
bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.PersistentCivilianPopulationPlayModeTests tmp/civilian-pop-play.xml tmp/civilian-pop-play.log
```

Expected: passing scene-spawn assertions.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/World/Scripts/Runtime
git add Reloader/Assets/_Project/NPCs/Prefabs/Civilians
git add Reloader/Assets/_Project/World/Tests/PlayMode/PersistentCivilianPopulationPlayModeTests.cs
git commit -m "feat: spawn persistent civilians in maintown"
```

### Task 6: Retire dead civilians and queue replacements

**Files:**
- Modify: runtime population management files from Tasks 4-5
- Create: `Reloader/Assets/_Project/World/Tests/EditMode/CivilianPopulationRetirementTests.cs`

**Step 1: Write the failing retirement test**

- Add a test that asserts a civilian death marks the civilian dead, prevents future spawn/target eligibility, and adds exactly one owed replacement entry.

**Step 2: Run the focused test to verify it fails**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.World.Tests.EditMode.CivilianPopulationRetirementTests tmp/civilian-retire-edit.xml tmp/civilian-retire-edit.log
```

Expected: failing retirement assertions.

**Step 3: Implement minimal retirement behavior**

- Update the runtime/save bridge so civilian death mutates only the roster state required for this slice.

**Step 4: Re-run the focused retirement test**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.World.Tests.EditMode.CivilianPopulationRetirementTests tmp/civilian-retire-edit.xml tmp/civilian-retire-edit.log
```

Expected: passing retirement assertions.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/World/Tests/EditMode/CivilianPopulationRetirementTests.cs
git add Reloader/Assets/_Project/Core/Scripts/Save
git add Reloader/Assets/_Project/World/Scripts/Runtime
git commit -m "feat: retire dead civilians from persistent population"
```

### Task 7: Run verification and update progress

**Files:**
- Modify: `docs/plans/progress/2026-03-07-persistent-civilian-population-progress.md`

**Step 1: Run the focused verification sweep**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.World.Tests.EditMode.CivilianAppearanceGeneratorTests tmp/civilian-appearance-edit.xml tmp/civilian-appearance-edit.log
bash scripts/run-unity-tests.sh editmode Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests tmp/civilian-save-edit.xml tmp/civilian-save-edit.log
bash scripts/run-unity-tests.sh editmode Reloader.World.Tests.EditMode.CivilianPopulationRetirementTests tmp/civilian-retire-edit.xml tmp/civilian-retire-edit.log
bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.PersistentCivilianPopulationPlayModeTests tmp/civilian-pop-play.xml tmp/civilian-pop-play.log
bash scripts/verify-docs-and-context.sh
git diff --check
```

Expected: all targeted tests pass, docs verification passes, and the worktree has no whitespace issues.

**Step 2: Update the progress doc**

- Record what shipped, what remained deferred, and the exact verification results.

**Step 3: Commit**

```bash
git add docs/plans/progress/2026-03-07-persistent-civilian-population-progress.md
git commit -m "docs: update persistent civilian population progress"
```

## Next Slice After This Plan

Once this plan is complete, the next implementation slice should add:

- a `MainTownPopulationDefinition` that owns map-wide pools and stable `populationSlotId`s
- generated occupant assignment into those slots for the whole `MainTown` scene
- random contract-target selection from the living civilian population, excluding dead occupants and protected roles
- appearance-derived contract descriptions and portrait data
- Monday `08:00` replacement execution so owed civilians refill their slots correctly

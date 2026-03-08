# MainTown Population Slots Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add slot-driven, map-wide `MainTown` population definitions so generated civilians occupy stable world-role slots instead of an anonymous roster.

**Architecture:** Introduce a `MainTownPopulationDefinition` asset that defines pools and stable `populationSlotId`s for the whole scene. Extend the persistent civilian record/runtime bridge so occupants are generated into slots, preserve slot identity across save/load, and eventually spawn `MainTown` civilians from those persisted slots. Keep contracts and Monday replacement execution out of scope, but harden the model so dead occupants can never become future contract targets.

**Tech Stack:** Unity ScriptableObjects, runtime save modules, NPC runtime bridges, MainTown world bootstrap hooks, EditMode/PlayMode tests, docs progress tracking

---

> **Plan update (2026-03-08):** Insert an infrastructure-only checkpoint before live MainTown spawning.
> Author the scene root, starter population definition asset, and minimal serialized appearance library first.
> Keep real visual assembly/prefab spawning deferred until the infrastructure checkpoint is green.

### Task 1: Document the slot-and-pool model

**Files:**
- Create: `docs/plans/progress/2026-03-07-maintown-population-slots-progress.md`
- Modify: `docs/plans/2026-03-07-maintown-population-slots-design.md`

**Step 1: Record the approved scope**

- Capture the approved `MainTown` map-wide model:
  - fixed pools
  - stable `populationSlotId`s
  - slot-owned world roles
  - occupant records keyed by slot

**Step 2: Record explicit guardrails**

- Note that:
  - vendors are protected from future contracts
  - dead occupants must later be excluded from target selection
  - this slice does not implement contracts or Monday refresh execution

**Step 3: Commit**

```bash
git add docs/plans/2026-03-07-maintown-population-slots-design.md
git add docs/plans/progress/2026-03-07-maintown-population-slots-progress.md
git commit -m "docs: define maintown population slots scope"
```

### Task 2: Add the failing slot-definition contract tests

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Tests/EditMode/MainTownPopulationDefinitionTests.cs`
- Create: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationSlotAssignmentTests.cs`

**Step 1: Write the failing definition test**

- Add an EditMode test that asserts:
  - slot IDs are unique
  - pools own fixed counts
  - protected vendor slots are marked correctly

**Step 2: Write the failing assignment test**

- Add an EditMode test that asserts:
  - one live occupant is generated per slot
  - each occupant preserves `populationSlotId`
  - slot/pool metadata is copied into the occupant record

**Step 3: Run the focused tests to verify they fail**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.MainTownPopulationDefinitionTests tmp/maintown-pop-def-edit.xml tmp/maintown-pop-def-edit.log
bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationSlotAssignmentTests tmp/maintown-slot-assign-edit.xml tmp/maintown-slot-assign-edit.log
```

Expected: failing assertions because the slot definition and slot assignment logic do not exist yet.

**Step 4: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Tests/EditMode/MainTownPopulationDefinitionTests.cs
git add Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationSlotAssignmentTests.cs
git commit -m "test: add maintown population slot contracts"
```

### Task 3: Add MainTown pool and slot definition assets/contracts

**Files:**
- Create under: `Reloader/Assets/_Project/NPCs/Data/Population/`
- Create under: `Reloader/Assets/_Project/NPCs/Scripts/Generation/`

**Step 1: Add the definition contracts**

- Create the minimal ScriptableObject/data classes for:
  - `MainTownPopulationDefinition`
  - pool definitions
  - slot definitions

**Step 2: Implement validation**

- Validate:
  - unique `populationSlotId`s
  - fixed pool counts
  - required `spawnAnchorId`
  - required `poolId`

**Step 3: Re-run the focused definition test**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.MainTownPopulationDefinitionTests tmp/maintown-pop-def-edit.xml tmp/maintown-pop-def-edit.log
```

Expected: passing definition tests.

**Step 4: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Data/Population
git add Reloader/Assets/_Project/NPCs/Scripts/Generation
git commit -m "feat: add maintown population slot definitions"
```

### Task 4: Extend the civilian record/runtime bridge for slot-owned occupants

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/CivilianPopulationRecord.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/CivilianPopulationModule.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Generation/CivilianAppearanceGenerator.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/CivilianPopulationSaveModuleTests.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`

**Step 1: Write the failing slot-persistence assertions**

- Extend the existing save/runtime tests so they assert:
  - `populationSlotId`
  - `poolId`
  - `areaTag`
  - `isProtectedFromContracts`
  survive save/load and runtime hydration

**Step 2: Run the focused tests to verify they fail**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests tmp/civilian-save-edit.xml tmp/civilian-save-edit.log
bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests tmp/civilian-runtime-bridge-edit.xml tmp/civilian-runtime-bridge-edit.log
```

Expected: failing assertions because slot metadata is not persisted yet.

**Step 3: Implement minimal slot-owned occupant wiring**

- Extend records and runtime copy paths to preserve slot metadata.
- Replace anonymous count-based seeding with slot-driven occupant generation from the `MainTownPopulationDefinition`.

**Step 4: Re-run the focused tests**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests tmp/civilian-save-edit.xml tmp/civilian-save-edit.log
bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests tmp/civilian-runtime-bridge-edit.xml tmp/civilian-runtime-bridge-edit.log
bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationSlotAssignmentTests tmp/maintown-slot-assign-edit.xml tmp/maintown-slot-assign-edit.log
```

Expected: passing slot-persistence and slot-assignment tests.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Save/Modules
git add Reloader/Assets/_Project/NPCs/Scripts/Generation
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime
git add Reloader/Assets/_Project/Core/Tests/EditMode/CivilianPopulationSaveModuleTests.cs
git add Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs
git add Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationSlotAssignmentTests.cs
git commit -m "feat: assign civilians into maintown population slots"
```

### Task 5: Spawn live occupants into MainTown from persisted slots

**Files:**
- Create or modify under: `Reloader/Assets/_Project/World/Scripts/Runtime/`
- Create or modify under: `Reloader/Assets/_Project/NPCs/Prefabs/Civilians/`
- Create: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownPopulationSlotsPlayModeTests.cs`

**Step 1: Write the failing scene-spawn test**

- Add a PlayMode test that boots `MainTown`, injects a slot-driven population roster, and asserts:
  - live occupants spawn
  - dead occupants do not spawn
  - spawned occupants preserve `populationSlotId`

**Step 2: Run the focused PlayMode test to verify it fails**

Run:

```bash
bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationSlotsPlayModeTests tmp/maintown-pop-slots-play.xml tmp/maintown-pop-slots-play.log
```

Expected: failing scene-spawn assertions because no slot-driven loader exists yet.

**Step 3: Implement the MainTown loader/spawner**

- Hook `MainTown` runtime loading so it spawns live occupants from persisted slots.
- Keep vendor/protected authored NPCs outside the generated population path.

**Step 4: Re-run the focused PlayMode test**

Run:

```bash
bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationSlotsPlayModeTests tmp/maintown-pop-slots-play.xml tmp/maintown-pop-slots-play.log
```

Expected: passing scene-spawn assertions.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/World/Scripts/Runtime
git add Reloader/Assets/_Project/NPCs/Prefabs/Civilians
git add Reloader/Assets/_Project/World/Tests/PlayMode/MainTownPopulationSlotsPlayModeTests.cs
git commit -m "feat: spawn maintown occupants from population slots"
```

### Task 6: Run verification and update progress

**Files:**
- Modify: `docs/plans/progress/2026-03-07-maintown-population-slots-progress.md`

**Step 1: Run the focused verification sweep**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.MainTownPopulationDefinitionTests tmp/maintown-pop-def-edit.xml tmp/maintown-pop-def-edit.log
bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationSlotAssignmentTests tmp/maintown-slot-assign-edit.xml tmp/maintown-slot-assign-edit.log
bash scripts/run-unity-tests.sh editmode Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests tmp/civilian-save-edit.xml tmp/civilian-save-edit.log
bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests tmp/civilian-runtime-bridge-edit.xml tmp/civilian-runtime-bridge-edit.log
bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationSlotsPlayModeTests tmp/maintown-pop-slots-play.xml tmp/maintown-pop-slots-play.log
bash scripts/verify-docs-and-context.sh
git diff --check
```

Expected: all targeted tests pass, docs verification passes, and the worktree has no whitespace issues.

**Step 2: Update the progress doc**

- Record what shipped, what remained deferred, and the exact verification results.

**Step 3: Commit**

```bash
git add docs/plans/progress/2026-03-07-maintown-population-slots-progress.md
git commit -m "docs: update maintown population slots progress"
```

## Next Slice After This Plan

Once slot-driven `MainTown` population is stable, the next implementation slice should curate the first committed appearance-part pool from the STYLE kit and wire real visual assembly/prefab selection so generated civilians use approved bodies, hair, clothes, and color variants in-game.

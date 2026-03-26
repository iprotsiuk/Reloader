# MainTown Contract Target Runtime Cutover Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Move MainTown contract targets onto a dedicated runtime-spawned civilian body at authored contract anchors while keeping existing contract identity and save/runtime flow intact.

**Architecture:** `CivilianPopulationRuntimeBridge` will keep selecting the target from the roster, but `RebuildScenePopulation()` will skip spawning that roster civilian in the ambient path and instead spawn one dedicated runtime civilian for that same `CivilianId` at configured contract-target anchors. MainTown scene authoring will add explicit target anchors near the demo lane cluster.

**Tech Stack:** Unity, C#, EditMode tests, PlayMode tests, YAML-authored Unity scene state

---

### Task 1: Lock The Bridge Contract In Tests

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`

**Step 1: Write failing EditMode coverage**

Add a focused test showing:
- a tracked contract target id is not spawned through the ambient rebuild path
- one dedicated target body is spawned at a dedicated contract-target anchor
- `TryResolveSpawnedCivilian` still resolves that target id

**Step 2: Run the focused EditMode test and verify it fails**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests" "$(pwd)/tmp/contract-target-cutover-edit-red.xml" "$(pwd)/tmp/contract-target-cutover-edit-red.log"
```

Expected:
- FAIL on missing dedicated contract-target spawn behavior

**Step 3: Write failing MainTown PlayMode coverage**

Add a focused assertion that the live target body resolves near the dedicated lane/reference cluster through the bridge, and that the same target id is not duplicated elsewhere as an ambient civilian.

**Step 4: Run the focused PlayMode test and verify it fails**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests" "$(pwd)/tmp/contract-target-cutover-play-red.xml" "$(pwd)/tmp/contract-target-cutover-play-red.log"
```

Expected:
- FAIL on old ambient-target behavior

### Task 2: Add Dedicated Contract-Target Anchor Support

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`

**Step 1: Add serialized contract-target anchor ids**

Add a dedicated serialized string array for contract-target anchors and a small normalization helper.

**Step 2: Add tracked-target record/anchor resolution helpers**

Add minimal helpers to:
- resolve the tracked roster record by target id
- resolve the dedicated target anchor
- decide whether to skip a record from the ambient spawn path

**Step 3: Rework scene rebuild flow**

Update `RebuildScenePopulation()` so it:
- skips the tracked target id in the normal ambient loop
- spawns that target once through a dedicated target path
- preserves existing provider refresh flow

**Step 4: Reuse the canonical runtime civilian path**

Use the same `CreateCivilianActor`, `InitializeSpawnedCivilian`, grounding, and capability init path for the dedicated target body. Do not add a second humanoid prefab family.

**Step 5: Run the focused EditMode test and make it pass**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests" "$(pwd)/tmp/contract-target-cutover-edit-green.xml" "$(pwd)/tmp/contract-target-cutover-edit-green.log"
```

Expected:
- PASS

### Task 3: Wire MainTown To Dedicated Target Anchors

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`

**Step 1: Add dedicated target anchor transforms**

Author one to three contract-target anchors near the existing lane/reference cluster under `MainTownPopulationRuntime`.

**Step 2: Serialize the anchor ids onto the population bridge**

Wire the new anchor names into the bridge’s dedicated contract-target anchor list.

**Step 3: Keep authored lanes stable**

Do not change lane roots or reference metrics beyond what is required to make the target body spawn near them.

### Task 4: Update MainTown Contract Slice Assertions

**Files:**
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`

**Step 1: Replace old ambient-target assumptions**

Update tests that assumed the live target was just another ambient civilian spawn.

**Step 2: Assert no duplicate target body**

Add a check that only one spawned civilian body in-scene resolves to the tracked target id.

**Step 3: Run the focused PlayMode test and make it pass**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests" "$(pwd)/tmp/contract-target-cutover-play-green.xml" "$(pwd)/tmp/contract-target-cutover-play-green.log"
```

Expected:
- PASS

### Task 5: Widen Verification Carefully

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`

**Step 1: Run the nearest owned EditMode coverage**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests|Reloader.Core.Tests.EditMode.StaticContractRuntimeProviderTests" "$(pwd)/tmp/contract-target-cutover-edit-wide.xml" "$(pwd)/tmp/contract-target-cutover-edit-wide.log"
```

Expected:
- PASS

**Step 2: Run the nearest owned PlayMode coverage**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests|Reloader.Core.Tests.PlayMode.StaticContractRuntimeProviderPlayModeTests|Reloader.UI.Tests.PlayMode.TabInventoryContractsBridgePlayModeTests" "$(pwd)/tmp/contract-target-cutover-play-wide.xml" "$(pwd)/tmp/contract-target-cutover-play-wide.log"
```

Expected:
- PASS

**Step 3: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs \
        Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs \
        Reloader/Assets/_Project/World/Scenes/MainTown.unity \
        Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs \
        docs/plans/2026-03-26-main-town-contract-target-runtime-cutover-design.md \
        docs/plans/2026-03-26-main-town-contract-target-runtime-cutover-plan.md
git commit -m "refactor: move MainTown contract target onto dedicated runtime spawn"
```

# NPC Spawn, Ragdoll, And Corpse Loot Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make `spawn npc` usable, add `random` and `randomContract` spawn commands, unify killable humanoid NPC death presentation around ragdolls, and make dead NPCs lootable through unique storage containers.

**Architecture:** Extend the dev-console spawn command to understand token-aware suggestions and explicit random spawn modes, then move killable humanoid NPC authoring toward one shared combat/death/loot contract. Reuse `HumanoidDamageReceiver` + `HumanoidRagdollController` for death presentation and `WorldStorageContainer` + `StorageContainerRuntime` for corpse looting so contract targets, catalog-spawned humanoids, and future authored humanoids share the same path.

**Tech Stack:** Unity 6.3 C#, NUnit EditMode and PlayMode tests, existing DevTools runtime, NPC combat runtime, inventory storage runtime, Unity MCP verification, git/GitHub PR workflow.

## Status Update [2026-03-17]

- `spawn npc` suggestions now cover trailing-space token entry and surface `random` plus `randomContract`.
- `spawn npc random` stays catalog-backed.
- `spawn npc randomContract` ships through `DevCommandContext` + `CivilianPopulationRuntimeBridge`, not through a separate `DevRandomContractNpcFactory`.
- Killable contract civilians now keep the shared death path: `HumanoidDamageReceiver` -> `HumanoidRagdollController` -> `ContractTargetDamageable` plus `HumanoidCorpseLootController`.
- Corpse storage is assigned and registered when death presentation begins, not while the NPC is alive.
- Thin prefabs without authored ragdoll bodies currently rely on a runtime root-rigidbody fallback so lethal hits still produce physical reaction.
- Broader authored prefab hardening remains follow-up work rather than a completed part of this slice.

Validated evidence in this branch:

- `bash scripts/run-unity-tests.sh editmode "Reloader.DevTools.Tests.EditMode.DevNpcSpawnCatalogTests|Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests" "$(pwd)/tmp/npc-dev-edit.xml" "$(pwd)/tmp/npc-dev-edit.log"`
- `bash scripts/run-unity-tests.sh playmode "Reloader.DevTools.Tests.PlayMode.DevSpawnNpcCommandPlayModeTests" "$(pwd)/tmp/dev-spawn-play.xml" "$(pwd)/tmp/dev-spawn-play.log"`
- `bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.HumanoidRagdollControllerPlayModeTests|Reloader.NPCs.Tests.PlayMode.NpcCorpseLootPlayModeTests" "$(pwd)/tmp/npc-ragdoll-corpse.xml" "$(pwd)/tmp/npc-ragdoll-corpse.log"`
- `bash scripts/run-unity-tests.sh playmode "Reloader.Inventory.Tests.PlayMode.WorldStorageContainerSeedLoadoutPlayModeTests" "$(pwd)/tmp/storage-seed-play.xml" "$(pwd)/tmp/storage-seed-play.log"`

---

### Task 1: Add Red Coverage For Spawn NPC Suggestions And Random Modes

**Files:**
- Modify: `Reloader/Assets/_Project/DevTools/Tests/EditMode/DevNpcSpawnCatalogTests.cs`
- Modify: `Reloader/Assets/_Project/DevTools/Tests/PlayMode/DevSpawnNpcCommandPlayModeTests.cs`

**Step 1: Write the failing tests**

Add coverage that proves:

- `spawn npc` and `spawn npc ` both suggest viable spawn ids
- `spawn npc ` suggestions include `random` and `randomContract`
- `spawn npc random` selects a valid catalog entry

Example assertions:

```csharp
Assert.That(suggestions.Select(x => x.Token), Does.Contain("random"));
Assert.That(suggestions.Select(x => x.Token), Does.Contain("randomContract"));
Assert.That(suggestions.Select(x => x.Token), Does.Contain("npc.police"));
```

**Step 2: Run test to verify it fails**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.DevTools.Tests.EditMode.DevNpcSpawnCatalogTests" "$(pwd)/tmp/dev-npc-suggestions-red.xml" "$(pwd)/tmp/dev-npc-suggestions-red.log"
bash scripts/run-unity-tests.sh playmode "Reloader.DevTools.Tests.PlayMode.DevSpawnNpcCommandPlayModeTests" "$(pwd)/tmp/dev-npc-spawn-red.xml" "$(pwd)/tmp/dev-npc-spawn-red.log"
```

Expected:
- existing suggestion logic fails the trailing-space or random-token assertions

**Step 3: Commit**

```bash
git add Reloader/Assets/_Project/DevTools/Tests/EditMode/DevNpcSpawnCatalogTests.cs \
        Reloader/Assets/_Project/DevTools/Tests/PlayMode/DevSpawnNpcCommandPlayModeTests.cs
git commit -m "test: add red coverage for spawn npc suggestions"
```

### Task 2: Implement Token-Aware Spawn Suggestions And `random`

**Files:**
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Data/DevNpcSpawnCatalog.cs`
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandExecutors/DevSpawnNpcCommand.cs`
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevNpcSpawnService.cs`

**Step 1: Write minimal implementation**

- add token-aware spawn suggestion handling that treats trailing whitespace as “editing next token”
- include synthetic suggestions for `random` and `randomContract`
- add catalog-random selection in `DevNpcSpawnService`
- keep crosshair/fallback placement authoritative in one method

**Step 2: Run focused tests to verify green**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.DevTools.Tests.EditMode.DevNpcSpawnCatalogTests" "$(pwd)/tmp/dev-npc-suggestions-green.xml" "$(pwd)/tmp/dev-npc-suggestions-green.log"
bash scripts/run-unity-tests.sh playmode "Reloader.DevTools.Tests.PlayMode.DevSpawnNpcCommandPlayModeTests" "$(pwd)/tmp/dev-npc-spawn-green.xml" "$(pwd)/tmp/dev-npc-spawn-green.log"
```

**Step 3: Commit**

```bash
git add Reloader/Assets/_Project/DevTools/Scripts/Data/DevNpcSpawnCatalog.cs \
        Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandExecutors/DevSpawnNpcCommand.cs \
        Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevNpcSpawnService.cs
git commit -m "feat: add token-aware spawn npc suggestions"
```

### Task 3: Add Red Coverage For Killable Contract Spawn And Corpse Storage

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`
- Create: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/NpcCorpseLootPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/HumanoidRagdollControllerPlayModeTests.cs`

**Step 1: Write the failing tests**

Add coverage that proves:

- spawned contract-eligible civilians receive `HumanoidRagdollController`
- lethal elimination keeps the body active instead of despawning
- each NPC instance gets a unique `WorldStorageContainer.ContainerId`
- empty corpse containers register unique runtimes

Example assertions:

```csharp
Assert.That(civilian.GetComponent<HumanoidRagdollController>(), Is.Not.Null);
Assert.That(civilian.activeSelf, Is.True);
Assert.That(containerA.ContainerId, Is.Not.EqualTo(containerB.ContainerId));
```

**Step 2: Run test to verify it fails**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests" "$(pwd)/tmp/civilian-ragdoll-red.xml" "$(pwd)/tmp/civilian-ragdoll-red.log"
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.HumanoidRagdollControllerPlayModeTests|Reloader.NPCs.Tests.PlayMode.NpcCorpseLootPlayModeTests" "$(pwd)/tmp/npc-corpse-red.xml" "$(pwd)/tmp/npc-corpse-red.log"
```

Expected:
- contract-spawned civilians fail the new ragdoll/storage contract assertions

**Step 3: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs \
        Reloader/Assets/_Project/NPCs/Tests/PlayMode/HumanoidRagdollControllerPlayModeTests.cs \
        Reloader/Assets/_Project/NPCs/Tests/PlayMode/NpcCorpseLootPlayModeTests.cs
git commit -m "test: add red coverage for corpse lootable humanoids"
```

### Task 4: Implement Shared Corpse Loot Wiring For Killable Humanoids

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidCorpseLootController.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/World/ContractTargetDamageable.cs`
- Modify: `Reloader/Assets/_Project/Inventory/Scripts/World/Storage/WorldStorageContainer.cs`

**Step 1: Write minimal implementation**

- add a corpse-loot controller that:
  - ensures a `WorldStorageContainer`
  - assigns a unique per-instance container id
  - registers an empty runtime container
  - enables corpse looting only after death if needed by interaction rules
- update civilian spawn wiring so contract-eligible and killable humanoids get:
  - `HumanoidHitboxRig`
  - `HumanoidDamageReceiver`
  - `HumanoidRagdollController`
  - `HumanoidCorpseLootController`
- keep `ContractTargetDamageable` preserving the body whenever the shared death presentation contract is satisfied
- extend `WorldStorageContainer` only if needed to support runtime-assigned ids/display names cleanly

**Step 2: Run focused tests to verify green**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests" "$(pwd)/tmp/civilian-ragdoll-green.xml" "$(pwd)/tmp/civilian-ragdoll-green.log"
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.HumanoidRagdollControllerPlayModeTests|Reloader.NPCs.Tests.PlayMode.NpcCorpseLootPlayModeTests" "$(pwd)/tmp/npc-corpse-green.xml" "$(pwd)/tmp/npc-corpse-green.log"
```

**Step 3: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidCorpseLootController.cs \
        Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs \
        Reloader/Assets/_Project/Weapons/Scripts/World/ContractTargetDamageable.cs \
        Reloader/Assets/_Project/Inventory/Scripts/World/Storage/WorldStorageContainer.cs
git commit -m "feat: make killable humanoid corpses lootable"
```

### Task 5: Add Red Coverage For `randomContract` Runtime Execution

**Files:**
- Modify: `Reloader/Assets/_Project/DevTools/Tests/PlayMode/DevSpawnNpcCommandPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`

**Step 1: Write the failing tests**

Add coverage that proves:

- `spawn npc randomContract` spawns a contract-eligible humanoid at the crosshair pose
- the spawned NPC exposes contract-target damage + ragdoll + corpse loot components

**Step 2: Run test to verify it fails**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.DevTools.Tests.PlayMode.DevSpawnNpcCommandPlayModeTests" "$(pwd)/tmp/random-contract-red.xml" "$(pwd)/tmp/random-contract-red.log"
```

Expected:
- `randomContract` is unknown or spawns an object without the shared contract stack

**Step 3: Commit**

```bash
git add Reloader/Assets/_Project/DevTools/Tests/PlayMode/DevSpawnNpcCommandPlayModeTests.cs \
        Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs
git commit -m "test: add red coverage for random contract spawn"
```

### Task 6: Implement `randomContract` Through The Shared Runtime Path

**Files:**
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandContext.cs`
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandExecutors/DevSpawnNpcCommand.cs`
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevNpcSpawnService.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`

**Step 1: Write minimal implementation**

- expose `CivilianPopulationRuntimeBridge` from the command context
- keep `randomContract` explicit in the command executor and avoid fallback-driven ambiguity
- reuse the shared spawn-pose resolver from `DevNpcSpawnService`
- have the bridge spawn one contract-eligible civilian/test target using the same shared combat/death/loot runtime contract as live civilians

**Step 2: Run focused tests to verify green**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.DevTools.Tests.PlayMode.DevSpawnNpcCommandPlayModeTests" "$(pwd)/tmp/random-contract-green.xml" "$(pwd)/tmp/random-contract-green.log"
```

**Step 3: Commit**

```bash
git add Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandContext.cs \
        Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandExecutors/DevSpawnNpcCommand.cs \
        Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevNpcSpawnService.cs \
        Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs
git commit -m "feat: add random contract npc spawning"
```

### Task 7: Harden Shared Runtime Wiring For Thin Humanoids

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidRagdollController.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidCorpseLootController.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/World/ContractTargetDamageable.cs`

**Step 1: Harden the runtime contract**

- add a root-rigidbody fallback for thin killable humanoids that do not yet carry authored ragdoll bodies
- ensure corpse-loot runtime resets cleanly with the shared death bridge
- keep contract-target elimination preserving the GameObject whenever ragdoll/corpse presentation is available

**Step 2: Verify with read-back**

- confirm required components are present on spawned civilians
- confirm lethal hits still apply impulse when only the runtime fallback body exists
- confirm corpse storage is only created for the dead-body state

**Step 3: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidRagdollController.cs \
        Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidCorpseLootController.cs \
        Reloader/Assets/_Project/Weapons/Scripts/World/ContractTargetDamageable.cs \
        Reloader/Assets/_Project/NPCs/Tests/PlayMode/HumanoidRagdollControllerPlayModeTests.cs \
        Reloader/Assets/_Project/NPCs/Tests/PlayMode/NpcCorpseLootPlayModeTests.cs
git commit -m "feat: harden shared humanoid death presentation"
```

### Task 8: Validate, Update Docs, And Open PR

**Files:**
- Modify: `docs/plans/2026-03-12-developer-testing-tools-design.md`
- Modify: `docs/plans/2026-03-13-ragdoll-hitboxes-blood-design.md`
- Modify: `docs/plans/2026-03-13-ragdoll-hitboxes-blood-implementation-plan.md`
- Modify: `docs/plans/2026-03-17-npc-spawn-ragdoll-loot-design.md`
- Modify: `docs/plans/2026-03-17-npc-spawn-ragdoll-loot-implementation-plan.md`

**Step 1: Run verification**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.DevTools.Tests.EditMode.DevNpcSpawnCatalogTests|Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests" "$(pwd)/tmp/npc-feature-editmode.xml" "$(pwd)/tmp/npc-feature-editmode.log"
bash scripts/run-unity-tests.sh playmode "Reloader.DevTools.Tests.PlayMode.DevSpawnNpcCommandPlayModeTests|Reloader.NPCs.Tests.PlayMode.HumanoidRagdollControllerPlayModeTests|Reloader.NPCs.Tests.PlayMode.NpcCorpseLootPlayModeTests" "$(pwd)/tmp/npc-feature-playmode.xml" "$(pwd)/tmp/npc-feature-playmode.log"
```

Also perform Unity Play Mode manual validation for:

- `spawn npc `
- `spawn npc random`
- `spawn npc randomContract`
- kill -> ragdoll
- corpse loot open

**Step 2: Update docs**

- update the existing dev-tools and ragdoll docs to reflect the explicit command set and corpse-loot behavior
- note that killable humanoids now rely on one shared death/loot contract

**Step 3: Prepare git + PR**

- create a `codex/` branch from the correct base without pulling unrelated dirty work into the feature commit set
- commit the feature changes
- push the branch
- open a non-draft PR to `main`
- post a PR comment tagging `@codex`

**Step 4: Final commit**

```bash
git add docs/plans/2026-03-12-developer-testing-tools-design.md \
        docs/plans/2026-03-13-ragdoll-hitboxes-blood-design.md \
        docs/plans/2026-03-13-ragdoll-hitboxes-blood-implementation-plan.md \
        docs/plans/2026-03-17-npc-spawn-ragdoll-loot-design.md \
        docs/plans/2026-03-17-npc-spawn-ragdoll-loot-implementation-plan.md
git commit -m "docs: update npc spawn ragdoll loot plans"
```

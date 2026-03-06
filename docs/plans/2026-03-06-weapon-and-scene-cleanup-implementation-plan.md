# Weapon And Scene Cleanup Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Clean the branch so only `Kar98k + Canik TP9` remain as authored supported weapons, remove hidden fallback behavior, make town/range weapon wiring deterministic, fix dropped-item visuals, and resolve the active PR review comment.

**Architecture:** Treat this as a strict cleanup/refactor pass, not a subsystem rewrite. Tighten authoritative weapon/content ownership, remove live runtime/editor rescue paths, enforce consistent scene wiring, and make missing visuals/configuration fail loudly instead of degrading into unrelated behavior or grey cubes. Keep the deeper contracts/law/NPC refactor deferred.

**Tech Stack:** Unity 6.3, C#, NUnit EditMode/PlayMode tests, GitHub PR review threads, repo docs under `docs/plans/` and `docs/design/`.

---

### Task 1: Lock docs, progress, and active PR feedback into the cleanup scope

**Files:**
- Modify: `docs/plans/2026-03-06-weapon-and-scene-cleanup-design.md`
- Modify: `docs/plans/2026-03-06-weapon-and-scene-cleanup-implementation-plan.md`
- Modify: `docs/plans/progress/2026-03-06-weapon-and-scene-cleanup-progress.md`
- Read/Reply: PR `#24` review thread on `Reloader/Assets/_Project/LawEnforcement/Scripts/Runtime/PoliceHeatController.cs`

**Step 1: Write the progress tracker entry for the PR review comment**

- Add an explicit progress item for `ReportLineOfSightLost` idempotence.

**Step 2: Verify live GitHub review state**

Run:

```bash
gh api graphql -f query='query { repository(owner:"iprotsiuk", name:"Reloader") { pullRequest(number:24) { reviewThreads(first:100) { nodes { isResolved isOutdated path line comments(first:10) { nodes { author { login } body url createdAt } } } } } } }'
```

Expected:
- One active unresolved thread on `PoliceHeatController.cs`

**Step 3: Keep the progress doc in sync before code starts**

- Mark design/plan complete
- Mark implementation tasks as `Planned`

**Step 4: Commit docs-only setup**

```bash
git add docs/plans/2026-03-06-weapon-and-scene-cleanup-design.md docs/plans/2026-03-06-weapon-and-scene-cleanup-implementation-plan.md docs/plans/progress/2026-03-06-weapon-and-scene-cleanup-progress.md
git commit -m "docs: add weapon and scene cleanup plan"
```

### Task 2: Fix the active PR review comment on police heat LOS-loss idempotence

**Files:**
- Modify: `Reloader/Assets/_Project/LawEnforcement/Tests/EditMode/PoliceHeatControllerTests.cs`
- Modify: `Reloader/Assets/_Project/LawEnforcement/Scripts/Runtime/PoliceHeatController.cs`

**Step 1: Write the failing test**

- Add an EditMode test proving repeated `ReportLineOfSightLost()` while already in `Search` does **not** refresh the countdown.

**Step 2: Run test to verify it fails**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.LawEnforcement.Tests.EditMode.PoliceHeatControllerTests" tmp/weapon-cleanup-police-red.xml tmp/weapon-cleanup-police-red.log
```

Expected:
- FAIL on the new repeated-LOS-loss test

**Step 3: Write minimal implementation**

- Make `ReportLineOfSightLost()` idempotent when already in `Search`
- Preserve remaining timer in that state

**Step 4: Run test to verify it passes**

Run the same command and expect green.

**Step 5: Resolve the GitHub review thread**

- Reply in-thread with the concrete fix
- Resolve the thread after verification

### Task 3: Remove live weapon-definition fallback behavior

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/WeaponRegistryFallbackResolutionTests.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryAttachmentsPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponRegistry.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryController.cs`

**Step 1: Write the failing tests**

- Flip `WeaponRegistryFallbackResolutionTests` so empty serialized definitions do **not** resolve `StarterRifle` through `AssetDatabase`
- Flip the tab-inventory attachments test so cross-registry resolution fails instead of succeeding

**Step 2: Run tests to verify they fail**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.WeaponRegistryFallbackResolutionTests" tmp/weapon-cleanup-registry-red.xml tmp/weapon-cleanup-registry-red.log
bash scripts/run-unity-tests.sh playmode "Reloader.UI.Tests.PlayMode.TabInventoryAttachmentsPlayModeTests" tmp/weapon-cleanup-tab-red.xml tmp/weapon-cleanup-tab-red.log
```

Expected:
- FAIL on the newly inverted strictness assertions

**Step 3: Write minimal implementation**

- Remove editor asset-scan rescue from `WeaponRegistry.TryGetWeaponDefinition`
- Remove `TabInventoryController.TryResolveWeaponDefinition` scanning of other registries
- Keep explicit assigned registry behavior only

**Step 4: Run tests to verify they pass**

Run the same commands and expect green.

### Task 4: Rename pistol content to Canik TP9 and prune unsupported authored weapon content

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Data/Weapons/StarterPistol.asset`
- Modify: `Reloader/Assets/_Project/Inventory/Data/Items/Pistol_9x19_Starter.asset`
- Modify: `Reloader/Assets/_Project/Inventory/Data/Items/Ammo_Factory_9x19_124_FMJ.asset`
- Modify: `Reloader/Assets/_Project/Inventory/Data/Spawns/Pistol_9x19_Starter_Spawn.asset`
- Modify: `Reloader/Assets/_Project/Inventory/Data/Spawns/Ammo_Factory_9x19_124_FMJ_Spawn.asset`
- Modify: `Reloader/Assets/_Project/Weapons/Data/AnimationProfiles/PlayerWeaponAnimatorOverrideProfile.asset`
- Modify: `Reloader/Assets/_Project/Weapons/Editor/WeaponsContentBuilder.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Editor/WeaponsSceneWiring.cs`
- Modify: `Reloader/Assets/_Project/World/Editor/MainTownCombatWiring.cs`
- Delete: stale authored unsupported weapon items/spawns under `Reloader/Assets/_Project/Inventory/Data/Items/` and `Reloader/Assets/_Project/Inventory/Data/Spawns/`

**Step 1: Write the failing tests**

- Add or update tests asserting the supported authored registry/builder output is exactly `weapon-kar98k` + `weapon-canik-tp9`
- Add tests or assertions covering the renamed pistol display/content identity where practical

**Step 2: Run tests to verify they fail**

Run targeted edit/play suites for affected builders/runtime consumers.

**Step 3: Write minimal implementation**

- Rename generic pistol authored ids/names to Canik TP9 / 9mm
- Keep current `9mm` ammo path rather than inventing a new caliber system slice
- Remove `.556` and other unsupported authored starter content from `_Project`
- Update scene/editor wiring constants to the renamed ids/assets

**Step 4: Run tests to verify they pass**

- Re-run targeted builder/runtime suites

### Task 5: Make `MainTown` and `IndoorRange` weapon wiring deterministic and parity-safe

**Files:**
- Modify: `Reloader/Assets/_Project/World/Editor/MainTownCombatWiring.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Editor/WeaponsSceneWiring.cs`
- Modify: `Reloader/Assets/_Project/World/Editor/WorldSceneTemplateScaffolds.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/WorldSceneContractValidatorEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/RoundTripTravelPlayModeTests.cs`

**Step 1: Write the failing tests**

- Add/edit tests that assert town/range share the same supported weapon ids/view bindings/attachment metadata expectations
- Add coverage for any discovered scene-specific drift that reproduces the current symptom

**Step 2: Run tests to verify they fail**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.WorldSceneContractValidatorEditModeTests|Reloader.World.Tests.EditMode.MainTownCombatWiringEditModeTests" tmp/weapon-cleanup-world-red.xml tmp/weapon-cleanup-world-red.log
bash scripts/run-unity-tests.sh playmode "Reloader.World.Tests.PlayMode.RoundTripTravelPlayModeTests|Reloader.World.Tests.PlayMode.SceneTopologySmokeTests" tmp/weapon-cleanup-travel-red.xml tmp/weapon-cleanup-travel-red.log
```

Expected:
- FAIL on the new parity assertions if current scene drift exists

**Step 3: Write minimal implementation**

- Align shared constants and weapon ids
- Remove permissive rifle-only or stale scene overrides where they create drift
- Keep scene-specific authored world objects, but not scene-specific weapon definitions/behavior

**Step 4: Run tests to verify they pass**

Run the same commands and expect green.

### Task 6: Remove grey-cube fallback from dropped-item and scene-pickup visuals

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerInventoryControllerPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/Inventory/Scripts/World/RuntimeDroppedItemFactory.cs`
- Modify: `Reloader/Assets/_Project/World/Editor/MainTownCombatWiring.cs`

**Step 1: Write the failing tests**

- Replace tests that currently assert fallback-cube parity with strict authored-visual behavior
- Add a test that dropping an authored item with a visual source produces a non-cube authored visual
- Add a test that missing visual sources fail loudly instead of silently creating a grey cube in live runtime

**Step 2: Run tests to verify they fail**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.Player.Tests.PlayMode.PlayerInventoryControllerPlayModeTests" tmp/weapon-cleanup-drop-red.xml tmp/weapon-cleanup-drop-red.log
```

Expected:
- FAIL on the new strict drop-visual assertions

**Step 3: Write minimal implementation**

- Remove live cube fallback from `RuntimeDroppedItemFactory`
- Remove scene-authoring cube fallback from `MainTownCombatWiring.SyncPickupVisual`
- Replace with explicit failure/logging behavior and deterministic authored-visual resolution

**Step 4: Run tests to verify they pass**

Run the same command and expect green.

### Task 7: Sync docs/progress/PR after implementation

**Files:**
- Modify: `docs/plans/progress/2026-03-06-weapon-and-scene-cleanup-progress.md`
- Modify: affected design docs under `docs/design/` as needed by changed contracts
- Update PR `#24` comment thread(s)

**Step 1: Update progress tracker**

- Mark completed tasks, evidence, commits, and any deferred follow-ups

**Step 2: Update affected design docs**

- Weapon/content naming
- Strict fallback policy
- Any scene parity contract text that changed materially

**Step 3: Run required doc validation**

Run:

```bash
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

Expected:
- all pass

**Step 4: Post PR status update**

- Summarize what landed
- Link the progress doc
- Note verification evidence

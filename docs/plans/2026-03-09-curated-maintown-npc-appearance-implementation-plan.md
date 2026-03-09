# Curated MainTown NPC Appearance Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make the approved STYLE module pool the default `MainTown` civilian appearance source, replace the old generic NPC visual source, and spawn runtime civilians/vendors with valid reviewed module combinations.

**Architecture:** Keep `CivilianPopulationRecord` as the save contract, but replace flat independent random picks with a curated modular generator. Add a runtime appearance applicator for spawned NPC actors, and switch authored MainTown vendor/service visuals away from the legacy third-party vendor model to STYLE-backed module assembly.

**Tech Stack:** Unity 6.3, C#, NUnit EditMode tests, Unity scene/prefab assets, existing `Reloader.NPCs` runtime + editor tooling.

---

### Task 1: Lock the curated module-pool contract into docs

**Files:**
- Create: `docs/plans/2026-03-09-curated-maintown-npc-appearance-design.md`
- Modify: `docs/plans/2026-03-07-persistent-civilian-population-design.md`
- Modify: `docs/plans/2026-03-07-maintown-population-slots-design.md`
- Modify: `docs/design/npcs-and-quests.md`
- Modify: `docs/design/save-and-progression.md`

**Step 1: Write the smallest doc updates that name the approved STYLE module pool as the `MainTown` source of truth**

Add:
- approved module pool vs demo-character distinction
- generated civilian contract
- runtime visual-application requirement
- temporary policy for authored roles

**Step 2: Run doc verification**

Run: `bash scripts/verify-docs-and-context.sh`
Expected: exit `0`

**Step 3: Run extended doc verification**

Run: `bash scripts/verify-extensible-development-contracts.sh && bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`
Expected: exit `0`

**Step 4: Commit**

```bash
git add docs/plans/2026-03-09-curated-maintown-npc-appearance-design.md docs/plans/2026-03-07-persistent-civilian-population-design.md docs/plans/2026-03-07-maintown-population-slots-design.md docs/design/npcs-and-quests.md docs/design/save-and-progression.md
git commit -m "docs: define curated MainTown NPC appearance contract"
```

### Task 2: Add failing tests for curated generation behavior

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianAppearanceGeneratorTests.cs`
- Create: `Reloader/Assets/_Project/NPCs/Tests/EditMode/MainTownNpcAppearanceApplicatorEditModeTests.cs`

**Step 1: Write failing generator tests**

Cover:
- generator resolves only approved module ids
- generator respects banned combinations
- generation is deterministic for a seed
- authored-role fixed combinations can be supplied without using demo-scene character names

**Step 2: Write failing runtime-appearance tests**

Cover:
- generated appearance ids can be applied onto a STYLE-backed actor root
- invalid modules normalize or reject according to the approved pool rules

**Step 3: Run focused EditMode tests to verify they fail for the right reason**

Run: `Unity EditMode tests for CivilianAppearanceGeneratorTests and MainTownNpcAppearanceApplicatorEditModeTests`
Expected: failures showing missing curated-pool/applicator behavior

**Step 4: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianAppearanceGeneratorTests.cs Reloader/Assets/_Project/NPCs/Tests/EditMode/MainTownNpcAppearanceApplicatorEditModeTests.cs
git commit -m "test: cover curated MainTown NPC appearance generation"
```

### Task 3: Replace flat array generation with curated module-pool generation

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Generation/CivilianAppearanceLibrary.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Generation/MainTownCuratedAppearanceLibrary.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Generation/MainTownCuratedAppearanceEntry.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Generation/CivilianAppearanceGenerator.cs`

**Step 1: Implement the minimal curated data model**

Add:
- approved module entry contract
- compatibility-safe fields for body/hair/beard/top/bottom/outerwear/material tags
- optional authored fixed combinations for named roles later

**Step 2: Implement minimal generator logic**

Change generation to:
- pick one compatible curated module combination
- emit existing `CivilianPopulationRecord` ids
- preserve deterministic seeded selection

**Step 3: Run focused generator tests**

Run: `Unity EditMode tests for CivilianAppearanceGeneratorTests`
Expected: pass

**Step 4: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Generation/CivilianAppearanceLibrary.cs Reloader/Assets/_Project/NPCs/Scripts/Generation/MainTownCuratedAppearanceLibrary.cs Reloader/Assets/_Project/NPCs/Scripts/Generation/MainTownCuratedAppearanceEntry.cs Reloader/Assets/_Project/NPCs/Scripts/Generation/CivilianAppearanceGenerator.cs
git commit -m "feat: generate MainTown civilians from curated module pools"
```

### Task 4: Apply generated appearance ids to runtime NPC visuals

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/MainTownNpcAppearanceApplicator.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/MainTownPopulationSpawnedCivilian.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`
- Reuse reference logic from: `Reloader/Assets/_Project/World/Editor/StyleCrowdReviewBuilder.cs`

**Step 1: Implement the failing runtime behavior minimally**

Add a runtime seam that:
- reads generated appearance ids from the civilian record
- enables matching STYLE modules on the actor visual root
- applies approved material families
- enforces reviewed normalization/layering rules

**Step 2: Wire the bridge to use the applicator on spawned civilians**

Keep changes narrow:
- no save-schema change
- no new contract-target behavior

**Step 3: Run focused applicator/runtime tests**

Run: `Unity EditMode tests for MainTownNpcAppearanceApplicatorEditModeTests and CivilianPopulationRuntimeBridgeTests`
Expected: pass for new appearance-application cases

**Step 4: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime/MainTownNpcAppearanceApplicator.cs Reloader/Assets/_Project/NPCs/Scripts/Runtime/MainTownPopulationSpawnedCivilian.cs Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs
git commit -m "feat: apply curated STYLE appearance to MainTown NPCs"
```

### Task 5: Replace legacy vendor/service visuals with STYLE-backed actors

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Editor/NpcVendorPrefabBuilder.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/NpcFoundation.prefab`
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/Roles/*.prefab`
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`

**Step 1: Write/update failing tests for vendor/service actor visuals if coverage exists**

Extend existing prefab/runtime tests where possible to assert:
- role prefabs no longer use the legacy `Lowpoly Animated Men Pack` model path
- vendor/service actors can host STYLE-backed appearance application

**Step 2: Replace the legacy foundation visual source**

Switch the NPC foundation/role prefab build path to STYLE-backed actor roots compatible with the new applicator.

**Step 3: Put temporary fixed random-looking combinations on authored roles**

For this pass:
- vendors and service roles use fixed combinations assembled from the same approved module pool
- no role should point at a named demo-scene review character

**Step 4: Rebuild affected prefabs/scene wiring and verify in MainTown**

Run: `Reloader/NPCs/Foundation/Rebuild NPC Foundation + Role Variants`
Then inspect `MainTown` vendor/service roots.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Editor/NpcVendorPrefabBuilder.cs Reloader/Assets/_Project/NPCs/Prefabs/NpcFoundation.prefab Reloader/Assets/_Project/NPCs/Prefabs/Roles Reloader/Assets/_Project/World/Scenes/MainTown.unity
git commit -m "feat: switch MainTown authored NPC visuals to curated STYLE parts"
```

### Task 6: Verify end-to-end behavior

**Files:**
- Verify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianAppearanceGeneratorTests.cs`
- Verify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`
- Verify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/MainTownPopulationInfrastructurePlayModeTests.cs`
- Verify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`

**Step 1: Run focused automated verification**

Run:
- `Unity EditMode tests for CivilianAppearanceGeneratorTests`
- `Unity EditMode tests for CivilianPopulationRuntimeBridgeTests`
- `Unity EditMode tests for MainTownNpcAppearanceApplicatorEditModeTests`

Expected: pass

**Step 2: Run scene/prefab verification**

Verify:
- `MainTown` civilians spawn with approved modules only
- vendors/service roles no longer use the old generic model
- scene console is clean after refresh/rebuild

**Step 3: Record residual risks**

Document any remaining limits, especially:
- placeholder police/service archetype coherence
- any module-family exclusions still kept out of runtime

**Step 4: Commit**

```bash
git add -A
git commit -m "test: verify curated MainTown NPC appearance integration"
```

Plan complete and saved to `docs/plans/2026-03-09-curated-maintown-npc-appearance-implementation-plan.md`.

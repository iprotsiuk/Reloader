# Procedural Civilian Public Identity Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add persisted public identity for generated `MainTown` civilians so contracts and target display use believable names while save/load, travel, sleep, death, and Monday replacements remain deterministic.

**Architecture:** Keep `civilianId` as the hidden stable internal key and extend `CivilianPopulationRecord` with persisted public identity fields. Generate those fields alongside appearance, surface them through `CivilianPopulationRuntimeBridge`, and keep slot continuity (`populationSlotId` / `poolId` / `spawnAnchorId` / `areaTag`) separate from occupant continuity.

**Tech Stack:** Unity 6.3, C#, NUnit EditMode/PlayMode tests, Newtonsoft.Json save payloads, ScriptableObject runtime contract assets

---

### Task 1: Extend the civilian save contract for public identity

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/CivilianPopulationRecord.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/CivilianPopulationModule.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/CivilianPopulationSaveModuleTests.cs`

**Step 1: Write the failing tests**

Add or update tests in `CivilianPopulationSaveModuleTests.cs` to assert:

- `CivilianPopulationRecord` payload round-trips `firstName`, `lastName`, and optional `nickname`
- `ValidateModuleState()` rejects missing required name fields for live civilians
- cloning preserves the new public identity fields

Example assertions to add:

```csharp
Assert.That(restoredRecord.FirstName, Is.EqualTo("Derek"));
Assert.That(restoredRecord.LastName, Is.EqualTo("Mullen"));
Assert.That(restoredRecord.Nickname, Is.EqualTo("Socks"));
```

**Step 2: Run the targeted failing tests**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests tmp/civilian-pop-save-names-red.xml tmp/civilian-pop-save-names-red.log
```

Expected:

- FAIL because the record/module contract does not yet include public identity fields

**Step 3: Write the minimal implementation**

Implement:

- `FirstName`, `LastName`, and `Nickname` on `CivilianPopulationRecord`
- `CloneRecord(...)` updates in `CivilianPopulationModule.cs`
- validation in `ValidateModuleState()` for required first/last name fields

Keep `civilianId` untouched as the stable internal key.

**Step 4: Run the targeted tests until they pass**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests tmp/civilian-pop-save-names-green.xml tmp/civilian-pop-save-names-green.log
```

Expected:

- PASS for the updated save-module coverage

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Save/Modules/CivilianPopulationRecord.cs Reloader/Assets/_Project/Core/Scripts/Save/Modules/CivilianPopulationModule.cs Reloader/Assets/_Project/Core/Tests/EditMode/CivilianPopulationSaveModuleTests.cs
git commit -m "feat: persist civilian public identity"
```

### Task 2: Generate stable civilian public names for new occupants and replacements

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Generation/CivilianAppearanceGenerator.cs`
- Create or Modify: `Reloader/Assets/_Project/NPCs/Scripts/Generation/*Name*.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianAppearanceGeneratorTests.cs`

**Step 1: Write the failing tests**

Add or update generator tests to assert:

- fresh generated records include non-empty `firstName` and `lastName`
- nickname is optional, not required
- generation is deterministic for a given seed
- a replacement generation path produces a different occupant identity when given a different seed/input

Example assertions:

```csharp
Assert.That(record.FirstName, Is.Not.Empty);
Assert.That(record.LastName, Is.Not.Empty);
Assert.That(recordA.FirstName, Is.EqualTo(recordB.FirstName));
Assert.That(recordA.LastName, Is.EqualTo(recordB.LastName));
```

**Step 2: Run the targeted failing tests**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianAppearanceGeneratorTests tmp/civilian-name-generator-red.xml tmp/civilian-name-generator-red.log
```

Expected:

- FAIL because generator output does not yet produce public identity fields

**Step 3: Write the minimal implementation**

Implement a small procedural civilian-name generator that:

- uses believable first/last name pools
- optionally injects a nickname at low probability
- remains deterministic for the generation seed
- writes the generated values into `CivilianPopulationRecord`

Keep YAGNI:

- do not add culture packs, localization systems, or title/honorific logic yet
- keep the source lists concise and maintainable

**Step 4: Run the targeted tests until they pass**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianAppearanceGeneratorTests tmp/civilian-name-generator-green.xml tmp/civilian-name-generator-green.log
```

Expected:

- PASS for generator identity coverage

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Generation/CivilianAppearanceGenerator.cs Reloader/Assets/_Project/NPCs/Scripts/Generation Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianAppearanceGeneratorTests.cs
git commit -m "feat: generate public names for procedural civilians"
```

### Task 3: Surface public identity through runtime cloning and spawned target display

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/MainTownPopulationSpawnedCivilian.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`

**Step 1: Write the failing tests**

Add runtime-bridge tests to assert:

- runtime clone paths preserve first/last/nickname fields
- active contract-target damageables use public display name instead of `civilianId`
- replacement execution preserves slot continuity while assigning a new occupant identity

Example assertions:

```csharp
Assert.That(damageable.DisplayName, Is.EqualTo("Derek Mullen"));
Assert.That(replacementRecord.PopulationSlotId, Is.EqualTo(vacatedRecord.PopulationSlotId));
Assert.That(replacementRecord.CivilianId, Is.Not.EqualTo(vacatedRecord.CivilianId));
Assert.That(replacementRecord.FirstName, Is.Not.EqualTo(vacatedRecord.FirstName).Or.Not.EqualTo(vacatedRecord.LastName));
```

**Step 2: Run the targeted failing tests**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests tmp/civilian-runtime-public-name-red.xml tmp/civilian-runtime-public-name-red.log
```

Expected:

- FAIL because runtime bridge and target damageable still expose `civilianId`

**Step 3: Write the minimal implementation**

Implement:

- runtime `CloneRecord(...)` updates in `CivilianPopulationRuntimeBridge.cs`
- a small helper to format public display name from record fields
- `ConfigureContractTargetIfEligible(...)` should pass the public display name
- optionally mirror resolved public name into `MainTownPopulationSpawnedCivilian` if useful for later runtime queries

Do not replace `civilianId` in internal lookups or record matching.

**Step 4: Run the targeted tests until they pass**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests tmp/civilian-runtime-public-name-green.xml tmp/civilian-runtime-public-name-green.log
```

Expected:

- PASS for runtime identity and replacement continuity coverage

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs Reloader/Assets/_Project/NPCs/Scripts/Runtime/MainTownPopulationSpawnedCivilian.cs Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs
git commit -m "feat: use public identity for spawned contract targets"
```

### Task 4: Publish contract name and description from the live occupant record

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`
- Test: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsBridgePlayModeTests.cs`

**Step 1: Write the failing tests**

Add or update PlayMode assertions to verify:

- available and active procedural contracts show the occupant public name in `TargetDisplayName`
- contract description is built from the current occupant visual-description tags
- when a dead occupant is replaced later, the next contract text matches the replacement identity instead of stale prior text

Example assertions:

```csharp
Assert.That(snapshot.TargetDisplayName, Is.EqualTo("Derek Mullen"));
Assert.That(snapshot.TargetDescription, Does.Contain("red jacket"));
Assert.That(snapshot.TargetDisplayName, Is.Not.EqualTo(previousSnapshot.TargetDisplayName));
```

**Step 2: Run the targeted failing tests**

Run:

```bash
bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests tmp/maintown-contract-public-name-red.xml tmp/maintown-contract-public-name-red.log
bash scripts/run-unity-tests.sh playmode Reloader.UI.Tests.PlayMode.TabInventoryContractsBridgePlayModeTests tmp/contracts-ui-public-name-red.xml tmp/contracts-ui-public-name-red.log
```

Expected:

- FAIL because contract offers still publish `civilianId` as target display name

**Step 3: Write the minimal implementation**

Implement:

- `RefreshProceduralContractOffer()` should supply public display name
- `BuildProceduralTargetDescription(...)` should continue using persisted appearance tags, with optional nickname flavor only if it improves clarity
- ensure offer refresh uses the current live occupant record after replacement, not stale cached target text

Do not redesign `AssassinationContractDefinition`; it already exposes `TargetDisplayName` and `TargetDescription`.

**Step 4: Run the targeted tests until they pass**

Run:

```bash
bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests tmp/maintown-contract-public-name-green.xml tmp/maintown-contract-public-name-green.log
bash scripts/run-unity-tests.sh playmode Reloader.UI.Tests.PlayMode.TabInventoryContractsBridgePlayModeTests tmp/contracts-ui-public-name-green.xml tmp/contracts-ui-public-name-green.log
```

Expected:

- PASS for contract-runtime and UI-public-name coverage

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsBridgePlayModeTests.cs
git commit -m "feat: publish contract names from live civilian identity"
```

### Task 5: Bump save schema and verify end-to-end persistence

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SaveBootstrapper.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SaveCoordinator.cs`
- Test: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownPopulationInfrastructurePlayModeTests.cs`
- Test: `Reloader/Assets/_Project/World/Tests/PlayMode/RoundTripTravelPlayModeTests.cs`
- Modify: `docs/design/npcs-and-quests.md`
- Modify: `docs/design/v0.1-demo-status-and-milestones.md`

**Step 1: Write the failing tests**

Add/update persistence coverage to assert:

- generated civilians preserve public identity and appearance through save/load and travel
- dead civilians remain absent after travel/load
- replacement civilians persist after arrival and continue matching contract copy

Example assertions:

```csharp
Assert.That(restoredCivilian.FirstName, Is.EqualTo(originalCivilian.FirstName));
Assert.That(restoredCivilian.LastName, Is.EqualTo(originalCivilian.LastName));
Assert.That(replacementCivilian.FirstName, Is.Not.EqualTo(retiredCivilian.FirstName).Or.Not.EqualTo(retiredCivilian.LastName));
```

**Step 2: Run the targeted failing tests**

Run:

```bash
bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests tmp/maintown-population-persistence-red.xml tmp/maintown-population-persistence-red.log
bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.RoundTripTravelPlayModeTests tmp/roundtrip-public-identity-red.xml tmp/roundtrip-public-identity-red.log
```

Expected:

- FAIL because save schema/runtime restore paths do not yet include public identity fields end-to-end

**Step 3: Write the minimal implementation**

Implement:

- global save schema bump for the changed civilian payload
- any required restore-path updates so the new fields survive end-to-end
- doc updates noting that procedural civilians now persist public identity and contracts target live occupant identity

**Step 4: Run the focused tests and the broader verification**

Run:

```bash
bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests tmp/maintown-population-persistence-green.xml tmp/maintown-population-persistence-green.log
bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.RoundTripTravelPlayModeTests tmp/roundtrip-public-identity-green.xml tmp/roundtrip-public-identity-green.log
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

Expected:

- PASS for focused playmode persistence coverage
- PASS for doc/context validation scripts

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Save/SaveBootstrapper.cs Reloader/Assets/_Project/Core/Scripts/Save/SaveCoordinator.cs Reloader/Assets/_Project/World/Tests/PlayMode/MainTownPopulationInfrastructurePlayModeTests.cs Reloader/Assets/_Project/World/Tests/PlayMode/RoundTripTravelPlayModeTests.cs docs/design/npcs-and-quests.md docs/design/v0.1-demo-status-and-milestones.md
git commit -m "feat: persist procedural civilian public identity across saves"
```

### Task 6: Run final focused regression sweep before handoff

**Files:**
- No new file edits expected
- Verification only

**Step 1: Run focused suites**

Run:

```bash
bash scripts/run-unity-tests.sh editmode Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests tmp/civilian-pop-save-final.xml tmp/civilian-pop-save-final.log
bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianAppearanceGeneratorTests tmp/civilian-generator-final.xml tmp/civilian-generator-final.log
bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests tmp/civilian-runtime-final.xml tmp/civilian-runtime-final.log
bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests tmp/maintown-contract-final.xml tmp/maintown-contract-final.log
bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests tmp/maintown-population-final.xml tmp/maintown-population-final.log
bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.RoundTripTravelPlayModeTests tmp/roundtrip-final.xml tmp/roundtrip-final.log
bash scripts/run-unity-tests.sh playmode Reloader.UI.Tests.PlayMode.TabInventoryContractsBridgePlayModeTests tmp/contracts-ui-final.xml tmp/contracts-ui-final.log
```

Expected:

- PASS for all focused suites touching civilian save/runtime/contracts/UI persistence

**Step 2: Review git diff**

Run:

```bash
git status --short
git diff --stat
```

Expected:

- only intended feature/test/doc changes remain

**Step 3: Commit the final sweep if needed**

```bash
git add docs/plans/2026-03-09-procedural-civilian-public-identity-design.md docs/plans/2026-03-09-procedural-civilian-public-identity-implementation-plan.md
git commit -m "docs: add procedural civilian public identity plans"
```

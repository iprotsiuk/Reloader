# First Contract Target NPC Vertical Slice Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build the first playable assassination-contract loop by accepting one contract from the TAB `Contracts` tab, binding it to one target NPC in `MainTown`, resolving kill state against the correct target, and paying out only after police heat clears.

**Architecture:** Keep the slice narrow. Reuse the existing TAB inventory shell, contract runtime state/events, NPC foundation, and police heat controller. Add one contract runtime service, one UI adapter path for `Contracts`, one target-NPC bridge in `MainTown`, and one payout bridge into the economy runtime. Do not add a general ambient NPC spawn system or portrait renderer in this pass.

**Tech Stack:** Unity 6.3, C#, UI Toolkit, ScriptableObject data assets, NUnit EditMode tests, Unity PlayMode tests.

---

### Task 1: Add Contract Runtime State And Event Tests

**Files:**
- Create: `Reloader/Assets/_Project/Contracts/Tests/EditMode/ContractRuntimeControllerTests.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/ContractEventContractsTests.cs`
- Modify: `Reloader/Assets/_Project/Contracts/Scripts/Reloader.Contracts.asmdef`
- Create or modify test asmdef if needed: `Reloader/Assets/_Project/Contracts/Tests/EditMode/Reloader.Contracts.Tests.EditMode.asmdef`

**Step 1: Write the failing tests**

Cover:
- accepting a contract stores one active contract
- accepting a second contract while one is active is rejected or ignored by contract
- completing a contract raises the typed completion event
- failing a contract raises the typed failure event
- clearing an active contract resets active runtime state

**Step 2: Run test to verify it fails**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath Reloader \
  -runTests -testPlatform EditMode \
  -testFilter Reloader.Contracts.Tests.EditMode.ContractRuntimeControllerTests \
  -logFile -
```

Expected:
- Missing `ContractRuntimeController` or equivalent runtime orchestration type
- Failing assertions for active-contract lifecycle

**Step 3: Write minimal implementation**

Create a small contract runtime controller/service under `Reloader/Assets/_Project/Contracts/Scripts/Runtime/` that:
- accepts one `AssassinationContractDefinition`
- exposes the active `AssassinationContractRuntimeState`
- raises `RuntimeKernelBootstrapper.ContractEvents`
- completes, fails, and clears one contract

Do not implement generation yet. Seed one available contract via data.

**Step 4: Run test to verify it passes**

Run the same EditMode command and expect PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Contracts/Scripts/Runtime \
        Reloader/Assets/_Project/Contracts/Tests/EditMode \
        Reloader/Assets/_Project/Core/Tests/EditMode/ContractEventContractsTests.cs
git commit -m "feat: add contract runtime controller"
```

### Task 2: Add Contracts Tab UI Tests First

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryViewBinder.cs`
- Create: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/EditMode/TabInventoryUxmlCopyEditModeTests.cs`

**Step 1: Write the failing tests**

Cover:
- `Quests` tab label is renamed to `Contracts`
- contracts section shows available contract summary
- accept button raises the correct intent
- active contract details render after acceptance
- result state renders completed/failed status

**Step 2: Run test to verify it fails**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath Reloader \
  -runTests -testPlatform PlayMode \
  -testFilter Reloader.UI.Tests.PlayMode.TabInventoryContractsPlayModeTests \
  -logFile -
```

Expected:
- missing contracts UI elements or missing binder/controller support

**Step 3: Write minimal implementation**

Add a `Contracts` section to the TAB UI with:
- available contract title
- appearance/location summary
- payout/distance band
- accept button
- active-state text
- result-state text

Keep it read-only except for `Accept` in the first pass.

**Step 4: Run tests to verify they pass**

Run the PlayMode command above plus:

```bash
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath Reloader \
  -runTests -testPlatform EditMode \
  -testFilter Reloader.UI.Tests.EditMode.TabInventoryUxmlCopyEditModeTests \
  -logFile -
```

Expected: PASS

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml \
        Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory \
        Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsPlayModeTests.cs \
        Reloader/Assets/_Project/UI/Tests/EditMode/TabInventoryUxmlCopyEditModeTests.cs
git commit -m "feat: add contracts tab to tab inventory"
```

### Task 3: Add Contract-To-UI Adapter Wiring

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs`
- Modify or create: `Reloader/Assets/_Project/Contracts/Scripts/Runtime/*UiAdapter*.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsPlayModeTests.cs`

**Step 1: Write the failing tests**

Cover:
- controller can read available contract and active contract status
- accept intent invokes contract runtime
- UI refreshes after accept and after result-state change

**Step 2: Run test to verify it fails**

Run the `TabInventoryContractsPlayModeTests` command from Task 2 and expect adapter/wiring failures.

**Step 3: Write minimal implementation**

Add a narrow UI-facing adapter interface similar to the existing device adapter pattern:
- query available contract status
- query active contract status
- accept available contract

Wire it into `TabInventoryController` through the runtime bridge.

**Step 4: Run tests to verify it passes**

Run the same `TabInventoryContractsPlayModeTests` command and expect PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryController.cs \
        Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs \
        Reloader/Assets/_Project/Contracts/Scripts/Runtime
git commit -m "feat: wire contracts runtime into tab ui"
```

### Task 4: Add Target NPC Runtime Tests

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Tests/EditMode/ContractTargetNpcTests.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcRoleKind.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/ContractTargetNpc.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/ContractTargetIdentity.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcAgent.cs` only if required

**Step 1: Write the failing tests**

Cover:
- target NPC exposes stable `targetId`
- target NPC can bind to the active contract target id
- wrong target does not complete the contract
- correct target reports completion

**Step 2: Run test to verify it fails**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath Reloader \
  -runTests -testPlatform EditMode \
  -testFilter Reloader.NPCs.Tests.EditMode.ContractTargetNpcTests \
  -logFile -
```

Expected: missing target-NPC bridge types and failing contract-match assertions

**Step 3: Write minimal implementation**

Add one narrow target component that:
- carries `targetId`, `displayName`, and appearance summary
- knows whether it is the active contract target
- exposes one kill/completion callback path

Do not add a full AI stack yet.

**Step 4: Run tests to verify they pass**

Run the same EditMode command and expect PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime \
        Reloader/Assets/_Project/NPCs/Tests/EditMode
git commit -m "feat: add contract target npc runtime"
```

### Task 5: Integrate Target Kill Resolution With Weapon Damage

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/World/DummyTargetDamageable.cs` only if reused
- Create preferred target-specific script(s): `Reloader/Assets/_Project/NPCs/Scripts/World/*ContractTarget*.cs`
- Create: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/ContractTargetKillPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/IDamageable.cs` only if required

**Step 1: Write the failing tests**

Cover:
- projectile damage against the correct target NPC completes the kill objective
- projectile damage against the wrong NPC fails the contract or leaves it unresolved according to the runtime rule
- target remains markable through the existing device flow if appropriate

**Step 2: Run test to verify it fails**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath Reloader \
  -runTests -testPlatform PlayMode \
  -testFilter Reloader.NPCs.Tests.PlayMode.ContractTargetKillPlayModeTests \
  -logFile -
```

Expected: target kill does not propagate contract result yet

**Step 3: Write minimal implementation**

Use the existing projectile/damage path. Add a target-NPC damageable or bridge component that converts lethal or first-hit contract resolution into contract runtime completion/failure.

Avoid broad damage-system refactors.

**Step 4: Run tests to verify it passes**

Run the same PlayMode command and expect PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/World \
        Reloader/Assets/_Project/NPCs/Tests/PlayMode \
        Reloader/Assets/_Project/Weapons/Scripts
git commit -m "feat: resolve contracts from target kills"
```

### Task 6: Gate Payout On Police Heat Clearing

**Files:**
- Create: `Reloader/Assets/_Project/Contracts/Tests/EditMode/ContractPayoutAfterHeatTests.cs`
- Modify: `Reloader/Assets/_Project/LawEnforcement/Scripts/Runtime/PoliceHeatController.cs` only if needed
- Modify or create: `Reloader/Assets/_Project/Contracts/Scripts/Runtime/*Payout*.cs`
- Modify: `Reloader/Assets/_Project/Economy/Scripts/Runtime/EconomyController.cs`
- Modify or create: `Reloader/Assets/_Project/Economy/Scripts/Runtime/EconomyRuntime.cs`

**Step 1: Write the failing tests**

Cover:
- correct target kill with no heat pays immediately or on next clear state
- correct target kill while heat is active delays payout
- payout fires exactly once when heat returns to `Clear`
- failed contracts never pay

**Step 2: Run test to verify it fails**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath Reloader \
  -runTests -testPlatform EditMode \
  -testFilter "Reloader.Contracts.Tests.EditMode.ContractPayoutAfterHeatTests,Reloader.LawEnforcement.Tests.EditMode.PoliceHeatControllerTests" \
  -logFile -
```

Expected: no payout bridge exists yet

**Step 3: Write minimal implementation**

Add a contract payout bridge that:
- listens to contract kill completion state
- listens to law-enforcement heat state
- awards money exactly once when allowed

Prefer a clean credit method in economy runtime/controller over shop-specific hacks.

**Step 4: Run tests to verify they pass**

Run the same EditMode command and expect PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Contracts/Scripts/Runtime \
        Reloader/Assets/_Project/Contracts/Tests/EditMode \
        Reloader/Assets/_Project/Economy/Scripts/Runtime
git commit -m "feat: gate contract payout on police heat"
```

### Task 7: Author MainTown Contract Target And Scene Tests

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `Reloader/Assets/_Project/World/Editor/MainTownCombatWiring.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownCombatWiringEditModeTests.cs`
- Modify or create: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`
- Create data assets under:
  - `Reloader/Assets/_Project/Contracts/Data/`
  - `Reloader/Assets/_Project/NPCs/Data/`
  - `Reloader/Assets/_Project/NPCs/Prefabs/`

**Step 1: Write the failing scene tests**

Cover:
- `MainTown` has one authored contract target root
- required target id / contract wiring is present
- scene defaults do not regress existing vendor/chest authority
- end-to-end smoke can accept contract, locate target, kill target, and clear payout gate in scene runtime

**Step 2: Run test to verify it fails**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath Reloader \
  -runTests -testPlatform EditMode \
  -testFilter Reloader.World.Tests.EditMode.MainTownCombatWiringEditModeTests \
  -logFile -
```

and

```bash
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath Reloader \
  -runTests -testPlatform PlayMode \
  -testFilter Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests \
  -logFile -
```

Expected: missing target authoring and missing contract slice scene flow

**Step 3: Write minimal implementation**

Author one target NPC in `MainTown`, bind one contract asset to it, and ensure the scene has any runtime bridge objects required by the TAB contract flow.

Keep the character/content import to one curated target preset.

**Step 4: Run tests to verify they pass**

Run both commands above and expect PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/World/Scenes/MainTown.unity \
        Reloader/Assets/_Project/World/Editor/MainTownCombatWiring.cs \
        Reloader/Assets/_Project/World/Tests \
        Reloader/Assets/_Project/Contracts/Data \
        Reloader/Assets/_Project/NPCs/Data \
        Reloader/Assets/_Project/NPCs/Prefabs
git commit -m "feat: author maintown first contract slice"
```

### Task 8: Sync Docs, Progress, And PR Notes

**Files:**
- Modify: `docs/design/v0.1-demo-status-and-milestones.md`
- Modify: `docs/plans/progress/2026-03-06-first-contract-target-npc-vertical-slice-progress.md`
- Modify: `docs/plans/2026-03-06-first-contract-target-npc-vertical-slice-design.md` only if implementation changed scope

**Step 1: Write the failing doc checklist**

Create a short checklist in the progress doc for:
- runtime controller
- contracts tab
- target NPC
- payout gate
- MainTown scene
- tests
- PR review status

**Step 2: Run validation to verify doc/test state**

Run:

```bash
git diff --check
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

Expected: clean diff and passing doc audits

**Step 3: Write minimal updates**

Mark completed work only when code and tests exist. Keep milestone wording aligned with the real implemented state.

**Step 4: Run validation to verify it passes**

Run the same commands above and expect PASS.

**Step 5: Commit**

```bash
git add docs/design/v0.1-demo-status-and-milestones.md \
        docs/plans/2026-03-06-first-contract-target-npc-vertical-slice-design.md \
        docs/plans/progress/2026-03-06-first-contract-target-npc-vertical-slice-progress.md
git commit -m "docs: update contract slice progress"
```


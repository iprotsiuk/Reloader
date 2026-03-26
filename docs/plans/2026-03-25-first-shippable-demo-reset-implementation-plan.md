# First Shippable Demo Reset Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Turn the current repo into one reproducible assassination demo by reusing the existing contract slice, freezing expansion work, and hardening only the prep-to-shot-to-escape path needed for an external demo.

**Architecture:** Keep the current contract-prep-escape spine and treat the existing `MainTown` contract slice as the canonical starting point. Narrow work to authored content, targeted tests, and deterministic runtime seams; do not open new world, progression, or floating-origin follow-on fronts during this plan.

**Tech Stack:** Unity 6 C#, existing `Core` contract/heat runtime, `UI` tab bridges, `World` PlayMode smoke tests, `Reloading` acceptance suites, `Economy` checkout tests, authored `MainTown` scene, docs under `docs/design/` and `docs/plans/`.

---

### Task 1: Lock The Reset In Docs And Progress Tracking

**Files:**
- Create: `docs/plans/progress/2026-03-25-first-shippable-demo-reset-progress.md`
- Modify: `docs/design/v0.1-demo-status-and-milestones.md`
- Reference: `docs/plans/2026-03-25-first-shippable-demo-reset-design.md`

**Step 1: Write the progress doc**

Record:

- the demo loop being hardened
- the freeze list
- commands run
- blockers found
- exact files touched per task

**Step 2: Re-run doc guardrails**

Run:

```bash
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

Expected: PASS before runtime changes start.

**Step 3: Commit**

```bash
git add docs/design/v0.1-demo-status-and-milestones.md docs/plans/2026-03-25-first-shippable-demo-reset-design.md docs/plans/2026-03-25-first-shippable-demo-reset-implementation-plan.md docs/plans/progress/2026-03-25-first-shippable-demo-reset-progress.md
git commit -m "docs: lock first shippable demo reset"
```

### Task 2: Freeze The Demo Acceptance Contract In PlayMode Tests

**Files:**
- Create: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownDemoLoopPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/PlayMode/StaticContractRuntimeProviderPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsBridgePlayModeTests.cs`

**Step 1: Write the failing demo-loop tests**

Cover:

- accept one available contract through the `Contracts` tab
- complete the kill objective only when the correct target dies
- hold payout until search clears
- require an explicit claim/cash-out step
- fail closed if the scene wiring cannot produce the one active demo path

**Step 2: Run the focused red suite**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.World.Tests.PlayMode.MainTownDemoLoopPlayModeTests|Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests|Reloader.Core.Tests.PlayMode.StaticContractRuntimeProviderPlayModeTests|Reloader.UI.Tests.PlayMode.TabInventoryContractsBridgePlayModeTests" "$(pwd)/tmp/demo-reset-task2-red.xml" "$(pwd)/tmp/demo-reset-task2-red.log"
```

Expected: FAIL only on the newly tightened demo-path assertions.

**Step 3: Tighten tests until they describe only the approved loop**

- no generic sandbox assertions
- no multi-contract expectations
- no new world-breadth assumptions

**Step 4: Commit**

```bash
git add Reloader/Assets/_Project/World/Tests/PlayMode/MainTownDemoLoopPlayModeTests.cs Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs Reloader/Assets/_Project/Core/Tests/PlayMode/StaticContractRuntimeProviderPlayModeTests.cs Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsBridgePlayModeTests.cs docs/plans/progress/2026-03-25-first-shippable-demo-reset-progress.md
git commit -m "test(demo): lock main town demo loop acceptance"
```

### Task 3: Harden The Existing Contract Runtime Instead Of Replacing It

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/ContractEscapeResolutionRuntime.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/StaticContractRuntimeProvider.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/PoliceHeatRuntime.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/World/ContractTargetDamageable.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/ContractEscapeResolutionRuntimeTests.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/PlayMode/StaticContractRuntimeProviderPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ContractTargetDamageablePlayModeTests.cs`

**Step 1: Write the failing focused tests**

Add or tighten cases for:

- one active demo contract only
- explicit payout claim after search clear
- dead-target-before-accept behavior staying deterministic
- target damage bridge refusing unrelated kills

**Step 2: Run the focused red suite**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.ContractEscapeResolutionRuntimeTests" "$(pwd)/tmp/demo-reset-task3-edit-red.xml" "$(pwd)/tmp/demo-reset-task3-edit-red.log"
bash scripts/run-unity-tests.sh playmode "Reloader.Core.Tests.PlayMode.StaticContractRuntimeProviderPlayModeTests|Reloader.Weapons.Tests.PlayMode.ContractTargetDamageablePlayModeTests" "$(pwd)/tmp/demo-reset-task3-play-red.xml" "$(pwd)/tmp/demo-reset-task3-play-red.log"
```

Expected: FAIL on the new deterministic demo-path expectations.

**Step 3: Implement the minimal runtime changes**

- prefer one canonical demo offer
- keep payout gating strict
- do not broaden procedural or civilian-simulation responsibilities

**Step 4: Re-run the same suites**

Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Runtime/ContractEscapeResolutionRuntime.cs Reloader/Assets/_Project/Core/Scripts/Runtime/StaticContractRuntimeProvider.cs Reloader/Assets/_Project/Core/Scripts/Runtime/PoliceHeatRuntime.cs Reloader/Assets/_Project/Weapons/Scripts/World/ContractTargetDamageable.cs Reloader/Assets/_Project/Core/Tests/EditMode/ContractEscapeResolutionRuntimeTests.cs Reloader/Assets/_Project/Core/Tests/PlayMode/StaticContractRuntimeProviderPlayModeTests.cs Reloader/Assets/_Project/Weapons/Tests/PlayMode/ContractTargetDamageablePlayModeTests.cs docs/plans/progress/2026-03-25-first-shippable-demo-reset-progress.md
git commit -m "feat(demo): harden contract resolution for first shippable loop"
```

### Task 4: Make `MainTown` A Demo Scene, Not A World Expansion Sandbox

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/RoundTripTravelPlayModeTests.cs`

**Step 1: Write the failing scene tests**

Require:

- one stable contract intake point
- one stable target exposure pattern
- multiple plausible firing lanes/sightlines
- at least one known-good reference setup for deterministic tests
- only the minimum shop/workshop/house wiring needed for the demo
- no dependence on new districts, planning labels, or optional shell landmarks

**Step 2: Run the focused red suite**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests|Reloader.World.Tests.PlayMode.RoundTripTravelPlayModeTests" "$(pwd)/tmp/demo-reset-task4-red.xml" "$(pwd)/tmp/demo-reset-task4-red.log"
```

Expected: FAIL where current scene authoring still depends on broader world assumptions.

**Step 3: Author the minimal scene fix**

- preserve required runtime object names and travel contracts
- remove or ignore non-demo dependencies
- optimize for smoke-test readability, not map scale

**Step 4: Re-run the suite**

Expected: `MainTownContractSlicePlayModeTests` PASS. If `RoundTripTravelPlayModeTests` still fails for unrelated broader travel reasons, document that and keep the first demo path scene-local until the failure is truly on the critical path.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/World/Scenes/MainTown.unity Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs Reloader/Assets/_Project/World/Tests/PlayMode/RoundTripTravelPlayModeTests.cs docs/plans/progress/2026-03-25-first-shippable-demo-reset-progress.md
git commit -m "feat(world): narrow main town to the shippable demo path"
```

### Task 5: Keep Prep Valuable But Optional

**Files:**
- Modify: `Reloader/Assets/_Project/PlayerDevice/Scripts/World/PlayerDeviceController.cs`
- Modify: `Reloader/Assets/_Project/PlayerDevice/Scripts/World/PlayerDeviceTargetSelectionController.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryDeviceSectionPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Reloading/Tests/PlayMode/WorkbenchMountFlowAcceptancePlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Reloading/Tests/PlayMode/WorkbenchLoadoutControllerPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Reloading/Tests/PlayMode/ReloadingBenchInteractionPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Economy/Tests/PlayMode/EconomyControllerCheckoutPlayModeTests.cs`

**Step 1: Write the failing prep-path tests**

Require:

- shop -> rifle/loadout -> optional device validation -> contract execution stays coherent
- the `PlayerDevice` helps the shot but does not block the contract loop
- bench/shop flows do not depend on unrelated world breadth

**Step 2: Run the focused red suite**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.UI.Tests.PlayMode.TabInventoryDeviceSectionPlayModeTests|Reloader.Reloading.Tests.PlayMode.WorkbenchMountFlowAcceptancePlayModeTests|Reloader.Reloading.Tests.PlayMode.WorkbenchLoadoutControllerPlayModeTests|Reloader.Reloading.Tests.PlayMode.ReloadingBenchInteractionPlayModeTests|Reloader.Economy.Tests.PlayMode.EconomyControllerCheckoutPlayModeTests" "$(pwd)/tmp/demo-reset-task5-red.xml" "$(pwd)/tmp/demo-reset-task5-red.log"
```

Expected: FAIL only if the prep flow still acts like a hidden requirement or leaks unrelated scene dependencies.

**Step 3: Implement the minimal runtime changes**

- keep prep expressive
- keep the `PlayerDevice` optional
- do not add new crafting or economy breadth

**Step 4: Re-run the suite**

Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/PlayerDevice/Scripts/World/PlayerDeviceController.cs Reloader/Assets/_Project/PlayerDevice/Scripts/World/PlayerDeviceTargetSelectionController.cs Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryDeviceSectionPlayModeTests.cs Reloader/Assets/_Project/Reloading/Tests/PlayMode/WorkbenchMountFlowAcceptancePlayModeTests.cs Reloader/Assets/_Project/Reloading/Tests/PlayMode/WorkbenchLoadoutControllerPlayModeTests.cs Reloader/Assets/_Project/Reloading/Tests/PlayMode/ReloadingBenchInteractionPlayModeTests.cs Reloader/Assets/_Project/Economy/Tests/PlayMode/EconomyControllerCheckoutPlayModeTests.cs docs/plans/progress/2026-03-25-first-shippable-demo-reset-progress.md
git commit -m "feat(demo): keep prep valuable without blocking the contract loop"
```

### Task 6: Verify The Narrow Demo And Capture The Real Exit Status

**Files:**
- Modify: `docs/plans/progress/2026-03-25-first-shippable-demo-reset-progress.md`
- Modify: `docs/design/v0.1-demo-status-and-milestones.md`

**Step 1: Run the targeted final ladder**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.ContractEscapeResolutionRuntimeTests|Reloader.Core.Tests.EditMode.WorldObjectStateSaveModuleTests|Reloader.Core.Tests.EditMode.PlayerDeviceSaveModuleTests" "$(pwd)/tmp/demo-reset-task6-edit.xml" "$(pwd)/tmp/demo-reset-task6-edit.log"
bash scripts/run-unity-tests.sh playmode "Reloader.World.Tests.PlayMode.MainTownDemoLoopPlayModeTests|Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests|Reloader.Core.Tests.PlayMode.StaticContractRuntimeProviderPlayModeTests|Reloader.UI.Tests.PlayMode.TabInventoryContractsBridgePlayModeTests|Reloader.UI.Tests.PlayMode.TabInventoryContractsSectionPlayModeTests|Reloader.UI.Tests.PlayMode.TabInventoryDeviceSectionPlayModeTests|Reloader.Reloading.Tests.PlayMode.WorkbenchMountFlowAcceptancePlayModeTests|Reloader.Reloading.Tests.PlayMode.WorkbenchLoadoutControllerPlayModeTests|Reloader.Reloading.Tests.PlayMode.ReloadingBenchInteractionPlayModeTests|Reloader.Economy.Tests.PlayMode.EconomyControllerCheckoutPlayModeTests|Reloader.Weapons.Tests.PlayMode.ContractTargetDamageablePlayModeTests|Reloader.Weapons.Tests.PlayMode.ScopeAttachmentAdsIntegrationPlayModeTests|Reloader.Weapons.Tests.PlayMode.WeaponProjectilePlayModeTests" "$(pwd)/tmp/demo-reset-task6-play.xml" "$(pwd)/tmp/demo-reset-task6-play.log"
```

Expected: PASS for the narrow demo suites. Any broader failure must be recorded as out of scope unless it blocks the demo path directly.

**Step 2: Update the docs honestly**

- record what the first demo can now do
- record what remains frozen
- do not mark unrelated world breadth as done

**Step 3: Commit**

```bash
git add docs/design/v0.1-demo-status-and-milestones.md docs/plans/progress/2026-03-25-first-shippable-demo-reset-progress.md
git commit -m "test(demo): verify first shippable demo path"
```

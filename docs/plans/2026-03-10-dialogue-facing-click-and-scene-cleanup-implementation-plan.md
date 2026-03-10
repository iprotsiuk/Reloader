# Dialogue Facing, Click Routing, and MainTown Cleanup Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make civilian dialogue in `MainTown` frame and face the active NPC correctly, restore mouse-based dialogue reply selection, and remove the legacy authored `ContractTarget_Volkov` scene dependency.

**Architecture:** Keep the current dialogue runtime and player look math intact. Improve speaker presentation by resolving a real visual focus anchor from the civilian appearance applicator, drive NPC facing from `DialogueConversationModeController`, fix pointer routing at the `MainTown` `EventSystem` plus overlay binder, and update scene/tests to rely on procedural civilians.

**Tech Stack:** Unity 6.3, C#, UITK, Input System, NUnit EditMode/PlayMode tests, Unity scene YAML

---

### Task 1: Document the approved design

**Files:**
- Create: `docs/plans/2026-03-10-dialogue-facing-click-and-scene-cleanup-design.md`
- Create: `docs/plans/2026-03-10-dialogue-facing-click-and-scene-cleanup-implementation-plan.md`

**Step 1: Write the design and plan docs**

- Capture the approved speaker-focus, NPC-facing, click-routing, and scene-cleanup design.

**Step 2: Commit**

Run:

```bash
git add docs/plans/2026-03-10-dialogue-facing-click-and-scene-cleanup-design.md docs/plans/2026-03-10-dialogue-facing-click-and-scene-cleanup-implementation-plan.md
git commit -m "docs: plan dialogue facing and maintown cleanup"
```

### Task 2: Write the red tests for conversation framing and facing

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/DialogueConversationModePlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownPopulationInfrastructurePlayModeTests.cs`

**Step 1: Write the failing tests**

- Add a play mode test proving the speaker rotates toward the player while dialogue is active and stops after close.
- Add/edit a test proving spawned civilians expose a dialogue focus target that follows the active visual anchor rather than a fixed offset fallback when the visual anchor exists.
- Add a `MainTown` infrastructure regression that verifies a spawned civilian can still open dialogue and that the resolved focus target exists on the NPC.

**Step 2: Run the targeted tests to verify red**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/ivanprotsiuk/unity/Reloader/Reloader -runTests -testPlatform PlayMode -testFilter Reloader.NPCs.Tests.PlayMode.DialogueConversationModePlayModeTests -quit
```

Expected: FAIL on missing speaker-facing / focus-anchor behavior.

**Step 3: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Tests/PlayMode/DialogueConversationModePlayModeTests.cs Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs Reloader/Assets/_Project/World/Tests/PlayMode/MainTownPopulationInfrastructurePlayModeTests.cs
git commit -m "test: capture dialogue facing and focus regressions"
```

### Task 3: Implement speaker presentation and dialogue-facing

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/World/NpcDialogueFacingController.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/World/DialogueConversationModeController.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/MainTownNpcAppearanceApplicator.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/MainTownPopulationSpawnedCivilian.cs`

**Step 1: Write the minimal implementation**

- Add a small component that yaws an NPC root toward a supplied target while active.
- Have `DialogueConversationModeController` detect the active speaker root and drive that component during conversation enter/refresh/exit.
- Add a helper on `MainTownNpcAppearanceApplicator` to resolve the best available dialogue focus anchor from the active visual hierarchy.
- Update `MainTownPopulationSpawnedCivilian` to use the resolved visual anchor first and only fall back to the synthetic focus target if needed.

**Step 2: Run the targeted tests to verify green**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/ivanprotsiuk/unity/Reloader/Reloader -runTests -testPlatform PlayMode -testFilter Reloader.NPCs.Tests.PlayMode.DialogueConversationModePlayModeTests -quit
```

Expected: PASS

**Step 3: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/World/NpcDialogueFacingController.cs Reloader/Assets/_Project/NPCs/Scripts/World/DialogueConversationModeController.cs Reloader/Assets/_Project/NPCs/Scripts/Runtime/MainTownNpcAppearanceApplicator.cs Reloader/Assets/_Project/NPCs/Scripts/Runtime/MainTownPopulationSpawnedCivilian.cs
git commit -m "feat: frame civilian dialogue around the active speaker"
```

### Task 4: Write the red tests for click routing and scene cleanup

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/DialogueOverlayUiToolkitPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownCombatWiringEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownPopulationInfrastructurePlayModeTests.cs`

**Step 1: Write the failing tests**

- Add a `MainTown` scene wiring regression that verifies the `EventSystem` UI module uses the current `_Project` input-actions asset and non-null point/left-click actions.
- Add/update a dialogue overlay test to exercise the binder’s explicit click submission path rather than only reflection-invoking `Button.clicked`.
- Replace authored-target assumptions with procedural/non-target NPC assertions so the tests fail until `ContractTarget_Volkov` is removed.

**Step 2: Run the targeted tests to verify red**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/ivanprotsiuk/unity/Reloader/Reloader -runTests -testPlatform PlayMode -testFilter Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests,Reloader.UI.Tests.PlayMode.DialogueOverlayUiToolkitPlayModeTests -quit
```

Expected: FAIL on stale scene input wiring / authored-target assumptions.

**Step 3: Commit**

```bash
git add Reloader/Assets/_Project/UI/Tests/PlayMode/DialogueOverlayUiToolkitPlayModeTests.cs Reloader/Assets/_Project/World/Tests/EditMode/MainTownCombatWiringEditModeTests.cs Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs Reloader/Assets/_Project/World/Tests/PlayMode/MainTownPopulationInfrastructurePlayModeTests.cs
git commit -m "test: capture maintown click wiring and target cleanup regressions"
```

### Task 5: Implement click routing and remove the authored target

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Dialogue/DialogueOverlayViewBinder.cs`
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownCombatWiringEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownPopulationInfrastructurePlayModeTests.cs`

**Step 1: Write the minimal implementation**

- Rebind `MainTown`’s `InputSystemUIInputModule` to the current `_Project` input-actions asset and valid click actions.
- Harden `DialogueOverlayViewBinder` so click submission uses explicit event registration and avoids unnecessary hover-triggered republish churn.
- Remove `ContractTarget_Volkov` from `MainTown` and update the scene tests to use other non-target NPCs.

**Step 2: Run the targeted tests to verify green**

Run:

```bash
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
```

Then run:

```bash
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/ivanprotsiuk/unity/Reloader/Reloader -runTests -testPlatform PlayMode -testFilter Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests,Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests,Reloader.UI.Tests.PlayMode.DialogueOverlayUiToolkitPlayModeTests,Reloader.NPCs.Tests.PlayMode.DialogueConversationModePlayModeTests -quit
```

Expected: PASS for the touched suites

**Step 3: Commit**

```bash
git add Reloader/Assets/_Project/UI/Scripts/Toolkit/Dialogue/DialogueOverlayViewBinder.cs Reloader/Assets/_Project/World/Scenes/MainTown.unity Reloader/Assets/_Project/World/Tests/EditMode/MainTownCombatWiringEditModeTests.cs Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs Reloader/Assets/_Project/World/Tests/PlayMode/MainTownPopulationInfrastructurePlayModeTests.cs
git commit -m "fix: polish maintown civilian dialogue interaction"
```

### Task 6: Run focused verification and subagent review

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/PlayerNpcInteractionPlayModeTests.cs` if verification exposes a missed interaction regression
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/DialogueOverlayBridgePlayModeTests.cs` if verification exposes bridge regressions

**Step 1: Run focused verification**

Run:

```bash
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
```

Then run the focused suites:

```bash
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/ivanprotsiuk/unity/Reloader/Reloader -runTests -testPlatform EditMode -testFilter Reloader.NPCs.Tests.EditMode,Reloader.World.Tests.EditMode.MainTownCombatWiringEditModeTests -quit
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/ivanprotsiuk/unity/Reloader/Reloader -runTests -testPlatform PlayMode -testFilter Reloader.NPCs.Tests.PlayMode.DialogueConversationModePlayModeTests,Reloader.NPCs.Tests.PlayMode.PlayerNpcInteractionPlayModeTests,Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests,Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests,Reloader.UI.Tests.PlayMode.DialogueOverlayUiToolkitPlayModeTests,Reloader.UI.Tests.PlayMode.DialogueOverlayBridgePlayModeTests -quit
```

**Step 2: Request subagent review**

- Run one spec-focused subagent review over the final diff.
- Run one code-quality subagent review over the final diff.
- Fix any real issues they find and rerun the affected tests.

**Step 3: Commit any follow-up fixes**

```bash
git add -A
git commit -m "fix: address dialogue interaction review feedback"
```

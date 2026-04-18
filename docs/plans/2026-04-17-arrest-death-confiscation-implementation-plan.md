# Arrest/Death Confiscation Hardening Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make arrest/death recovery confiscate carried belt/backpack inventory exactly once, respawn at the correct anchor, and avoid replaying confiscated items through normal travel snapshot restore while leaving storage containers intact.

**Architecture:** Keep the only player death authority on `HumanoidDamageReceiver.Died -> PlayerDeathContractBridge`. Keep arrest/death recovery on `IPlayerRecoveryService` / `PlayerStateRuntimeBridge`. Add a recovery-specific travel/snapshot suppression seam in `IPlayerRecoveryTravelCoordinator` / `WorldTravelCoordinator` so recovery handoffs do not replay carried-inventory snapshots; normal travel must continue to snapshot/restore carried inventory, and storage/container state must remain isolated. Future arrest triggering stays in police responder/contact logic and must call the existing recovery service, not a new heat enum or heat event.

**Tech Stack:** Unity 6 C#, EditMode tests, PlayMode tests, `PlayerStateRuntimeBridge`, `WorldTravelCoordinator`, `IPlayerRecoveryTravelCoordinator`, `PlayerInventoryRuntime`, `ContainerStorage`, docs guardrails.

**Status:** Implemented on 2026-04-18. Recovery travel now suppresses carried-inventory replay, normal travel replay remains covered, and failure paths clear pending travel state.

---

### Task 1: Add A Failing Regression For Recovery Travel Replay

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Tests/EditMode/PlayerStateRuntimeBridgeEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/WorldTravelCoordinatorEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/RoundTripTravelPlayModeTests.cs`

**Step 1: Write the failing tests**

Add a recovery-specific regression that proves:
- `TryApplyArrestRecovery()` and `TryApplyDeathRecovery()` do not allow confiscated carried items to be replayed from travel snapshots.
- normal travel still preserves carried inventory when the player is not in a recovery handoff.
- the recovery path does not touch storage-container contents.

**Step 2: Run the focused red suite**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Player.Tests.EditMode.PlayerStateRuntimeBridgeEditModeTests|Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests|Reloader.Core.Tests.EditMode.PlayerInventoryRuntimeTests" "$(pwd)/tmp/arrest-confiscation-task1-red.xml" "$(pwd)/tmp/arrest-confiscation-task1-red.log"
```

Expected: FAIL because recovery currently clears carried inventory after travel instead of suppressing the travel replay path.

**Step 3: Tighten the regression until it names the real bug**

- Keep the assertion focused on carried belt/backpack replay, not generic respawn geometry.
- Do not make this test assert a new arrest heat enum or a new law-enforcement event.

**Step 4: Re-run the same suite**

Expected: failures point at the recovery/travel seam, not stale expectations.

---

### Task 2: Add The Minimal Recovery-Only Travel Suppression Seam

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Scripts/IPlayerRecoveryTravelCoordinator.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerStateRuntimeBridge.cs`
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/WorldTravelCoordinator.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/WorldTravelCoordinatorEditModeTests.cs`

**Step 1: Write the failing seam test**

Add a test that proves the recovery handoff can opt out of the normal carried-inventory snapshot replay path while leaving ordinary travel unchanged.

**Step 2: Run the focused red suite**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Player.Tests.EditMode.PlayerStateRuntimeBridgeEditModeTests|Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests" "$(pwd)/tmp/arrest-confiscation-task2-red.xml" "$(pwd)/tmp/arrest-confiscation-task2-red.log"
```

Expected: FAIL until a recovery-specific suppression path exists.

**Step 3: Implement the smallest recovery-only hook**

- Thread a recovery-only travel mode or suppression flag through `IPlayerRecoveryTravelCoordinator` into `WorldTravelCoordinator`.
- Use that hook only for arrest/death recovery.
- Keep the existing normal-travel snapshot path intact for non-recovery travel.
- Keep `PlayerStateRuntimeBridge` responsible for choosing police or hospital recovery anchors.

**Step 4: Re-run the same suite**

Expected: PASS.

---

### Task 3: Preserve Normal Travel And Container Isolation

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/PlayerInventoryRuntimeTests.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/StorageTransferEngineTests.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/ContainerStorageSaveModuleTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/RoundTripTravelPlayModeTests.cs`

**Step 1: Write the safety regressions**

Add coverage that proves:
- `PlayerInventoryRuntime.ClearCarriedItems()` clears belt/backpack only.
- container storage and other non-carried storage remain intact.
- normal travel still restores carried inventory on the non-recovery path.

**Step 2: Run the focused green suite**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.PlayerInventoryRuntimeTests|Reloader.Core.Tests.EditMode.StorageTransferEngineTests|Reloader.Core.Tests.EditMode.ContainerStorageSaveModuleTests" "$(pwd)/tmp/arrest-confiscation-task3-edit-green.xml" "$(pwd)/tmp/arrest-confiscation-task3-edit-green.log"
bash scripts/run-unity-tests.sh playmode "Reloader.World.Tests.PlayMode.RoundTripTravelPlayModeTests" "$(pwd)/tmp/arrest-confiscation-task3-play-green.xml" "$(pwd)/tmp/arrest-confiscation-task3-play-green.log"
```

Expected: PASS.

**Step 3: Fix only what the tests prove is broken**

- Keep the confiscation behavior limited to carried belt/backpack items.
- Do not add stash raids, container wipes, or broad persistence changes.

**Step 4: Re-run the same suites**

Expected: PASS with normal travel still restoring inventory.

---

### Task 4: Verify And Sync Docs

**Files:**
- Modify: `docs/design/law-enforcement.md`
- Modify: `docs/design/v0.1-demo-status-and-milestones.md`

**Step 1: Update the docs after the runtime slice is green**

- Keep law-enforcement text focused on confiscation correctness, not a new heat enum.
- Mark the demo status row `In Progress` only if the slice description explicitly calls out that the recovery suppression fix is still the remaining work.

**Step 2: Run the required guardrails**

Run:

```bash
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

Expected: PASS.

**Step 3: Run one final targeted gameplay verification pass**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Player.Tests.EditMode.PlayerStateRuntimeBridgeEditModeTests|Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests|Reloader.Core.Tests.EditMode.PlayerInventoryRuntimeTests|Reloader.Core.Tests.EditMode.StorageTransferEngineTests|Reloader.Core.Tests.EditMode.ContainerStorageSaveModuleTests" "$(pwd)/tmp/arrest-confiscation-final-edit.xml" "$(pwd)/tmp/arrest-confiscation-final-edit.log"
bash scripts/run-unity-tests.sh playmode "Reloader.World.Tests.PlayMode.RoundTripTravelPlayModeTests|Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests" "$(pwd)/tmp/arrest-confiscation-final-play.xml" "$(pwd)/tmp/arrest-confiscation-final-play.log"
```

Expected: PASS.

# Dialogue Cinematic Facing Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make dialogue entry frame NPC faces with correct pitch and smooth player/NPC turning, without changing the global player rig.

**Architecture:** Reuse the existing dialogue presentation anchor and conversation mode seams. Add dialogue-specific exponential smoothing to `PlayerLookController`'s focus-target override path, let that override bypass normal menu-open suppression while dialogue is active, match the rig's real pitch sign convention, and remove the startup snap from `NpcDialogueFacingController` so the speaker rotates toward the player over time.

**Tech Stack:** Unity 6.3, C#, NUnit PlayMode tests, Unity MCP for test execution

---

### Task 1: Document the approved design

**Files:**
- Create: `docs/plans/2026-03-10-dialogue-cinematic-facing-design.md`
- Create: `docs/plans/2026-03-10-dialogue-cinematic-facing-implementation-plan.md`

**Step 1: Write the design and plan docs**

- Capture the approved dialogue-only camera/facing design and the intended tests.

**Step 2: Commit**

```bash
git add docs/plans/2026-03-10-dialogue-cinematic-facing-design.md docs/plans/2026-03-10-dialogue-cinematic-facing-implementation-plan.md
git commit -m "docs: plan dialogue cinematic facing"
```

### Task 2: Write the failing tests for dialogue-facing behavior

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/DialogueConversationModePlayModeTests.cs`

**Step 1: Write the failing tests**

- Add a `PlayerLookController` test proving focus-target override does not instantly snap all the way to the target on the first tick.
- Add a `DialogueConversationModeController` play mode test proving the player pitch converges toward a higher NPC face anchor over several ticks.
- Add a dialogue-facing test proving the NPC rotates toward the player gradually and stops rotating after dialogue exits.

**Step 2: Run tests to verify red**

Run:

```bash
Unity PlayMode tests for:
- Reloader.Player.Tests.PlayMode.PlayerControllerPlayModeTests
- Reloader.NPCs.Tests.PlayMode.DialogueConversationModePlayModeTests
```

Expected: FAIL because the current implementation snaps immediately and lacks explicit gradual-rotation assertions.

### Task 3: Implement player dialogue camera smoothing

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerLookController.cs`

**Step 1: Write the minimal implementation**

- Add serialized dialogue focus rotation speeds
- Apply smoothed yaw/pitch convergence while a focus target override is active
- Let the focus-target override continue updating while the dialogue overlay/menu-open state is active
- Match the rendered rig pitch direction when aiming at higher/lower NPC face anchors
- Keep standard look input behavior unchanged outside dialogue focus mode

**Step 2: Run tests to verify green**

Run:

```bash
Unity PlayMode tests for:
- Reloader.Player.Tests.PlayMode.PlayerControllerPlayModeTests
- Reloader.NPCs.Tests.PlayMode.DialogueConversationModePlayModeTests
```

Expected: PASS for the new player-facing assertions.

### Task 4: Implement NPC cinematic facing

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/World/NpcDialogueFacingController.cs`

**Step 1: Write the minimal implementation**

- Remove the immediate startup snap in `StartFacing`
- Keep the configured per-frame rotation toward the player target

**Step 2: Run tests to verify green**

Run:

```bash
Unity PlayMode tests for:
- Reloader.NPCs.Tests.PlayMode.DialogueConversationModePlayModeTests
```

Expected: PASS for gradual NPC rotation and exit behavior.

### Task 5: Run focused verification

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/World/DialogueConversationModeController.cs` only if verification exposes integration gaps

**Step 1: Run focused verification**

Run:

```bash
Unity PlayMode tests for:
- Reloader.Player.Tests.PlayMode.PlayerControllerPlayModeTests
- Reloader.NPCs.Tests.PlayMode.DialogueConversationModePlayModeTests
- Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests
```

**Step 2: Fix any integration gaps and rerun the affected suites**

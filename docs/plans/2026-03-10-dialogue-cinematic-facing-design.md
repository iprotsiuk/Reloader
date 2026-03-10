# Dialogue Cinematic Facing Design

**Date:** 2026-03-10

## Goal

Make NPC conversations feel cinematic without changing the global player rig:

- the player's camera should lock onto the NPC's face/head with correct pitch
- the camera should rotate into that framing smoothly instead of snapping
- the NPC should turn toward the player smoothly during dialogue instead of instantly snapping

## Problem Summary

The current dialogue flow already resolves a speaker-side focus anchor and already locks the player into conversation mode, but two rough edges remain:

- `PlayerLookController` snaps directly to the focus target every frame, so dialogue entry feels abrupt
- `NpcDialogueFacingController` does a fast initial rotation burst on `StartFacing`, so the speaker effectively snaps toward the player

The reported "wrong pitch" issue is most likely not a missing NPC head anchor. Current civilian runtime already resolves a presentation-driven face anchor. The remaining issue is that the player-side override path needs to use the live focus target with a dialogue-specific cinematic rotation path.

## Constraints

- Do not change global player height or the default rig contract
- Keep the existing dialogue runtime stack intact
- Keep the fix dialogue-scoped, not a broad camera-system rewrite
- Preserve current movement lock and cursor unlock behavior while dialogue is active

## Options

### 1. Recommended: Smooth the existing dialogue focus path

- keep the current speaker anchor resolution
- add smoothing parameters to the player focus override path
- remove the startup snap from NPC dialogue facing

Pros:
- lowest risk
- local to dialogue behavior
- directly fixes pitch + cinematic feel

Cons:
- less flexible than a dedicated cinematic camera state

### 2. Add a dedicated dialogue camera rig/state

Pros:
- more control over framing later

Cons:
- heavier
- higher regression risk for the current first-person camera stack

## Recommendation

Use option 1.

The current system already has the right seams: `DialogueConversationModeController` toggles conversation mode, the NPC presentation provider resolves the face anchor, and `PlayerLookController` owns the yaw/pitch math. The smallest correct change is to make that dialogue override move smoothly and to let NPC dialogue-facing ease in naturally.

## Approved Design

### Player camera

- Extend `PlayerLookController` with dialogue-focus smoothing for the focus-target override path
- Compute desired yaw/pitch from the actual camera pivot origin toward the resolved NPC face anchor
- Apply the focus override before the normal menu-open suppression so dialogue framing keeps working while the overlay is visible
- Use exponential smoothing (`Mathf.LerpAngle` / `Mathf.Lerp`) for the dialogue-focus yaw/pitch convergence
- Match the actual player rig sign convention so looking down toward a lower NPC produces the correct rendered pitch
- Keep dialogue focus lock active for the full conversation so the camera stays on the NPC's face

### NPC facing

- Keep `NpcDialogueFacingController` as the dialogue-only NPC turning seam
- Remove the immediate startup snap in `StartFacing`
- Let the controller rotate over time at the configured speed so NPCs turn toward the player cinematically

### Testing

- Add a player look controller regression test that proves dialogue focus does not instantly snap to the target
- Add a dialogue conversation play mode test that proves the player camera converges to the correct vertical framing over time
- Add an NPC dialogue-facing test that proves NPCs rotate gradually toward the player, then stop when the conversation ends

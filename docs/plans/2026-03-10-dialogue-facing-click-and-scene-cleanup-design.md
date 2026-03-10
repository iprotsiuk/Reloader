# Dialogue Facing, Click Routing, and MainTown Cleanup Design

**Date:** 2026-03-10

## Goal

Tighten the first-pass civilian dialogue experience in `MainTown` so conversations frame the actual NPC, the speaker visibly acknowledges the player, pointer-based reply selection works in the scene, and the old authored `ContractTarget_Volkov` fixture is removed now that procedural civilians own the contract loop.

## Problem Summary

The current procedural-civilian dialogue slice works, but four rough edges break the intended contract-confirmation flow:

- the player camera focuses a synthetic chest-height point instead of a stable eye/head anchor
- the speaking NPC does not turn toward the player during an active conversation
- mouse click reply submission is broken in the live `MainTown` scene even though keyboard submit works
- `MainTown` still contains the authored `ContractTarget_Volkov` scene fixture and some tests still depend on it

## Constraints

- Keep the existing dialogue runtime stack intact: `DialogueCapability` -> `DialogueRuntimeController` -> `DialogueConversationModeController` -> overlay bridge/controller
- Keep dialogue presentation concerns on the speaker/presentation side, not inside the generic player look math
- Keep UI binders intent-only; fix pointer routing without pushing gameplay state into the binder
- Update the scene/test contract so `MainTown` relies on spawned civilians rather than a legacy authored target actor

## Approach Options

### 1. Patch symptoms in-place

Hardcode higher focus offsets, manually rotate spawned civilians from their spawn component, and add another click workaround in the overlay.

Pros:
- fastest short-term

Cons:
- brittle
- duplicates logic across systems
- still leaves scene wiring drift

### 2. Presentation-driven conversation polish

Use the active civilian visual to resolve a real dialogue focus anchor, add a dedicated NPC dialogue-facing component driven by conversation mode, fix the `MainTown` UI input wiring, and remove the authored target fixture.

Pros:
- keeps responsibilities clean
- solves elevation correctly
- gives a reusable seam for future civilian dialogue polish
- addresses the live click bug at the scene wiring level

Cons:
- touches runtime, scene, and tests together

### 3. Expand the dialogue runtime into a full staging system

Teach the dialogue runtime to own speaker anchors, body rotation, cinematic framing, and full scene UI input assumptions.

Pros:
- future-proof long term

Cons:
- too large for this slice
- over-builds a starter conversation feature

## Recommendation

Use approach 2.

It fixes the actual bugs without widening the runtime model unnecessarily. The player look controller already supports proper vertical focus, so the right move is better speaker presentation data plus a small conversation-facing seam. The click failure should be fixed at the `MainTown` UI input/module wiring first, then hardened in the dialogue overlay binder so pointer interaction is stable after the scene fix.

## Approved Design

### 1. Speaker presentation and facing

- `MainTownNpcAppearanceApplicator` exposes the best available dialogue focus anchor from the currently active visual hierarchy, preferring a live eye/head object when present
- `MainTownPopulationSpawnedCivilian` uses that resolved anchor for dialogue presentation and only falls back to the synthetic focus target when no visual anchor can be found
- `DialogueConversationModeController` owns active-conversation facing orchestration
- a small NPC-side controller rotates the speaker root on the Y axis toward the player while dialogue is active, then releases cleanly when the conversation ends

### 2. Pointer reply routing

- `MainTown` scene `EventSystem` is rebound so `InputSystemUIInputModule` uses the current `_Project` input actions asset, matching the player input setup already used elsewhere in the scene
- add a scene-level regression that proves the UI module is wired to the current actions asset and has point/left-click actions configured
- harden `DialogueOverlayViewBinder` so reply submission uses explicit click handling and avoids hover-driven rebuild churn right before submission

### 3. MainTown cleanup

- remove `ContractTarget_Volkov` from `MainTown`
- update edit/play mode tests that still assume an authored wrong target to use other non-target NPCs instead
- keep the existing procedural contract slice as the only player-facing target flow in `MainTown`

## Testing Strategy

- add red tests first for NPC facing, eye-level focus selection, click routing, and scene cleanup
- verify the specific focused suites for NPC runtime/playmode, UI dialogue overlay, and `MainTown` world slice
- run the repo guardrail scripts touched by this cross-domain work before claiming completion

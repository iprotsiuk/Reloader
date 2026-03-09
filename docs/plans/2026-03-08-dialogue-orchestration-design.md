# Dialogue Orchestration Design

> **Prerequisites:** Read [../design/core-architecture.md](../design/core-architecture.md), [../design/npcs-and-quests.md](../design/npcs-and-quests.md), and [../design/extensible-development-contracts.md](../design/extensible-development-contracts.md) first.

---

## Goal

Finish the dialogue foundation so conversations work like Fallout: New Vegas in the ways that matter for `Reloader`:
- one compact live-world conversation UI
- one shared camera/input conversation mode
- one unified runtime path whether the player starts the talk or a nearby NPC starts it
- one orchestration seam that future police, quest, fixer, vendor, and handler flows can reuse

The system should stay non-pausing and systemic, not cinematic.

---

## Problem

The current slice proves the dialogue runtime, overlay, and conversation mode work, but initiation is still fragmented:
- `DialogueCapability` opens conversations directly
- police dialogue was added as an outcome-driven spike
- there is no shared start request contract
- there is no reusable trigger/orchestration layer for NPC-initiated or scripted conversations

If we keep adding starts ad hoc, we will end up with multiple half-authoritative entry points and inconsistent focus/input teardown rules.

---

## Recommended Approach

Add a thin shared dialogue orchestration layer above the existing runtime.

Keep the current runtime/UI/focus stack:
- `DialogueRuntimeController`
- `DialogueConversationModeController`
- `DialogueRuntimeOverlayBridge`
- `DialogueOverlayController`
- `DialogueDefinition`

Add a unified start path:
- `DialogueOrchestrator`
- `DialogueStartRequest`
- `DialogueStartResult`

Add small initiator seams:
- player interaction still routes through the orchestrator
- NPC/world logic can also submit start requests through the same orchestrator
- one starter component for nearby/proximity-driven conversations
- one starter helper for future scripted/quest-driven conversations

This gives maximum unification without jumping straight into a huge quest/dialogue framework.

---

## Architecture

### 1. Shared orchestrator

`DialogueOrchestrator` becomes the only place allowed to start or deny a conversation.

Responsibilities:
- resolve the player host/runtime
- validate the definition and speaker
- deny or allow starts based on active conversation state
- apply simple interruption policy
- open the shared runtime
- return a structured start result/reason

Initial policy:
- one active conversation at a time
- deny overlapping requests by default
- allow explicit forced interruption later, but do not ship complex priority logic now

### 2. Start request contract

Add a structured start request model with:
- source kind: `PlayerInteract`, `NpcInitiated`, `Trigger`, `Script`
- dialogue definition
- speaker transform
- optional payload/context token
- optional interrupt policy

This keeps runtime entry generic instead of baking police or quest assumptions into the controller.

### 3. Existing runtime remains authoritative for active conversation state

`DialogueRuntimeController` should continue to own:
- active conversation
- selected reply
- confirmation outcomes
- conversation close behavior

The orchestrator starts conversations. The runtime owns them once started.

### 4. Conversation mode stays unified

Whether the player talked first or an NPC nearby initiated:
- movement locks
- look locks
- focus target override
- cursor unlock
- overlay rendering

must all be driven by the same current active runtime conversation.

No special police-only or trigger-only focus paths.

---

## Initiation Model

### Player-started

Current `DialogueCapability` should stop opening the runtime directly.

Instead it should:
- build a `DialogueStartRequest`
- send it to `DialogueOrchestrator`
- return the orchestrator result through the existing NPC action execution path

### NPC-started nearby

Add a small initiator seam for autonomous starts, for example:
- `DialogueProximityInitiator`

Responsibilities:
- watch for player proximity / eligibility
- submit a start request through `DialogueOrchestrator`
- optionally gate repeat attempts with simple cooldown or one-shot behavior

This is the first step toward police stops, ambient NPC calls, or quest beats, but it stays generic.

### Script/quest-started

Add a minimal helper API for future runtime systems, for example:
- `DialogueScriptStarter`

This does not need full quest integration now. It only needs a clean seam so future quest systems do not bypass the orchestrator.

---

## UI / Camera / Input Contract

The interaction feel should remain:
- NPC line on top
- replies on bottom
- no game pause
- world simulation continues
- camera held on the active speaker
- `W/S` cycles replies
- `E` confirms
- mouse hover/click works

Unified rule:
- the overlay should not care how the conversation started
- the conversation mode should not care how the conversation started

The only state that matters is the active conversation on the shared runtime.

---

## Data Model

Keep the current node-based data model:
- `DialogueDefinition`
- `DialogueNodeDefinition`
- `DialogueReplyDefinition`

Do not add full quest gating or branching condition systems in this slice.

But the orchestration model must leave room for:
- multi-step conversations
- reply conditions
- domain-specific outcomes
- scripted start reasons
- future interruption policies

That means request/result objects should already be structured, not raw string plumbing.

---

## Acceptance Scope

Ship now:
- unified dialogue orchestrator
- current player talk path rerouted through it
- one generic nearby NPC initiation component
- one scripted-start helper seam
- tests proving player-started and NPC-started talks use the same runtime/focus/UI path
- docs updated to make orchestration the canonical dialogue entrypoint

Do not ship now:
- full police stop system
- full quest dialogue system
- deep interruption/priority trees
- cooldown authoring tools beyond one small generic behavior
- branching-condition authoring

---

## Testing Strategy

### EditMode
- start request validation
- overlap denial behavior
- interruption policy defaults
- player and NPC initiation both flow into the same runtime

### PlayMode
- player interaction opens dialogue through the orchestrator
- nearby NPC/proximity initiator opens dialogue through the same runtime
- camera/input/focus behavior is identical for both start sources
- terminal reply still closes correctly and preserves structured outcome behavior

---

## Recommendation

The right next move is not “more police.” It is finishing dialogue as a reusable world system.

The smallest correct slice is:
- one unified orchestrator
- one shared runtime
- one shared overlay
- one shared conversation mode
- multiple start sources feeding the same path

That is the cleanest route to future police stops, quest conversations, vendors, fixers, and FNV-style social flow without rewriting the foundation.

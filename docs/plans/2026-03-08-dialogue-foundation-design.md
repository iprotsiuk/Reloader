# Dialogue Foundation Design

> **Prerequisites:** Read [../design/core-architecture.md](../design/core-architecture.md), [../design/npcs-and-quests.md](../design/npcs-and-quests.md), and [../design/extensible-development-contracts.md](../design/extensible-development-contracts.md) first.

---

## Goal

Add an extendable dialogue foundation that supports live-world NPC conversations without pausing the game.

The first shipped slice should feel like a compact first-person conversation overlay:
- NPC phrase at the top
- player replies at the bottom
- player movement/look inputs locked while talking
- camera held on the NPC
- world simulation remains live
- replies selectable with `W/S + E` or mouse

This slice must be structured so police stops, vendors, fixers, and later multi-step conversations can expand on the same runtime instead of replacing it.

---

## Recommended Architecture

Use a hybrid approach:
- keep the existing NPC action/capability seam
- route dialogue-capable actions into a shared dialogue runtime
- render a dedicated non-pausing overlay through the UI Toolkit runtime bridge

Why this shape:
- preserves the current `NpcAgent -> action -> interaction controller` flow
- avoids hard-coding conversation logic inside individual capabilities
- gives police/vendor/fixer conversations a shared foundation
- lets v1 ship as a one-node conversation while still supporting multi-step branching later

---

## Runtime Model [v0.1]

### Entry point

`DialogueCapability` remains the NPC-facing interaction capability.

It should no longer just echo string payloads. Instead, it should:
- reference a `DialogueDefinition`
- provide a `Talk` action when a valid definition exists
- request the shared dialogue runtime to open that definition for the current NPC

### Shared dialogue runtime

Add a `DialogueRuntimeController` that owns:
- active conversation state
- selected reply index
- active speaker target transform
- open/close lifecycle
- structured outcome result after reply selection

The runtime is responsible for:
- opening the conversation
- exposing render state for UI
- handling reply navigation and confirmation
- closing cleanly when the conversation ends or becomes invalid

### Conversation mode

When dialogue is active:
- gameplay does not pause
- player movement input is locked
- player look input is locked
- camera focus is held on the NPC
- mouse cursor is visible and unlocked
- replies can be selected by keyboard or mouse

The camera behavior should be owned through a dedicated seam, not buried inside the UI binder.

### Failure / close conditions

Dialogue should close cleanly when:
- player confirms a terminal reply
- active NPC target becomes invalid
- active NPC is disabled or destroyed
- future distance/LOS rules invalidate the conversation

v0.1 does not need complex interruption rules yet, but the runtime should centralize close behavior so future police/vendor edge cases are handled in one place.

---

## Data Model [v0.1]

Add data assets that model node-based dialogue from day one:

### `DialogueDefinition`
- `dialogueId`
- `entryNodeId`
- `nodes[]`

### `DialogueNodeDefinition`
- `nodeId`
- `speakerText`
- `replies[]`

### `DialogueReplyDefinition`
- `replyId`
- `replyText`
- `nextNodeId`
- `outcomeActionId`
- `outcomePayload`

v0.1 usage rule:
- first shipped conversations use a single node
- `nextNodeId` may stay empty in initial content

This keeps the first slice small while preserving a clean expansion path to authored branching characters later.

---

## UI Shape [v0.1]

Add a dedicated dialogue overlay screen in UI Toolkit.

Required render pieces:
- speaker line label
- reply list
- selected reply highlight
- optional NPC name label if available through future content

Interaction rules:
- `W` moves selection up
- `S` moves selection down
- `E` confirms selected reply
- mouse hover can update the selection
- mouse click confirms a hovered reply

The overlay should be compact and readable, not a full RPG conversation menu.

---

## Camera and Input Contract [v0.1]

Dialogue requires a small but explicit player-state contract:

### Input lock behavior
- lock movement input
- lock look input
- keep dialogue navigation input active
- show/unlock mouse cursor

### Camera focus behavior
- hold the view on the active NPC talk target
- do not hand control back to free look until dialogue closes

The dialogue runtime should request this state through dedicated controller seams so police conversations and other future focused interactions can reuse the same mode.

---

## Integration Path [v0.1]

### Existing seams to preserve
- `NpcAgent`
- `INpcActionProvider`
- `INpcActionExecutor`
- `PlayerNpcInteractionController`
- `PlayerNpcInteractionUiBridge`

### New integration responsibilities
- NPC capability opens dialogue runtime
- runtime publishes current dialogue render state
- UI binder sends reply navigation/selection intents
- controller maps intents into dialogue runtime calls
- runtime emits structured dialogue outcome for future domain hooks

This avoids coupling the dialogue overlay directly to a specific NPC capability implementation.

---

## Extensibility Rules [v0.2+]

This foundation should expand into:
- multi-step branching dialogue
- police stop/question/frisk flows
- vendor and fixer conversations
- reply conditions from reputation, quest, or contract state
- outcome-driven transitions into trade, police escalation, or quest updates

Do not hard-code v0.1 around a one-off civilian talk widget.

The correct mental model is:
- one dialogue runtime
- many dialogue definitions
- many domain systems reacting to dialogue outcomes later

---

## Testing Strategy [v0.1]

### EditMode
- dialogue definition validity
- runtime open/select/close behavior
- reply outcome emission
- invalid active target closes safely

### PlayMode
- interacting with an NPC opens the dialogue overlay
- conversation mode locks movement/look state
- camera focus engages on the NPC
- `W/S/E` navigation works
- mouse hover/click selection works
- confirming a reply closes the overlay and returns a structured result

---

## Recommendation

Ship the first dialogue slice as:
- one shared runtime
- one compact non-pausing overlay
- one-node conversations
- explicit conversation mode for camera/input handling

That is the smallest implementation that still gives the project a durable social-interaction foundation for police, vendors, fixers, and richer authored characters later.

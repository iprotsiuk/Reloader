# Police Stop Dialogue Design

## Goal
Ship the first police-specific dialogue slice that turns the new conversation system into real law-enforcement gameplay.

## Problem
The dialogue foundation currently ends at generic `DialogueOutcome` payloads. Police interaction still uses a stub capability and does not consume dialogue outcomes, so the new conversation stack has no domain-level consequence path yet.

## Recommended Approach
Use the existing shared dialogue runtime and add one authored police-stop conversation plus one small runtime consumer that maps reply outcomes into police heat/escalation behavior.

Why this is the right first slice:
- It validates that dialogue outcomes can drive gameplay, not just UI.
- Police is the highest-value consumer because it affects contracts, escape, and sandbox consequence.
- It stays incremental: one authored conversation, one police capability path, one outcome consumer.

## Slice Scope
Implement now:
- one authored police-stop dialogue asset
- one police capability path that opens that dialogue
- one outcome consumer that handles `comply`, `question`, and `leave/resist`
- one heat/escalation consequence seam connected to current police runtime
- tests for dialogue open + outcome-driven state change

Do not implement yet:
- full pursuit AI
- frisk/inventory search
- arrest/combat resolution
- multi-step branching dialogue
- witness dispatch/search routing

## Runtime Shape
- `LawEnforcementInteractionCapability` becomes the police-facing action executor, not just an action provider.
- It opens a `DialogueDefinition` through `DialogueRuntimeController`, exactly like other talk-capable NPCs.
- A small police dialogue outcome runtime listens for confirmed dialogue outcomes and applies domain behavior.
- Keep the domain consumer separate from the UI and separate from `DialogueRuntimeController`; dialogue remains generic.

## Initial Outcome Contract
Use explicit outcome ids:
- `police.stop.comply`
- `police.stop.question`
- `police.stop.leave`

Initial behavior:
- `comply`: no escalation; police remain in stop state or de-escalate if already calm
- `question`: no immediate escalation; allows flavor and future multi-step extension
- `leave`: raises or preserves police heat and escalates toward search/chase-ready state

## Data / Authoring
- Add one `DialogueDefinition` asset for police stop copy.
- Wire `Npc_Police.prefab` with `LawEnforcementInteractionCapability` using that dialogue asset.
- Keep authored copy compact and systemic, not cinematic.

## Testing
- EditMode: police capability opens dialogue and emits expected outcome ids.
- EditMode or PlayMode: confirmed police outcome maps into the expected heat/escalation state.
- PlayMode: interacting with police opens the shared conversation mode and choosing a reply drives the expected police runtime effect.

## Acceptance
The slice is done when:
- a police NPC opens dialogue through the shared conversation stack
- the chosen reply produces a police-specific outcome id
- that outcome changes police-runtime state in a tested way
- docs reflect the new police/dialogue contract

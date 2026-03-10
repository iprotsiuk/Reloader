# Procedural Civilian Dialogue Design

## Goal

Let the player talk to any spawned procedural civilian and confirm that civilian's public name in-world.

## Scope

- Every spawned procedural civilian in `MainTown` should expose a `Talk` interaction.
- The first slice only needs a lightweight identity exchange.
- The dialogue should be rebuilt from persisted civilian identity at runtime and should not add new save-schema fields.

## Approach

Use the existing dialogue runtime and generate a minimal `DialogueDefinition` in memory for each spawned civilian.

Why this approach:

- avoids authored-asset explosion for procedural NPCs
- keeps the conversation content tied to the persisted civilian identity already used by contracts
- automatically works for Monday replacements because the definition is regenerated from the replacement record

## Runtime Behavior

- `CivilianPopulationRuntimeBridge` already creates/initializes each spawned civilian actor.
- During actor setup, the bridge should also ensure a `DialogueCapability`.
- The bridge should generate a one-node `DialogueDefinition` from the current civilian record and assign it to the capability before `NpcAgent.InitializeCapabilities()`.
- The generated line should confirm identity, for example:
  - `I'm Sonya Novak.`
  - `Name's Derek Mullen.`
- The reply side can stay minimal, for example a single `Alright.` / `Later.` response that closes the conversation.

## Data Source

- Civilian identity comes from `CivilianPopulationRecord` / `MainTownPopulationSpawnedCivilian`.
- Do not parse GameObject names.
- Do not persist generated dialogue data.

## Extensibility

The first slice should keep the generator separate enough that later we can add:

- pool-specific tone (`cop`, `drifter`, `quarry worker`)
- optional nickname mentions
- extra opener variants
- follow-up questions beyond identity

without replacing the runtime plumbing.

## Testing

Add focused EditMode coverage that proves:

- spawned civilians get a `DialogueCapability`
- the generated definition is valid
- the generated opening line contains the current civilian's public name
- a replacement civilian would therefore surface the replacement name when rebuilt


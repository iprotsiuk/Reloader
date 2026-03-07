# First Contract Target NPC Vertical Slice Design

## Goal

Ship the first playable assassination-contract slice by centering the loop on one real target NPC in `MainTown`, with contract intake in TAB UI, target identification in the world, and payout gated by police heat resolution.

This slice proves the post-pivot fantasy without requiring a full civilian spawn system, full procedural population, or handler NPC interactions.

---

## Decision Summary

### Recommended direction

Adopt a `Contracts TAB + Target NPC` slice:

- accept one contract from the TAB menu instead of the world
- rename TAB `Quests` to `Contracts`
- keep `Device` as the field-prep tab for target marking and validation notes
- back the contract with one reusable target NPC prefab in `MainTown`
- complete the contract only after the correct target dies and police heat clears

### Why this direction wins

- It removes an unnecessary handler-NPC interaction surface from the first slice.
- It proves the player fantasy with a real human target instead of a dummy lane target.
- It keeps the world-runtime scope small enough to ship quickly.
- It leaves a clean future path for handler NPCs, portraits, and broader procedural contracts.

### Rejected directions

- `Contract system first, NPC later`: risks building mission logic around faceless targets and then retrofitting identity.
- `Full ambient NPC spawner first`: too broad for the first playable slice.
- `Portrait/screenshot first`: useful later, but not worth blocking the first working contract loop.

---

## Player Flow

1. Open TAB and switch to `Contracts`.
2. Review one available contract briefing.
3. Accept the contract.
4. Read target identity cues:
   - alias or generated name
   - appearance summary
   - target archetype
   - likely location or exposure window
   - payout and distance band
5. Travel in `MainTown`, observe the target, and optionally use the `Device` tab to mark them.
6. Kill the correct target.
7. If the kill is exposed, survive the police search window.
8. Receive payout once heat clears.

Wrong-target kills fail the contract and still raise police heat.

---

## UI Boundary

### Contracts tab

The existing TAB `Quests` section becomes `Contracts`.

First-slice content:

- one available contract card
- accept button
- active contract summary
- contract result state

The `Contracts` tab is the only contract intake surface for this slice. No handler NPC is required yet.

### Device tab

The existing `Device` tab remains the field-prep surface:

- marked target
- validation shots
- spread / MOA
- logged groups

The device is not the contract inbox. It supports execution after the player has accepted a contract.

---

## World Runtime Boundary

The world side stays intentionally narrow:

- one reusable target NPC prefab built on the existing NPC shell
- one stable `targetId`
- one simple authored routine or exposure loop in `MainTown`
- one contract bridge that activates or binds that target when the contract is accepted

No generic civilian population system ships in this slice.

The target runtime should be reusable for later contract generation, but the first implementation only needs one contract target.

---

## Target Identity Model

The first slice needs target identity, not just target existence.

The contract should carry:

- `contractId`
- `targetId`
- `displayName`
- `appearanceSummary`
- `distanceBand`
- `payout`
- `archetype`

The first slice should use `text briefing only`.

Portraits or screenshots are explicitly deferred until:

- appearance presets are stable
- target prefab rigging is stable
- camera framing and persistence rules are clear

---

## Character Content Strategy

Use the local `STYLE_CharacterCustomizationKit` as a content source, but not as a runtime procedural assembly system in this PR.

Recommended first step:

- import the character kit into the Unity project
- create a small curated set of appearance presets
- bind one preset to the first target NPC
- describe that preset in the contract briefing

This avoids first-slice risk around live mesh-part randomization, material setup, animation compatibility, and future portrait capture.

---

## Heat and Payout Rule

The contract does not cash out on kill alone.

Rules:

- Correct target kill marks the kill objective complete.
- If heat is active, payout waits until the heat controller returns to `Clear`.
- If the wrong target dies, the contract fails.
- If the target survives, the contract stays unresolved or fails depending on the active contract state.

For this slice, payout gating is required.

Arrest/death confiscation and respawn consequences remain part of the larger vision, but they are not required to prove this first playable contract loop.

---

## Testing Boundary

### EditMode

- contract runtime lifecycle
- contract identity validation
- target kill matching against active `targetId`
- payout gating after heat clears

### PlayMode

- TAB `Contracts` section renders contract state and accepts the contract
- accepting a contract activates or binds the target NPC
- killing the correct target completes the contract
- killing the wrong target fails it
- police heat blocks payout until search clears
- `MainTown` keeps the required authored contract anchors and target wiring

---

## PR / Progress Workflow

The branch should use small commits and an early non-draft PR to `main`.

Required process:

- commit docs first
- open PR immediately after docs/plan land
- tag `@codex`
- keep a progress doc updated as tasks are completed
- commit runtime work in narrow, reviewable checkpoints

This slice should optimize for visible progress and fast external review, not for hiding unfinished work until the end.

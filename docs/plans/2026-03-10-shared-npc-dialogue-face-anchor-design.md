# Shared NPC Dialogue Face Anchor Design

## Goal

Replace the current runtime-derived dialogue look target with one shared fixed face point per STYLE rig archetype so player dialogue framing aims at a single point inside the NPC head, approximately nose / center-face level.

## Problem

The current dialogue focus anchor is still too approximate:

- it has been derived from runtime head-bone math rather than a fixed authored point
- repeated tuning passes have made the framing unstable and hard to reason about
- the live dialogue target has sometimes landed in front of the face instead of inside the head volume
- the same issue affects both procedural civilians and authored vendors because they share the same STYLE presentation rigs

This makes dialogue framing feel wrong at close range and on elevation changes, even when the conversation is otherwise functioning.

## Decision

Use one shared local-space dialogue face point for each supported STYLE presentation root:

- `StyleMaleRoot`
- `StyleFemaleRoot`

These shared points become the source of truth for all dialogue-capable NPCs using `MainTownNpcAppearanceApplicator`, including:

- procedural civilians
- shop vendors
- other authored NPCs that use the same STYLE rig layout

The point should be treated as:

- a single point in space
- inside the head volume
- near nose / center-face level
- not projected forward from the face

## Architecture

### Appearance layer ownership

`MainTownNpcAppearanceApplicator` should own dialogue face anchor resolution because it already owns the STYLE presentation root selection and visual application.

For known STYLE roots:

- resolve the active presentation root
- choose the correct shared local face point for male or female presentation
- place/update a runtime `DialogueFaceAnchorRuntime` child at that exact local position
- return that transform as the dialogue focus target

For unknown or unsupported layouts:

- keep the existing generic bounds fallback

This preserves a safe fallback while making the common path explicit and stable.

### Shared point model

The anchor data should be stored as serialized local-space vectors on `MainTownNpcAppearanceApplicator`:

- `_maleDialogueFaceLocalPoint`
- `_femaleDialogueFaceLocalPoint`

These values are shared configuration, not per-NPC state. They should be tuned once and then reused everywhere the applicator is used.

This is preferred over head-bone math because:

- tuning becomes deliberate and repeatable
- the target stays a single point rather than a derived zone
- the same point applies to civilians and vendors automatically

## Data Flow

1. NPC spawn or authoring apply chooses an active STYLE root.
2. `MainTownNpcAppearanceApplicator.ResolveDialogueFocusAnchor()` checks whether the active root is `StyleMaleRoot` or `StyleFemaleRoot`.
3. If yes, it creates/updates `DialogueFaceAnchorRuntime` at the archetype’s shared local-space point.
4. `MainTownPopulationSpawnedCivilian` and other dialogue-capable NPCs resolve that transform through the existing presentation provider path.
5. `DialogueCapability` opens conversations using that shared point.
6. `DialogueConversationModeController` and `PlayerLookController` keep using the existing runtime path, but now receive a stable nose/face point instead of a derived head approximation.

## Cleanup Rules

This change should also remove the temporary head-direction/forward-bias logic introduced while debugging:

- no forward projection from `headBone.forward`
- no tests that assert the dialogue anchor sits in front of the head
- no leftover probe-only assertions or temporary investigation helpers

After cleanup, the dialogue anchor logic should be easy to explain:

- known STYLE rig -> fixed shared local face point
- anything else -> generic fallback

## Testing

Required coverage:

- EditMode tests for male shared face point resolution
- EditMode tests for female shared face point resolution
- existing MainTown PlayMode dialogue interaction coverage stays green
- vendor/civilian shared behavior is covered indirectly because both use the same applicator path

Verification should also include the existing docs/context and extensible-contract guardrail scripts.

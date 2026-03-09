# STYLE Demo Crowd Review Design

**Date:** 2026-03-09
**Status:** Approved for implementation

## Goal

Turn `Assets/STYLE - Character Customization Kit/Scene/Demo.unity` into a disposable visual review playground with `100` labeled NPCs so outfit combinations, clipping, silhouette readability, and archetype fit can be judged quickly.

## Chosen Approach

Reuse the imported STYLE male and female rig roots as the source templates, then rebuild the scene into two crowd blocks:

- `PlausibleBatch` with `50` mostly believable archetype-driven variants
- `StressBatch` with `50` deliberately risky combinations

Each NPC will be labeled through its hierarchy name only.

## Why This Approach

- The scene is explicitly non-production, so speed and breadth matter more than authoring purity.
- The imported STYLE rigs already expose modular child parts for hair, beard, tops, bottoms, boots, and body meshes.
- Building a large review crowd in the demo scene is the fastest way to validate which combinations are worth keeping before any runtime generator work.
- Splitting plausible and stress-test groups gives two different QA signals in one pass: what reads well and what breaks.

## Scene Shape

- Keep the demo scene as the working playground.
- Replace the original six-character lineup with a larger review layout.
- Create separate world-space blocks for `PlausibleBatch` and `StressBatch`.
- Use readable grid spacing so the camera can pan through rows without crowd overlap hiding clipping problems.
- Preserve simple lighting/background unless a minimal cleanup is needed for readability.

## Character Composition Rules

### Plausible Batch

- Build around broad style buckets: police, EMS, blue-collar, jogger, hunter, park ranger, hiker, white-collar, student, rough-living/unhoused.
- Keep combinations mostly coherent within each named role.
- Allow some repetition where the pack does not expose enough truly distinct parts.

### Stress Batch

- Deliberately mix louder or less coherent tops, bottoms, hair, and beard choices.
- Favor combinations likely to expose clipping, proportion oddities, or poor silhouette reads.
- Keep the roots named clearly enough that failed combos can be called out by name.

## Naming Contract

- Name every root with the intended read, for example:
  - `Police_Plausible_01`
  - `EMS_Plausible_04`
  - `Hiker_Stress_12`
- Group roots under batch parents so the hierarchy remains scannable.

## Non-Goals

- No production NPC prefab pipeline
- No runtime procedural generator yet
- No gameplay behavior or NPC foundation wiring in this scene
- No final content approval pass for in-game use

## Verification Target

- Scene opens without pink materials or console errors.
- The hierarchy contains `100` clearly named NPC roots split into the two batch parents.
- The scene is visually useful for manual QA of part combinations.

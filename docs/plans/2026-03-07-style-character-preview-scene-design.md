# STYLE Character Preview Scene Design

**Date:** 2026-03-07
**Status:** Approved for implementation

## Goal

Create a project-owned preview scene that showcases several imported STYLE character variants in a neutral lineup so visual quality, variety, and fit with `Reloader` can be evaluated before MainTown population work.

## Chosen Approach

Build a small `_Project` scene that uses the imported STYLE character assets as visuals, but wraps each preview character in the existing NPC shell so the resulting lineup can evolve into real ambient civilians later.

## Why This Approach

- Pure mannequin scenes are faster, but they are throwaway content.
- A project-owned preview scene avoids mutating the third-party demo scene.
- Using `NpcFoundation` preserves the existing architecture and gives us a direct path from preview characters to future town NPCs.
- Keeping the lineup static avoids premature AI/behavior authoring while still validating style, proportions, and variety.

## Scene Shape

- Scene location: `_Project/World/Scenes`
- Character count: `4-6`
- Layout: evenly spaced lineup with one clear player-facing inspection angle
- Presentation: neutral daylight, simple ground, low visual noise
- Runtime behavior: static only for now; no patrols, no schedules, no combat logic

## Character Composition Rules

- Use imported STYLE models as the visible character content.
- Wrap each preview character in the existing NPC foundation prefab pattern.
- Aim for visible silhouette and outfit variation across the lineup:
  - male/female mix where practical
  - different hair/outfit/material combinations where available
  - different color variants where straightforward
- Keep scale and pose readable rather than trying to fully animate the lineup in this slice.

## Deliverables

- One new project-owned preview scene
- Several preview NPC variants placed in the scene
- Minimal validation coverage so scene wiring regressions are detectable
- Progress notes updated in `docs/plans/progress/`

## Non-Goals

- Full MainTown population
- Full ambient NPC behavior
- Contract-target generation
- Final civilian animation set
- Solving all customization/editor tooling for the STYLE kit

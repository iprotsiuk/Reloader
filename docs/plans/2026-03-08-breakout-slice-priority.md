# Breakout Slice Priority

> **Prerequisites:** Read [../design/core-architecture.md](../design/core-architecture.md), [2026-03-08-reloader-breakout-vision-design.md](2026-03-08-reloader-breakout-vision-design.md), [2026-03-08-reloader-breakout-vision-implementation-plan.md](2026-03-08-reloader-breakout-vision-implementation-plan.md), and [2026-03-08-underworld-progression-design.md](2026-03-08-underworld-progression-design.md) first.

---

## Purpose

Pick the single next prototype slice that best improves the breakout fantasy without exploding scope.

Selection rule:
- strongest visible payoff
- strongest prep-to-shot connection
- lowest dependence on missing police/social simulation

---

## Candidate Scorecard

Scoring:
- `Impact` = fantasy payoff
- `Trailer` = screenshot / video readability
- `Cost` = engineering cost where `5` is cheapest
- `Dependency` = independence from missing systems where `5` is safest

| Slice | Impact | Trailer | Cost | Dependency | Read |
|---|---:|---:|---:|---:|---|
| Specialty ammo first slice | 5 | 5 | 3 | 4 | strongest "right round for this exact job" payoff |
| One signature contract modifier | 5 | 5 | 2 | 3 | great fantasy, but can sprawl if it needs new world behavior |
| Underworld progression surface | 4 | 3 | 3 | 4 | valuable framing, but weaker as a first dramatic proof point |
| Target routine / intel upgrade | 4 | 4 | 2 | 2 | strong later, but depends more on schedule/routine support |

---

## Recommendation

Choose **specialty ammo first slice** as the next prototype.

Recommended exact proof point:
- one AP-oriented ammo fantasy
- one contract setup where glass or light cover is the visible problem
- one clear player takeaway: the load choice solved the job

Why this goes first:
- it sharpens the pitch immediately
- it makes prep visibly consequential
- it is easy to explain in one sentence
- it creates a strong trailer moment without requiring broad simulation upgrades

This is the cleanest bridge between the current hardcore-prep strength and the breakout-vision need for stronger mission expression.

---

## What To Delay

Delay for later slices:
- broad underworld progression systems
- multiple specialty ammo families at once
- deep target-routine simulation
- several contract modifiers in parallel

The first breakout slice should prove one memorable fantasy, not open four new fronts.

---

## Suggested Implementation Boundary

Keep the slice narrow:
- one specialty ammo type
- one authored contract problem built around that ammo
- one UI/intel note that explains why the round matters
- one validation seam showing the wrong ammo is a worse solution

Good example:
- target visible behind office glass
- AP-capable round gives the clean solution
- standard round creates a less reliable or noisier outcome

This keeps the slice practical while still making the fantasy legible.

---

## Existing Seams To Reuse

- contract definition and briefing surfaces for the player-facing problem statement
- current contract-policy/restriction text surfaces for communicating mission context
- existing weapon/ammo runtime seams so the first slice extends current prep rather than replacing it
- current MainTown/authored-contract smoke-test style for validating the scenario end to end

The slice should feel like a focused extension of the contract-prep-escape spine, not a parallel prototype.

---

## Why Not Start With Progression

Underworld progression matters, but it is better as the framing around visible job-expression wins.

If progression lands before a strong new contract fantasy, it risks reading as:
- more menus
- more unlocks
- more structure around the same jobs

That is the wrong order for breakout value.

First prove a nastier, more watchable contract problem.
Then build progression around access to more of those moments.

---

## Open Questions

1. Should the first specialty-ammo slice be strictly AP-through-glass, or a broader "light cover penetration" fantasy?
2. Does the first authored scenario belong in `MainTown`, or should it be a dedicated landmark contract setup?
3. How explicit should the contract briefing be about the required ammo solution versus leaving discovery to the player?

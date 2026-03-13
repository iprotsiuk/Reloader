# Combat Feedback And Damage Brainstorm

> **Prerequisites:** Read [../design/core-architecture.md](../design/core-architecture.md), [../design/weapons-and-ballistics.md](../design/weapons-and-ballistics.md), [../design/assassination-contracts.md](../design/assassination-contracts.md), and [2026-03-13-sandbox-pivot-brainstorm-index.md](2026-03-13-sandbox-pivot-brainstorm-index.md) first.

---

## Purpose

Capture shot-feedback, hit-detection, damage-model, and long-shot presentation ideas in a focused combat brainstorm file.

---

## Hit Detection Direction

The game needs more readable and more satisfying shot feedback.

Recommended rule:
- stop relying on a single generic humanoid collision capsule as the meaningful shot target
- use mesh-aligned hitboxes or named body-region colliders for real shot resolution

Mandatory body regions for first pass:
- `Head`
- `Torso`
- `Arms`
- `Legs`

---

## Damage Model Direction

Recommended first-pass damage behavior:
- `Headshot` = highest lethality and strongest chance of immediate kill or incapacitation
- `Torso` = high lethality, but less guaranteed than head
- `Limb hit` = lower lethality and high chance to wound, stagger, or trigger flee/hide behavior instead of instant death

This matters because:
- a limb hit can turn a clean kill into a wounded fleeing target
- a body hit can create a panic scene without instant resolution
- a headshot can remain the most reliable precision payoff

---

## Why Collision Needs To Improve

If the game wants:
- meaningful ammo choice
- long-range precision identity
- target panic and wound states
- high-stakes contract shots

then crude capsule-only NPC collision will undermine all of it.

Players need to feel that:
- headshots are earned
- bad hits are visibly bad hits
- misses near limbs or partial cover failures are believable

---

## Slow-Mo Bullet Camera

The game should have a selective slow-motion bullet camera for especially strong long shots.

Recommended direction:
- add a cinematic bullet-follow / impact-confirm camera inspired by `Sniper Elite`
- reserve it for notable long-range hits, premium kills, or especially clean shots
- use it as reward feedback, not for every routine shot

This should be treated as a major fantasy amplifier, not a gimmick.

Probable implementation direction:
- use `Cinemachine` as the first candidate for the slow-motion bullet camera system

Why it fits:
- camera blending is already a solved problem there
- it should be easier to author a temporary shot-follow camera than to build a full custom cinematic stack first
- it can support clean transition back to player control after the impact moment

---

## Recommendation

Capture these as linked combat priorities:
- improve NPC hit detection from capsule-style simplification toward mesh/body-region hitboxes
- add distinct damage resolution for head, torso, and limbs
- plan for a selective slow-motion bullet camera to sell premium long shots

These are not isolated polish requests.
They directly support the game's precision-shooter identity and make both success and failure more legible.

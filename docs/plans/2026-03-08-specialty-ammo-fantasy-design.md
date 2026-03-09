# Specialty Ammo Fantasy Design

> **Purpose:** Define which ammo fantasies make `Reloader` feel like a criminal prep sandbox instead of a generic caliber checklist.
> **References:** [2026-03-08-reloader-breakout-vision-design.md](2026-03-08-reloader-breakout-vision-design.md), [2026-03-08-reloader-breakout-vision-implementation-plan.md](2026-03-08-reloader-breakout-vision-implementation-plan.md), [../design/assassination-contracts.md](../design/assassination-contracts.md), [../design/prototype-scope.md](../design/prototype-scope.md)

---

## Working Goal

Specialty ammo should create mission-solving identity, not just deeper simulation.

The player fantasy is:
- read the contract
- understand the obstacle
- build or buy the right round
- feel the difference in the shot outcome

If the ammo choice is not visible in contract strategy, it is probably too deep for the current slice.

---

## Ammo Fantasies Worth Keeping

### AP / Glass-Capable Load [v0.1 candidate]

Use case:
- target is behind glass or light cover

Player-facing payoff:
- the player wins because they chose a purpose-built solution

Why it matters:
- easiest specialty ammo pitch
- pairs directly with signature modifier work
- high fantasy value with relatively low content surface

### Match / Precision Load [v0.1 support]

Use case:
- narrow exposure window
- longer shot where consistency matters

Player-facing payoff:
- tighter, cleaner shot solution
- validates careful prep and confidence in the load

Why it matters:
- makes ammo quality feel concrete
- supports the existing contract-solving identity

### Low-Penetration / Crowd-Safe Load [v0.2]

Use case:
- public-space contract with collateral risk

Player-facing payoff:
- cleaner solve in witness-dense spaces

Why it matters:
- expands the sandbox beyond "always use the strongest round"
- gives contract modifiers and ammo fantasy a two-way relationship

### Subsonic / Low-Signature Load [v0.2 or later]

Use case:
- stealthier jobs, suppressor-focused setups, lower-signature shots

Player-facing payoff:
- lower immediate chaos in the aftermath when used well

Why it matters:
- strong fantasy, but depends on more mature stealth/response systems

---

## Scope Split

### Keep in v0.1 discussion

- AP / glass-capable load as the first distinct ammo fantasy
- match load language as a supporting quality fantasy

### Defer to later

- low-penetration anti-collateral rounds
- subsonic and deep suppression behavior
- broad ammo family taxonomy across many calibers

The current risk is turning ammo into a catalog before it becomes a dramatic decision.

---

## Recommended First Slice

### Slice

`AP / glass-capable ammo for a behind-glass contract`

### Rule

- ordinary ammo is unreliable or disadvantaged when the shot must pass through glass first
- AP-capable ammo preserves enough shot authority to make the intended solve practical

### User-visible payoff

- contract briefing clearly hints at the obstacle
- player can deliberately choose AP as the answer
- the shot result visibly rewards correct prep and punishes lazy prep

### Test seam

The first implementation should be verifiable at three levels:
- contract/readability: the modifier clearly tells the player what problem exists
- ammo behavior: glass interaction changes outcome between standard and AP-capable loads
- mission payoff: the right ammo materially improves success odds on the authored scenario

---

## Design Guardrails

- Do not make AP a flat rarity upgrade for every job.
- Do not add multiple specialty ammo families at once.
- Do not let the first ammo slice become a full armor and material simulation project.
- Keep the first payoff obvious enough that players can explain it after one failed and one successful attempt.

---

## Discussion Notes

The strongest version of specialty ammo in `Reloader` is not:
- more calibers
- more stat lines
- more manufacturing trivia

The strongest version is:
- "this contract made me choose a different round"

That is the bar the first ammo slice should clear.

---

## Open Questions

- Should AP be a handcrafted premium round the player assembles, a rare store option, or both?
- Is match ammo a separate named fantasy, or just the visible top end of the existing ammo-quality system?
- Does the first AP slice need real cover penetration beyond glass, or is "glass first" enough to prove the concept?

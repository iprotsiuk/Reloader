# Signature Contract Modifiers Design

> **Purpose:** Narrow the breakout-vision discussion into a small set of contract modifiers that make `Reloader` easier to pitch, more watchable, and more dependent on prep choices.
> **References:** [2026-03-08-reloader-breakout-vision-design.md](2026-03-08-reloader-breakout-vision-design.md), [2026-03-08-reloader-breakout-vision-implementation-plan.md](2026-03-08-reloader-breakout-vision-implementation-plan.md), [../design/assassination-contracts.md](../design/assassination-contracts.md)

---

## Working Goal

Contract modifiers should change how the player solves a job, not just how the job is described.

Good modifiers for the next slice should be:
- readable in one sentence
- visible in screenshots or trailer beats
- tied to rifle/ammo/optic/prep choices
- implementable through existing contract, target, and police seams

---

## Shortlist

### 1. Behind Glass

- Readability: very high
- Trailer value: very high
- Prep leverage: very high
- Existing-system fit: high

Player-facing read:
- the target is visible, but standard shots may fail or underperform because the line passes through glass first

Prep pressure:
- pushes AP or glass-capable ammo choice
- makes angle and material awareness matter

Failure shape:
- wrong ammo gives a weak hit, deflection, or botched kill window
- player may need a risky follow-up shot and escape

### 2. Narrow Exposure Window

- Readability: high
- Trailer value: high
- Prep leverage: high
- Existing-system fit: high

Player-facing read:
- the target appears only briefly at a balcony, window, walkway, or routine stop

Prep pressure:
- rewards stable zero, sight picture confidence, and match-grade ammo
- makes careful prep and validation feel justified before the contract

Failure shape:
- rushed shot misses
- delayed shot loses the window and extends the job into a more dangerous loop

### 3. No Civilian Casualties

- Readability: high
- Trailer value: medium-high
- Prep leverage: high
- Existing-system fit: medium

Player-facing read:
- the target is in a public or witness-dense space and collateral damage fails or heavily botches the contract

Prep pressure:
- pushes cleaner angles, patience, and low-penetration ammo fantasy later
- makes target isolation and shot discipline part of the prep problem

Failure shape:
- collateral hit fails the contract or crushes payout
- police response escalates faster

### 4. Minimum Shot Distance

- Readability: medium-high
- Trailer value: medium
- Prep leverage: high
- Existing-system fit: high

Player-facing read:
- the fixer demands a distant kill, forcing a real sniper solve instead of a walk-up fallback

Prep pressure:
- rewards optic choice, zeroing, and ballistic confidence

Failure shape:
- player breaks the rule by taking an easy close shot
- player takes the long shot unprepared and misses

### 5. Protected Zone Deadline

- Readability: medium
- Trailer value: medium-high
- Prep leverage: medium
- Existing-system fit: medium

Player-facing read:
- kill the target before they enter a protected building, convoy, or guarded area

Prep pressure:
- pushes route reading and timing more than gear

Failure shape:
- missed window forces abort or much riskier follow-up

### 6. Bodyguarded Target

- Readability: medium
- Trailer value: medium
- Prep leverage: medium
- Existing-system fit: medium

Player-facing read:
- target is surrounded by moving protectors or cover breaks

Prep pressure:
- favors patience, angle hunting, and cleaner openings

Failure shape:
- target ducks behind cover
- witness and search pressure spike after a bad opening shot

### 7. Suppression Preferred

- Readability: medium
- Trailer value: medium
- Prep leverage: medium-high
- Existing-system fit: low for v0.1

Player-facing read:
- the contract strongly favors a low-signature solution

Prep pressure:
- future-facing hook for subsonic and suppressor fantasies

Failure shape:
- loud shot causes a much hotter aftermath

---

## Recommended Top 3

### 1. Behind Glass

Why it stands out:
- instantly legible
- strongly reinforces specialty ammo fantasy
- creates a memorable "I brought the right round" moment

### 2. Narrow Exposure Window

Why it stands out:
- validates the whole precision-prep loop
- creates strong streamable tension without needing new world simulation
- pairs cleanly with prep validation, optic choice, and match ammo

### 3. No Civilian Casualties

Why it stands out:
- broadens the sandbox from pure marksmanship into judgment and discipline
- creates stronger moral/tactical pressure than a generic payout modifier

---

## First Demo Candidate

Recommended first implementation:
- `Behind Glass`

Why first:
- best single-sentence hook
- strongest connection to ammo fantasy
- easiest to market as a signature `Reloader` problem instead of generic contract flavor

Expected player payoff:
- player reads the contract and immediately understands that prep matters
- AP feels like a mission-solving tool, not a flat stat upgrade
- wrong loadout produces a visible, memorable mistake

---

## Practical Scope Notes

Keep the first modifier slice narrow:
- one authored material case
- one contract tag or modifier callout
- one obvious user-visible consequence

Do not expand the first slice into:
- broad destructible-material simulation
- many glass types
- a full collateral rules framework
- generic modifier combinatorics

---

## Open Questions

- Should `Behind Glass` be framed as a hard requirement or as a payout/risk warning that skilled players can still brute-force badly?
- Should `No Civilian Casualties` be an immediate fail condition, or a strong payout/heat penalty first?
- Does the first modifier need unique UI language in the contract panel, or is strong briefing copy enough for the first pass?

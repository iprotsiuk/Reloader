# Reloader Breakout Vision Design

> **Prerequisites:** Read [../design/core-architecture.md](../design/core-architecture.md), [../design/assassination-contracts.md](../design/assassination-contracts.md), [../design/law-enforcement.md](../design/law-enforcement.md), [../design/prototype-scope.md](../design/prototype-scope.md), and [../design/v0.1-demo-status-and-milestones.md](../design/v0.1-demo-status-and-milestones.md) first.

---

## Goal

Pressure-test the current `Reloader` fantasy against what tends to break out on Steam, then sharpen the project into a stronger commercial identity before the sandbox hardens around weaker assumptions.

This document is not a milestone spec. It is a discussion document for deciding whether the current core loop is strong enough, what it is missing, and which additions would make the game more watchable, more marketable, and more replayable.

---

## Current Read

The current fantasy is good:
- assassination sandbox
- hardcore prep
- reloading and ammo quality
- long-range shooting
- police escape

The weakness is not that the idea is bad. The weakness is that it can easily drift into a "respectable gun nerd sim" instead of a "dirty, stylish criminal sandbox with obsessive prep."

That distinction matters commercially.

The game should not sell itself as:
- firearm realism
- ballistics hobby software
- reloading sim first

It should sell itself as:
- a criminal sniper sandbox
- where hardcore prep is what makes the jobs feel earned

The prep is the amplifier, not the headline.

---

## Steam Breakout Read

The current Steam environment still rewards games that are:
- easy to pitch in one sentence
- easy to understand visually in a trailer or screenshot
- mechanically rich enough to create stories
- satisfying to watch on streams
- strong in fantasy and tone, not just in correctness

Observed signals relevant to `Reloader`:
- `Schedule I` broke out because its fantasy is instantly legible and its work loop is tactile, expressive, and social-chaos-friendly.
- Steam tag/discovery guidance still reinforces that games need a readable identity, not a blurry feature pile.
- Wishlists and demos still matter, but they only help if the game already has a strong commercial fantasy.

Implication for `Reloader`:
- deeper systems are not enough
- the game needs stronger moments, stronger tone, and more visible job variety

---

## Product Thesis

Recommended thesis:

`Reloader` should become a first-person assassination sandbox where the player earns underworld money by solving expressive sniper jobs through prep, ammo choice, positioning, and escape discipline.

The prep fantasy should stay hardcore.
The presentation and mission structure should stay fun, legible, and dramatic.

The target player fantasy is:
- get the contract
- study the problem
- build the round
- choose the weapon and optic
- find the angle
- take the shot
- survive the fallout
- upgrade the workshop, gear, and underworld access

That is a much stronger pitch than "realistic reloading sim with contracts."

---

## What Already Feels Strong

- The contract loop is a strong economy spine.
- Long-range execution gives the game a clean signature fantasy.
- Ammo quality and prep create meaningful ownership over success.
- Police aftermath creates consequence and tension.
- The workshop/reloading stack gives the player a strong pre-mission ritual.

These are real strengths. The vision should build on them, not replace them.

---

## What Feels Missing

### 1. Stronger job expression

Right now the fantasy can collapse into "same mission, different target."

The game needs jobs that materially change the solution:
- target behind glass
- target in transit
- target visible only in a narrow exposure window
- target in a witness-dense public space
- target where overpenetration is risky
- target where suppression matters
- target where armor or cover changes ammo choice

The AP-through-glass example is exactly the right kind of differentiator.

### 2. More tactile prep payoff

The prep loop must feel satisfying even before the shot.

The player should feel:
- "I brought the right round for this job"
- "I built this setup for this exact problem"
- "This clean hit happened because I prepared properly"

If the prep is mechanically correct but emotionally flat, the fantasy will underperform.

### 3. A stronger underworld fantasy

The player needs to feel like they are climbing through a criminal ecosystem, not just checking mission tickets.

Potential progression pillars:
- better workshop
- better optics
- access to specialty bullets and components
- better intel sources
- better firing positions and staging tools
- fixers or handlers unlocking nastier jobs

### 4. Stronger tone and personality

The game should not feel sterile.

It needs:
- memorable contract language
- dirtier underworld flavor
- stronger regional identity in `MainTown`
- visually distinct targets and situations
- more personality in intel, handlers, vendors, and world spaces

### 5. Better watchability

The biggest commercial risk is becoming impressive but dull to watch.

The game needs moments that create stories:
- the AP shot that punches through office glass
- the perfect clean hit during a tiny balcony exposure
- the botched contract that turns into a desperate escape
- the wrong ammo choice that ruins a premium job
- the target surviving and forcing improvisation

Those are shareable moments.

---

## Recommended Product Pillars

### Pillar 1: Expressive Assassination Sandbox

Contracts should be problems, not just destinations.

The player should ask:
- what is the target situation?
- what is the angle?
- what is the risk?
- what is the right loadout?

### Pillar 2: Hardcore Prep With Immediate Payoff

Reloading, zeroing, optic choice, and ammo selection should be deep enough to matter, but always tied to obvious mission outcomes.

The player should never feel like they are doing homework for no dramatic reason.

### Pillar 3: Criminal Workshop And Underworld Progression

The workshop is not only a crafting room.

It should be:
- a prep sanctuary
- a trophy room of solved jobs
- a progression surface
- the emotional home of the player fantasy

### Pillar 4: Consequences And Escape

The shot is not the end.

Police heat, witnesses, public risk, and failed-clean-hit fallout should turn a successful shot into a tense aftermath loop.

---

## Signature Features Worth Owning

These are the ideas most likely to make `Reloader` feel distinct instead of generically "realistic."

### Specialty ammunition as mission-solving tool

Not just statistical upgrades.

Examples:
- AP for glass or light cover
- low-penetration rounds for crowded environments
- subsonic rounds for low-signature jobs later
- match loads for narrow exposure windows

This is much stronger than generic rarity tiers.

### Contract modifiers that force prep decisions

Examples:
- target behind glass
- no civilian casualties
- minimum shot distance
- target must be killed during a short schedule window
- target must be killed before entering a protected zone

These modifiers make the workshop matter.

### A workshop that feels lived in

The player should gradually turn a shabby criminal hideout into an elite prep space.

This gives long-term emotional progression even outside money.

### Intel that changes the shot

Intel should eventually affect:
- target route
- best vantage points
- witness density
- glass / cover / bodyguard risk
- probable distance

If intel is only flavor text, the sandbox loses depth.

---

## Anti-Goals

The project should explicitly avoid these traps.

### Pure realism for its own sake

If a mechanic is realistic but not legible, tense, or satisfying, it should not be a priority.

### Spreadsheet drift

If the game starts feeling like ballistic bookkeeping instead of criminal preparation, the fantasy weakens.

### Generic procedural contract spam

A large number of contracts is not valuable if they do not create different stories.

### Sterile presentation

A serious tone is fine.
A lifeless tone is not.

### Side-system dilution

Optional hunting, competitions, or auxiliary systems must never steal focus from the contract-prep-escape spine.

---

## Candidate Additions By Impact

### Highest impact

- specialty ammo that changes mission-solving
- more expressive contract modifiers
- stronger underworld progression framing
- stronger target routines and exposure windows
- stronger tone/personality in mission presentation

### Medium impact

- richer intel tools
- cleaner witness / police aftermath
- better workshop visual progression
- more authored landmark contracts that teach the sandbox

### Lower impact for breakout fantasy

- additional calibers before mission variety improves
- ultra-deep simulation layers that the player rarely feels
- optional side modes before the contract fantasy is unmistakable

---

## Proposed North-Star Loop

The strongest future loop looks like this:

1. Receive a contract with a distinct problem.
2. Read intel and identify the real constraint.
3. Build or select the right weapon, optic, and round.
4. Validate only if needed.
5. Travel and set up.
6. Take the shot.
7. Deal with fallout, witnesses, and search pressure.
8. Cash out.
9. Reinvest into workshop, gear, intel access, and nastier jobs.

This loop is commercially stronger than a loop centered mainly on reloading craftsmanship.

---

## Discussion Questions

These are the most important open questions for the project.

1. Should `Reloader` frame itself more like underworld career progression or more like a pure open-ended sandbox?
2. Which specialty ammo fantasies are most exciting and readable for players?
3. How dirty, funny, or stylized should the tone be?
4. How much target routine/social simulation is enough before it stops paying back?
5. Which three contract modifiers best prove the fantasy in a trailer or Steam demo?

---

## Recommendation

Do not pivot away from hardcore prep.
Do pivot the presentation and content priorities toward:
- job variety
- specialty problem-solving
- underworld progression
- stronger tone
- better watchability

The current idea is solid.
What it still needs is swagger, stronger mission expression, and more obvious "I have to use the right round for this exact kill" moments.

That is the version of `Reloader` most likely to pop instead of becoming a niche admiration project.

---

## External Notes

Relevant current-market references used for this discussion:
- Steam store page for `Schedule I`
- SteamDB concurrency history for `Schedule I`
- Steamworks documentation for tags, visibility, and Next Fest
- PC Gamer coverage highlighting tactile/minigame work as part of `Schedule I`'s appeal


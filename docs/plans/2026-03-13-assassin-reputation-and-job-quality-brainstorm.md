# Assassin Reputation And Job Quality Brainstorm

> **Prerequisites:** Read [../design/core-architecture.md](../design/core-architecture.md), [../design/assassination-contracts.md](../design/assassination-contracts.md), [2026-03-08-underworld-progression-design.md](2026-03-08-underworld-progression-design.md), and [2026-03-13-sandbox-pivot-brainstorm-index.md](2026-03-13-sandbox-pivot-brainstorm-index.md) first.

---

## Purpose

Capture a reputation direction for `Reloader` where the player's style of violence affects what kinds of assassin work they attract, what they get paid, and what underworld opportunities remain open.

This should not be a generic morality bar.
It should be a criminal reputation read based on how professionally or messily the player works.

---

## Core Idea

The player should develop an assassin reputation that moves between two rough poles:
- `professional hitman`
- `local butcher / bloodbath operator`

Clean work should push reputation toward the professional side.
Chaotic public massacres should push it toward the butcher side.

This gives the game a way to judge not only whether the player succeeded, but how they succeeded.

---

## Reputation Direction

Recommended reputation signals:

Moves reputation toward `professional`:
- clean kill on the correct target
- low witness exposure
- no raised alarm on contracts that reward stealth
- no collateral deaths
- quick clean escape
- disciplined use of the right tool for the job

Moves reputation toward `butcher`:
- killing large numbers of police
- public bloodbath behavior
- repeated collateral deaths
- loud chaotic follow-up violence after the main shot
- unnecessary civilian harm
- visibly botched jobs that turn into island-wide panic

The low-end fantasy should feel like:
- "that psycho butcher from <islandName>"

The high-end fantasy should feel like:
- a feared professional who can be trusted with harder and cleaner jobs

---

## Design Rule

This reputation should affect assassin work quality, not just flavor text.

It should influence:
- what contracts appear
- who is willing to post jobs to the player
- payout quality
- unlock quality
- how much trust handlers or fixers place in the player

It should not be a pure reward-only system.
The player can absolutely choose to become a feared butcher, but that should steer them into uglier work with worse upside.

---

## Job Quality Effects

Recommended contract outcomes by reputation direction:

`Professional reputation` should trend toward:
- better-paying jobs
- cleaner, more precise contracts
- higher-trust handlers
- better intel
- premium long-range jobs
- specialty opportunities that assume discipline

`Butcher reputation` should trend toward:
- worse-paying jobs
- dirtier contracts
- rougher handlers and lower-status work
- less trust and less pre-job intel
- more disposable or ugly assignments
- fewer prestigious unlocks

This does not mean the low-reputation path has no content.
It means it should feel cheaper, uglier, and less respected.

---

## Why This Helps

This solves a useful progression problem:
- success is not binary
- stealth and discipline become economically meaningful
- random chaos no longer competes evenly with crafted assassination play

It also helps reinforce the product fantasy:
- the player is building a name
- the name can be respected or feared
- the kind of work offered reflects that name

That is stronger than a generic XP ladder because it stays inside the fiction.

---

## Relationship To Existing Systems

This reputation should sit alongside, not replace:
- contract completion state
- police heat and wanted state
- underworld progression and unlocks

Recommended relationship:
- `heat` is the immediate tactical consequence
- `reputation` is the long-term underworld read of how the player operates

So:
- heat asks "how hot is the situation right now?"
- reputation asks "what kind of assassin are you becoming?"

---

## Recommendation

Treat assassin reputation as a style-of-work progression layer.

The first useful version should:
- score jobs on cleanliness versus chaos
- bias future contract pools based on that score
- improve pay and unlock quality for disciplined professional work
- degrade job quality toward low-status butcher work when the player repeatedly turns the island into a bloodbath

This keeps stealth, precision, and controlled violence economically meaningful without forbidding chaos outright.

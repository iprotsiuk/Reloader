# Employment Cover Loop Brainstorm

> **Prerequisites:** Read [../design/core-architecture.md](../design/core-architecture.md), [../design/assassination-contracts.md](../design/assassination-contracts.md), [../design/inventory-and-economy.md](../design/inventory-and-economy.md), [2026-03-08-reloader-breakout-vision-design.md](2026-03-08-reloader-breakout-vision-design.md), and [2026-03-08-underworld-progression-design.md](2026-03-08-underworld-progression-design.md) first.

---

## Purpose

Capture a stronger non-killing gameplay loop for `Reloader`.

The current contract slice already supports:
- accept contract
- eliminate target
- wait out heat
- claim payout
- buy from vendors

That vertical slice is useful, but it does not yet provide enough everyday play outside of taking the shot. The missing layer is not more menu progression. The missing layer is a repeatable world role that gives the player something meaningful to do, somewhere meaningful to belong, and a reason to move through spaces without immediately acting as an assassin.

---

## Problem Read

The current game risks feeling too narrow if the player only:
- reads a contract
- prepares gear
- kills the target
- goes shopping

That structure can support the sniper fantasy, but by itself it does not create enough social, logistical, or identity-based play.

The stronger direction is to let the player occupy legal roles inside the town:
- jobs provide cash
- jobs provide schedule
- jobs provide access
- jobs provide cover
- jobs create new opportunities for scouting, infiltration, and setup

This turns the player into someone who can manipulate the town from the inside instead of only visiting it as a contract tourist.

---

## Core Idea

Add an `employment-as-cover` loop.

The player can get hired into certain workplaces and use employment as:
- a legal income source
- a believable social identity
- a way to enter restricted locations
- a way to learn routines, schedules, and habits
- a way to stash gear, plant tools, and prepare future jobs

The especially strong part of the idea is that job openings can be created by world events, including violence. A dark but fitting example is:

- the lighthouse already has a worker
- that worker dies
- the position becomes vacant
- the player can pursue the opening
- employment at the lighthouse grants access, routine, and positional advantage

That means assassination is no longer only contract completion. It also becomes a way of reshaping the town's social and occupational structure.

---

## Chosen Direction

Selected option: `Hybrid progression`

This is the recommended path because it avoids two weak extremes:

- `Access-only jobs` are efficient, but risk feeling thin and purely transactional.
- `Full labor sim jobs` could become rich, but would likely swallow the whole project and compete with the assassination sandbox instead of supporting it.

`Hybrid progression` keeps jobs valuable immediately while leaving room for depth later.

Rule of thumb:
- early job play should be simple, readable, and useful
- deeper workplace mechanics should unlock only after the job already proves its value as cover

In practice, that means jobs should begin as:
- light routine
- access pass
- schedule anchor
- trust builder

Then later evolve into:
- deeper privileges
- role-specific tasks
- better intel
- workplace-specific sabotage or setup actions
- stronger contract-solving opportunities

---

## First-Order Gameplay Value

Jobs should add gameplay other than killing people by giving the player repeatable non-violent verbs:

- show up on time
- perform a basic shift routine
- move through legally restricted areas
- observe target and civilian routines from inside
- handle keys, storage, maintenance spaces, and equipment rooms
- build rapport with coworkers or supervisors
- leave items in believable places
- decide whether to protect cover or burn it

This creates real play even on days when the player does not fire a shot.

The point is not to simulate employment for its own sake.
The point is to make work function as social camouflage, logistical infrastructure, and world access.

---

## Why The Lighthouse Example Works

The lighthouse is a strong first employment location because it naturally supports:
- isolation
- authority over a specific place
- believable restricted access
- elevated vantage
- maintenance tasks that can stay simple in v1
- opportunities for hiding gear or observing movement
- a memorable visual identity

It also cleanly supports a vacancy story:
- one worker can plausibly own the role
- a death can plausibly create an opening
- the player can plausibly fill it

That is much stronger than a generic shop clerk role because the workplace itself already implies position, solitude, and strategic visibility.

---

## Recommended Player Loop

The employment loop should read like this:

1. Learn that a workplace exists and can become accessible.
2. Discover or create a vacancy.
3. Qualify for the role through a simple gate such as conversation, reputation, or showing up at the right place and time.
4. Perform light recurring job tasks to maintain employment.
5. Use the role to gain legal access, schedule knowledge, and environmental leverage.
6. Exploit that leverage for scouting, staging, sabotage, intel gathering, or future contract setup.
7. Choose whether to keep the cover identity or burn it for a higher-value opportunity.

This loop creates meaningful play before, between, and after contracts.

---

## Design Rules

- Jobs must support the assassination sandbox, not replace it.
- Legal income from jobs should matter, but stay below contract money.
- The first version of a job should be useful before it is deep.
- Jobs should expose new spaces, routines, and opportunities, not just another UI panel.
- A job should change how the player reads the town.
- Losing a job should matter because it removes access, trust, and cover, not just pay.

---

## v1 Shape

The first implementation of jobs should stay narrow.

Recommended v1 contract:
- one authored workplace role
- one vacancy path
- one simple hiring path
- one small set of repeatable shift tasks
- one access benefit set
- one clear way that the job helps with future criminal activity

For the lighthouse, that likely means:
- basic maintenance/checklist task flow
- lawful access to the structure and storage
- a fixed shift schedule
- legal reason to be present at unusual hours
- a high-value observation or staging advantage

This is enough to prove the loop without turning the game into a work sim.

---

## Why This Matters

This idea strengthens multiple weak points at once:

- `More gameplay variety:` the player has meaningful verbs outside contracts and shopping.
- `Stronger underworld fantasy:` the player manipulates the town's institutions, not just its targets.
- `Better world coherence:` workplaces, NPC roles, and vacancies become part of the sandbox.
- `More replayable setup:` jobs create different legal identities and access patterns.
- `Richer consequences:` losing a job or abusing a role can become a real setback.

Most importantly, it gives `Reloader` a stronger answer to the question:
"What do I do when I am not pulling the trigger?"

Answer:
You build a life, a cover identity, and a position inside the town that can later be exploited.

---

## Recommendation

Proceed with `hybrid progression` for employment systems.

Start with jobs as:
- light routine
- legal income
- access and cover
- a staging/scouting advantage

Only deepen the workplace mechanics after the access-and-identity value is proven fun.

The first serious candidate should be the lighthouse role because it is structurally distinctive, easy to explain, and naturally supports both legal routine and criminal leverage.

---

## Open Questions

1. Should vacancies be created only by authored events, or can systemic deaths and disappearances also open jobs?
2. How much friction should hiring have in the first version: instant acceptance, dialogue gate, or reputation gate?
3. Should job loss come mostly from missed shifts, suspicious behavior, or explicit witnessed crimes tied to that workplace?
4. Should some contracts explicitly require job-based access, or should jobs remain optional leverage at first?

---

## Employment-Driven Town Population Brainstorm

Town population should be derived from `jobs`, not from arbitrary floating civilians.

Recommended world rule:
- every civilian belongs to a job slot
- `unemployed` is also a job category
- each job definition has a fixed number of positions
- the town population is the sum of all available job positions

This makes the population feel authored and systemic at the same time.
People are in town because they occupy a role, not because the game spawned random ambient bodies.

### Core Model

Recommended structure:
- `JobDefinition`
  - job id
  - workplace or district
  - role type
  - shift schedule
  - access privileges
  - number of positions

- `JobSlot`
  - references one job definition
  - can be occupied or vacant
  - points to the current civilian assigned to that role

- `Unemployed`
  - treated as a real job bucket with its own slots
  - supports civilians who wander town, loiter, socialize, or perform lightweight everyday routines without formal workplace access

This creates a clean rule:
- population = total job slots

### Vacancy Rule

If the player kills someone, that person's job slot becomes vacant.

This is the critical systemic behavior because it means murder changes the town's labor structure instead of only removing a body from the map.

Consequences of a death:
- the victim is removed from their job slot
- the job slot becomes open
- the workplace can visibly function with a missing role or partial disruption
- the player may be able to pursue that vacancy if the job is one that players can hold

This directly supports ideas like:
- kill the lighthouse worker
- lighthouse position opens
- player takes the job and gains access, cover, and new leverage

### Weekly Replacement Rule

Recommended replenishment rule:
- on Monday, replacement civilians are generated or assigned to fill all currently vacant jobs

This does several useful things:
- prevents the town from permanently hollowing out too quickly
- creates a readable world rhythm
- gives the player a window to exploit a vacancy before normalcy begins to restore itself
- avoids instantly respawning replacements in a fake-feeling way

Important exception:
- if the player takes a vacant job before Monday, that position is no longer available for civilian replacement

This gives job theft a clear systemic payoff.

### Why This Is Strong

This model solves multiple problems at once:
- town population feels grounded instead of arbitrary
- vacancies become meaningful world-state changes
- jobs become the bridge between civilians, access, schedules, and replacement logic
- the player can manipulate the town by changing who occupies key roles
- employed and unemployed civilians still fit the same systemic framework

It also makes the island/town feel more like a real place:
- every visible civilian has a role
- every workplace has capacity
- every absence matters

### Recommendation

Use `employment-driven population` as a major world rule.

The town should be built from:
- fixed job slots
- unemployed slots
- weekly replacement cadence
- player ability to occupy some vacancies before the replacement cycle refreshes the town

This is a stronger foundation than arbitrary ambient spawning because it ties population, jobs, routine, access, and murder consequences into one shared system.

---

## Related Brainstorms

Related notes split out from this file:
- [2026-03-13-police-investigation-and-wanted-brainstorm.md](2026-03-13-police-investigation-and-wanted-brainstorm.md)
- [2026-03-13-island-driving-world-pivot-brainstorm.md](2026-03-13-island-driving-world-pivot-brainstorm.md)
- [2026-03-13-combat-feedback-and-damage-brainstorm.md](2026-03-13-combat-feedback-and-damage-brainstorm.md)
- [2026-03-13-sandbox-pivot-brainstorm-index.md](2026-03-13-sandbox-pivot-brainstorm-index.md)

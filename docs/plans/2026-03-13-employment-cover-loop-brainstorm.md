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

## Missed Shot And Police Reaction Brainstorm

The game should punish a missed shot by collapsing the player's clean firing opportunity and starting an investigation, not by despawning the target or auto-failing every contract.

Baseline rule:
- the target stays in the town until they die
- a miss does not end the contract by default
- only contracts with explicit `no alarm raised` or similar restrictions should fail immediately from panic or witness escalation

This keeps the world believable while still making misses expensive.

### Target Reaction

After a near miss, visible impact, or obvious incoming shot, the target should immediately break routine and try to survive.

Recommended first target states:
- `Covering` if immediate hard cover is available
- `Hiding` if they can reach a secure interior or protected room
- `Fleeing` if they have an escape route, vehicle, or escort support

Important rule:
- the target should not simply disappear from the game
- the player should still be able to recover the contract unless that contract explicitly forbids alarm

The punishment is:
- the clean shot window is gone
- the target becomes harder to reach
- witnesses panic and call police
- repeated shots from the same area become much riskier

### Police Investigation Flow

Police should respond to the impact site first, not teleport directly to the player.

Recommended investigation loop:

1. A shot event or miss is reported.
2. Police arrive at the impact site, target location, or panic cluster.
3. Police secure the area and question witnesses.
4. Witness statements create a `probable firing zone`.
5. Police move outward to check likely shot positions.
6. Repeated evidence tightens the search into a more accurate local hunt.

This creates a better sandbox rule than omniscient police:
- cops know where the shot probably came from
- cops do not know the player's exact coordinates unless someone actually saw flash, silhouette, movement, or escape

### Witness Evidence Model

Shots should generate different evidence channels instead of a single stealth value.

Recommended evidence channels:
- `Impact evidence` — where the bullet landed, what it broke, and who panicked there
- `Audio origin evidence` — where witnesses think the shot came from based on muzzle blast
- `Visual origin evidence` — muzzle flash, silhouette, window movement, rooftop movement
- `Escape evidence` — movement after the shot, vehicle departure, fleeing from a vantage point

Impact evidence anchors the case.
Police go there first, then build a lead from witnesses.

Readable witness logic:
- `heard only` = rough direction
- `saw flash` = much tighter likely firing point
- `saw shooter or movement` = near-exact firing position or escape lead

### Search Zone Logic

Once police have statements, they should investigate likely firing locations rather than magically pathing to the player.

Typical search targets:
- windows facing the impact site
- rooftops
- towers and lighthouses
- treelines and ridgelines
- parked vehicles or obvious hides with a line of fire

Repeated shots from the same position should increase police confidence and tighten the zone quickly.

### Attachment And Ammo Effects

Attachment and ammo choice should affect which evidence channels become strong, not act as one generic stealth modifier.

Recommended first-pass behavior:

- `Flash hider`
  - reduces visual origin evidence
  - especially useful at dusk, night, or against witnesses with a direct line to the firing point

- `Suppressor`
  - reduces muzzle-blast audio evidence
  - lowers witness confidence in the origin area
  - should help, but should not erase investigation on its own

- `Subsonic ammo`
  - removes the supersonic crack
  - makes directional audio inference much worse for people near the bullet path
  - should pair strongly with suppressors for the best stealth package
  - should pay for that stealth with worse long-range performance and more drop

- `Muzzle brake`
  - increases audio origin evidence
  - should be the worst stealth option
  - more people should orient toward the firing area after the shot

Recommended stealth hierarchy:
- `muzzle brake + supersonic` = easiest to localize
- `flash hider + supersonic` = harder to see, still easy to hear
- `suppressor + supersonic` = quieter muzzle, but crack still gives the event away
- `suppressor + subsonic` = best stealth combination, with meaningful ballistic tradeoffs

### Contract Rule

Default contract behavior after a miss:
- contract remains active
- target reaction escalates difficulty
- police and witness pressure increases

Special contract behavior:
- some contracts can explicitly fail if the target panics, civilians alert authorities, or any public alarm state is raised

This preserves the sandbox while still allowing authored high-discipline contracts later.

---

## Police Identification And Stop-Frisk Brainstorm

Police response should separate `area alert` from `player identification`.

The world can know that a shooting happened without instantly knowing that the player was the shooter. This creates a more believable escalation ladder and gives the player room to recover if they break contact early, hide weapons, or leave the firing zone before stronger evidence appears.

### Identification Ladder

Recommended player-facing police identification states:
- `Unknown` — police are responding to an incident, but have no player-specific lead
- `Person of Interest` — police believe the player may be connected and can actively intervene
- `Identified Suspect` — police have enough evidence to treat the player as the likely shooter
- `Wanted / Arrest Target` — police commit to arrest, chase, confiscation, and persistent suspect handling

This should sit alongside area-level police heat instead of replacing it.

### Person Of Interest Rule

Locked-in rule:
- if the player is a `person of interest`, police are allowed to stop and frisk them

This should be the main bridge between soft investigation and hard confirmation.

The player becomes a person of interest through evidence such as:
- being near the probable firing zone
- leaving an obvious vantage point after the shot
- matching witness description
- behaving evasively near an active search
- carrying visible suspicious gear

### Stop And Frisk Purpose

Stop-and-frisk should not exist as random ambient harassment.
It should exist to answer the police question:

"Is this person plausibly connected to the reported shooting?"

The stop creates a high-tension checkpoint where the player may still survive the investigation if they look clean enough.

Examples of what a frisk can reveal:
- carried rifle or sidearm
- illegal ammo
- suspicious attachments
- contraband tied to the shooting setup
- other evidence that upgrades the player from `person of interest` to `identified suspect`

### Escalation Rule

Recommended escalation logic:
- `Unknown -> Person of Interest`
  - witness reports, firing-zone proximity, or suspicious movement tie the player to the incident

- `Person of Interest -> Identified Suspect`
  - frisk finds incriminating evidence
  - officers directly see a weapon, flash position, or escape behavior
  - multiple witness statements strongly agree on the player

- `Identified Suspect -> Wanted / Arrest Target`
  - the player flees the stop
  - the player resists
  - police confirm enough evidence to arrest

### Wanted Decay Shape

This also implies that wanted state should not disappear through one blunt cooldown.

Recommended decay tracks:
- `Immediate pursuit` ends first after line-of-sight break
- `Local search` ends later after the firing zone is checked without confirmation
- `Person of Interest` suspicion ends last if police fail to connect the player to hard evidence

Because police can stop and frisk a person of interest, the suspicion phase remains dangerous even after the hot chase is over.

### Design Rule

Use readable escalation with evidence-based intervention:
- area alert can be broad
- identification should be conditional
- `person of interest` should be enough to justify stop-and-frisk
- hard arrest state should still require stronger confirmation, resistance, or incriminating discovery

---

## Driving And Island Setting Brainstorm

Two additional direction changes should be captured together:
- driving should become a real part of the core sandbox
- the setting should pivot from a generic inland town read toward an island location

These ideas reinforce each other. An island naturally makes roads, chokepoints, coastlines, ferry access, lighthouse jobs, and police containment more legible.

### Why Driving Matters

Driving should not be treated as a side feature or only a future travel screen wrapper.
It should become part of the player's everyday criminal and civilian loop.

Driving can support:
- getting to jobs and shifts on time
- carrying rifles, ammo, and contraband in vehicle storage
- moving between firing positions and fallback routes
- escaping police searches after a shot
- relocating before the firing zone collapses around the player
- passing through police stops and checkpoints while under suspicion

If jobs are part of the fantasy, driving also supports the ordinary life layer:
- commuting to work
- arriving at authored locations with believable rhythm
- parking near access points and hides
- deciding what to leave in the car versus carry on-body

### Why An Island Setting Helps

Switching the location to an island is a strong fantasy and systems choice.

It helps because:
- geography becomes easier to read
- escape routes become naturally limited
- police containment makes more sense
- harbors, ferries, bridges, and coastal roads become strong world anchors
- cliffs, shorelines, ridges, and lighthouses create memorable shot positions
- the town can feel more specific and less generic

An island also strengthens the employment-as-cover idea:
- lighthouse keeper
- harbor worker
- ferry operator or dock labor
- maintenance or utility worker
- coastal police patrol

These roles feel more grounded and spatially important on an island than in an anonymous inland sandbox.

### Core World Implications

An island pivot should not just be visual dressing.
It should shape the whole sandbox.

Recommended island-world advantages:
- fewer but more meaningful road routes
- strong ingress/egress bottlenecks
- clear districts with different visibility and law-enforcement pressure
- coastal vantage points for long shots
- harbors and ferries as civilian and police traffic anchors
- more believable route planning for both player and police

This also improves missed-shot and wanted-state gameplay:
- police can lock down exits
- bridges, ferry terminals, and causeways become natural search pressure points
- vehicle checks become more meaningful
- relocating across the island remains possible, but not trivial

### Driving As Core Sandbox Verb

Recommended rule:
- driving should become a core sandbox verb, not just a traversal shortcut

That means vehicles should matter for:
- logistics
- concealment
- timing
- escape
- police exposure

Examples:
- stash the rifle in the trunk before a stop-and-frisk
- leave a vantage point before witnesses produce a stronger lead
- risk driving through a checkpoint while only a person of interest
- use a work vehicle or job access route as legal cover

### Recommendation

Capture both pivots as part of the broader sandbox direction:
- `driving` should be promoted from future flavor to core loop support
- `island setting` should be treated as a serious world-direction pivot because it improves readability, tone, containment, and employment/location fantasy

The island should not be chosen only because it sounds cool.
It should be chosen because it gives the game stronger geography for contracts, jobs, investigation, checkpoints, and escape.

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

## Combat Feedback And Damage Model Brainstorm

The game needs more readable and more satisfying shot feedback.

Two linked changes should be treated as important combat-direction work:
- NPC collision should move beyond simple broad capsules toward mesh-driven hit detection or mesh-aligned hit zones
- damage resolution should distinguish between head, torso, and limb hits

This is important both for realism-adjacent readability and for the fantasy of precision long-range shooting.

### Hit Detection Direction

Recommended rule:
- stop relying on a single generic humanoid collision capsule as the meaningful shot target
- use mesh-aligned hitboxes or named body-region colliders for real shot resolution

This does not require full soft-body simulation.
It does require that bullet hits map to body regions that players can understand and intentionally target.

Mandatory body regions for first pass:
- `Head`
- `Torso`
- `Arms`
- `Legs`

### Damage Model Direction

Recommended first-pass damage behavior:
- `Headshot`
  - highest lethality
  - strongest chance of immediate kill or incapacitation
  - should be the clean premium sniper outcome

- `Torso`
  - high lethality, but less guaranteed than head
  - can still produce survivable or delayed-collapse states depending on context later

- `Limb hit`
  - lower lethality
  - high chance to wound, stagger, panic, or trigger flee/hide behavior instead of instant death

This matters a lot for missed-shot and botched-shot gameplay:
- a limb hit can turn a clean kill into a wounded fleeing target
- a body hit can create a panic scene without instant resolution
- a headshot can remain the most reliable precision payoff

### Why Collision Needs To Improve

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

### Slow-Mo Bullet Camera

The game should also have a slow-motion bullet camera for especially strong long shots.

Recommended direction:
- add a cinematic bullet-follow / impact-confirm camera inspired by `Sniper Elite`
- reserve it for notable long-range hits, premium kills, or especially clean shots
- use it as reward feedback, not for every routine shot

This should be treated as a major fantasy amplifier, not a gimmick.
The game's commercial and emotional identity benefits a lot from occasionally letting the player savor a difficult long shot.

### Cinemachine Note

Probable implementation direction:
- use `Cinemachine` as the first candidate for the slow-motion bullet camera system

Why it fits:
- camera blending is already a solved problem there
- it should be easier to author a temporary shot-follow camera than to build a full custom cinematic stack first
- it can support clean transition back to player control after the impact moment

### Recommendation

Capture these as linked combat priorities:
- improve NPC hit detection from capsule-style simplification toward mesh/body-region hitboxes
- add distinct damage resolution for head, torso, and limbs
- plan for a selective slow-motion bullet camera to sell premium long shots

These are not isolated polish requests.
They directly support the game's precision-shooter identity and make both success and failure more legible.

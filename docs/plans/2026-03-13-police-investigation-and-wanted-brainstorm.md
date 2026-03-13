# Police Investigation And Wanted Brainstorm

> **Prerequisites:** Read [../design/core-architecture.md](../design/core-architecture.md), [../design/law-enforcement.md](../design/law-enforcement.md), [../design/assassination-contracts.md](../design/assassination-contracts.md), and [2026-03-13-sandbox-pivot-brainstorm-index.md](2026-03-13-sandbox-pivot-brainstorm-index.md) first.

---

## Purpose

Capture the police, witness, and wanted-state brainstorm separately from the employment loop so future work can target law-enforcement behavior without dragging in unrelated job-system context.

---

## Missed Shot Reaction

The game should punish a missed shot by collapsing the player's clean firing opportunity and starting an investigation, not by despawning the target or auto-failing every contract.

Baseline rule:
- the target stays in the town until they die
- a miss does not end the contract by default
- only contracts with explicit `no alarm raised` or similar restrictions should fail immediately from panic or witness escalation

Recommended first target states:
- `Covering`
- `Hiding`
- `Fleeing`

The punishment is:
- the clean shot window is gone
- the target becomes harder to reach
- witnesses panic and call police
- repeated shots from the same area become much riskier

---

## Police Investigation Flow

Police should respond to the impact site first, not teleport directly to the player.

Recommended investigation loop:
1. A shot event or miss is reported.
2. Police arrive at the impact site, target location, or panic cluster.
3. Police secure the area and question witnesses.
4. Witness statements create a `probable firing zone`.
5. Police move outward to check likely shot positions.
6. Repeated evidence tightens the search into a more accurate local hunt.

This keeps the cops readable without giving them omniscient knowledge.

---

## Witness Evidence Model

Shots should generate different evidence channels instead of a single stealth value.

Recommended evidence channels:
- `Impact evidence`
- `Audio origin evidence`
- `Visual origin evidence`
- `Escape evidence`

Readable witness logic:
- `heard only` = rough direction
- `saw flash` = much tighter likely firing point
- `saw shooter or movement` = near-exact firing position or escape lead

Typical search targets:
- windows facing the impact site
- rooftops
- towers and lighthouses
- treelines and ridgelines
- parked vehicles or hides with a line of fire

---

## Attachment And Ammo Effects

Attachment and ammo choice should affect which evidence channels become strong, not act as one generic stealth modifier.

Recommended first-pass behavior:
- `Flash hider` reduces visual origin evidence
- `Suppressor` reduces muzzle-blast audio evidence
- `Subsonic ammo` removes the supersonic crack and pairs strongly with suppressors
- `Muzzle brake` increases audio origin evidence and should be the worst stealth option

Recommended stealth hierarchy:
- `muzzle brake + supersonic` = easiest to localize
- `flash hider + supersonic` = harder to see, still easy to hear
- `suppressor + supersonic` = quieter muzzle, but crack still gives the event away
- `suppressor + subsonic` = best stealth combination, with meaningful ballistic tradeoffs

---

## Identification And Stop-Frisk

Police response should separate `area alert` from `player identification`.

Recommended identification ladder:
- `Unknown`
- `Person of Interest`
- `Identified Suspect`
- `Wanted / Arrest Target`

Locked-in rule:
- if the player is a `person of interest`, police are allowed to stop and frisk them

This is the intended bridge between soft investigation and hard confirmation.

Player can become a person of interest through:
- firing-zone proximity
- leaving an obvious vantage point
- matching witness description
- evasive behavior near an active search
- carrying visible suspicious gear

Frisks can reveal:
- carried rifle or sidearm
- illegal ammo
- suspicious attachments
- contraband tied to the shooting setup

---

## Wanted Decay Shape

Wanted state should not disappear through one blunt cooldown.

Recommended decay tracks:
- `Immediate pursuit` ends first after line-of-sight break
- `Local search` ends later after the firing zone is checked without confirmation
- `Person of Interest` suspicion ends last if police fail to connect the player to hard evidence

Because police can stop and frisk a person of interest, the suspicion phase remains dangerous even after the hot chase is over.

---

## Recommendation

Use readable escalation with evidence-based intervention:
- area alert can be broad
- identification should be conditional
- `person of interest` should be enough to justify stop-and-frisk
- hard arrest state should still require stronger confirmation, resistance, or incriminating discovery

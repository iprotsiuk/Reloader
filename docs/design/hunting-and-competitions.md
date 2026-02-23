# Hunting & Competitions Design

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.
> **Related:** [weapons-and-ballistics.md](weapons-and-ballistics.md) for the competition spectrum and ballistics model.

---

## Hunting [v1+]

### Animal Progression

| Tier | Animals | Caliber | Reward |
|------|---------|---------|--------|
| Small game | Rabbits, squirrels | .22 LR | Low |
| Varmints | Coyotes, prairie dogs | .223, .22-250 | Medium (pest bounties) |
| Deer | Whitetail, mule deer | .308, .30-06, 6.5 CM | Good |
| Big game | Elk, moose | .300 WM, .338 | High |
| Trophy/Special | Rare animals | Appropriate caliber | Very high (quest-gated tags) |

### AnimalDefinition SO (to be created)

Each animal species needs an `AnimalDefinition` ScriptableObject:
- `species`, `tier`, `vitalZoneSize`
- `appropriateCalibers` — minimum caliber for ethical kill
- `terminalEnergyThreshold` — minimum bullet energy (ft-lbs) for clean kill
- `expansionRequired` — bool (does the bullet need to expand for ethical kill?)
- `peltValue`, `bountyValue`
- `habitat` — which hunting areas this species spawns in
- `spawnWeight`, `difficulty`
- `customProperties`

### Mechanics

- **Tracking:** Follow signs, use calls, buy hints from local NPCs for approximate animal locations
- **Shot placement:** Vital hits = clean kill = full value + ethics boost. Poor hits = wounded animal = penalty.
- **Caliber matching:** Using too-small caliber on big game = wounding = ethical violation + possible fine
- **Ammo quality:** Poor ammo = inconsistent hits = wounding instead of clean kills
- **Bullet type matters:** Use appropriate hunting bullets (soft point, controlled expansion). Match/FMJ bullets = poor terminal performance, possible pass-through, lost animal.
- **Carry capacity:** Limited by what you can carry. Vehicle needed for big game transport.

### Ethics & Enforcement

| Behavior | Consequence |
|----------|-------------|
| Clean kill | Full pelt value, reputation boost, ethics score up |
| Wounded & lost | Reputation penalty, possible fine if warden sees |
| Wrong season / no tag | Poaching charge if caught (jail + fine) |
| Over-harvest | Animal population declines in that area |
| Wrong ammo type | May not be illegal but poor results + NPC judgment |
| Game warden check | Random inspection: license, tags, weapon, ammo legality |

---

## Competitions [v0.2]

### Types

See [weapons-and-ballistics.md](weapons-and-ballistics.md) § Competition Spectrum for the full category breakdown (pistol, carbine, precision rifle, long range, ELR, benchrest).

### Progression

| Tier | Opponents | Prizes | Requirements |
|------|-----------|--------|-------------|
| Local club / informal | Easy NPCs, friendly bets | Small cash | None |
| Regional | Tougher NPCs | Medium cash + equipment | Qualify at local level |
| National | Top tier | Large cash + rare items | Qualify at regional |
| Special invitational | Story events | Unique rewards | Quest-gated |

### Scoring Factors

- Shot placement (accuracy)
- Group size (ammo consistency = reloading quality)
- Time (for practical/speed events)
- Equipment class restrictions (some comps restrict caliber/weapon type)

### CompetitionDefinition SO (to be created)

Each competition type needs a `CompetitionDefinition` ScriptableObject:
- `competitionType` — from Competition Spectrum in weapons-and-ballistics.md
- `tier` — Local / Regional / National / Special
- `scoringMethod`, `timeLimit`
- `caliberRestrictions`, `equipmentClassRestrictions`
- `entryFee`, `prizes[]`
- `qualificationRequirements` — reference to prerequisite tier/placement
- `customProperties`

`CompetitionResult` (runtime class, not SO):
- `competition` → CompetitionDefinition
- `placement`, `score`, `groupSizeMOA`
- `prizeAwarded`, `reputationChange`

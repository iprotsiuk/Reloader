# Law Enforcement & Black Market Design

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.

---

## Legal System [v1+]

| Regulation | Details |
|-----------|---------|
| Hunting license | Required, purchased at town hall |
| Hunting tags | Per-animal, per-season, limited quantity |
| Weapon legality | Some items restricted (suppressors, SBRs, full-auto) |
| NFA tax stamps | Required for suppressors, SBRs, etc. (legal path = expensive + slow) |
| Ammo restrictions | Some types restricted in certain contexts |
| Carry permits | May be required for concealed carry in town |

---

## Black Market [v1+]

- Shady NPC dealers in specific locations (back alleys, remote areas)
- Sell restricted/illegal items: unregistered suppressors, full-auto parts, stolen weapons
- Higher risk: can be caught during transaction
- No warranty: items may be defective or traceable (serial numbers)
- Higher profit margins when selling illegal ammo/items

---

## Law Enforcement [v1+]

| Authority | Where | Checks For |
|-----------|-------|-----------|
| Game warden | Hunting areas | License, tags, legal caliber, season compliance |
| Town police | Town, roads | Illegal weapons/attachments, stolen items, warrants |
| Random stops | Roads | Vehicle search (if probable cause), weapon inspection |

---

## Consequences [v1+]

| Offense | Consequence |
|---------|-------------|
| Minor violation | Fine, confiscation of item |
| Hunting violation | Fine + hunting license suspension (days) |
| Illegal weapon possession | Large fine, weapon confiscated, possible arrest |
| Resisting / fleeing | Chase sequence (car or foot), additional charges |
| Arrest | Jail time (time skip), heavy fine, reputation damage |
| Repeat offenses | Escalating penalties, NPCs become wary |

---

## Data Model [v1+]

`CrimeType` enum: Poaching / HuntingWithoutLicense / IllegalWeapon / IllegalAttachment / IllegalAmmo / Trespassing / Resisting / Fleeing / BlackMarketTransaction

Each offense maps to a consequence tier (minor violation, hunting violation, illegal possession, arrest). The law enforcement system fires `GameEvents.OnCrimeCommitted(CrimeType)` for other systems to react (reputation, NPC relationships, quest state).

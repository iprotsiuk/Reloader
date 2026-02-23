# NPCs & Quests Design

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.

---

## NPC Types [v0.2]

| Type | Role |
|------|------|
| Shopkeepers | Buy/sell, restock daily, give opinions and advice |
| Competitors | Appear at competitions, have skill levels, can befriend |
| Hunting buddies | Share tips, reveal spots, quest givers |
| Old timers | Reloading wisdom, lore, mentorship |
| Game warden | Patrols hunting areas, checks licenses, enforces law |
| Police | Patrol town, can stop/search, enforce weapon laws |
| Customers | Want to buy your ammo, give specs and their brass |
| Black market contacts | Sell illegal items, offer shady jobs |

---

## Quest Types [v1+]

| Type | Example |
|------|---------|
| Tutorial | "Grandpa's old rifle needs ammo — reload some .30-06" |
| Fetch/delivery | "Bring me 50 rounds of match .308 by Friday" |
| Competition entry | "Qualify for regionals with a sub-MOA group" |
| Hunting contract | "Clear the coyotes from Henderson's ranch" |
| Equipment unlock | "Help fix the old press and you can keep it" |
| Story beats | Narrative milestones unlocking new areas/features |
| Reputation | "Prove yourself at the local match to earn shop discount" |

---

## Relationship System [v1+]

Each NPC tracks relationship level with the player. Higher relationship → better prices, tips, quest access, ammo sale reputation. Negative actions (crime, rudeness, bad ammo) decrease relationship.

---

## Data Model [v1+]

### NPCDefinition SO
- `npcType` — Shopkeeper / Competitor / HuntingBuddy / OldTimer / Warden / Police / Customer / BlackMarket
- `defaultRelationship` — starting relationship level
- `dialogueTree` — reference to dialogue data
- `shopInventory` — for vendor NPCs: list of items they stock
- `skillLevel` — for competitor NPCs: how good they are
- `patrolArea` — for wardens/police: where they patrol
- `customProperties`

### QuestDefinition SO
- `questType` — Tutorial / Fetch / Competition / Hunting / Equipment / Story / Reputation
- `objectives[]` — list of objectives with progress tracking
- `rewards` — money, items, reputation, access unlocks
- `deadline` — optional time limit in game days
- `prerequisites` — required quests, reputation levels, or items
- `customProperties`

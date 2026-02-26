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

---

## NPC Capability Foundation Status + World Authoring Quickstart [v0.1]

### Current Implemented Slice
- Data contracts exist for `NpcDefinition`, `NpcRolePreset`, and `NpcCapabilityConfig`.
- `NpcAgent` discovers `INpcCapability` components on the prefab and manages lifecycle (`Initialize`/`Shutdown`).
- `NpcAgent.CollectActions()` aggregates player actions from capabilities implementing `INpcActionProvider`.
- `NpcAiController` consumes `INpcDecisionProvider`; if none is assigned, it falls back to `RuleBasedDecisionProvider`.
- Vendor interaction is wired through `VendorTradeCapability` (`vendor.trade.open`) and remains compatible with shop-vendor targets.

### Designer Quickstart (Now)
1. Drag a role prefab from `Reloader/Assets/_Project/NPCs/Prefabs/Roles/` into `MainWorld`.
2. On `NpcAgent`, assign an `NpcDefinition` asset.
3. In `NpcDefinition`, set `NpcId` and assign `RolePreset`.
4. In `NpcRolePreset`, set `RoleKind` and capability config assets (`NpcCapabilityConfig[]`) for the role intent.
5. On the prefab instance, add/remove capability MonoBehaviours (`INpcCapability`) to match the role. For vendors, ensure `VendorTradeCapability` can resolve an `IShopVendorTarget`.
6. If the NPC needs AI ticking now, keep `NpcAiController` with default rule-based evaluation.

### Implemented vs Deferred
| Area | Status | Notes |
|------|--------|-------|
| Capability composition shell (`NpcAgent` + `INpcCapability`) | Implemented now | Reusable across role prefabs. |
| Action-provider seam (`INpcActionProvider`) | Implemented now | Enables interaction UI action aggregation. |
| Decision-provider seam (`INpcDecisionProvider`) | Implemented now | Runtime seam exists via `NpcAiController`. |
| `BehaviorTreeDecisionProvider` and BT assets/tooling | Deferred | Planned drop-in provider implementing `INpcDecisionProvider`. |
| Dialogue/quest behavior integration on capabilities | Deferred | Will attach to capability and action flows later. |
| Full capability runtime-state save integration | Deferred | Current slice does not persist all capability internals. |

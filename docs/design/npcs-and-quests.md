# NPCs & Quests Design

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.

---

## NPC Types [v0.2]

| Type | Role |
|------|------|
| Shopkeepers | Buy/sell, restock daily, give opinions and advice |
| Contract handlers | Offer jobs, intel, payout, and escalation into better work |
| Targets | Contract victims with routines, schedules, and risk profiles |
| Witnesses / civilians | Populate the town and feed police response when they observe violence |
| Old timers | Reloading wisdom, lore, mentorship |
| Police | Patrol town, search, pursue, arrest, and escalate heat |
| Spotters / fixers | Sell intel, routes, ranges, and positioning help |
| Black market contacts | Sell illegal items and risky support services |

---

## Quest Types [v1+]

| Type | Example |
|------|---------|
| Tutorial | "Grandpa's old rifle needs ammo — reload some .30-06" |
| Fetch / delivery | "Bring me specific components before tonight's job" |
| Assassination contract | "Window target, 420 meters, low collateral, leave unseen" |
| Intel setup | "Photograph the route and mark the clean sightline" |
| Equipment unlock | "Help fix the old press and you can keep it" |
| Story beats | Narrative milestones unlocking new areas/features |
| Reputation | "Land a clean long shot and unlock better contracts" |

---

## Relationship System [v1+]

Each NPC tracks relationship level with the player. Higher relationship unlocks better prices, better contract intel, safer introductions, and cleaner payout opportunities. Negative actions (public chaos, failed jobs, collateral damage, rudeness) decrease relationship.

---

## Data Model [v1+]

### NPCDefinition SO
- `npcType` — Shopkeeper / Handler / Target / Witness / OldTimer / Police / Fixer / BlackMarket
- `defaultRelationship` — starting relationship level
- `dialogueTree` — reference to dialogue data
- `shopInventory` — for vendor NPCs: list of items they stock
- `skillLevel` — optional accuracy/intel/security skill for relevant roles
- `patrolArea` — for police or guard roles: where they patrol
- `customProperties`

### QuestDefinition SO
- `questType` — Tutorial / Fetch / Assassination / Intel / Equipment / Story / Reputation
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
- Existing role/runtime seams should now be used for handlers, targets, witnesses, and police response actors rather than competition or hunting-only content.

### Designer Quickstart (Now)
1. Drag a role prefab from `Reloader/Assets/_Project/NPCs/Prefabs/Roles/` into the active runtime world scene (currently `MainTown`; `MainWorld` is compatibility-only).
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

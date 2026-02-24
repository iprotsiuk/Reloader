# Inventory & Economy Design

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.

---

## Item Persistence [v0.1]

**Every physical item in the world persists.** Dropped a brass casing? It stays on the floor until you pick it up. Left a tool on the bench? Still there tomorrow. This applies to all scenes.

```
WorldItem (MonoBehaviour on every physical item in the world)
├── itemInstance        → reference to the runtime ItemInstance
├── sceneID             → which scene this item belongs to
├── position/rotation   → transform data
├── isStatic            → placed in editor vs. dynamically spawned
└── containerID         → if stored in a container (shelf, box, cabinet)

ScenePersistenceManager (one per scene)
├── OnSceneUnload()     → serialize all dynamic WorldItems via SaveCoordinator modules
├── OnSceneLoad()       → deserialize and respawn all saved WorldItems
└── Track: spawned items, moved items, destroyed/consumed items
```

ScenePersistenceManager coordinates with SaveCoordinator during scene transitions: SaveCoordinator orchestrates serialization/restore, ScenePersistenceManager handles per-scene item state, and SceneLoader notifies both systems when scenes load/unload.

---

## Performance Management [v1+]

Hundreds of items (brass casings, etc.) on the floor:
- **Object pooling** for frequently spawned/despawned items
- **GPU instancing** for identical meshes (500 brass casings = 1 draw call)
- **Physics sleep** on stationary objects
- **LOD groups** for item density management at distance
- **"Sweep into container"** interaction for batch cleanup

---

## Storage [v0.1]

- **Carried inventory:** Weight + slot limited. Player carries items on person.
- **Containers:** Shelves, cabinets, ammo boxes, vehicle trunk — each has capacity.
- **Workshop storage:** Organized by type (powder shelf, bullet drawer, brass bins).

### Belt Quick Slots [v0.1]

- Belt has 5 slot indices mapped to keys `1..5`.
- Selection is slot-based, not equip/unequip based.
- Pressing `1..5` always selects that slot, including empty slots.
- Pressing the currently selected slot key again is a no-op.
- Pickup (`E`) inserts into first empty belt slot, then backpack if unlocked.

### Belt HUD Test Surface [v0.1]

- A reusable belt HUD surface should show only the 5 belt slots for this phase.
- Selected slot visuals use brighter tint + slight scale increase.
- Occupied slots can use a placeholder icon until item-specific icons are implemented.
- HUD wiring should stay decoupled from the future TAB shell (inventory/quests/manuals tabs).

---

## Currency [v0.1]

Single currency: dollars. Simple and clear.

---

## Component Pricing Schema [v0.1]

Canonical pricing fields for components are normalized across powder, bullets, cases, and primers:

- `packageCount` = quantity sold in a single package
- `packagePrice` = full package price
- `unitPrice` = derived at runtime (`packagePrice / packageCount`), not manually authored

| Component | `packageCount` unit expectation |
|-----------|---------------------------------|
| Powder | Pounds per container (e.g., 1 or 8) |
| Bullets | Bullet count per box |
| Cases | Case count per bag/box |
| Primers | Primer count per sleeve/box |

This schema is the source of truth for economy balancing and UI breakdowns (package vs per-unit cost).
Factory ammo templates use the same package schema (`packageCount`, `packagePrice`, derived `unitPrice`) even though their round-quality consistency is controlled by factory process fields.

---

## Income Sources [v0.2]

| Source | Details |
|--------|---------|
| Competition prizes | Scale with difficulty tier, skill-based |
| Hunting bounties/pelts | Sell at general store, price varies by quality |
| Selling custom ammo | NPC buyers, price based on your reputation and ammo quality |
| Odd jobs / quests | NPC requests, deliveries, cleanup work |
| Brass scavenging | Collect spent brass at range, sell or reload it |
| Black market sales | Higher profit, higher risk |

---

## Expenses [v0.2]

Reloading components, equipment upgrades, weapons, attachments, workshop/house upgrades, vehicle fuel and maintenance, competition entry fees, hunting licenses and tags, medical bills (after catastrophic failures), fines and legal fees.

---

## Shops [v0.2]

All physically located in the main world (seamless entry):

| Shop | Sells |
|------|-------|
| Gun Store | Weapons, factory ammo, accessories, optics |
| Reloading Supply | Components (powder, primers, bullets, brass), equipment, dies, tools |
| General Store | Food, cleaning supplies, miscellaneous |
| Online Catalog | Wider selection, delivery next day, shipping cost. Interact with computer in house. |
| Black Market Dealer | Restricted/illegal items. Risky. See [law-enforcement.md](law-enforcement.md). |
| NPC Direct Trade | Buy/sell with NPCs you have rapport with |

Shops both buy and sell. The General Store buys hunting pelts/bounties. The Gun Store buys used weapons. NPCs buy custom ammo. Buy/sell inventory may differ from the sell inventory shown in the table.

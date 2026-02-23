# Save System & Progression Design

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.

---

## Save Triggers [v0.1]

- On sleep (end of day)
- On scene transition
- Manual save (menu)
- Autosave interval (configurable)

---

## Save Envelope and Versioning [v0.1]

All save files use a versioned envelope so payload contracts can evolve without breaking milestone compatibility:

```
SaveEnvelope
├── schemaVersion   (int, migration source/target)
├── buildVersion    (string, diagnostic only)
├── createdAtUtc    (ISO-8601 timestamp)
├── featureFlags    (which optional systems were active when saved)
└── modules         (Dictionary<string, ModuleSaveBlock>)
    └── ModuleSaveBlock
        ├── moduleVersion
        └── payloadJson
```

Compatibility policy:
- Guaranteed compatibility is milestone-to-milestone (`v0.1` -> `v0.2`) through explicit migrations.
- Intermediate experiment builds are not guaranteed to remain compatible.
- New schema versions must include migration steps from prior milestone schemas.

Forward-compatibility behavior:
- Unknown module keys are ignored safely during load.
- Missing required module keys fail load before any module restore is applied.
- Corrupted payload JSON fails load before any module restore is applied.

## Save Size Budgets [v0.1]

Budget policy (uncompressed envelope JSON):

- **Soft threshold:** `500 KB` total save size. Exceeding this emits a warning.
- **Hard threshold:** `1 MB` total save size. Exceeding this fails verification.
- **Module growth guardrail:** any single module growing more than `10%` versus baseline must be explicitly acknowledged in docs/plan notes.

Data-shape rules for staying within budget:

- Persist only authoritative state; never persist values derivable at load/runtime.
- Keep cross-system references ID-based (`ItemRegistry` owns full payloads; other blocks store IDs + placement metadata).
- Quantize high-volume numeric fields (especially transforms) only where gameplay-safe.
- Preserve JSON as canonical debug/migration format for module payloads.
- If needed, add optional **file-level** compression in repository I/O (for example in `SaveFileRepository`) without changing module contracts.

## Deterministic Load Phases [v0.1]

Load order is deterministic and must stay stable:

1. Read and deserialize `SaveEnvelope`.
2. Run migration chain from `schemaVersion` -> current runtime schema.
3. Validate required module presence and payload JSON well-formedness.
4. Restore modules in explicit registration order (baseline: `CoreWorld` first, `Inventory` second).
5. Run post-restore module validation.
6. Publish load-complete events.

This ordering prevents partial loads and keeps cross-system dependencies predictable.

## Current Implemented Save Slice [v0.1]

Current repository implementation only requires these registered module payloads:

- `CoreWorld` module payload: `dayCount`, `timeOfDay`
- `Inventory` module payload: `carriedItemIds`

The broader schema below is the v0.1 design target and forward schema contract. Treat it as planned module scope until those modules are registered and migration-backed in runtime.

## Save Data Structure (Target Schema) [v0.1]

The canonical gameplay payload blocks remain:

```
SaveData (logical domain content carried by modules)
├── PlayerState
│   ├── position, rotation, currentScene
│   ├── health
│   ├── activeDebuffs[] (shaky hands, tinnitus, blurry vision + duration)
│   ├── money
│   ├── reputation scores (competition, hunting, ammo crafting, legal standing)
│   └── licenses and permits owned
├── ItemRegistry
│   └── uniqueId -> full ItemInstance payload (single source of truth)
├── ItemLocation
│   └── uniqueId -> owner/location metadata (canonical placement map)
├── InventoryState
│   ├── carriedItemIds[]
│   └── containers[] (containerId + itemIds[])
├── WorldItemState (per scene)
│   ├── dynamicItems[] (uniqueId + sceneId + position + rotation + containerId)
│   └── movedStaticItems[] (uniqueId + sceneId + position + rotation)
├── WeaponState[]
│   ├── weaponId
│   ├── partIds[]
│   ├── barrel chamberId
│   ├── magazineAmmoIds[] (ordered ItemRegistry ids)
│   └── custom name if player named it
├── RecipeBook
│   ├── discovered/saved load recipes
│   └── player notes per recipe
├── NPCState[] (forward-compatible block; may be empty in early phases)
│   ├── npcId + position + rotation + currentScene
│   ├── relationship level per NPC
│   ├── dialogue flags / quest state
│   └── shop inventory state
├── QuestState[]
│   ├── active, completed, failed quests with progress
│   └── timed quest deadlines
├── WorldState
│   ├── dayCount, timeOfDay
│   ├── animalPopulation per hunting area (v1+ when hunting is active)
│   ├── weather state + seed
│   ├── shop restock timers
│   └── law enforcement alert level (v1+)
├── WorkshopState
│   ├── equipment placement (position, rotation per tool)
│   ├── in-progress reload batches
│   └── employee tasks (if hired)
└── VehicleState
    ├── vehicleId
    ├── fuel, condition
    ├── cargoItemIds[]
    └── parked location
```

`ItemRegistry` is the only store of full item-instance payloads. `ItemLocation` is the canonical ownership/location map. System-specific state blocks (`InventoryState`, `WorldItemState`, `WeaponState`, `VehicleState`) reference IDs plus location/slot metadata only. Chamber history remains on each `CasingInstance` (`lastFiredInChamber`) in `ItemRegistry`, not in a separate index.

Field naming convention for payload examples: use lowerCamelCase with `Id`/`Ids` suffixes (for example `carriedItemIds`, `containerId`, `magazineAmmoIds`) to match serialized JSON naming style.

Forward-compatibility rule: target schema blocks for later-phase systems (for example `NPCState`, hunting population fields, and law-enforcement state) are defined from v0.1 so save files stay migratable. In phases where those systems are not active yet, these blocks can remain empty/default when modules exist, and may be absent in earlier implementation slices.

**Exact restore contract [v0.1]:** Loading a save restores the same world state the player left for every active system in the current phase: player transform, dropped item transforms (including floor items), inventory/container contents, weapon/vehicle state, and progression flags. If NPC simulation is active, NPC transforms/state are restored exactly as well.

**Save format:** JSON envelope + per-module payload JSON. Each domain module serializes its own payload contract; migration code evolves schema versions explicitly.

---

## Game Loop [v0.1]

```
WAKE UP     → At home (bed), or wherever the player fell asleep,
              or jail (if arrested the previous day).
              Check debuffs, check quest deadlines.

MORNING     → Decide what to do today:
              - Reload ammo at the workshop bench
              - Drive to town for supplies
              - Head to the range to practice or compete
              - Drive to hunting checkpoint (v1+)
              - Fill NPC orders for custom ammo
              - Do odd jobs for cash

DAYTIME     → Execute the plan. Multiple activities in one day
              are possible depending on time management.
              Time passes during activities.

EVENING     → Return home (or don't — player choice).
              Sell goods, organize inventory, review earnings.
              Clean weapons, prep brass for tomorrow.

NIGHT       → Sleep (time skip to next morning) or stay up
              (fatigue debuff next day). Optional night activities
              (black market deals happen at night).

SAVE        → Triggers on sleep, scene transition, manual, autosave.
```

The player is never forced into this loop. They can do whatever they want in whatever order.

---

## Equipment Progression [v1+]

| Phase | What Unlocks | How |
|-------|-------------|-----|
| Start | Grandpa's rifle + old press + one caliber | Story intro |
| Early | Case trimmer, tumbler, calipers, second caliber | Buy from shop or quest reward |
| Mid | Digital scale, powder measure, multiple calibers, better press | Earn from competitions + hunting |
| Late | Progressive press, electronic dispenser, chronograph | Major purchase or quest chain |
| Endgame | Bullet casting, custom dies, automated systems, employees | Long-term investment |

## Weapon Progression [v1+]

| Phase | Weapons |
|-------|---------|
| Start | Grandpa's bolt-action rifle (one caliber, worn but functional) |
| Early | Buy a .22 LR for small game, or a pistol for competition |
| Mid | Precision rifle for long range, AR-platform for 3-gun |
| Late | Multiple specialized weapons for different competition types |
| Endgame | Custom-built rifles, wildcat chamberings, full competition arsenal |

## World Progression [v1+]

| Phase | Access |
|-------|--------|
| Start | House + town + local range + nearby hunting area |
| Mid | Regional competition venues, more hunting areas |
| Late | National venues, premium hunting grounds, second property |
| Endgame | Full map access, all competition tiers, hire employees |

## Reputation Progression [v1+]

| Reputation | Effect |
|-----------|--------|
| Ammo quality rep | NPCs seek you out, pay more, refer others |
| Competition rep | Invitations to higher tiers, sponsorship offers |
| Hunting rep | Access to better grounds, tips from veterans |
| Legal standing | Affects police suspicion, NPC trust |

**Implementation note:** Precision systems (component sorting, annealing tracking, per-lot powder variance, fire-forming) are designed into the data model from v0.1 even though full gameplay implementation is phased. The data fields exist on SOs and instances from day one — the gameplay UI and interactions that expose them may come in later versions. See prototype-scope.md for version targets.

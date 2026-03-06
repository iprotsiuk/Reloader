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
4. Restore modules in explicit registration order (`CoreWorld`, `Inventory`, `Weapons`, `WorldObjectState` in current v0.1 implemented baseline).
5. Run post-restore module validation.
6. Publish load-complete events.

This ordering prevents partial loads and keeps cross-system dependencies predictable.

## Current Implemented Save Slice [v0.1]

Current repository implementation requires these registered module payloads:

- `CoreWorld` module payload: `dayCount`, `timeOfDay`
- `Inventory` module payload: `carriedItemIds`, `beltSlotItemIds`, `backpackItemIds`, `backpackCapacity`, `selectedBeltIndex`
- `Weapons` module payload: `itemId`, `chamberLoaded`, `magCount`, `reserveCount`, `chamberRound`, `magazineRounds[]`
- `WorldObjectState` module payload:
  - `sceneObjectStates[]` keyed by `scenePath` with per-object records (`objectId`, `consumed`, `destroyed`, optional transform override, `lastUpdatedDay`, optional `itemInstanceId`)
  - `reclaimEntries[]` for daily cleanup handoff (`scenePath`, `objectId`, `itemInstanceId`, `cleanedOnDay`)
- `ContainerStorage` module payload: `containers[]` keyed by `containerId` with stored item-instance IDs
- `PlayerDevice` module payload: `selectedTarget`, `activeGroupShots[]`, `savedGroups[]`, `notesText`, `installedHooks[]`
- `WorkbenchLoadout` module payload: `workbenches[]` keyed by `workbenchId` with nested `slotNodes[]`
- `ContractState` module payload: `contractId`, `targetId`, `distanceBand`, `payout`, `generatedContractIds[]`, `completedContractIds[]`
- `PoliceHeatState` module payload: `level`, `lastCrimeType`, `searchTimeRemainingSeconds`, `hasLineOfSightToPlayer`

`chamberRound` and `magazineRounds[]` serialize ammo ballistic snapshots for the active weapon state (`ammoSource`, `muzzleVelocityFps`, `velocityStdDevFps`, `projectileMassGrains`, `ballisticCoefficientG1`, `dispersionMoa`).

In-flight projectile state is intentionally out-of-scope for v0.1 saves.

Schema note:
- Runtime save schema is now `v6`.
- `SchemaV1ToV2AddWorldObjectStateMigration` inserts a default `WorldObjectState` block when loading older saves.
- `SchemaV2ToV3AddContainerStorageMigration` inserts a default `ContainerStorage` block when loading older saves.
- `SchemaV3ToV4AddPlayerDeviceMigration` inserts a default `PlayerDevice` block when loading older saves.
- `SchemaV4ToV5AddWorkbenchLoadoutMigration` inserts a default `WorkbenchLoadout` block when loading older saves.
- `SchemaV5ToV6AddContractAndPoliceHeatStateMigration` inserts default `ContractState` and `PoliceHeatState` blocks when loading older saves.
- Load remains transactional: missing required module blocks still fail before restore.

The broader schema below is the v0.1 design target and forward schema contract. Treat it as planned module scope until those modules are registered and migration-backed in runtime.

### Feature Flags and Module Registration Coherence [v0.1]

- `SaveFeatureFlags` are declarative capability toggles, not runtime guarantees by themselves.
- A flag may be enabled only when the corresponding module is registered in `SaveBootstrapper` and covered by migration-aware payload handling.
- Introducing a new enabled save feature requires all of: registration, payload contract, restore validation, and migration notes in the same change.

### Save Module Readiness Matrix [v0.1]

| Module / Payload Block | Readiness | Runtime Registration (v0.1) | Notes |
|------------------------|-----------|------------------------------|-------|
| `CoreWorld` | Implemented now | Yes | Persists `dayCount`, `timeOfDay`. |
| `Inventory` | Implemented now | Yes | Persists carried/belt/backpack ids + capacity + belt selection. |
| `Weapons` | Implemented now | Yes | Persists active weapon loadout + chamber/mag ammo ballistic snapshots. |
| `WorldObjectState` | Implemented now | Yes | Unified world-object state + reclaim entries for daily cleanup. |
| `ContainerStorage` | Implemented now | Yes | Persists stored item-instance IDs for containers/chests. |
| `PlayerDevice` | Implemented now | Yes | Persists selected target, shot groups, notes, and installed hooks. |
| `WorkbenchLoadout` | Implemented now | Yes | Persists nested workbench slot loadouts by `workbenchId`. |
| `ContractState` | Implemented now | Yes | Persists the active assassination contract plus generated/completed contract history IDs. |
| `PoliceHeatState` | Implemented now | Yes | Persists current police heat/search state for wanted-level recovery. |
| `PlayerState` | Planned target | No | Listed in target schema only. |
| `ItemRegistry` | Planned target | No | Listed in target schema only. |
| `ItemLocation` | Planned target | No | Listed in target schema only. |
| `WorldItemState` | Planned target | No | Listed in target schema only. |
| `RecipeBook` | Planned target | No | Listed in target schema only. |
| `NPCState` | Planned target | No | Listed in target schema only; may be empty in early phases. |
| `QuestState` | Planned target | No | Listed in target schema only. |
| `WorldState` (extended fields) | Planned target | No | `dayCount`/`timeOfDay` are currently in `CoreWorld`; other fields are planned. |
| `WorkshopState` | Planned target | No | Listed in target schema only. |
| `VehicleState` | Planned target | No | Listed in target schema only. |

Use this matrix as the implementation source of truth for v0.1 feature work: only `Implemented now` rows are currently persistence-backed in runtime.

## World Object Persistence Policy + Day Boundary [v0.2]

- Runtime world-object state is keyed by `scenePath + objectId` and applied on scene load.
- Scene policy mode controls cleanup behavior:
  - `Persistent`: records remain across day changes.
  - `DailyReset`: records are retained same-day, then cleaned on day increment.
- Day-boundary cleanup moves cleaned `DailyReset` records with `itemInstanceId` into reclaim storage so state is not silently lost.
- Travel behavior no longer depends on ownership-based pickup hiding workarounds; scene apply uses the unified world-object state contract.

## Verification Sweep Expectations [v0.2]

When this contract changes, expected suites include:

- Core save EditMode coverage (`WorldObjectStateSaveModuleTests`, migration + module registration/load invariants).
- Core persistence PlayMode coverage (`WorldObjectPersistenceRuntimeBridgePlayModeTests`, including `DailyReset` cleanup + reclaim behavior).
- World travel PlayMode coverage (`RoundTripTravelPlayModeTests`, including no ownership-based hide workaround).
- Pickup flow coverage impacted by world identity/state capture (`PlayerInventoryControllerPlayModeTests`, `PickupTargetWorldIdentity*` tests).

## Save Data Structure (Target Schema) [v0.1]

The canonical gameplay payload blocks remain:

```
SaveData (logical domain content carried by modules)
├── PlayerState
│   ├── position, rotation, currentScene
│   ├── health
│   ├── activeDebuffs[] (shaky hands, tinnitus, blurry vision + duration)
│   ├── money
│   ├── reputation scores (contract reliability, ammo crafting, fixer trust, legal heat)
│   └── arrest/death recovery metadata
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
├── ContractState[]
│   ├── activeContractId
│   ├── generatedContracts[]
│   └── completedContractHistory[]
├── WorldState
│   ├── dayCount, timeOfDay
│   ├── weather state + seed
│   ├── shop restock timers
│   └── police heat / search state
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

Forward-compatibility rule: target schema blocks for later-phase systems (for example `NPCState`, `ContractState`, and advanced law-enforcement state) are defined from v0.1 so save files stay migratable. In phases where those systems are not active yet, these blocks can remain empty/default when modules exist, and may be absent in earlier implementation slices.

**Exact restore target contract [v0.1]:** Loading a save should restore the same world state the player left for every active system in the current phase: player transform, dropped item transforms (including floor items), inventory/container contents, weapon/vehicle state, and progression flags. If NPC simulation is active, NPC transforms/state should restore exactly as well. **Current implemented scope is partial** (`CoreWorld`, `Inventory`, `Weapons`, `WorldObjectState`) and full exact restoration remains in progress per `v0.1-demo-status-and-milestones.md`.

**Save format:** JSON envelope + per-module payload JSON. Each domain module serializes its own payload contract; migration code evolves schema versions explicitly.

---

## Game Loop [v0.1]

```
WAKE UP     → At home (bed), or wherever the player fell asleep,
              or at a police station / hospital after a failed job.
              Check debuffs, check quest deadlines.

MORNING     → Decide what to do today:
              - Accept or review a contract
              - Reload ammo at the workshop bench
              - Buy parts and supplies
              - Head to the range to validate the setup
              - Scout a route or firing position
              - Do side work for cash

DAYTIME     → Execute the plan. Multiple activities in one day
              are possible depending on time management.
              Time passes during activities.

EVENING     → Return home (or don't — player choice).
              Collect payout, organize inventory, review heat and earnings.
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
| Mid | Better optics, digital scale, powder measure, contract-intel tools | Earn from clean contracts |
| Late | Progressive press, electronic dispenser, chronograph, premium optics | Major purchase or high-tier contract chain |
| Endgame | Bullet casting, custom dies, automated systems, employees | Long-term investment |

## Weapon Progression [v1+]

| Phase | Weapons |
|-------|---------|
| Start | Grandpa's bolt-action rifle (one caliber, worn but functional) |
| Early | Buy a sidearm, basic optics, or support gear for lower-risk jobs |
| Mid | Precision rifle for long range and cleaner contract execution |
| Late | Multiple specialized weapons for different contract types |
| Endgame | Custom-built rifles, wildcat chamberings, full premium contract arsenal |

## World Progression [v1+]

| Phase | Access |
|-------|--------|
| Start | House + town + local range |
| Mid | More contract spaces, better vantage routes, wider town access |
| Late | Premium job locations, second property, stronger escape infrastructure |
| Endgame | Full map access, multiple operating spaces, hire employees |

## Reputation Progression [v1+]

| Reputation | Effect |
|-----------|--------|
| Ammo quality rep | Better trust in your contract-prep competence |
| Contract reliability | Higher-tier jobs, cleaner intros, better payouts |
| Fixer trust | Access to safer intel and gear sources |
| Legal heat | Affects police suspicion, search intensity, NPC caution |

**Implementation note:** Precision systems (component sorting, annealing tracking, per-lot powder variance, fire-forming) are designed into the data model from v0.1 even though full gameplay implementation is phased. The data fields exist on SOs and instances from day one — the gameplay UI and interactions that expose them may come in later versions. See prototype-scope.md for version targets.

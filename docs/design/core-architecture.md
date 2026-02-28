# Reloader — Core Architecture

> **For agents:** This is the shared architecture reference. Read this before working on any game system. For domain-specific design, see the relevant module doc in this folder — check [README.md](README.md) for routing.

**Game:** Reloader — a first-person open-world reloading simulator with hunting and shooting competitions.

**Engine:** Unity 6.3, Universal Render Pipeline (URP), Universal 3D template.

**Platform:** PC only (Windows/Mac/Linux).

**Perspective:** First person. **Singleplayer only.**

**Development:** Solo developer assisted by AI agents (Opus 4.6, Codex 5.3, etc).

**Comparable games:** My Summer Car (sandbox simulation, learn by failure), Schedule 1 (open world game loop, persistent items, driving).

**Core differentiators:**
- No hand-holding. The game simulates physics and consequences, not rules.
- Fire-formed brass tracking. Brass fired in a specific chamber fits that chamber better.
- Modular everything. Every weapon part is individually tracked, worn, and swappable.
- Full reloading spectrum. From basic pistol plinking ammo to extreme long range wildcat cartridges.
- Extensible architecture. Every system is built to accept new content without code changes.

---

## Implementation Status Contract [v0.1]

This document may include both:
- **Implemented in repository** contracts (current APIs/types in `Reloader/Assets/_Project/**`)
- **Target design** contracts (planned milestone architecture)

For coding tasks, prefer implemented contracts unless the task explicitly requests forward-design work.

Terminology contract:
- `SaveCoordinator` is the canonical current save orchestration term.
- If a save facade is introduced later, document it as a thin wrapper over `SaveCoordinator` rather than a separate save architecture.

---

## Project Structure [v0.1]

Feature-based folder organization. Each feature is self-contained so an AI agent can work on one system without understanding the whole project.

```
Reloader/Assets/
├── _Project/                    # All custom code and assets
│   ├── Core/                    # Shared utilities, singletons, events, save system
│   ├── Player/                  # FPS controller, camera, interaction system
│   ├── Weapons/                 # Weapon models, shooting, ballistics
│   ├── Reloading/               # Core system: press, dies, powder, bench
│   ├── Inventory/               # Item management, storage, weight/capacity
│   ├── Economy/                 # Currency, transactions, pricing, shops
│   ├── World/                   # Terrain, locations, scene management
│   ├── NPCs/                    # NPC behavior, dialogue, vendors
│   ├── Competitions/            # Shooting competitions, scoring, rules
│   ├── Hunting/                 # Animal AI, tracking, scoring
│   ├── Quests/                  # Quest/mission system
│   ├── LawEnforcement/          # Police, wardens, legal system
│   ├── Vehicles/                # Driveable cars, transport
│   ├── UI/                      # Shared UI components, HUD, menus
│   └── Audio/                   # SFX, music, ambient
├── ThirdParty/                  # Asset store packages (never modify directly)
├── Scenes/
│   ├── Bootstrap.unity          # Runtime entrypoint for persistent bootstrap flow
│   ├── MainWorld.unity          # Current baseline scene scaffold (compatibility-only; not active topology source of truth)
│   ├── MainMenu.unity           # Planned menu scene (not in scaffold yet)
│   └── ...
└── Resources/                   # Optional. Add only for runtime name-based loading.
```

Each feature folder contains: `Scripts/`, `Data/` (SO assets), `Prefabs/`, optionally `UI/` and `Scenes/`. Global scenes live at `Reloader/Assets/Scenes/` (runtime topology starts from `Bootstrap`). Active world scenes are under `Reloader/Assets/_Project/World/Scenes/` (`MainTown`, `IndoorRangeInstance` for current v0.1 slice). `MainWorld` remains compatibility-only during migration.

**Rules:**
- `_Project/` prefix keeps custom code sorted to the top in Unity's Project window.
- `ThirdParty/` is never modified. Custom code imports from it but never changes it.
- Each feature folder is self-contained by default. Cross-feature changes are allowed only when the task explicitly spans domains, and should be limited to event contracts/shared interfaces rather than direct system coupling.
- ScriptableObject data assets live in `Data/` subfolders alongside their scripts.

---

## ScriptableObject Data Assets [v0.1]

Every "thing" in the game is defined as a ScriptableObject (SO) — a data file that lives in the project, not in a scene.

```
ItemDefinition (abstract base SO — physical items the player can carry/own)
├── WeaponDefinition       → platform/model, default parts, part slots, price, weight, magazineCapacity
├── WeaponPartDefinition   → abstract base for swappable weapon parts:
│   ├── BarrelDefinition   → caliber, twistRate, freeBore, quality, profile, length, isFreefloated
│   ├── ActionDefinition   → quality, beddingType, ratedPressure
│   ├── TriggerDefinition  → pullWeight, creep, type
│   ├── OpticDefinition    → magnification, quality, mountQuality, reticleType
│   ├── MuzzleDeviceDefinition → type, quality, suppressionRating
│   └── GenericPartDefinition  → bolt, stock, grip, bipod, cosmetics, etc.
├── ComponentDefinition    → type (powder/primer/case/bullet), properties
│   ├── PowderDefinition
│   ├── BulletDefinition   (projectile, not cartridge)
│   ├── CaseDefinition
│   └── PrimerDefinition
├── EquipmentDefinition    → type (press/die/scale/etc.), capabilities
├── ConsumableDefinition   → food, lube, cleaning supplies
└── VehicleDefinition      → capacity, speed, fuel consumption, price

CaliberDefinition (standalone SO — reference data, not a physical item)
→ dimensions, pressure limits, headspace, bolt face
→ Compatible weapons derived at runtime from barrel definitions

FactoryAmmoTemplate (standalone SO — spawning template, not a physical item)
→ brand, SKU, quality tier, component references, factory consistency profile, SAAMI compliance flag
→ Used by shops to spawn pre-built AmmoInstance batches from component definitions with factory-process variance targets
```

To add a new item: create one `.asset` file with stats. No code changes required.

Every SO base class includes an extensibility field:

```csharp
[System.Serializable]
public class CustomProperty
{
    public string key;
    public float value;
}

public List<CustomProperty> customProperties;
```

---

## Manager Singletons [v0.1]

Managers survive scene transitions via `DontDestroyOnLoad`:

| Runtime entrypoint | Responsibility |
|--------------------|---------------|
| `GameManager` | Game state, time of day, day cycle, pause, game-over |
| `SceneLoader` | Scene transitions, loading screens, additive loading |
| `SaveCoordinator` (service) | Save envelope capture/load orchestration, migration chain, deterministic module restore order |
| `InventoryManager` | Player carried items, weight, capacity |
| `EconomyManager` | Money, transactions, price fluctuations |
| `QuestManager` | Active quests, progress tracking, rewards |
| `AudioManager` | Music, SFX, ambient sounds per scene |

Manager entries provide a static `Instance` property (singleton pattern). `SaveCoordinator` is a plain service built by `SaveBootstrapper`, not a `DontDestroyOnLoad` singleton manager. Gameplay code should use runtime event ports/hub (`IGameEventsRuntimeHub` via `RuntimeKernelBootstrapper.Events`) for cross-system communication, not call other managers directly. Managers may call each other's `Instance` for infrastructure tasks.

**Domain managers:** Not every game system requires a persistent singleton manager. Systems like Hunting, Competitions, Law Enforcement, and Vehicles may use scene-level MonoBehaviours or static utility classes instead. The singletons above are for systems that MUST persist across scene transitions. Domain-specific management patterns are defined in each domain's design doc.

---

## Event Bus [v0.1]

Systems communicate through runtime event ports + a runtime hub.

### Current `GameEvents` surface (implemented in repository) — legacy compatibility note

`GameEvents` is retired. Keep this heading for guardrail compatibility only; runtime code must use `IGameEventsRuntimeHub` and related event-port interfaces.

### Current runtime event contract (implemented in repository)

- `IGameEventsRuntimeHub` is the canonical cross-domain contract surface.
- It composes bounded event ports: `IRuntimeEvents`, `IInventoryEvents`, `IWeaponEvents`, `IShopEvents`, `IUiStateEvents`, `IInteractionHintEvents`.
- `DefaultRuntimeEvents` is the default in-process hub implementation.
- `RuntimeKernelBootstrapper.Events` is the runtime access point used by modules/adapters.
- Cross-domain extension guardrails are defined in [extensible-development-contracts.md](extensible-development-contracts.md).

### Target cross-domain `GameEvents` surface (planned) — represented as runtime hub contracts

```csharp
public interface IGameEventsRuntimeHub
{
    event Action<ItemInstance> OnItemPickedUp;
    event Action<ItemInstance> OnItemDropped;
    event Action<float> OnMoneyChanged;
    event Action<AmmoInstance> OnAmmoAssembled;
    event Action<CompetitionResult> OnCompetitionEnded;
    event Action OnDayAdvanced;
    event Action<WeaponInstance, AmmoInstance> OnWeaponFired;
    event Action<CrimeType> OnCrimeCommitted;
    event Action<VehicleInstance> OnVehicleParked;
    event Action<VehicleInstance> OnVehicleDriven;
    event Action<AnimalInstance, AmmoInstance> OnAnimalKilled;
    event Action<string> OnHuntingViolation;
    event Action<QuestInstance> OnQuestCompleted;
    event Action<QuestInstance> OnQuestFailed;
    event Action<string, float> OnReputationChanged;
    event Action<NPCInstance, float> OnRelationshipChanged;
    event Action<CrimeType, float> OnFineIssued;
    event Action OnShopTransaction;
}
```

When one system fires an event, it doesn't need to know what other systems listen. This keeps systems isolated.

---

## Runtime Instances vs. Definitions [v0.1]

- **Definition** (SO) = template/blueprint. "A .308 Winchester case made by Lapua." Exists once in the project.
- **Instance** (runtime class) = a specific physical item. "This particular .308 case, fired 3 times in weapon #A7F2." Created at runtime, serialized by the save pipeline (`SaveCoordinator` + domain modules).

```csharp
[System.Serializable]
public class ItemInstance
{
    public string uniqueID; // GUID
    [SerializeField] private ItemDefinition _definition; // canonical storage (nullable for composites like AmmoInstance)
    public ItemDefinition definition => _definition;
    public List<CustomProperty> customProperties; // extensibility — same pattern as definitions
}
```

**Contract rule:** `ItemInstance.definition` is the only definition field. Subclasses must not redeclare `definition` with typed variants. Use cast/accessor helpers instead:

```csharp
public class CasingInstance : ItemInstance
{
    public CaseDefinition CaseDefinition => definition as CaseDefinition;
}
```

All instance subclasses use explicit typed fields for runtime state only. The `customProperties` list provides extensibility using the same serializable pattern as definitions. AmmoInstance is a composite type that uses explicit provenance fields (`ammoSource`, optional `factoryTemplateID`) and may keep `definition` null because factory templates are standalone SOs, not `ItemDefinition` (see reloading-system.md).

---

## Persistence Contract and Evolution [v0.1]

Save/load uses a versioned envelope contract so milestone evolution stays compatible:

- `SaveEnvelope.schemaVersion` is the migration source/target.
- `SaveEnvelope.modules` stores per-domain `ModuleSaveBlock` payloads (`moduleVersion` + `payloadJson`).
- Compatibility is guaranteed milestone-to-milestone (`v0.1` -> `v0.2`) via explicit migration steps.
- Unknown module blocks are ignored safely; missing required blocks fail before module restore.

Deterministic restore order is mandatory. Baseline ordering is:
1. `CoreWorld`
2. `Inventory`
3. Additional systems in explicit registered order

When changing persisted runtime fields, update three artifacts in the same change:
1. Domain payload contract
2. Save/load module implementation
3. Migration notes/steps for schema evolution

---

## Design Principles [v0.1]

These principles govern every design decision:

1. **Simulate, don't restrict.** Never prevent the player from doing something. Simulate the consequence instead.
2. **Extensible by default.** Every SO has `customProperties`. Every system accepts new content without code changes.
3. **Feature isolation.** Each system lives in its own folder. Communication goes through runtime event ports/hub (`IGameEventsRuntimeHub`).
4. **Data-driven content.** New content is added via ScriptableObject assets, not code changes.
5. **Earned knowledge, not given.** Players learn by experimentation and consequences, not UI popups.
6. **Physical world.** Items exist in the world as physical objects. No abstract inventory-only items.
7. **Prototype fast, polish later.** Get systems working with placeholder art first.

---

## Runtime UI Contract [v0.1]

- Runtime gameplay UI uses **UI Toolkit** (`UIDocument` + UXML/USS templates).
- View/binder layers are **dumb** by contract:
  - They only query elements, render state, and emit intents.
  - They never call gameplay/economy/reloading services directly.
- Domain wiring is centralized in screen controllers/adapters.
- Runtime extension points are required:
  - Action mapping table (intent key -> command mapping)
  - Screen composition config (enabled components per screen)
  - Stable element naming conventions for query/style contracts
- See [extensible-development-contracts.md](extensible-development-contracts.md) for enforced UI/runtime bridge and integration workflow rules.

---

## Asset Store Packages in Use [v0.1]

| Asset | Use |
|-------|-----|
| Prototype Map | World blocking, terrain layout |
| Modular First Person Controller | Player movement, camera, interaction base |
| Low Poly Weapons VOL.1 | Weapon models |
| Survivalist Character | Player model or NPC base |
| Low-Poly Simple Nature Pack | Hunting environments, outdoor areas |

---

## Domain Design Docs [v0.1]

Each game system has its own detailed design doc. See [README.md](README.md) for routing.

| Domain | Doc | Related Skills |
|--------|-----|----------------|
| Reloading System | [reloading-system.md](reloading-system.md) | `reloading-domain-knowledge`, `adding-game-content` |
| Weapons & Ballistics | [weapons-and-ballistics.md](weapons-and-ballistics.md) | `reloading-domain-knowledge` |
| World & Vehicles | [world-and-vehicles.md](world-and-vehicles.md) | — |
| Inventory & Economy | [inventory-and-economy.md](inventory-and-economy.md) | `adding-game-content` |
| Hunting & Competitions | [hunting-and-competitions.md](hunting-and-competitions.md) | — |
| NPCs & Quests | [npcs-and-quests.md](npcs-and-quests.md) | — |
| Law Enforcement | [law-enforcement.md](law-enforcement.md) | — |
| Save System & Progression | [save-and-progression.md](save-and-progression.md) | — |
| Extensible Development Contracts | [extensible-development-contracts.md](extensible-development-contracts.md) | `writing-agent-docs`, `reviewing-design-docs` |
| Prototype Scope | [prototype-scope.md](prototype-scope.md) | — |

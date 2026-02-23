# World & Vehicles Design

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.

---

## Main World [v0.1]

One non-instanced Unity scene containing the entire playable town area:

- **Player's House** — bedroom (sleep/save), kitchen, office with computer (online catalog orders), garage/spare room as starter workshop
- **Town** — streets, buildings, NPCs walking around
- **Gun Store** — enter through door (no loading screen), buy weapons/ammo/accessories
- **Reloading Supply** — components, equipment, dies, tools
- **General Store** — food, cleaning supplies, miscellaneous
- **Gas Station** — refuel vehicles, buy snacks/drinks
- **Town Hall** — purchase hunting licenses, tags, and permits
- **Shooting Range** — practice lanes + competition area
- **Roads** — driveable roads connecting all locations
- **Checkpoints** — at world edges, optional transitions to separate instanced areas when implemented

The player walks/drives between all main-world locations with zero loading screens. The main world is a small town, not a city — performance is manageable.

**Hospital** — the player is taken here after catastrophic reloading failures or severe injuries. Not a player-navigable location in v0.1; implemented as a time-skip event with medical bills and debuffs.

---

## Instanced Scenes [v0.2]

Checkpoint-driven separate scenes loaded when the player reaches a world-edge checkpoint and chooses to enter:

- **Hunting Grounds** — multiple biomes (forest, plains, mountains), each a separate scene (v1+ gameplay)
- **Competition Venues** — for major/special competitions that need unique layouts
- **Future expansion areas** — new towns, wilderness areas, etc.

Transition (default): player drives to checkpoint in MainWorld -> interact -> loading screen -> arrive at destination spawn point.

Vehicle scope rule:
- Free driving is scoped to MainWorld roads.
- Instanced scenes use on-foot gameplay by default unless a specific scene explicitly opts into drivable vehicles.
- The vehicle is the travel trigger and persistence anchor (parked transform + cargo snapshot) for checkpoint transitions.

---

## Building Interiors [v0.1]

Most building interiors are part of the main world scene (enter through doors with trigger colliders, no loading). For very large interiors or future expansion buildings, use additive scene loading.

---

## Workshop Evolution [v1+]

- **Starter:** Garage or spare room in the player's house (part of house interior, same scene)
- **Upgraded:** Purchase a dedicated workshop building in town (separate interior in main world)
- **Premium:** Large workshop with multiple benches, storage, employee workspace

Workshop equipment is physically placed by the player. Tools sit on benches. Shelves hold components. Everything is a physical object with a position.

---

## Vehicles [v0.2]

Vehicles provide transportation between locations in MainWorld and serve as the travel mechanism for checkpoint transitions.

**VehicleDefinition (SO):**
- Capacity (cargo space for hauling game, equipment, ammo)
- Speed
- Fuel consumption
- Price

**VehicleInstance (runtime):**
- Fuel level
- Condition (wear, damage)
- Cargo contents (list of ItemInstance)
- Parked location (transform data)

**Driving mechanics:**
- Third-person or first-person driving camera
- Roads connect all main-world locations
- Vehicle trunk acts as mobile storage container
- Fuel management (refuel at gas station in town)
- Driving simulation is limited to MainWorld in baseline architecture
- Instanced destinations can reference vehicle cargo state without enabling full in-scene driving

**Integration points:**
- `GameEvents.OnVehicleParked` / `GameEvents.OnVehicleDriven` for other systems
- Law enforcement can perform vehicle stops on roads when that system is active (see [law-enforcement.md](law-enforcement.md))
- Carry capacity limits what you can bring to/from hunting once hunting gameplay is enabled (see [hunting-and-competitions.md](hunting-and-competitions.md))

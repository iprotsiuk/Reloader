# World & Vehicles Design

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.

---

## Main World [v0.1]

Current implemented v0.1 world slice uses:
- `Bootstrap` (runtime entrypoint) -> `MainTown` hub -> `IndoorRangeInstance` activity scene.
- `MainWorld` is a temporary compatibility scene during migration and is not the primary runtime topology source of truth.

MainTown target layout continues to represent the core town experience:

- **Player's House** — bedroom (sleep/save), kitchen, office with computer (online catalog orders), garage/spare room as starter workshop
- **Town** — streets, buildings, NPCs walking around
- **Gun Store** — enter through door (no loading screen), buy weapons/ammo/accessories
- **Reloading Supply** — components, equipment, dies, tools
- **General Store** — food, cleaning supplies, miscellaneous
- **Gas Station** — refuel vehicles, buy snacks/drinks
- **Town Hall** — purchase hunting licenses, tags, and permits
- **Shooting Range access** — indoor range currently reached via travel flow into `IndoorRangeInstance`
- **Roads** — forward target for driveable links between locations
- **Checkpoints** — forward target for edge transitions to additional instanced areas

In the current implemented slice, travel between MainTown and indoor range uses explicit scene travel. Seamless no-load interiors remain a design target for broader town coverage.

**Hospital** — the player is taken here after catastrophic reloading failures or severe injuries. Not a player-navigable location in v0.1; planned as a future time-skip consequence event with medical bills/debuff hooks.

---

## Instanced Scenes [v0.1]

Checkpoint-driven separate scenes loaded when the player reaches a world-edge checkpoint and chooses to enter:

- **Hunting Grounds** — multiple biomes (forest, plains, mountains), each a separate scene (v1+ gameplay)
- **Competition Venues** — for major/special competitions that need unique layouts
- **Future expansion areas** — new towns, wilderness areas, etc.

Current implemented transition: player interacts with authored travel triggers for `MainTown <-> IndoorRangeInstance`.
Forward transition target: player drives to checkpoint in town-world routes -> interact -> loading screen -> destination spawn.

Vehicle scope rule:
- Free driving is scoped to active town-world roads once driving is enabled.
- Instanced scenes use on-foot gameplay by default unless a specific scene explicitly opts into drivable vehicles.
- The vehicle is the travel trigger and persistence anchor (parked transform + cargo snapshot) for checkpoint transitions.

---

## Building Interiors [v0.1]

Current slice includes both authored in-scene spaces and scene-travel based interiors (IndoorRangeInstance). For future expansion buildings, additive scene loading or reusable instance scenes are valid.

---

## Workshop Evolution [v1+]

- **Starter:** Garage or spare room in the player's house (part of house interior, same scene)
- **Upgraded:** Purchase a dedicated workshop building in town (separate interior in main world)
- **Premium:** Large workshop with multiple benches, storage, employee workspace

Workshop equipment is physically placed by the player. Tools sit on benches. Shelves hold components. Everything is a physical object with a position.

---

## Vehicles [v0.2]

Vehicles provide transportation between locations in town-world routes and serve as the travel mechanism for checkpoint transitions once driving is in scope.

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
- Driving simulation is limited to active town-world routes in baseline architecture
- Instanced destinations can reference vehicle cargo state without enabling full in-scene driving

**Integration points:**
- Runtime hub vehicle events `OnVehicleParked` / `OnVehicleDriven` for other systems (formerly `GameEvents` hooks)
- Law enforcement can perform vehicle stops on roads when that system is active (see [law-enforcement.md](law-enforcement.md))
- Carry capacity limits what you can bring to/from hunting once hunting gameplay is enabled (see [hunting-and-competitions.md](hunting-and-competitions.md))

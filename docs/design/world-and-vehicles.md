# World & Vehicles Design

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.

---

## Main World [v0.1]

Current implemented v0.1 world slice uses:
- `Bootstrap` (runtime entrypoint) -> `MainTown` hub -> `IndoorRangeInstance` activity scene.
- `MainWorld` is a temporary compatibility scene during migration and is not the primary runtime topology source of truth.

`MainTown` is the player's operating sandbox for assassination contracts. It still contains the house/workshop and commerce loop, but the town must also support target routines, witnesses, police response, and clean escape routes.

MainTown target layout:
- **Player's House** — bedroom (sleep/save), kitchen, office with computer/terminal, garage or spare room as starter workshop
- **Town** — streets, buildings, civilians, police presence, target routines, and witness lines
- **Gun Store** — buy weapons, factory ammo, accessories, optics
- **Reloading Supply** — buy components, dies, tools, and bench upgrades
- **General Store** — food, cleaning supplies, miscellaneous
- **Gas Station** — refuel vehicles, buy snacks/drinks
- **Contract access point** — handler, terminal, or board for assassination jobs and payouts
- **Shooting Range access** — indoor range currently reached via travel flow into `IndoorRangeInstance` and used as a prep/validation space
- **Police station** — arrest respawn and confiscation-recovery anchor
- **Sniper vantage structure** — rooftops, windows, alleys, parking lots, and sightlines that support remote execution plus escape
- **Roads** — forward target for driveable links between locations
- **Checkpoints** — forward target for edge transitions to additional instanced contract areas or escape routes

In the current implemented slice, travel between MainTown and indoor range uses explicit scene travel. Seamless no-load interiors remain a design target for broader town coverage.

**Hospital** — the player is taken here after severe injury or a failed police encounter. In the contract fantasy it is a respawn/consequence anchor, not just a reloading-failure note.

---

## Instanced Scenes [v0.1]

Checkpoint-driven separate scenes loaded when the player reaches a world-edge checkpoint and chooses to enter:

- **Indoor Range** — current validation/test space for the precision-prep loop
- **Contract Spaces** — authored scenes for special targets, protected compounds, industrial yards, or remote overwatch jobs
- **Future expansion areas** — new towns, wilderness areas, etc.

Current implemented transition: player interacts with authored travel triggers for `MainTown <-> IndoorRangeInstance`.
Forward transition target: player drives to checkpoint in town-world routes -> interact -> loading screen -> destination spawn.

Vehicle scope rule:
- Free driving is scoped to active town-world roads once driving is enabled.
- Instanced scenes use on-foot gameplay by default unless a specific scene explicitly opts into drivable vehicles.
- The vehicle is the travel trigger and persistence anchor (parked transform + cargo snapshot) for checkpoint transitions.

---

## Building Interiors [v0.1]

Current slice includes both authored in-scene spaces and scene-travel based interiors (`IndoorRangeInstance`). For future expansion buildings, additive scene loading or reusable instance scenes are valid.

---

## Workshop Evolution [v1+]

- **Starter:** garage or spare room in the player's house (part of house interior, same scene)
- **Upgraded:** purchase a dedicated workshop building in town
- **Premium:** large workshop with multiple benches, storage, and contract-prep space

Workshop equipment is physically placed by the player. Tools sit on benches. Shelves hold components. Everything is a physical object with a position.

---

## Vehicles [v0.2]

Vehicles provide transportation between locations in town-world routes and later become part of contract staging and escape.

**VehicleDefinition (SO):**
- Capacity (cargo space for gear, evidence, and ammo)
- Speed
- Fuel consumption
- Price

**VehicleInstance (runtime):**
- Fuel level
- Condition (wear, damage)
- Cargo contents (list of `ItemInstance`)
- Parked location (transform data)

**Driving mechanics:**
- Third-person or first-person driving camera
- Roads connect all main-world locations
- Vehicle trunk acts as mobile storage container
- Fuel management (refuel at gas station in town)
- Driving simulation is limited to active town-world routes in baseline architecture
- Instanced destinations can reference vehicle cargo state without enabling full in-scene driving

**Integration points:**
- Runtime hub vehicle events `OnVehicleParked` / `OnVehicleDriven`
- Law enforcement can perform vehicle stops on roads when that system is active (see [law-enforcement.md](law-enforcement.md))
- Vehicle routes can later become part of contract staging and escape planning

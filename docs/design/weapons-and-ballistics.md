# Weapons & Ballistics Design

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.
> **Related:** [reloading-system.md](reloading-system.md) for ammo data models that feed into ballistics.
> **Related skill:** `.agent/skills/reloading-domain-knowledge/SKILL.md` for the full accuracy calculation and ballistics reference.

---

## Modular Weapon System [v0.1]

Every weapon is an assembly of individual parts. Each part has its own condition and can be swapped.

```
WeaponInstance
├── definition          → WeaponDefinition SO (base platform/model)
├── parts[]             → list of WeaponPartInstance:
│   ├── Barrel          → round count, throat erosion, accuracy degradation
│   │                      caliber → CaliberDefinition (chambering is a barrel property)
│   │                      chamberID → unique GUID (for fire-forming tracking)
│   │                      twistRate → inches per rotation (e.g., 1:10")
│   │                      freeBore → distance from case mouth to rifling lands (inches).
│   │                      Erodes forward with use (throatErosion accumulates).
│   │                      quality → match-grade lapped vs production vs budget
│   │                      profile → heavy/bull, sporter, lightweight
│   │                      length → barrel length in inches
│   │                      isFreefloated → bool (free-floated vs stock-contact)
│   ├── Action/Receiver → locking lug wear, bolt face condition, ratedPressure (action strength)
│   ├── Bolt            → wear, firing pin condition
│   ├── Trigger         → type (milspec, match, adjustable), pull weight
│   ├── Stock/Chassis   → wood, polymer, folding, custom chassis
│   ├── Grip            → type, material
│   ├── Handguard/Forend→ type, rail space for accessories
│   ├── Muzzle Device   → brake, suppressor, flash hider, harmonic tuner
│   ├── Optic/Mount     → scope, red dot, iron sights, magnifier
│   ├── Bipod           → optional, adjustable
│   └── Cosmetics       → cerakote color, stickers, engravings
├── magazineStack[]     → ordered list of AmmoInstance (order = feeding order)
├── magazineCapacity    → max rounds the magazine holds
├── chamberRound        → AmmoInstance or null
└── cleanlinessLevel    → affects reliability
```

**Part condition is per-part, not per-weapon.** A rifle with a shot-out barrel but a perfect action just needs a new barrel.

**Muzzle device slot is generic** — accepts any `WeaponPartDefinition` with `slotType == MuzzleDevice` (brakes, suppressors, flash hiders, harmonic tuners, etc.).

**Caliber is a barrel property, not a weapon property.** An AR-15 lower receiver accepts .223, .300 Blackout, or 6.5 Grendel barrels. Swapping barrels changes the weapon's effective caliber. Access via `weapon.barrel.caliber`. Similarly, `chamberID` lives on the barrel — swapping barrels changes which chamber brass is fire-formed to.

---

## Ballistics Model [v1+]

Full exterior ballistics simulation for shooting:

| Factor | Source | Effect |
|--------|--------|--------|
| Bullet BC | BulletDefinition (G1/G7) | Base trajectory curve |
| Muzzle velocity | AmmoInstance.estimatedVelocity | Drop rate |
| Wind speed + direction | Weather system | Lateral drift |
| Altitude | Scene/location data | Air density → drop correction |
| Temperature | Weather system | Air density + powder burn rate |
| Humidity | Weather system | Minor air density effect |
| Coriolis effect | Player compass heading + latitude | Deflection at extreme range |
| Spin drift | Barrel twist rate + bullet length | Lateral drift at long range |
| Scope cant | Player rifle cant angle | Diagonal miss at range |
| Barrel condition | BarrelPart.roundCount | Velocity loss as throat erodes |
| Jump (distance from lands) | barrel.freeBore + barrel.throatErosion vs. ammo.cbto | Group size multiplier |

**Ammo quality directly affects ballistic consistency.** Hand-tuned match ammo has tight velocity spread → tight groups. Sloppy ammo has wide velocity spread → vertical stringing at distance.

---

## Precision / Benchrest Factors [v1+]

Niche accuracy factors that casual players can ignore but dedicated benchrest competitors must master:

| Factor | Detail | Match Grade Spec |
|--------|--------|-----------------|
| Bullet weight sorting | Weigh every bullet, sort into groups | ±0.1-0.3gr for match |
| Bullet diameter consistency | Measure with micrometer | ±0.0001" match |
| Case weight sorting | Indicates internal volume consistency | ±0.5gr groups |
| Case neck wall thickness | Uneven = non-concentric bullet release | Neck turn to ±0.0005" |
| CBTO consistency | Repeatable measurement for seating depth | ±0.0005" for match |
| Distance from lands (jump) | Requires OAL gauge to measure per rifle+bullet | 0.010-0.030" off lands typical |
| Cartridge runout | Bullet axis vs. case axis | <0.001" TIR for match |

**Environmental/handling factors the game simulates:**

| Factor | Effect |
|--------|--------|
| Storage temperature | Powder burn rate shifts. Hodgdon Extreme powders resist this. |
| Ammo age / moisture | Degraded ignition, inconsistent velocity |
| Magazine recoil setback | Recoil pushes bullets deeper if neck tension is low / no crimp |
| Powder position in case | Large case + small charge = powder settles to one side |
| Bore fouling state | Clean bore ≠ fouled bore POI. Fire fouling shots before competing. |
| Barrel temperature | Hot barrel = velocity shift + accuracy loss. Rapid fire walks the group. |

---

## Shooting Mechanics [v0.1]

| Mechanic | Driven By |
|----------|-----------|
| Accuracy | Base weapon (all parts) + ammo quality + barrel condition + optic quality |
| Recoil | Powder charge + bullet weight + weapon weight + muzzle device |
| Reliability | Ammo quality + weapon cleanliness + part condition → jam probability |
| Jam types | Stovepipe, double feed, failure to extract, failure to fire, squib |

Each round in the magazine is a specific `AmmoInstance`. Match-grade loads carry their exact stats into the field. Sloppy speed-reloads carry theirs too.

### External Ballistics v0.1 Contract [v0.1]

- Shooting consumes a normalized `CartridgeBallisticSpec` resolved from ammo runtime snapshot data.
- Canonical velocity unit for ammo/runtime contracts is `muzzleVelocityFps` (feet per second).
- Fire path is source-agnostic: factory and handload rounds use the same firing/projectile pipeline.
- v0.1 projectile sim includes gravity + BC-informed drag + velocity spread.
- v0.1 non-goals: wind drift, Coriolis, spin drift, temperature/humidity effects, reloading-bench interactions.

### Runtime Weapon Event Port [v0.1]

`IWeaponEvents` on the runtime hub (`IGameEventsRuntimeHub`) publishes baseline weapon-loop events for decoupled UI/audio/VFX listeners:

- `OnWeaponEquipStarted(string itemId)`
- `OnWeaponEquipped(string itemId)`
- `OnWeaponUnequipStarted(string itemId)`
- `OnWeaponFired(string itemId, Vector3 origin, Vector3 direction)`
- `OnWeaponReloadStarted(string itemId)`
- `OnWeaponReloadCancelled(string itemId, WeaponReloadCancelReason reason)`
- `OnWeaponReloaded(string itemId, int magCount, int reserveCount)`
- `OnWeaponAimChanged(string itemId, bool isAiming)`
- `OnProjectileHit(string itemId, Vector3 point, float damage)`

### ADS Movement Rule [v0.1]

- ADS is gameplay-authoritative in v0.1: movement speed is multiplied by weapon/profile ADS multiplier (default `0.7`) while aiming.

### ADS + Optics Runtime Contract [v0.1 Implemented]

Implemented FPS ADS/optics framework lives under `Reloader/Assets/Game/Weapons/**` and uses camera-authoritative alignment:

- `AttachmentManager` mounts optics and exposes active `SightAnchor`.
- `AdsStateController` drives ADS blend (`AdsT`), variable zoom (`1x-40x`), FOV mapping, and visual mode state.
- `WeaponAimAligner` runs in `LateUpdate`, solving alignment from camera to sight anchor:
  - `delta = Camera_world * inverse(SightAnchor_world)`
  - apply delta to `AdsPivot` with position/rotation smoothing.
- `ScopeMaskController` handles mask + reticle UI for high-magnification ADS.
- `RenderTextureScopeController` is a lightweight PiP stub path for opt-in scope rendering.

Visual policy (`AdsVisualMode.Auto`):
- magnification `<= 2x` -> no mask
- magnification `>= 4x` -> mask

Prefab contract:

```text
ViewModelRoot
 |- AdsPivot
 |- Attachments/ScopeSlot
 |- Defaults/IronSightAnchor
```

```text
OpticPrefab
 |- SightAnchor
```

Authoring details and setup are defined in [ads-optics-framework.md](ads-optics-framework.md).

---

## Competition Spectrum [v0.2]

| Category | Distance | Key Skill | Caliber Examples |
|----------|----------|-----------|-----------------|
| Pistol (USPSA/IDPA) | 7-25 yards | Speed + accuracy | 9mm, .45 ACP |
| Carbine / 3-Gun | Mixed | Versatility | 5.56, 9mm, 12ga |
| Precision Rifle | 100-600 yards | Accuracy, wind reading | .308, 6.5 Creedmoor |
| Long Range | 600-1200 yards | Advanced ballistics | .308, .300 WM, 6.5 CM |
| ELR (Extreme Long Range) | 1+ mile | Master ballistics | .338 Lapua, .50 BMG, wildcats |
| Benchrest | 100-300 yards | Pure accuracy, no time | .22 PPC, 6mm BR |

For competition gameplay mechanics, see [hunting-and-competitions.md](hunting-and-competitions.md).

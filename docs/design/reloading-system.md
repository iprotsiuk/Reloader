# Reloading System Design

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.
> **Related skill:** `.agent/skills/reloading-domain-knowledge/SKILL.md` for real-world reference data and accuracy calculations.
> **Related skill:** `.agent/skills/adding-game-content/SKILL.md` for creating new component data assets.

This is the core mechanic and primary differentiator. It must be deep enough for enthusiasts while learnable for newcomers through experimentation and consequences.

---

## The Real Reloading Process [v1+]

Each step is a physical interaction at the workbench:

| Step | Action | Equipment Needed | What Can Go Wrong |
|------|--------|-----------------|-------------------|
| 1. Inspect | Check cases for cracks, measure length | Calipers, eyes | Miss a cracked case → case head separation |
| 2. Clean | Tumble or ultrasonic clean dirty brass | Tumbler or ultrasonic cleaner | Skip → dirty chamber, feeding issues |
| 3. Lube | Apply case lube before resizing | Case lube (spray or pad) | Skip → case stuck in die |
| 4. Resize | Run case through sizing die | Press + sizing die | Wrong die setup → improper headspace. Too much shoulder bump = excessive headspace = case head separation after a few firings. Too little = won't chamber. |
| 5. Trim | Trim case to spec length | Case trimmer, calipers | Skip when needed → excess pressure, jams |
| 6. Deburr/Chamfer | Clean case mouth after trim | Deburring tool | Skip → bullet seating problems |
| 7. Prime | Insert new primer | Priming tool or press-mounted | Upside down primer → misfire. Too deep/shallow → sensitivity issues |
| 8. Charge Powder | Measure and pour powder | Scale + powder measure/trickler | Over-charge → catastrophic. Under-charge → squib |
| 9. Seat Bullet | Press bullet into case to target CBTO/OAL | Press + seating die (+ bullet comparator to verify CBTO) | Wrong depth → pressure/feeding issues. Too deep = pressure spike. Jammed into lands = dangerous overpressure. |
| 10. Crimp (optional) | Crimp case mouth around bullet | Crimp die or seating die with crimp | Too much → buckled case. Too little → bullet setback |
| 11. Inspect | Check OAL, measure CBTO, visual inspection | Calipers, bullet comparator, concentricity gauge | Miss a defect → varies by defect |

---

## Sandbox Philosophy [v0.1]

**Hard rule: NEVER prevent the player from performing an action.** Instead, simulate the consequence.

| Player Mistake | Consequence |
|---------------|-------------|
| Seat bullet without primer | Dead round. Wasted components. |
| Forgot case lube | Case stuck in sizing die. Need stuck case remover tool. |
| Double powder charge | Catastrophic failure on firing. Weapon destroyed. Hospital visit. Debuffs: shaking hands (accuracy penalty), blurry vision, tinnitus (ringing audio filter) for several in-game days. |
| Powder charge too low | Squib load — bullet lodged in barrel. If player fires another round, barrel destroyed. |
| Case too long, untrimmed | Excess pressure, hard bolt lift, potential case failure |
| Wrong primer size | Doesn't seat properly — misfire or slam-fire risk |
| Mixed brass from different rifles | Inconsistent accuracy, possible chambering issues |
| Excessive shoulder bump during sizing | Case stretches on each firing → case head separation after 2-3 firings. |
| Crimping without proper die setup | Bent/buckled case neck |
| Exceeded max pressure for caliber | Progressive damage: flattened primers → ejector marks → cracked case → blown action |

**Learning aids (earned, not given):**
- Buy reloading manuals at the store (in-game books with load data)
- NPC old-timers give tips in conversation
- Player's recipe book tracks what worked and what didn't
- Upgraded equipment adds safety features

### Runtime Workbench UI Contract [v0.1]

- Reloading bench runtime UI is implemented in UI Toolkit.
- Workbench view binder is render/intent only; operation authority remains in reloading runtime controllers.
- Operation select/execute actions flow through intent keys (`reloading.operation.select`, `reloading.operation.execute`) mapped by runtime UI action config.

### Workbench Mount Graph Slice (Implemented) [v0.1]

- Landed runtime mount graph contracts and nested slot model:
  - `WorkbenchDefinition`, `MountSlotDefinition`, `MountableItemDefinition`, `CompatibilityRuleSet`
  - `WorkbenchRuntimeState`, `MountNode`, `MountSlotState`
  - `WorkbenchCompatibilityEvaluator`, `WorkbenchCompatibilityResult`
- Landed operation-gating + runtime integration:
  - `WorkbenchLoadoutController` mount/unmount flow with diagnostics
  - `ReloadingOperationGate` capability checks wired to `ReloadingFlowController`
  - `ReloadingBenchTarget` exposes bench runtime state to runtime/UI surfaces
- Landed save/load persistence for mounted graphs:
  - `WorkbenchLoadout` save module and `SchemaV4ToV5AddWorkbenchLoadoutMigration`
  - `WorkbenchRuntimeSaveBridge` capture/restore hooks via runtime bridge registry
- Evidence tests include:
  - `WorkbenchMountDefinitionsEditModeTests`
  - `WorkbenchRuntimeStateEditModeTests`
  - `WorkbenchCompatibilityEvaluatorEditModeTests`
  - `WorkbenchLoadoutControllerPlayModeTests`
  - `WorkbenchMountFlowAcceptancePlayModeTests`
  - `WorkbenchRuntimeSaveBridgeEditModeTests`

---

## Data Model [v0.1]

```
CaliberDefinition (standalone SO — reference data, not a physical item)
├── name                → ".308 Winchester", "9mm Luger"
├── parentCaliber       → for wildcats: what caliber it's formed from (nullable)
├── bulletDiameter      → inches
├── maxCaseLength       → SAAMI max case length in inches
├── trimToLength        → recommended trim-to length in inches
├── maxChamberPressure  → SAAMI max PSI (reference only — player isn't bound by it)
├── boltFace            → small / large / magnum / rimfire / .50BMG
├── nominalHeadspace    → SAAMI nominal headspace in inches (bolt face to datum)
├── headspaceType       → Shoulder / CaseMouth / Rim / Belt (which datum controls headspace)
├── isReloadable        → bool (rimfire calibers are false: buy-only factory ammo)
├── (compatible weapons derived at runtime from barrel definitions)
├── customProperties    → extensibility

PowderDefinition (SO)
├── name                    → "Hodgdon Varget"
├── burnRate                → numeric index (lower = faster)
├── loadData                → List<LoadDataEntry>:
│   each entry: caliber (CaliberDefinition), minBulletWeight, maxBulletWeight,
│   minCharge, maxCharge (grains). Per-caliber-per-bullet-weight-range entries.
├── packageCount            → pounds per package
├── packagePrice            → in-game package price
├── unitPrice               → derived: packagePrice / packageCount
├── temperatureSensitivity  → 0.0 (stable) to 1.0 (shifts with temp)
├── chargeWeightVariance    → how consistently it meters through a measure
├── lotToLotVariance        → velocity spread between production lots
├── batchBurnRateDeviation  → burn rate variation within a production batch
├── customProperties

BulletDefinition (SO) [projectile only, not cartridge]
├── name                → "Sierra MatchKing 168gr HPBT"
├── weight              → nominal weight in grains
├── type                → FMJ / HP / SP / Match / Solid / Cast / BondedHP / etc.
├── ballisticCoefficient → BC value (see bcModel)
├── bcModel             → G1 / G7 (drag model — MUST match the BC number)
├── caliber             → reference to CaliberDefinition
├── bulletLength        → total length base to tip in inches (for twist stability via Greenhill formula)
├── packageCount        → bullets per box/package
├── packagePrice        → in-game package price
├── unitPrice           → derived: packagePrice / packageCount
├── weightDeviation     → Match: ±0.1-0.3gr. Budget: ±1.0gr+
├── diameterDeviation   → Match: ±0.0001". Budget: ±0.001"
├── ogiveLength         → base to ogive datum (inches). Used with barrel.freeBore to compute jump.
├── lengthDeviation     → Match: ±0.001". Budget: ±0.003"
├── meplatUniformity    → 0.0 (uniform) to 1.0 (wild tip variation). Affects BC.
├── concentricity       → 0.0 (perfect jacket) to 1.0 (uneven jacket)
├── customProperties

CaseDefinition (SO)
├── name                      → brand + caliber (e.g., "Lapua .308 Win Brass")
├── caliber
├── material                  → brass / nickel / steel
├── primerPocketSize          → Small / Large / .50BMG
├── maxReloads                → approximate lifespan
├── packageCount              → cases per bag/package
├── packagePrice              → in-game package price
├── unitPrice                 → derived: packagePrice / packageCount
├── weightDeviation           → Lapua: ±0.5gr. Budget: ±3.0gr+
├── neckThicknessDeviation    → Premium: ±0.0005". Budget: ±0.002"
├── flashHoleBurriness        → 0.0 (clean) to 1.0 (heavy burr)
├── primerPocketDepthDeviation → Premium: ±0.001". Budget: ±0.003"
├── customProperties

PrimerDefinition (SO)
├── name                  → brand + type (e.g., "CCI BR-2 Large Rifle")
├── primerSize            → Small / Large / .50BMG
├── primerApplication     → Pistol / Rifle
├── isMagnum              → bool (enhanced flame for magnum cartridge loads)
├── sensitivity           → ignition reliability
├── packageCount          → primers per box/package
├── packagePrice          → in-game package price
├── unitPrice             → derived: packagePrice / packageCount
├── sensitivityDeviation  → match primers: very tight. Standard: minor variance.
├── brisanceDeviation     → flame intensity consistency. Affects velocity SD.
├── customProperties
```

**Rimfire policy:** Rimfire calibers are buy-only (`isReloadable=false`). They remain valid for weapons/shop inventory as factory ammo, but do not use the bench reload workflow or rimfire primer-pocket assembly paths.

**Powder variance field usage:**
- `chargeWeightVariance` feeds into `charge_sd` in accuracy formula when using a volumetric measure. Ball powders meter well (low). Extruded stick powders meter poorly (higher). Hand-trickling bypasses this.
- `lotToLotVariance` is sampled when a `PowderLotInstance` is created and becomes that lot's systematic offset (`lotOffset`), NOT per-shot SD.
- `batchBurnRateDeviation` is the per-shot contribution to `powder_sd` in the accuracy formula.

**Runtime instances (per physical item in the world):**

```
CasingInstance
├── definition          → CaseDefinition SO
├── uniqueID            → GUID
├── timesFired          → tracks reload life
├── lastFiredInChamber  → weapon.barrel.chamberID (for fire-forming)
├── neckSizedOnly       → bool (neck bump vs full-length resize)
├── currentLength       → measured length after last trim
├── shoulderDatum       → shoulder position relative to case head (inches)
├── annealed            → bool (since last firing)
├── lubed, cleaned      → bool (current state)
├── primerInstance      → PrimerInstance reference (null if unprimed)
├── condition           → good / cracked / stretched / crushed
├── actualWeight        → sampled from definition.weightDeviation at spawn
├── actualNeckThickness → sampled from definition.neckThicknessDeviation at spawn

BulletInstance (per physical projectile)
├── definition          → BulletDefinition SO
├── uniqueID            → GUID
├── actualWeight        → sampled from definition.weightDeviation at spawn
├── actualDiameter      → sampled from definition.diameterDeviation at spawn
├── actualLength        → sampled from definition.lengthDeviation at spawn
├── actualBC            → derived from actualLength + meplatUniformity + concentricity

PrimerInstance (per physical primer)
├── definition          → PrimerDefinition SO
├── uniqueID            → GUID
├── actualSensitivity   → sampled from definition.sensitivityDeviation at spawn
├── actualBrisance      → sampled from definition.brisanceDeviation at spawn

PowderLotInstance
├── lotID               → stable lot identifier (string)
├── powderDefinition    → PowderDefinition SO
├── lotOffset           → sampled once from PowderDefinition.lotToLotVariance when lot is created
│                         (authoritative lot-level value)

PowderCanisterInstance
├── uniqueID            → GUID
├── powderDefinition    → PowderDefinition SO
├── powderLotID         → PowderLotInstance.lotID
│                         (canisters purchased from the same shop batch may share one lot)
├── remainingWeightLb   → pounds remaining in this canister

AmmoInstance (a completed cartridge)
├── uniqueID
├── ammoSource          → Handload / Factory (provenance)
├── factoryTemplateID   → FactoryAmmoTemplate identifier/reference (set only when ammoSource=Factory)
├── casing              → CasingInstance (carries all its history)
├── bullet              → BulletInstance (carries per-instance weight/diameter/BC)
├── powderDefinition    → PowderDefinition
├── powderLotID         → PowderLotInstance.lotID copied from the source canister at assembly time
├── powderLotOffsetApplied → lot offset baked at assembly time (float, no canister foreign key)
│
│  RAW ASSEMBLY DATA (what actually happened at the bench):
├── powderCharge        → exact grains loaded
├── cartridgeOAL        → actual overall length tip-to-base
├── cbto                → Cartridge Base To Ogive: the repeatable measurement
│                         that determines distance from lands (jump)
├── crimpAmount         → none / light / heavy
│
│  COMPUTED INTRINSIC QUALITY (calculated at assembly, weapon-independent):
├── neckTension         → from brass neck thickness + annealing state + sizing die/press
├── concentricity       → cartridge runout TIR
├── estimatedVelocity   → predicted MV for THIS specific round
├── estimatedPressure   → predicted chamber pressure
├── velocityUncertainty → per-round MV deviation potential
├── safetyRating        → 0.0 = safe, 1.0 = at limit, >1.0 = danger
├── intrinsicQuality    → 0.0-1.0 summary score for UI display

**Factory ammo:** Pre-built AmmoInstances are generated from component definitions
(`bulletDefinition`, `caseDefinition`, `powderDefinition`, `primerDefinition`) plus
a factory process consistency profile on `FactoryAmmoTemplate` (charge, seating,
runout, primer-seating SD targets). Quality tiers (match, hunting, plinking, surplus)
set defaults, and templates can tune them per SKU. Black market / cheap ammo may
exceed SAAMI specs.
Factory-created rounds set `ammoSource=Factory` and include `factoryTemplateID`.
Bench-assembled rounds set `ammoSource=Handload` and leave `factoryTemplateID` null.

FIRING TIME CALCULATION (happens when trigger pulled, NOT stored on AmmoInstance):
  actual_velocity = estimatedVelocity ± sample_from(velocityUncertainty)
  chamber_fit     = check casing.lastFiredInChamber vs weapon.barrel.chamberID
  weapon_accuracy = compute from weapon parts
  jump            = effective_freebore - bullet_protrusion
  shot_placement  = ballistics(actual_velocity, bullet.actualBC, wind, range, ...)
                    * weapon_accuracy * chamber_fit * concentricity_penalty
                    * jump_multiplier
```

---

## Fire-Formed Brass [v0.2]

When a round is fired in a specific chamber, the brass expands to fit that chamber exactly.

- **Full-length resize:** Returns brass to SAAMI spec. Fits any chamber. Less precise.
- **Neck size only (bump):** Only sizes the neck/shoulder back slightly. Retains fire-formed fit. Much more precise in the original rifle.
- **CasingInstance.lastFiredInChamber** tracks which barrel the brass was last fired in.
- When `casing.lastFiredInChamber == weapon.barrel.chamberID` and brass was neck-sized, the ammo gets a significant accuracy bonus.
- NPC customers can give you their once-fired brass. Track that it came from their rifle's chamber.

---

## Equipment Progression [v1+]

| Tier | Equipment | Capability |
|------|-----------|------------|
| Starter (Grandpa's) | Old single-stage press, beam scale, basic dies (one caliber), hand priming tool | Slow, manual, one caliber, low precision |
| Early purchase | Case trimmer, tumbler, deburring tool, calipers | Can properly prep brass now |
| Mid-game | Digital scale, powder measure, more die sets, better press, headspace gauges | Faster, more precise, more calibers |
| Mid-late | OAL gauge, bullet comparator | Can measure distance from lands, measure CBTO |
| Late-game | Progressive press, electronic powder dispenser, chronograph, concentricity gauge, annealer | Batch production, extreme precision |
| Endgame | Bullet casting setup, custom die making, automated systems, employee workstations | Full ammunition manufacturing |

**Equipment quality modeling uses TWO independent axes: `precision` and `speed`.** Higher precision reduces error at that reloading step. Higher speed increases throughput. These are NOT correlated. For serialization safety (Unity assets + save JSON), optional axes use a serializable wrapper pattern (for example `OptionalFloat { isSpecified, value }`) rather than nullable primitives. Diagnostic tools set `isSpecified=false` for non-applicable axes.

See `.agent/skills/reloading-domain-knowledge/SKILL.md` for the complete equipment→accuracy mapping.

---

## Interaction Model [v0.1]

When the player approaches the reloading bench:

1. Enter "workstation mode" — camera locks to bench area, HUD changes to workbench UI
2. Tools and components visible on bench as physical objects
3. Click-based interactions with physics feedback
4. Each step has hidden quality variance based on equipment precision and player inputs
5. Visual + audio feedback: metallic clicks, powder trickling, press leverage sounds

---

## Core Precision Systems [v1+]

These are NOT future features — the accuracy calculation depends on them from v1.

- **Brass annealing:** Heat-treat case necks to reset work-hardening. Extends case life + improves sizing consistency.
- **Component sorting:** Weigh/measure components, sort into groups. Tighter groups = better accuracy.
- **Per-lot powder variance:** Different lots of the same powder perform slightly differently.

## Future Expansion Hooks [v1+]

- Wildcat calibers (reform brass from parent cases)
- Bullet casting
- Custom chamber reamers
- Annealing automation
- Custom die making

---
name: adding-game-content
description: Guides creation of new weapons, ammo components, equipment, NPCs, contract assets, and other game content via ScriptableObjects. Use when adding new items, calibers, powders, bullets, weapons, equipment, or other data-driven content to Reloader's precision-contract sandbox.
---

# Adding Game Content

## When to Use

- Adding a new weapon, caliber, powder, bullet, case, primer, or equipment
- Creating NPC definitions, quest data, or shop inventories (SO definitions for these are specified in their respective domain design docs — see docs/design/README.md)
- Creating contract, target-intel, or role-support data assets once those SOs exist
- Any task that involves creating ScriptableObject data assets
- NOT appropriate when: modifying game logic, fixing bugs, or changing UI behavior

## Prerequisites

Read these docs first (in order):
1. `docs/design/core-architecture.md` — shared patterns, SO system, project structure
2. `docs/design/assassination-contracts.md` — if the content affects contracts, targets, or premium long-range jobs
3. `docs/design/reloading-system.md` — reloading data models and mechanics (if adding reloading components)
4. `docs/design/weapons-and-ballistics.md` — weapon data models (if adding weapons/parts)
5. `docs/design/ads-optics-framework.md` — implemented ADS/optics runtime + data contract (if adding optics/sights/ADS content)
6. `.agent/skills/unity-project-conventions/SKILL.md` — coding standards
7. `.agent/skills/weapon-view-attachment-framework/SKILL.md` — required when the content includes first-person weapon views, attachment slots, scopes, muzzles, magazines, slides, or other mountable upgrades

See `docs/design/README.md` for the full routing index.

## Workflow

```
Content Addition Progress:
- [ ] Identify content type and verify SO definition class exists
- [ ] Create .asset file in correct Data/ folder
- [ ] Fill all required fields with realistic values
- [ ] Verify no code changes needed (data-driven principle)
- [ ] Test in-editor: asset appears in game systems

If the content being added is a weapon, optic, muzzle, or other runtime-mounted attachment, also:

- [ ] Verify the weapon item id binds to an explicit runtime view prefab
- [ ] Verify the runtime view prefab exposes `WeaponViewAttachmentMounts`
- [ ] Verify the prefab starts with empty runtime attachment slots
- [ ] Verify the attachment definition points at an explicit attachment prefab
```

## Content Type Reference

### Adding a New Caliber

**File:** `Reloader/Assets/_Project/Weapons/Data/Calibers/<CaliberName>.asset`
**Class:** `CaliberDefinition`

Required fields:
- `name` — official designation (e.g., ".308 Winchester", "6.5 Creedmoor")
- `bulletDiameter` — in inches (e.g., 0.308)
- `maxChamberPressure` — SAAMI max in PSI (e.g., 62000)
- `maxCaseLength` — SAAMI max case length in inches. Used by consequence system to detect over-length cases.
- `trimToLength` — recommended trim-to length in inches. Reference for player when trimming brass.
- `boltFace` — Small / Large / Magnum / Rimfire / .50BMG
- `parentCaliber` — null for standard calibers, reference to parent CaliberDefinition for wildcats
- `nominalHeadspace` — SAAMI nominal headspace in inches (bolt face to shoulder datum). Example: .308 Win = 1.6320". Used to calculate shoulder bump during sizing and to check chamber safety with GO/NO-GO gauges.
- `headspaceType` — Shoulder / CaseMouth / Rim / Belt. Determines which datum point controls headspace for this caliber.
- `isReloadable` — bool. Rimfire calibers must be `false` (buy-only factory ammo).

Compatible weapons are derived at runtime from barrel definitions.
Policy: rimfire calibers (for example .22 LR, .22 WMR, .17 HMR) are buy-only for this phase and should be authored with `isReloadable=false`.

Reference: [../reloading-domain-knowledge/resources/caliber-reference.md](../reloading-domain-knowledge/resources/caliber-reference.md)

### Adding a New Powder

**File:** `Reloader/Assets/_Project/Reloading/Data/Powders/<BrandName_ProductName>.asset`
**Class:** `PowderDefinition`

Required fields:
- `name` — brand + product (e.g., "Hodgdon Varget")
- `burnRate` — numeric index, lower = faster. See [../reloading-domain-knowledge/resources/burn-rate-chart.md](../reloading-domain-knowledge/resources/burn-rate-chart.md)
- `loadData` — `List<LoadDataEntry>`, each entry containing: caliber (CaliberDefinition reference), minBulletWeight, maxBulletWeight (grains), minCharge, maxCharge (grains). Real load data is per powder + caliber + bullet weight range (e.g., Varget in .308 with 150gr bullets has different min/max than with 175gr bullets). One entry per caliber+weight combination.
- `packageCount` — pounds per package (typically 1 or 8)
- `packagePrice` — in-game package price
- `unitPrice` — derived (`packagePrice / packageCount`), not manually entered
- `temperatureSensitivity` — 0.0 (stable like Hodgdon Extreme) to 1.0 (shifts significantly with temp)

Factory deviation fields (REQUIRED — drive runtime quality simulation):
- `chargeWeightVariance` — how consistently powder meters through a measure. 0.0 = perfect, 1.0 = wild. Fine-grained powders meter better. Determines charge-to-charge deviation for non-hand-trickled loads.
- `lotToLotVariance` — velocity spread between different production lots of the same powder. 0.0 = identical lots, 1.0 = significant lot variation. Affects players who buy bulk.
- `batchBurnRateDeviation` — how much burn rate can vary within a production batch. Even good powders have slight variation. Premium powders (Vihtavuori) have tighter QC than budget.

**How powder variance fields are used:**
- `chargeWeightVariance` → feeds into `charge_sd` in accuracy formula when player uses a volumetric powder measure. Fine ball powders meter consistently (low value). Extruded stick powders meter poorly (higher value). When hand-trickling to exact weight (autotrickler), this has no effect.
- `lotToLotVariance` → per-lot systematic offset, NOT per-shot. When a `PowderLotInstance` is created, the game samples one burn rate offset for that lot. Canisters tied to the same lot (for example, from one shop batch) share this offset.
- `batchBurnRateDeviation` → per-shot contribution to velocity SD (burn rate variation within a canister).

### Adding a New Bullet (Projectile)

**File:** `Reloader/Assets/_Project/Reloading/Data/Bullets/<BrandName_Weight_Type>.asset`
**Class:** `BulletDefinition`

Required fields:
- `name` — full description (e.g., "Sierra MatchKing 168gr HPBT")
- `weight` — nominal weight in grains
- `type` — FMJ / HP / SP / Match / Solid / Cast / BondedHP / etc.
- `ballisticCoefficient` — BC value (numeric). See `bcModel` for which drag model this number uses.
- `bcModel` — G1 / G7. The drag model the BC value is measured against. A G7 BC of 0.220 and G1 BC of 0.462 can describe the SAME bullet — using the wrong model produces wildly incorrect trajectories. G7 is preferred for modern boat-tail rifle bullets. G1 for flat-base pistol/varmint bullets.
- `caliber` — reference to CaliberDefinition
- `bulletLength` — total length in inches, base to tip. Used for twist rate stability calculation (Greenhill formula). Two 168gr .308 bullets can have different lengths due to ogive design — the stability calc needs length, not weight.
- `packageCount` — bullets per package
- `packagePrice` — in-game package price
- `unitPrice` — derived (`packagePrice / packageCount`), not manually entered

Geometry fields (REQUIRED — these determine jump and chamber interaction):
- `ogiveLength` — distance in inches from bullet base to the ogive datum point (the diameter on the ogive curve that contacts the rifling lands at bore diameter). Bullet-specific: a VLD/secant ogive has the datum farther from the base than a tangent ogive of the same weight. Combined with `barrel.freeBore` and `ammo.cbto` to compute distance from lands (jump) at firing time. Look up or derive from manufacturer bullet drawings.

Factory deviation fields (REQUIRED — these define component quality tier):
- `weightDeviation` — max weight spread in grains. Match grade: ±0.1-0.3gr. Budget: ±1.0gr+. Drives per-instance weight when spawned.
- `diameterDeviation` — max diameter spread in inches. Match: ±0.0001". Budget: ±0.001". Affects chamber fit, bore seal, gas blow-by.
- `lengthDeviation` — max ogive-to-base length spread in inches. Affects BC consistency across a batch AND CBTO consistency (ogiveLength varies per instance when lengthDeviation is nonzero). Match: ±0.001". Budget: ±0.003".
- `meplatUniformity` — 0.0 (perfectly uniform tips) to 1.0 (wild tip variation). Match bullets have trimmed meplats. Affects BC at long range.
- `concentricity` — 0.0 (perfectly concentric jacket) to 1.0 (uneven jacket). Cheap bullets have thicker jacket on one side → they fly crooked.

When spawning a BulletInstance at runtime, sample from a normal distribution using these deviations. A box of match bullets has nearly identical instances; a box of budget bullets has scattered stats. The player can sort them (weigh, measure) to find the best ones.

### Adding a New Case (Brass)

**File:** `Reloader/Assets/_Project/Reloading/Data/Cases/<BrandName_Caliber>.asset`
**Class:** `CaseDefinition`

Required fields:
- `name` — brand + caliber (e.g., "Lapua .308 Win Brass", "Starline 9mm")
- `caliber` — reference to CaliberDefinition
- `primerPocketSize` — Small / Large / .50BMG. Determines which primer sizes physically fit the case. Wrong size primer: small falls out of large pocket, large won't seat into small pocket (and forcing it risks detonation at the bench).
- `material` — Brass / Nickel / Steel
- `maxReloads` — approximate number of reloads before failure risk
- `packageCount` — cases per package
- `packagePrice` — in-game package price
- `unitPrice` — derived (`packagePrice / packageCount`), not manually entered

Factory deviation fields (REQUIRED — these define brass quality tier):
- `weightDeviation` — max case weight spread in grains. Indicates internal volume consistency. Lapua: ±0.5gr. Budget: ±3.0gr+. Player can sort by weight to group by volume.
- `neckThicknessDeviation` — max neck wall thickness spread in inches. Premium: ±0.0005". Budget: ±0.002". Uneven neck = non-concentric bullet release. Player can neck-turn to fix.
- `flashHoleBurriness` — 0.0 (clean punched) to 1.0 (heavy burr). Budget brass often has burrs from manufacturing. Player deburrs with tool.
- `primerPocketDepthDeviation` — consistency of primer pocket depth. Affects primer seating depth = ignition consistency. Premium: ±0.001". Budget: ±0.003".

When spawning a CasingInstance, sample from these deviations. Premium Lapua brass in a box is nearly identical piece to piece. Budget brass varies significantly. Player can sort, prep, and reject to build consistent batches.

### Adding a New Primer

**File:** `Reloader/Assets/_Project/Reloading/Data/Primers/<BrandName_Type>.asset`
**Class:** `PrimerDefinition`

Required fields:
- `name` — brand + type (e.g., "CCI BR-2 Large Rifle", "Federal 210M Large Rifle Match")
- `primerSize` — Small / Large / .50BMG (physical size of the primer cup)
- `primerApplication` — Pistol / Rifle (flame characteristics suited to powder type)
- `isMagnum` — bool. Magnum primers have a hotter/larger flame for igniting slow-burning powders in large cases. Physically the same size as standard (e.g., CCI 200 and CCI 250 both fit Large Rifle pockets). The difference is internal compound.
- `sensitivity` — ignition reliability. Higher = more reliable, also slightly hotter.
- `packageCount` — primers per package (typically 100 or 1000)
- `packagePrice` — in-game package price
- `unitPrice` — derived (`packagePrice / packageCount`), not manually entered

Factory deviation fields:
- `sensitivityDeviation` — how consistent the primer compound is. Match primers (CCI BR series, Federal Gold Medal): very tight. Standard primers: minor variance. Affects ignition consistency → velocity SD.
- `brisanceDeviation` — how consistent the flame intensity is across a box. Hotter/weaker primers in the same box → velocity spread.

### Adding a New Weapon

**File:** `Reloader/Assets/_Project/Weapons/Data/Weapons/<ModelName>.asset`
**Class:** `WeaponDefinition`

Required fields:
- `name` — model name (e.g., "Remington 700 SPS")
- `weaponType` — BoltAction / SemiAuto / Lever / Pump / Revolver / Pistol
- `defaultParts` — list of default WeaponPartDefinition references (includes a default barrel which determines the weapon's initial caliber)
- `partSlots` — which slot types this weapon supports
- `magazineCapacity` — max rounds the magazine holds
- `price` — base purchase price
- `weight` — in pounds (affects recoil, carry)

**Caliber is a barrel property.** Access via `weapon.barrel.caliber`. Swapping barrels can change a weapon's chambering (e.g., AR-15 with .223 vs .300 BLK barrel).

### Adding a New Weapon Part

**File:** `Reloader/Assets/_Project/Weapons/Data/WeaponParts/<SlotType_ModelName>.asset`
**Class:** Subclass of `WeaponPartDefinition` (e.g., `BarrelDefinition`, `TriggerDefinition`)

WeaponPartDefinition is an abstract base. Each slot type has specific fields:

**BarrelDefinition** — the most field-rich part type:
- `name` — model (e.g., "Bartlein #7 Heavy Palma .308")
- `slotType` — Barrel
- `caliber` — CaliberDefinition (the chambering this barrel provides)
- `twistRate` — inches per rotation (e.g., 10 means 1:10")
- `freeBore` — distance from case mouth to rifling lands (inches). Match chambers have tight freebore.
- `quality` — 0.0-1.0. Match-grade lapped (Bartlein, Krieger): ~0.9+. Production (Bergara): ~0.6. Budget: ~0.3.
- `profile` — Heavy/Bull / Sporter / Lightweight / Fluted
- `length` — barrel length in inches
- `isFreefloated` — bool (free-floated vs stock-contact)
- `price`
- `customProperties`

**ActionDefinition:**
- `name`, `slotType` (Action)
- `quality` — trued/blueprinted vs factory
- `beddingType` — Pillar / BeddingCompound / Factory
- `ratedPressure` — action strength in PSI (for overpressure cascade)
- `price`, `customProperties`

**TriggerDefinition:**
- `name`, `slotType` (Trigger)
- `pullWeight` — in pounds
- `creep` — 0.0 (crisp break) to 1.0 (mushy)
- `type` — Milspec / Match / Adjustable
- `price`, `customProperties`

**Legacy modular-part optic metadata (`WeaponPartDefinition` path):**
- `name`, `slotType` (Optic)
- `magnification` — e.g., "4-16x" or fixed
- `quality` — 0.0-1.0 (affects tracking, parallax, clarity)
- `mountQuality` — 0.0-1.0 (loose rings shift POI under recoil)
- `reticleType` — MOA / MRAD / BDC / Duplex
- `price`, `customProperties`

**MuzzleDeviceDefinition:**
- `name`, `slotType` (MuzzleDevice)
- `type` — Brake / Suppressor / FlashHider / HarmonicTuner
- `quality` — 0.0-1.0
- `suppressionRating` — for suppressors: sound reduction level
- `price`, `customProperties`

**GenericPartDefinition** — for simpler parts (bolt, stock, grip, bipod, cosmetics):
- `name`, `slotType`, `price`, `customProperties`
- Additional fields as needed via customProperties

Corresponding runtime instances (e.g., `BarrelPartInstance`) track per-instance state: `roundCount`, `throatErosion`, `condition`, etc.

### Adding a New ADS Weapon Definition (Implemented FPS Framework)

Use this when authoring first-person ADS/viewmodel weapon behavior.

**File:** `Reloader/Assets/Game/Weapons/WeaponDefinitions/<WeaponId>.asset`  
**Class:** `Reloader/Assets/Game/Weapons/WeaponDefinitions/WeaponDefinition.cs`

Required fields:
- `weaponId`
- `viewModelPrefab`
- `adsInTime`
- `adsOutTime`
- `baseAdsSensitivityScale`
- `baseAdsSwayScale`
- `defaultWorldFov`
- `defaultViewmodelFov`

Viewmodel prefab contract:

```text
ViewModelRoot
 |- AdsPivot
 |- Attachments/ScopeSlot
 |- Defaults/IronSightAnchor
 |- Muzzle
 |- Eject
```

### Adding a New ADS Optic / Sight Definition (Implemented FPS Framework)

Use this for red dots, holos, prisms, LPVOs, and high-mag scopes used by the camera-aligned ADS runtime.

**File:** `Reloader/Assets/Game/Weapons/WeaponDefinitions/<OpticId>.asset`  
**Class:** `Reloader/Assets/Game/Weapons/WeaponDefinitions/OpticDefinition.cs`

Required fields:
- `opticId`
- `category` (`Irons`, `RedDot`, `Holo`, `Prism`, `LPVO`, `ScopeHighMag`)
- `opticPrefab`
- `isVariableZoom`
- `magnificationMin` / `magnificationMax` / `magnificationStep`
- `visualModePolicy` (`Auto`, `Mask`, `RenderTexturePiP`)
- `eyeReliefBackOffset`

Optional fields:
- reticle sprite
- scope render profile (`renderTextureResolution`, `scopeCameraFov`)

Rules:
- Magnification contract is clamped to `1x..40x`.
- `Auto` visual mode: `<=2x` no mask, `>=4x` mask.
- Optic prefab must include `SightAnchor` child at eye position behind optic.

```text
OpticPrefab
 |- SightAnchor
```

Runtime integration for authored content:
- `AttachmentManager.EquipOptic(OpticDefinition)`
- `AdsStateController` consumes magnification/visual policy
- `WeaponAimAligner` aligns active `SightAnchor` to camera in `LateUpdate`

### Factory Ammo

Factory ammo does NOT use a separate `AmmoDefinition` item. Instead, shops stock factory ammo using `FactoryAmmoTemplate` SOs:

**File:** `Reloader/Assets/_Project/Reloading/Data/FactoryAmmo/<BrandName_Caliber_Type>.asset`
**Class:** `FactoryAmmoTemplate`

Required fields:
- `name` — brand + product (e.g., "Federal Gold Medal Match .308 168gr HPBT")
- `caliber` — CaliberDefinition
- `bulletDefinition` — BulletDefinition to use
- `caseDefinition` — CaseDefinition to use
- `powderDefinition` — PowderDefinition to use
- `primerDefinition` — PrimerDefinition to use
- `targetChargeWeight` — nominal powder charge in grains
- `targetCBTO` — nominal cartridge base-to-ogive
- `qualityTier` — Match / Hunting / Plinking / Surplus / Junk
- `factoryConsistencyProfile` — assembly-process SD targets layered on top of component variance:
  - `chargeWeightSD` (grains)
  - `cbtoSD` (inches)
  - `runoutSD` (inches TIR)
  - `primerSeatingDepthSD` (inches)
- `saamiCompliant` — bool (black market / surplus may not comply)
- `packageCount` — rounds per package
- `packagePrice` — package price
- `unitPrice` — derived (`packagePrice / packageCount`)

Factory ammo is purchased as a packaged product (`packagePrice` + `packageCount`). `unitPrice` should stay derived for consistent economy math.

When purchased, the game spawns N `AmmoInstance` objects from the template. Component behavior comes from component definition variance (bullet/case/powder/primer quality), and factory-specific consistency comes from `factoryConsistencyProfile`. Match factory ammo has tight process SD targets; surplus has wide targets. Spawned rounds set `ammoSource=Factory` and record `factoryTemplateID` for provenance.

### Adding a New Equipment Piece

**File:** `Reloader/Assets/_Project/Reloading/Data/Equipment/<Type_ModelName>.asset`
**Class:** `EquipmentDefinition`

Required fields (all equipment):
- `name` — model name (e.g., "Area 419 Zero Press", "K&M Arbor Press")
- `equipmentType` — Press / ArborPress / SizingDie / SeatingDie / Scale / PowderMeasure / Trimmer / Tumbler / PrimingTool / Annealer / ShellHolder / ConcentricityGauge / Chronograph / etc.
- `price`
- `customProperties` — extensibility

Applicability-driven fields (serializable optional pattern):
- `precisionSetting` — `OptionalFloat` with:
  - `isSpecified` (bool)
  - `value` (0.0 to 1.0)
- `speedSetting` — `OptionalFloat` with:
  - `isSpecified` (bool)
  - `value` (operations per minute)
- `calibersSupported` — for caliber-specific tools (dies, gauges, inserts). Use empty list (not null) when not applicable.

Use this pattern instead of nullable primitives for Unity-asset + save serialization safety.

**What `precisionSetting.value` means per equipment type** (maps to accuracy calculation in `reloading-domain-knowledge` skill):

| Equipment Type | Precision Drives | Low Precision Example | High Precision Example |
|---------------|-----------------|----------------------|----------------------|
| Press (sizing) | `sizing_sd` — frame rigidity, ram alignment → case dimension consistency | Lee (flex frame, loose ram) | Area 419 (zero flex, aligned) |
| Press/ArborPress (seating) | `seating_sd` — straight-line force, depth consistency, runout | Lee single-stage | K&M arbor press + hand die |
| SizingDie | `sizing_sd` + `neck_tension_sd` — neck tension control | Lee steel (fixed neck) | Redding Comp bushing (selectable tension) |
| SeatingDie | `seating_sd` + `runout_multiplier` — depth consistency, bullet alignment | Lee seating die | Wilson hand die / Forster BR micrometer |
| Scale | `charge_sd` — weight resolution | Lee beam (±0.1gr) | A&D FX-120i (±0.02gr) |
| PowderMeasure | `charge_sd` — throw consistency | Lee dipper | Autotrickler V4 |
| Trimmer | `neck_tension_sd` (via trim consistency) | Lee gauge | WFT / Giraud |
| PrimingTool | `primer_seating_sd` — depth consistency | Lee hand prime (±0.003") | K&M bench (±0.0005") |
| Annealer | `neck_tension_sd` — anneal consistency → sizing consistency | Propane torch | AMP induction |
| ShellHolder | `runout_multiplier` — case alignment | Standard stamped | Competition lapped |
| HeadspaceGauge | Diagnostic tool — enables checking chamber safety (GO/NO-GO/FIELD). Detects worn/over-reamed chambers. Also lets player verify their sizing die is set correctly by comparing sized brass shoulder position to gauge dimensions. | None (hope for the best) | Forster GO/NO-GO set per caliber |
| HeadspaceComparator | Enables measuring shoulder bump on sized brass. Without this, player adjusts die blindly → risk of too much bump (case head separation) or too little (won't chamber). With this, player verifies exact bump amount. | None (adjust die by feel) | Hornady/Sinclair headspace comparator insert |
| OALGauge | `jump_multiplier` — enables measuring jam length (CBTO where bullet touches lands) per rifle+bullet combo. Without this, player uses published OAL data and may be far from optimal jump → groups penalized. With this, player finds optimal seating CBTO → jump_multiplier ≈ 1.0. | None (guess from manual) | Hornady Lock-N-Load |
| BulletComparator | Enables measuring CBTO instead of OAL. OAL varies ±0.005-0.010" from tip inconsistency; CBTO is repeatable to ±0.0005". Required to verify seating consistency and set target CBTO from OAL gauge measurements. | None (calipers measure OAL only) | Hornady/Sinclair comparator insert |

When creating an equipment asset, set `precisionSetting.value` relative to other equipment of the same type only when `precisionSetting.isSpecified=true`. The accuracy calculation reads `precisionSetting` only for equipment types that contribute variance.

## Data Accuracy Rules

1. **Use real-world data.** Look up actual ballistic coefficients, powder charge ranges, and dimensions. Reloading enthusiasts will notice inaccuracies.
2. **SAAMI specs are reference, not law.** The player can exceed specs. The data model tracks maximums for consequence calculation, not for prevention.
3. **Price balance is separate from realism.** Real-world MSRP is a starting point but in-game economy may need tuning for gameplay balance.
4. **When unsure, add a `customProperties` entry.** Future expansion should not require refactoring the base SO class.

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Creating assets in wrong folder | Check the path template above for each type |
| Making up ballistic data | Look up real manufacturer specs |
| Forgetting `[CreateAssetMenu]` on new SO classes | Every SO needs it for editor workflow |
| Hardcoding values that should be data | If it varies per item, it's a SO field |
| Adding a new SO class when existing one covers it | Use `customProperties` for edge cases first |

## Resources

- [../reloading-domain-knowledge/resources/caliber-reference.md](../reloading-domain-knowledge/resources/caliber-reference.md) — common caliber specs (canonical shared reference)
- [../reloading-domain-knowledge/resources/burn-rate-chart.md](../reloading-domain-knowledge/resources/burn-rate-chart.md) — relative burn rates of common powders (canonical shared reference)

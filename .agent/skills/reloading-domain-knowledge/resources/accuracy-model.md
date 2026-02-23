# Accuracy and Failure Modeling Reference

This file is the canonical detailed reference for reloading failure logic and
accuracy formulas used by `reloading-domain-knowledge`.

## Squib Load Detection

A squib (insufficient powder charge) can lodge a bullet in the barrel. Firing
another round behind it should destroy the barrel.

Implementation pattern:
1. Track `barrelObstructed` on the barrel runtime instance.
2. On shot resolution, if computed velocity is below the squib threshold:
   - set `barrelObstructed = true`
   - play a muted "pop" shot profile
3. If firing when `barrelObstructed == true`:
   - force catastrophic barrel failure path
4. Clear obstruction only through explicit player action (rod/bench workflow).

## Overpressure Cascade

Pressure failure is progressive, not binary.

1. Compute effective chamber pressure from charge, burn rate, bullet mass, case
   volume, seating depth/jump, temperature, and chamber fit.
2. Compare against action strength (`ActionDefinition.ratedPressure`), not
   SAAMI reference pressure alone.
3. Apply staged consequences:
   - `100-105%`: flattened primers, hard extraction
   - `105-115%`: ejector marks, cracked case risk
   - `115%+`: blown primer, head-separation risk
   - `130%+`: high catastrophic failure probability

## Two-Phase Accuracy Model

Phase 1: assembly time (store on `AmmoInstance`):
- `estimatedVelocity`
- `estimatedPressure`
- `velocityUncertainty`
- `neckTension`
- `concentricity`
- `safetyRating`
- `intrinsicQuality`
- provenance (`ammoSource`, optional `factoryTemplateID`)

Phase 2: firing time (not stored on round):
- Combine intrinsic round quality with weapon state and environment.
- Resolve shot velocity sample and point-of-impact distribution.

## Stage 1 Formula: Velocity SD

Velocity SD is modeled as root-sum-of-squares (RSS) of independent terms.

```text
charge_sd          = f(scale precision, player care, powder metering behavior)
powder_sd          = PowderDefinition.batchBurnRateDeviation * chargeWeight
primer_sd          = f(PrimerInstance brisance deviation)
case_volume_sd     = f(batch spread of case actualWeight / volume proxy)
sizing_sd          = f(press rigidity/alignment + die quality/type)
seating_sd         = f(seating tool and die quality)
neck_tension_sd    = f(annealing + neck thickness + die strategy)
primer_seating_sd  = f(primer tool quality + pocket variation)
temperature_sd     = PowderDefinition.temperatureSensitivity * abs(tempDelta)

velocity_SD = sqrt(
  charge_sd^2 + powder_sd^2 + primer_sd^2 + case_volume_sd^2
  + sizing_sd^2 + seating_sd^2 + neck_tension_sd^2
  + primer_seating_sd^2 + temperature_sd^2
)
```

Powder lot rule:
- `lotOffset` is a systematic per-lot velocity/burn-rate shift.
- Do not include `lotOffset` in RSS SD terms.
- Apply as a baseline shift before sampling per-shot variation.

## Stage 2 Formula: Group Size

Group size combines weapon baseline, ammo consistency, and multipliers.

```text
weapon_base_moa = f(barrel, action, muzzle device)
base_moa        = weapon_base_moa * (1 + barrel_wear_penalty)

velocity_moa    = f(velocity_SD, muzzle_velocity, range)
bullet_moa      = f(batch BC spread)

chamber_fit_multiplier = 0.85 when fire-formed brass matches chamber and neck-sized
runout_multiplier      = 1 + f(cartridge runout)
stability_factor       = f(twist, bulletLength, bulletDiameter)
jump_multiplier        = f(jump distance from lands)

group_moa = (base_moa + velocity_moa + bullet_moa)
            * chamber_fit_multiplier
            * runout_multiplier
            * stability_factor
            * jump_multiplier
```

Per-shot impact is sampled from the group distribution, then environmental
deflections are applied (wind, and Coriolis for ELR regimes).

## Jump and Pressure Coupling

Compute jump from barrel freebore/throat erosion and round CBTO geometry.

```text
effective_freebore = barrel.freeBore + barrel.throatErosion
bullet_protrusion  = bullet.ogiveLength - (ammo.cbto - casing.currentLength)
jump               = effective_freebore - bullet_protrusion
```

Pressure trend expectations:
- Negative jump (jam): higher pressure, risk spike with deep jam.
- Zero jump (touching): modest pressure increase.
- Typical positive jump (~0.010-0.030"): baseline regime.
- Large positive jump: slight pressure decrease, usually worse grouping.

## Equipment -> Variable Mapping

Use equipment quality to affect specific error terms:

- Press: `sizing_sd`, `seating_sd`, `runout_multiplier`
- Sizing die: `sizing_sd`, `neck_tension_sd`
- Seating die/arbor press: `seating_sd`, `runout_multiplier`
- Scale/powder measure: `charge_sd`
- Annealer: `neck_tension_sd`
- Primer seater: `primer_seating_sd`
- Trimmer: `neck_tension_sd` (length consistency path)
- Shell holder: `runout_multiplier`

Measurement tools do not directly lower SD; they unlock player decisions:
- Chronograph: exposes velocity SD
- Concentricity gauge: exposes runout
- Headspace gauges/comparator: validates chamber and shoulder bump
- OAL gauge + bullet comparator: enables CBTO/jump tuning

## Data-Driven Requirement

Do not hardcode ammo quality by recipe name. Always:
1. Read deviation fields from definitions.
2. Sample per-instance runtime values.
3. Let sorting/prep workflows reduce batch spread.

Without this, quality-control gameplay and progression collapse.

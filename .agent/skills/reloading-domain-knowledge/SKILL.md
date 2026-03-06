---
name: reloading-domain-knowledge
description: Provides real-world ammunition reloading and ballistic reference knowledge for load development and precision shooting systems. Use when implementing reloading mechanics, ballistics calculations, ammunition behavior, weapon interactions, or debugging load-development game logic that supports Reloader's assassination-contract sandbox.
---

# Reloading Domain Knowledge

## When to Use

- Implementing any reloading workbench mechanic
- Writing ballistics or accuracy calculations
- Modeling ammo quality consequences for long-range shots
- Designing failure modes and danger mechanics
- Creating realistic equipment behavior
- NOT appropriate when: working on UI, contract generation, police heat, driving, or non-reloading systems unless the task directly depends on load-development knowledge

## Prerequisites

Read these docs first:
1. `docs/design/core-architecture.md` — shared patterns, project structure
2. `docs/design/reloading-system.md` — reloading system design and data models
3. `docs/design/weapons-and-ballistics.md` — weapon system and ballistics design (if working on accuracy/shooting)
4. `docs/design/assassination-contracts.md` — when the work affects premium long-range contract play

## Core Concepts

### The Cartridge

A complete round of ammunition ("cartridge") consists of four components:

```
┌──────────────┐
│   Bullet     │  ← Projectile (what flies downrange)
├──────────────┤
│   Case       │  ← Brass/metal container (holds everything together)
│   ┌────────┐ │
│   │ Powder │ │  ← Propellant (burns to create gas pressure)
│   └────────┘ │
│   [Primer]   │  ← Ignition source (detonates when struck by firing pin)
└──────────────┘
```

Terminology: "bullet" = just the projectile. "Round" or "cartridge" = the whole assembly. Common misconception: calling the whole cartridge a "bullet."

### The Reloading Process (Real World)

Steps in order, with WHY each matters:

| Step | Purpose | Skip Consequence |
|------|---------|-----------------|
| Inspect brass | Find cracked, stretched, or damaged cases | Case head separation (catastrophic) |
| Clean | Remove carbon, dirt, sizing lube residue | Feeding issues, chamber contamination |
| Lube cases | Reduce friction in sizing die | Case stuck in die (common beginner mistake) |
| Full-length resize OR neck size | Return case to proper dimensions | Won't chamber (FL) or inconsistent neck tension (NS) |
| Trim to length | Remove brass that stretched during firing | Excess pressure from crimped-in mouth |
| Deburr/chamfer | Clean case mouth after trimming | Shaves bullet jacket during seating |
| Prime | Seat new primer in primer pocket | No ignition / hangfire / slam-fire |
| Charge powder | Measure and dispense propellant | Under: squib. Over: catastrophic failure |
| Seat bullet | Press projectile into case mouth | Affects pressure (deeper = higher) and feeding |
| Crimp (optional) | Lock bullet in place | Bullet setback under recoil (semi-autos) |
| Final inspection | Check OAL, visual defects | Varies by missed defect |

### Fire-Forming and Brass Fit

When fired, brass expands to fill the chamber exactly. This is "fire-forming."

- **Full-length resize:** Pushes brass back to SAAMI minimum dimensions. Fits ANY chamber of that caliber. Creates more working (brass movement per firing cycle) = shorter case life.
- **Neck sizing / shoulder bump:** Only sizes the neck diameter and bumps the shoulder back 0.001-0.002". Brass retains its fire-formed body dimensions. Fits ONLY the chamber it was fired in (or very similar). Less brass working = longer life + better consistency.

This is why benchrest shooters neck-size only and never mix brass between rifles.

### Headspace

Headspace is the distance from the bolt face to the chamber's datum point:

```
Bottleneck rifle cartridge headspace:

     Bolt Face                    Shoulder Datum
        │                              │
        │◄──── headspace (inches) ────►│
        │                              │
   ═════╪════════════════════╗    ╔════╪═══
        │    Case Body       ║    ║  Neck
        │                    ║    ║
   ═════╪════════════════════╝    ╚════════
        │
```

- **Bottleneck cases (.308, 6.5CM, etc.):** datum = shoulder angle. Most common.
- **Straight-wall cases (9mm, .45 ACP):** datum = case mouth. Headspace on the case mouth.
- **Rimmed cases (.30-30, .44 Mag):** datum = rim. Headspace on the rim.
- **Belted magnums (.300 Win Mag):** datum = belt (though modern practice headspaces on shoulder).

**SAAMI headspace tolerances:**
- **GO gauge:** minimum safe headspace. A new chamber must accept this.
- **NO-GO gauge:** maximum safe headspace. If a NO-GO gauge drops in, the chamber is out of spec — worn or over-reamed. Ammo may still fire but cases stretch excessively.
- **FIELD gauge:** absolute maximum. If a FIELD gauge drops in, the rifle is unsafe to fire.

**Why headspace matters for reloading:**

1. **Shoulder bump is a headspace adjustment.** When the player sets up a sizing die, they're controlling how far the shoulder gets pushed back. The ideal is 0.001-0.002" bump from the fire-formed dimension — just enough to chamber smoothly.

2. **Too much shoulder bump = excessive headspace.** The case is now shorter (shoulder-to-base) than the chamber. On firing, the case slams back against the bolt face and stretches to fill the gap. After 2-3 firings with excessive bump, a bright ring appears just above the case web (incipient head separation). One more firing and the case tears apart — hot gas vents through the action. This is one of the most common catastrophic failures in reloading.

3. **Too little shoulder bump = won't chamber.** The bolt won't close because the shoulder hits the chamber shoulder before the bolt can lock. The player has to screw the die down further.

4. **Neck sizing avoids the problem entirely** for fire-formed brass in the same chamber — the shoulder isn't touched, so headspace stays matched to that specific chamber. This is why neck sizing is preferred for precision (already modeled via fire-forming).

5. **Different chambers have different headspace** within SAAMI tolerance. Brass fire-formed in a generous chamber and then full-length resized to SAAMI minimum will have maximum case stretching in a tight chamber that barely accepts a GO gauge. This is why mixing brass between rifles is risky.

**Headspace gauges** (GO/NO-GO/FIELD) are diagnostic equipment. The player can check if a chamber is safe and use a headspace comparator (Hornady/Sinclair) on sized brass to verify shoulder bump amount.

### CBTO, OAL, and Distance from Lands (Jump)

Three length measurements matter for a loaded cartridge:

```
                        ┌─────── cartridgeOAL (tip to base) ───────┐
                        │                                           │
                   ┌────┼──── cbto (ogive datum to base) ───┐      │
                   │    │                                    │      │
            ╔══════╩════╩═══╗                                │      │
            ║    Bullet     ║◄── ogive datum (where lands    │      │
            ║               ║    would contact)              │      │
         ───╫───────────────╫────────────────────────────────┤      │
            ║  Case  Neck   ║           Case Body            │ Base │
         ───╫───────────────╫────────────────────────────────┤      │
            ╚═══════════════╝                                       │
                        └───────────────────────────────────────────┘
```

- **OAL (Overall Length):** Tip-to-base. Varies with bullet tip shape (meplat). Used for magazine fit checks. NOT reliable for controlling distance from lands because tips vary.
- **CBTO (Cartridge Base to Ogive):** Base of the cartridge to the ogive datum point — the spot on the bullet's curved section that contacts the rifling lands. Measured with a bullet comparator (Hornady/Sinclair). This is the **repeatable, precision-critical measurement** that controls jump.
- **Ogive datum:** The specific diameter on the bullet's ogive curve that matches the bore diameter at the start of the rifling. Different bullet designs (secant vs. tangent ogive, VLD, hybrid) have this point at different positions along the bullet.

**Distance from lands (jump):**

```
effective_freebore = barrel.freeBore + barrel.throatErosion
bullet_protrusion  = bullet.ogiveLength - (ammo.cbto - casing.currentLength)
jump               = effective_freebore - bullet_protrusion
```

- **Positive jump (0.010-0.030"):** Normal. Bullet travels a short distance before engaging rifling. Most loads shoot best in this range.
- **Zero jump ("kiss"):** Bullet ogive just touches the lands when chambered. Some loads prefer this. Increases pressure slightly.
- **Negative jump ("jam"):** Bullet is pressed into the rifling when chambered. Significant pressure increase. Risky, but some benchrest shooters use light jams (0.005-0.010" jam) with specific bullets.
- **Large jump (0.050"+):** Bullet must travel far before engaging rifling. Allows the base to yaw before stabilization. Groups open up. Common in factory chambers with SAAMI-spec freebore.

**Why jump matters for accuracy:**
1. Jump affects how the bullet enters the rifling — less jump = more consistent alignment = tighter groups.
2. Jump affects pressure — less jump = higher starting pressure = different optimal charge.
3. Each bullet/powder combo has an optimal jump distance. Finding it is part of load development.
4. Throat erosion slowly increases effective freebore over the barrel's life, which increases jump and degrades the load's tune. Precision shooters re-measure and adjust seating depth as the barrel wears.

**Why CBTO > OAL for precision:**
OAL varies ±0.005-0.010" even among identical bullets because meplat (tip) shape is inconsistent. CBTO varies ±0.0005" or less with match bullets. A reloader chasing 0.020" jump can't use OAL — the tip variance alone is larger than the jump window. CBTO is the only measurement that controls jump with sufficient precision.

**Measuring jam length (per rifle + bullet combo):**
The player uses an OAL gauge (modified case + plunger) inserted into the chamber. Push a dummy bullet forward until it contacts the lands. Measure the resulting CBTO — that's the "jam length." Subtract desired jump to get the target seating CBTO. Must be re-measured as the barrel wears.

### Pressure and Safety

Pressure is the critical safety parameter. It's affected by:

| Factor | Effect on Pressure |
|--------|-------------------|
| More powder | Higher |
| Faster-burning powder | Higher |
| Heavier bullet | Higher (more resistance) |
| Deeper bullet seating | Higher (less case volume) |
| Bullet jammed into lands | Higher (bullet resists initial movement, peak pressure rises) |
| Bullet touching lands ("kiss") | Slightly higher |
| Large jump (off the lands) | Slightly lower (bullet accelerates before engaging rifling) |
| Hotter primer | Slightly higher |
| Higher ambient temperature | Higher (powder burns faster) |
| Shorter barrel | Same pressure, less velocity |
| Tight chamber | Higher |
| Dirty chamber | Higher |
| Excessive headspace | Case stretching → head separation risk (not pressure per se, but catastrophic) |

**Warning signs of overpressure (in escalating order):**
1. Flattened primers (primer metal flows into firing pin hole)
2. Ejector marks on case head
3. Hard bolt lift (sticky extraction)
4. Bright marks on case head (brass flow)
5. Cracked case neck or body
6. Blown primer (primer falls out)
7. Case head separation (case tears apart)
8. Catastrophic failure (action blows apart) — this destroys the weapon and injures the shooter

### Accuracy Factors

What makes ammo accurate (tight groups):

**Fundamental factors (every reloader should know):**

| Factor | Why |
|--------|-----|
| Consistent powder charge | Same velocity shot to shot = same drop |
| Consistent bullet weight | Same BC = same trajectory |
| Consistent neck tension | Same bullet release = same velocity |
| Consistent seating depth | Same chamber pressure = same velocity |
| Fire-formed brass to that rifle | Perfect chamber fit = consistent release |
| Matched bullet to twist rate | Proper stabilization = minimal yaw |
| Concentric cartridge | Bullet aligned with bore = straight exit |

SD (Standard Deviation) of muzzle velocity is the #1 predictor of long-range accuracy. A 5 fps SD load will shoot dramatically better at 1000 yards than a 25 fps SD load.

**Benchrest / precision nerd factors (the game MUST model these for depth):**

| Factor | Detail | Match Grade Spec |
|--------|--------|-----------------|
| Bullet weight sorting | Weigh every bullet, sort into groups | ±0.1-0.3gr deviation for match grade. Budget bullets can vary ±1gr+ |
| Bullet diameter consistency | Measure with micrometer, reject outliers | ±0.0001" for top match bullets. Budget may vary ±0.001" |
| Bullet length (ogive to base) | Affects BC directly. Longer = higher BC | Sort by bearing surface length for ultimate consistency |
| Bullet meplat uniformity | Tip shape affects BC | BR shooters use meplat trimming tools to uniform tips |
| Case weight sorting | Indicates internal volume consistency | Sort by weight — ±0.5gr groups. Volume drives pressure. |
| Case neck wall thickness | Uneven = non-concentric bullet release | Neck turn cases to uniform wall thickness (±0.0005") |
| Case internal volume | Directly affects pressure for given charge | Measure with water overflow method, sort into groups |
| Primer pocket depth uniformity | Affects primer seating depth = ignition consistency | Uniform primer pockets with reaming tool |
| Flash hole deburring | Burr from manufacturing diverts flame unevenly | Deburr flash holes for consistent ignition |
| Neck tension consistency | Brass hardness varies = tension varies | Anneal necks to reset hardness to known state |
| Annealing (life + consistency) | Resets work-hardened brass neck/shoulder. Two benefits: (1) extends case life — un-annealed brass cracks after fewer firings, (2) makes sizing more consistent — uniform hardness = uniform neck tension after sizing. Equipment quality matters: cheap propane torch is inconsistent; quality induction annealer (AMP Annealing) gives precise repeatable results. | Anneal every 2-5 firings for consistency |
| Primer seating tool quality | Uniform primer depth = uniform ignition = tighter velocity SD. A wobbly hand primer gives ±0.003" depth variation; a quality bench-mounted seater (K&M, Primal Rights) gives ±0.0005". Combines with primer pocket uniformity — even a great tool can't fix a sloppy pocket. | Bench-mounted seater for premium long-range work |
| CBTO consistency | Cartridge Base To Ogive measured to ogive datum with comparator. Repeatable to ±0.0005". OAL (tip-based) varies ±0.005-0.010" even with identical bullets — useless for controlling jump. | ±0.0005" for match, verify with comparator |
| Distance from lands (jump) | Distance bullet ogive travels before engaging rifling. Computed from barrel.freeBore + throatErosion vs. bullet.ogiveLength and ammo.cbto. Optimal: 0.010-0.030" for most loads. Must be measured per rifle+bullet combo with OAL gauge. | Measure jam length, seat to target CBTO |
| Jump consistency (batch) | Even with optimal average jump, CBTO variance round-to-round creates jump variance → opens groups. Driven by seating die/tool quality. | CBTO SD < 0.0005" for match |
| Cartridge concentricity (runout) | Bullet axis vs. case axis misalignment | <0.001" TIR for match. Budget ammo can be 0.003-0.005" |

**Environmental and handling factors (often overlooked):**

| Factor | Detail |
|--------|--------|
| Ammo storage temperature | Powder burn rate changes with temperature. Ammo stored in hot car vs. cool room will shoot to different POI. Temperature-insensitive powders (Hodgdon Extreme series) mitigate this. |
| Ammo age | Powder degrades over time (decades, but still). Very old ammo may have inconsistent velocity. Primers can lose sensitivity with age/moisture. |
| Humidity exposure | Primers and powder absorb moisture if not sealed. Degraded ignition. |
| Magazine feeding / recoil setback | In semi-autos or bolt guns with magazine feeding, recoil can push bullets deeper into the case (setback) if neck tension is low or no crimp is applied. Each feeding cycle from mag can bump the bullet. OAL may decrease 0.002-0.010" over multiple feedings. This changes pressure and accuracy. |
| Bullet-to-case friction (pull force) | How hard it is to pull the bullet out of the case. Directly related to neck tension. Should be consistent across a batch. |
| Powder position in case | In large cases with small charges, powder can settle to one side. Vertical vs. horizontal ammo storage matters for extreme precision. Some shooters tip the rifle muzzle-up before each shot to settle powder near the primer. |
| Bore fouling state | A clean bore shoots to a different POI than a fouled bore. Most precision shooters fire "fouling shots" before competing. Copper fouling builds over hundreds of rounds. |
| Barrel temperature | Hot barrel = larger bore diameter = velocity change + accuracy degradation. Strings of rapid fire walk the group. Wait between shots for precision. |

These niche factors create gameplay depth: a casual player ignores them and shoots "okay" groups. A dedicated player who weighs brass, sorts bullets, anneals necks, and manages storage shoots tiny groups that solve premium long-range contracts.

### Bullet Types and Terminal Performance

| Type | Full Name | Use | Terminal Behavior |
|------|-----------|-----|-------------------|
| FMJ | Full Metal Jacket | Range validation, cheap practice | Pass-through, minimal expansion |
| HP | Hollow Point | Match, varmint | Expands rapidly, fragments at high velocity |
| SP | Soft Point | Hunting | Controlled expansion, good weight retention |
| BondedHP | Bonded Hollow Point | Hunting large game | Maximum weight retention, reliable expansion |
| Match | Match (usually HPBT) | Precision contracts | Ultra-consistent BC, optimized for accuracy rather than terminal effect |
| Solid | Solid copper/brass | Dangerous game, lead-free | No fragmentation, deep penetration |
| Cast | Cast lead | Plinking, cowboy | Low velocity, lead fouling at high velocity |

Match bullets are optimized for precision, not reliable terminal performance. Use them when the design goal is accuracy, not expansion.

**Terminal performance fields (future):** `BulletDefinition` may later need terminal ballistics fields (`expansionFactor`, `weightRetention`, `minExpansionVelocity`) for systems that model body damage or post-hit lethality in more detail. For v0.1, bullet `type` enum is sufficient for basic behavior differentiation. Detailed terminal fields should use `customProperties` until that gameplay is built, then promote to first-class fields.

### Equipment Tiers

**Speed and Precision are SEPARATE axes.** A progressive press is fast but less precise. An arbor press is slow but the most precise seating tool. EquipmentDefinition may use one, both, or neither axis depending on tool type, but optional numeric axes should use a serializable optional wrapper pattern (for example `OptionalFloat { isSpecified, value }`) instead of nullable primitives.

**Precision tiers (affects accuracy — maps directly to accuracy calculation variables):**

| Equipment | Budget (high error) | Mid | Premium Precision (low error) |
|-----------|---------------------|-----|-------------------------------|
| Press (sizing) | Lee single-stage (flex frame) | RCBS Rock Chucker | Area 419 / Forster Co-Ax (rigid, aligned ram) |
| Press (seating) | Lee single-stage | Quality single-stage | Arbor press (K&M) + Wilson hand die = GOLD STANDARD |
| Sizing die | Lee steel (fixed neck) | Redding standard | Redding Comp bushing (choose exact neck tension) |
| Seating die | Lee seating | Redding micrometer | Wilson hand die / Forster BR micrometer (self-centering) |
| Scale | Lee beam (±0.1gr) | RCBS 505 beam | A&D FX-120i + Autotrickler V4 (±0.02gr) |
| Powder measure | Lee dipper | RCBS Uniflow | Autotrickler (electronic trickle to exact charge) |
| Trimmer | Lee gauge (rough) | Wilson hand | WFT / Giraud power (consistent, fast) |
| Primer seater | Lee hand prime (wobbly) | RCBS hand | K&M bench / Primal Rights (±0.0005") |
| Annealer | Propane torch (inconsistent) | Torch jig (better) | AMP induction (precise, repeatable) |
| Headspace gauge set | None (guess/hope) | GO/NO-GO gauges per caliber | Same + FIELD gauge |
| Headspace comparator | None (can't measure bump) | Hornady/Sinclair headspace comparator | Same (insert + quality calipers) |
| OAL gauge | None (guess from manual OAL) | Hornady Lock-N-Load OAL gauge | Same (standard tool) |
| Bullet comparator | None (calipers measure OAL only) | Hornady/Sinclair comparator insert | Same (insert + quality calipers) |
| Shell holder | Standard stamped | — | Competition lapped (flat, aligned) |

**Speed tiers (affects throughput — separate from precision):**

| Type | Speed | Precision | Use Case |
|------|-------|-----------|----------|
| Single-stage press | Slow (1 op/pull) | High (rigid, controlled) | Match ammo, precision loads |
| Turret press | Medium (multiple dies) | Medium | General reloading |
| Progressive press (Dillon, Hornady) | Fast (all ops/pull, 400-600 rds/hr) | Lower (less control) | Volume ammo: plinking, 3-gun |
| Arbor press + hand die | Slowest | Highest | Benchrest seating only |

A benchrest shooter uses arbor press for seating + premium single-stage for sizing. A 3-gun competitor uses a progressive for volume. Both are valid — different optimization targets.

## Ballistics Quick Reference

### Exterior Ballistics Factors

```
Point of aim → point of impact affected by:

Gravity        → bullet drops. More drop at slower velocity / longer range.
Wind           → lateral drift. Proportional to wind speed, time of flight, and bullet BC.
Air density    → altitude + temp + humidity. Lower density = less drag = flatter trajectory.
Coriolis       → at extreme range (1000+ yards), Earth's rotation deflects bullet.
                 Right in Northern Hemisphere, left in Southern.
                 Vertical component exists too (Eötvös effect).
Spin drift     → gyroscopic precession causes bullet to drift in direction of twist.
                 Right-hand twist (most common) = drift right.
Magnus effect  → crosswind + spinning bullet = vertical deflection. Usually minor.
```

### Barrel Twist and Bullet Stability

Barrel twist rate (e.g., 1:10" means one full rotation per 10 inches) must match bullet length:
- Too slow → bullet doesn't stabilize → keyholing (hitting target sideways)
- Too fast → over-stabilized → won't transition to nose-down flight at long range
- Heavier/longer bullets need faster twist
- Example: .308 with 168gr needs ~1:11". 175gr needs ~1:10". 220gr needs ~1:8".

The stability calculation uses bullet LENGTH, not weight. `BulletDefinition.bulletLength` (total base-to-tip in inches) is the input to the Greenhill formula. Two 168gr .308 bullets can have different lengths due to ogive design differences — a VLD has a longer ogive than a flat-base, making it longer overall for the same weight.

**BC drag model:** `BulletDefinition.bcModel` specifies G1 or G7. The same bullet can have BC 0.462 (G1) or 0.220 (G7) — using the wrong model produces wildly incorrect trajectories. G7 is preferred for modern boat-tail rifle bullets; G1 for flat-base pistol/varmint bullets.

### Common Caliber Data

Use `resources/caliber-reference.md` for canonical caliber specs (including rimfire buy-only policy via `isReloadable=false`).

## Implementation Notes

Detailed implementation pseudocode and formulas are in `resources/accuracy-model.md`.

Non-negotiable implementation rules:
1. Use two-phase modeling: assembly-time intrinsic fields on `AmmoInstance`, then firing-time composition with weapon + environment.
2. Keep lot variance as a systematic offset (`PowderLotInstance` behavior), not a per-shot SD term.
3. Sample per-instance component variance from definition deviation fields. Never treat all rounds from one recipe as identical.
4. Model speed and precision as independent equipment axes.
5. Use CBTO/jump and chamber-fit effects in firing-time group-size computation.

An agent implementing accuracy MUST read the factory deviation fields from `adding-game-content` skill and sample per-instance values. Without per-instance variance, all ammo from the same recipe would perform identically, defeating the sorting/quality-control gameplay.

Equipment precision matters at every step — not just the components but the tools used to assemble them drive the final accuracy.

## Resources

- [resources/caliber-reference.md](resources/caliber-reference.md) — detailed caliber specifications
- [resources/burn-rate-chart.md](resources/burn-rate-chart.md) — relative burn rate ordering
- [resources/accuracy-model.md](resources/accuracy-model.md) — squib/overpressure logic, velocity SD model, group-size model, and equipment mapping

# Bullet Energy Health, Hit Zones, And Blood VFX Design

> **Status (2026-04-16):** Stages A/B/C are implemented. This remains the current contract for energy ownership, 100 health, exact zone damage, NPC live hit-zone selection, player shared-receiver/default-torso behavior, contract target shared-death routing, and project-owned blood VFX. Third-party `RealisticBloodVFX` mappings are optional only after validation.

## Goal

Make humanoid bullet damage depend on projectile energy at impact, preserve the existing shared humanoid death event path, restore readable red blood feedback, and avoid a global one-shot rule that makes limb hits instantly lethal.

This is a v0.1 cutover/restoration slice. It should make the starter .308 rifle feel lethal where expected without adding penetration, exit wounds, limb impairment AI, bleeding simulation, or a direct dependency on third-party blood prefabs.

## Current Repository Baseline

Implemented seams already present:

- `WeaponProjectile` builds `ProjectileImpactPayload` with impact direction, speed, projectile mass, and delivered energy.
- `ImpactEnergyMath` owns the joules formula.
- `WeaponAmmoDefaults` defines `ammo-factory-308-147-fmj` as 147 gr at 2780 fps with G1 BC 0.398 and 55 fps SD.
- `StarterRifle.asset` uses `ammo-factory-308-147-fmj`.
- `HumanoidDamageReceiver` is the shared damage/death authority for NPCs and the player.
- `HumanoidDamageReceiver.Died` drives existing ragdoll, corpse loot, witness reporting, player death recovery, and contract elimination seams.
- `HumanoidBodyZone` already includes `Head`, `Neck`, `Torso`, `Pelvis`, `ArmL`, `ArmR`, `LegL`, and `LegR`.
- `BodyZoneHitbox` and `HumanoidHitboxRig` provide live NPC body-zone hit selection, with same-rig zone colliders preferred over root/default fallback.

Implemented cutover:

- `HumanoidDamageReceiver` defaults to 100 max health for NPCs, the player, and procedural/contract targets.
- `ContractTargetDamageable` is a contract-elimination bridge over `HumanoidDamageReceiver.Died`, not a legacy private one-shot health authority.
- `HumanoidImpactResolution` uses the exact zone-specific lethal thresholds and capped damage table below.
- NPC live body-zone colliders are enabled on `NpcFoundation`; projectile selection prefers same-rig `BodyZoneHitbox` colliders over broad root/default torso fallback.
- The player remains on shared receiver/default torso behavior in v0.1.
- `HealthHud` is the read-only runtime UI Toolkit presentation of the shared `HumanoidDamageReceiver` on `PlayerRoot`; it shows current/max health, fill state, low/critical/dead styling, and a short damage flash through the existing UI screen mapping and installer prefab.
- Blood VFX is integrated through project-owned `HumanoidBloodController`, `BloodVfxCatalog`, and red placeholder prefabs/materials; `RealisticBloodVFX` remains optional after validation.

## Energy Contract

Projectile energy stays in the weapons/core seam:

- `ImpactEnergyMath.ComputeDeliveredEnergyJoules(speedMetersPerSecond, massGrains)` remains the only formula owner.
- `WeaponProjectile` remains responsible for current impact speed after gravity/drag and for placing the computed delivered energy in `ProjectileImpactPayload`.
- `HumanoidImpactResolution` consumes `ProjectileImpactPayload.DeliveredEnergyJoules`; it must not recompute exterior ballistics or own ammo defaults.

The current factory .308 default should remain realistic-ish:

| Ammo | Velocity | Mass | Approx muzzle energy | Notes |
|---|---:|---:|---:|---|
| `ammo-factory-308-147-fmj` | 2780 fps | 147 gr | about 3.42 kJ | Starter rifle default; impact energy decreases downrange through projectile drag/BC. |

For deterministic v0.1 damage tests, use `3500 J` as the full-power rifle reference and keep a separate energy-contract test proving the authored .308 default computes about `3419.6 J` at the muzzle.

## Health Model

Use one shared humanoid health authority:

- `HumanoidDamageReceiver` is the authoritative health/death component for NPCs and the player.
- Default max health becomes exactly `100`.
- Runtime and prefab defaults must be cut over together so fresh NPCs, the player root, and procedurally spawned targets agree.
- Death must always go through `HumanoidDamageReceiver.Died`; do not bypass it with player-only, contract-only, witness-only, or ragdoll-only damage code.

Contract target cutover:

- Active contract targets must use the same `HumanoidDamageReceiver` health pool as other humanoids: `MaxHealth == 100` and current health reset to 100 unless a future explicit target archetype says otherwise.
- `ContractTargetDamageable` must become a contract-elimination bridge over shared humanoid death, not a second health authority.
- Legacy `_maxHealth = 1f` serialized/default/reset behavior must be removed, ignored, or replaced so it cannot make any hit, arm hit, or leg hit eliminate a contract from full health.
- Contract elimination must occur only after `HumanoidDamageReceiver.Died`; non-lethal shared damage must not complete, fail, retire, or claim a contract target.

Health is still simple in v0.1:

- No armor.
- No bleeding-over-time.
- No wound infection or medical system.
- No limb impairment AI.
- Repeated non-lethal hits can kill by accumulated health damage.

## Zone-Specific Damage Rules

Do not use one global lethal threshold for all zones. Zone tuning must state both instant-lethal behavior and capped health damage.

Formula:

```text
if zone.lethalEnergyJ != null and impactEnergyJ >= zone.lethalEnergyJ:
    instant lethal
else:
    damage = min(zone.maxDamage, impactEnergyJ * zone.damagePerJoule)
```

Each zone owns its `damagePerJoule`, `maxDamage`, and optional `lethalEnergyJ`. Avoid an opaque global multiplier because it makes tuning hard to reason about and reintroduces limb one-shots.

Exact v0.1 constants:

| Zone | Instant lethal threshold | `damagePerJoule` | Max per-hit health damage | Full-power `3500 J` result from 100 health |
|---|---:|---:|---:|---|
| Head | `200 J` | `0.50` | `100` | Instant lethal. |
| Neck | `200 J` | `0.50` | `100` | Instant lethal. |
| Torso | `1800 J` | `0.045` | `100` | Instant lethal. |
| Pelvis | none in v0.1 | `0.030` | `80` | 80 damage; one hit does not kill, two can through accumulated health. |
| ArmL / ArmR | none in v0.1 | `0.010` | `35` | 35 damage; one hit never kills, three full-power hits can through accumulated health. |
| LegL / LegR | none in v0.1 | `0.015` | `45` | 45 damage; one and two full-power hits do not kill, three can through accumulated health. |

Deterministic implications:

- Head and neck hits at `200 J` or higher kill immediately.
- Torso hits at `1800 J` or higher kill immediately; lower-energy torso hits use accumulated health damage.
- Pelvis has no immediate lethal threshold in v0.1 and is capped at `80` damage per hit.
- One full-power arm hit from 100 health leaves at least 65 health; repeated arm hits can still kill by accumulation.
- One full-power leg hit from 100 health leaves at least 55 health; two leave at least 10 health; the third can kill by accumulation.

Suggested implementation shape:

- Add a small `HumanoidZoneDamageRule` value type or static table near `HumanoidImpactResolution`.
- Return both raw `ImpactEnergyJoules` and final `RecommendedHealthDamage`.
- Keep `EffectiveEnergyJoules` only if it remains clearly documented as display/debug metadata, not as the hidden global lethal input.

## NPC Hit-Zone Authoring

NPC live hit zones are part of this slice.

Rules:

- `BodyZoneHitbox` colliders must be valid live ballistic colliders, not dormant authoring remnants.
- Projectile hit selection should prefer the first non-trigger non-ignored collider, but NPC zone colliders must not be hidden behind a broad root capsule that forces torso/default hits.
- On NPCs, a hit on a body-zone collider resolves that exact `HumanoidBodyZone`.
- Root/default fallback remains only for non-rigged or malformed NPCs, and should resolve to torso.
- Editor tests should load `NpcFoundation.prefab` and representative role prefabs to prove authored zone coverage and live collider state.

The v0.1 goal is correct gameplay selection, not perfect anatomy. Simple colliders on the existing humanoid skeleton are acceptable if they produce stable head/body/arm/leg routing.

## Player Damage Boundary

Use the shared receiver already on `PlayerRoot.prefab`:

- Do not introduce a separate player-only health authority.
- Keep `PlayerDeathContractBridge` subscribed to `HumanoidDamageReceiver.Died`.
- Verify death recovery runs once per alive-to-dead edge.
- Verify player health save/restore still uses the shared receiver state.
- Keep `HealthHud` bound to the same shared receiver so UI reflects the player health contract rather than owning a parallel authority.

Player zone scope for v0.1:

- Default player ballistic damage to torso unless a lightweight proxy hitbox rig can be added without destabilizing the controller/camera rig.
- Do not block NPC zone correctness on player limb/head authoring.

## Blood VFX Restoration

Gameplay should depend on project-owned blood semantics, not asset-store package internals.

Add a project wrapper/catalog:

- `HumanoidBloodController` listens to humanoid impact/death results.
- `BloodVfxCatalog` maps semantic requests to project-owned prefabs.
- Semantic kinds should stay small: `LightImpact`, `HeavyImpact`, `NeckImpact`, `DeathPuddle`.
- Project-owned red wrapper prefabs/materials live under `_Project`, for example `Reloader/Assets/_Project/NPCs/Prefabs/VFX/` and `Reloader/Assets/_Project/NPCs/Materials/VFX/`.
- Direct references to `Assets/HIVEMIND/RealisticBloodVFX/**` are allowed only after validating the specific extracted prefab/material in this project.

Minimal validation-gated acceptance:

- Project-owned red default spray/splatter/puddle prefabs and materials are required for v0.1.
- Tests or a manual smoke scene must prove the project-owned defaults spawn on humanoid hits/death and are visibly red.
- Missing project-owned defaults fail validation because gameplay would regress to generic spark-only or black-only feedback.
- Missing `RealisticBloodVFX` package mappings must not block gameplay or tests; package assets are optional polish after validation.

Visual rules:

- Blood is red and readable against the scene.
- Avoid black puddle-only results.
- Avoid generic gray/yellow impact-only results for humanoid hits.
- Humanoid blood spawning should suppress or coexist cleanly with the generic projectile impact VFX so bullet hits on people do not look like sparks.

## Cross-Domain Event And Death Constraints

Do not change cross-domain event contracts for this slice unless a concrete test requires it.

Existing death listeners must keep working:

- Ragdoll/corpse loot uses humanoid death.
- Witness reporting uses humanoid death.
- Contract target elimination uses humanoid death/bridge semantics.
- Player death recovery uses `PlayerDeathContractBridge`.
- Police hostile shooting should continue to use the shared projectile path into the player's `HumanoidDamageReceiver`.

## Non-Goals

- Bullet penetration or pass-through.
- Exit wounds.
- Armor or material penetration modeling.
- Limb AI impairment, limping, stagger, panic, or flee behavior.
- Bleeding-over-time or blood trails.
- Full third-party blood package dependency.
- Player ragdoll or fully authored player limb/head hitboxes unless a trivial proxy falls out safely.

## Acceptance Criteria

- A 147 gr .308 at 2780 fps produces about `3419.6 J` muzzle energy through the existing formula and loses speed/energy downrange.
- Default humanoid health is exactly `100` across runtime defaults, NPC foundation, player prefab, and procedural target setup.
- Active contract targets use shared `100` health; `ContractTargetDamageable` legacy `_maxHealth = 1f` / reset behavior does not preserve limb one-shot elimination.
- A full-power .308 arm or leg hit from full health does not eliminate an active contract target.
- A head/neck hit at `200 J` or higher kills.
- A torso hit at `1800 J` or higher kills through torso-specific instant-lethal tuning.
- Pelvis has no immediate lethal threshold in v0.1 and caps at `80` damage per hit.
- A single arm or leg hit from full health does not kill, even at full-power .308 rifle energy.
- One and two full-power leg hits from full health do not kill; three can through accumulated health.
- One arm hit never kills; several arm hits can kill through accumulated health if enough land.
- NPC body-zone colliders receive live projectile hits instead of being bypassed by a root capsule/default torso fallback.
- Player damage uses `HumanoidDamageReceiver` and `PlayerDeathContractBridge`; no parallel player health authority exists.
- `HealthHud` displays the shared player health contract and does not create a second authority.
- Humanoid bullet hits spawn red project-owned blood effects, with no black-only or generic spark-only result.
- Missing third-party blood package mappings do not block gameplay, but missing project-owned red defaults fail validation.
- Contract, witness, ragdoll, corpse loot, and player recovery death listeners still go through `HumanoidDamageReceiver.Died`.

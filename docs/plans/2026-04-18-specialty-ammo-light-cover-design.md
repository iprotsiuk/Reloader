# Specialty Ammo Light Cover Design

> **Status:** Shipped slice. Runtime, save, and UI evidence now exists in repository for the AP/light-cover proof point.
> **Prerequisites:** Read `docs/design/v0.1-demo-status-and-milestones.md`, `docs/plans/2026-03-08-breakout-slice-priority.md`, and `docs/design/weapons-and-ballistics.md`.

## Goal

Record the first specialty-ammo proof point for v0.1: a .308 AP/light-cover round that solves one visible contract problem better than the default `.308` factory FMJ round.

This slice must make load choice matter without opening a broad material simulation, armor model, ricochet model, or contract-authoring overhaul.

## Selected Approach

Use a small explicit gameplay marker for light cover and a single penetration scalar carried by the existing ammo/ballistics payload.

- Default `.308` factory FMJ keeps `CoverPenetrationPower = 0` and stops on marked light cover.
- The new `.308 AP` round gets enough `CoverPenetrationPower` to pass through one marked light-cover collider.
- Passing through cover applies a simple energy/speed reduction before the projectile continues.
- Contract briefing text uses the existing `BriefingText` field to communicate the scenario: the target may be behind office glass or light cover.

## Alternatives Considered

1. **Explicit marker + ammo penetration scalar**
   This is the selected option. It is deterministic, easy to test, and keeps the first slice game-design driven.

2. **Physics material database**
   This would classify glass, drywall, metal, and wood from materials or assets. It is too broad for the first specialty-ammo proof point and would turn one trailer-readable feature into a systems project.

3. **Energy-only generic pass-through**
   This would let any sufficiently energetic projectile penetrate ordinary colliders. It is too unpredictable for authored contract gameplay and risks making the default `.308` solve every light-cover problem.

## Runtime Contract

`AmmoBallisticSnapshot` remains the canonical ammo stat source. Add one optional penetration field and thread it through:

- `CartridgeBallisticSpec`
- `WeaponsModule.AmmoBallisticRecord`
- `WeaponsRuntimeSaveBridge`
- `ProjectileImpactPayload`
- `WeaponProjectile`

The projectile owns pass-through behavior. Damage receivers should not decide whether cover was penetrated; they only consume the final impact payload after cover attenuation has already been applied.

## Light Cover Contract

Add a tiny component for explicitly authored cover:

- Suggested name: `LightCoverPenetrable`
- Suggested serialized fields:
  - `requiredPenetrationPower`
  - `energyRetentionMultiplier`
  - `exitOffsetMeters`

Default values should support the first proof point:

- `requiredPenetrationPower = 1`
- `energyRetentionMultiplier = 0.65`
- `exitOffsetMeters = 0.05`

FMJ with `0` power stops. AP with `1` power passes through one marked collider and continues with reduced energy/speed.

## Ammo Content Contract

Add one new inventory item id for the AP round:

```text
ammo-specialty-308-150-ap
```

The exact display name can be player-facing and concise:

```text
.308 150gr AP
```

Initial ballistic values can stay close to factory `.308` to avoid retuning all damage expectations:

- `150 gr`
- `2780 fps`
- `G1 BC 0.398`
- similar velocity variance and dispersion to factory ammo
- `CoverPenetrationPower = 1`

## Contract/Intel Contract

Do not add new UI for this first slice. The existing contract briefing/intel surfaces are enough.

Update the first authored contract or add one narrow authored proof point so the briefing communicates:

- the target may be behind office glass or light cover
- default ball ammunition is less reliable
- AP/light-cover ammunition is the clean solution

This is sufficient for a testable prep-to-shot loop without adding a new contract modifier system yet.

## Testing Strategy

Use targeted tests before scene work:

- EditMode: default FMJ has no cover penetration; AP has `CoverPenetrationPower = 1`; save/load preserves the new scalar.
- PlayMode: a projectile with FMJ stops on a marked light-cover collider before reaching a target.
- PlayMode: the AP projectile passes through one marked light-cover collider and delivers reduced energy to a target.
- UI/contract tests: briefing text containing the light-cover hint flows through the existing contracts tab path.

## Non-Goals

- No generic material/thickness database.
- No ricochet/spall.
- No armor system.
- No broad specialty-ammo catalog.
- No new contract UI widgets.
- No MainTown map expansion in this slice.

## As-Built Evidence

- Ammo defaults: `WeaponAmmoDefaults` ships `ammo-factory-308-147-fmj` at `0` and `ammo-specialty-308-150-ap` at `1`.
- Light cover: `LightCoverPenetrable` and `WeaponProjectile` handle the explicit cover pass-through.
- Save/state: `WeaponsRuntimeSaveBridge` and `WeaponsModule` preserve the scalar.
- Contract intel: `MainTown_FirstContract.asset` and the existing Contracts tab `BriefingText` path carry the office-glass/light-cover hint through to the player.
- UI coverage: `TabInventoryContractsSectionPlayModeTests` and `TabInventoryContractsBridgePlayModeTests`.

This record now reflects the shipped proof point. No generic material system, ricochet model, or new contract UI was added for this slice.

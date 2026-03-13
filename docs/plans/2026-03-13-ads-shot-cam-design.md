# ADS Shot Cam Design

## Goal

Add a live cinematic shot camera for long ADS shots so premium-distance shots become readable and dramatic without breaking projectile truth or world causality.

## Scope

- Trigger shot cam only when the player fires while aiming down sights.
- Trigger immediately on fire when the current aim solution predicts a first meaningful hit beyond `100m`.
- Keep shots at `<= 100m` in normal realtime first person.
- Follow the real projectile for the full sequence.
- Allow manual cancel with `Esc`.
- Allow temporary fast-forward while holding `Space`.

## Architecture

### Eligibility And Trigger

Shot cam is a fire-time decision owned by `PlayerWeaponController`.

On a valid ADS fire:
- resolve the current authoritative aim camera
- estimate the first meaningful impact point from the current aim solution
- compare predicted distance from the firing origin to that projected point
- if the predicted first hit is `> 100m`, enter shot cam immediately on the live fired projectile

First pass non-goals:
- hip-fire shot cam
- delayed trigger after the projectile has already traveled `100m`
- replay-based or prerecorded kill cams

### Runtime Ownership

The fired projectile remains authoritative for flight, collision, and impact.

The first-pass shot-cam runtime should live under `_Project/Weapons` even though it is camera-facing work. `Reloader.Weapons` already references `Reloader.Player` and `Unity.Cinemachine`; moving the runtime into `Reloader.Player` would introduce a new assembly-cycle problem if it needs to coordinate a live `WeaponProjectile`.

Responsibilities split:
- `PlayerWeaponController`: decide eligibility, register the live projectile with the shot-cam runtime
- `WeaponProjectile`: expose enough live follow state and support a temporary cinematic-visible projectile presentation mode
- `ShotCameraRuntime`: own temporary camera activation, time-scale control, input handling, and cleanup

### Time And Camera Control

When shot cam starts:
- activate a temporary `Cinemachine` projectile-follow camera
- reduce global time scale to `0.10`
- keep the camera framed just behind and slightly above the projectile, looking along travel direction

While shot cam is active:
- holding `Space` raises time scale to `0.25`
- releasing `Space` returns to `0.10`
- pressing `Esc` exits shot cam immediately, restores the normal player-eye camera, and returns time scale to `1.0`

`Esc` is presentation cancel only:
- the projectile keeps flying
- the shot keeps its same collision and damage outcome
- the world resumes full speed immediately after the camera exits

### Projectile Visibility

The projectile must be deliberately readable in shot cam.

First pass rule:
- the live projectile enters a cinematic-visible state while followed
- this state may increase emissive intensity, apparent size, trail readability, or contrast
- it must not change ballistics, collision, damage, or hit resolution

This is a presentation override, not a simulation override.

### Exit Conditions

Shot cam ends on any of:
- projectile impact
- projectile miss / despawn termination
- manual `Esc` cancel

On exit:
- restore the normal player camera
- restore global time scale to `1.0`
- remove the projectile cinematic-visible presentation override if the projectile still exists

### World-Causality Rule

Shot cam is a live slowed-time mode, not a detached replay.

Because time scale is global in the first pass:
- police, NPCs, and other world actors slow with the shot
- the visible bullet path stays truthful relative to target motion
- the player never watches a delayed replay while the world continues attacking at full speed off-camera

## Risks

- fire-time prediction can misclassify borderline shots if the first-hit estimate is too naive
- time-scale changes may expose systems that assume full realtime behavior
- camera restore may feel abrupt if it does not re-enter the player view cleanly during ADS
- projectile readability may still fail if the visual uplift is too subtle against sky or terrain

## Testing

Focused verification should prove:
- ADS shots with predicted first hit `> 100m` enter shot cam immediately on fire
- ADS shots with predicted first hit `<= 100m` do not enter shot cam
- hip-fire never enters shot cam
- `Esc` restores the player camera and normal time while the projectile continues and can still hit
- holding `Space` raises slow-mo only while held
- impact and miss termination both restore camera/time correctly
- projectile readability state is enabled during shot cam and cleared on exit
- repeated qualifying shots do not leave the game stuck in cinematic camera or slowed time

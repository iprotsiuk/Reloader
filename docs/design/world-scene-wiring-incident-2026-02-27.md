# MainTown Wiring Incident (February 27, 2026)

## What Happened

During MainTown bring-up, item pickup succeeded but weapon gameplay did not: ADS/scope, reload, and shooting were non-functional after picking up a rifle.

## Why It Happened

The scene had a **partial runtime chain**:

- working:
  - pickup resolver and inventory pickup path
- broken/missing:
  - `PlayerWeaponController` runtime combat loop wiring
  - `WeaponRegistry` definitions mapping item ID -> `WeaponDefinition`
  - camera/muzzle/default references needed by weapon + look controllers

This produced misleading behavior where pickups looked healthy while equip/combat logic never activated.

## Root Cause

No contract gate existed to enforce complete combat wiring for world scenes. We relied on manual scene edits and incremental fixes, which allowed drift.

## Resolution

Implemented deterministic scene wiring for MainTown and re-verified via MCP read-back plus targeted tests.

Key artifacts:
- `Assets/_Project/World/Editor/MainTownCombatWiring.cs`
- menu command: `Reloader/World/Wire MainTown Combat Setup`

## Preventive Actions

1. Use scene contracts as required acceptance criteria for any world scene changes.
2. Run MCP read-back verification after scene mutations.
3. Run targeted EditMode/PlayMode tests before claiming completion.
4. Keep scene setup deterministic through editor wiring tools and templates.

## Acceptance Checklist (Minimum)

- `WeaponRegistry` has required definitions (not empty).
- `PlayerWeaponController` exists and core refs are assigned.
- `PlayerCameraDefaults` + `PlayerLookController` references are assigned.
- `CameraPivot/CameraLookTarget` and `CameraPivot/WeaponMuzzle` exist.
- pickup -> equip -> ADS/reload/fire smoke passes.

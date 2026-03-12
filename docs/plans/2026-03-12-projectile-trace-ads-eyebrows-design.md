# Projectile Trace, Scoped ADS Resync, and NPC Eyebrows Design

## Goal

Fix three live-facing issues without adding more one-off patches:

- dev traces must render the exact projectile path, including long-range arc and miss/despawn termination
- scoped weapons must ADS through the live optic immediately after restore/equip/travel, not only after a manual detach/reattach
- generated STYLE NPCs must render eyebrows through a real appearance field instead of overloading another slot

## Scope

- Replace endpoint-based dev traces with projectile-simulation-backed path rendering.
- Repair the authoritative sync between `WeaponRuntimeState` and the live equipped weapon view / ADS runtime.
- Remove duplicate loose-scope behavior from `give test`; the starter rifle should seed its authored default optic state.
- Add eyebrow data to the civilian appearance model, persistence contract, generator, and runtime applicator so MainTown and review/demo flows can activate `brous*` meshes.

## Architecture

### Exact Projectile Trace

The exact projectile path exists only inside `WeaponProjectile.Update()`, where each simulation step computes a concrete `start -> next` world-space segment and the terminal hit or lifetime-expiry position. Reconstructing traces in devtools from `OnWeaponFired` and `OnProjectileHit` is fundamentally lossy and is the reason long-range traces currently flatten into a short straight segment.

The fix is to add a narrow projectile-path observer seam in the weapons/ballistics layer. `WeaponProjectile` should publish each simulated segment and an explicit terminal event for both hit and non-hit expiry. `DevTraceRuntime` should aggregate one path per projectile id and render that path as a polyline. TTL remains a display concern only: it controls how long the completed trace remains visible after termination.

This keeps exact path ownership in the projectile simulator, avoids polluting the broad runtime event hub with high-frequency debug traffic, and removes the current fake 120 m fallback segment.

### Scoped ADS Runtime Resync

The scope bug is broader than `give test`: travel restore and starter-kit seeding both feed attachment state into `PlayerWeaponController`, but there is no single idempotent seam that guarantees the currently equipped view and ADS bridge have been rebuilt from the latest runtime state. That allows a split-brain state where `WeaponRuntimeState` says the scope is attached, while the live `AttachmentManager` still exposes the iron-sight anchor until a manual swap path forces a rebuild.

The fix is to centralize runtime-to-view projection inside `PlayerWeaponController`. Any path that restores, seeds, or equips attachment state should call one authoritative sync method that:

1. ensures the equipped view exists
2. ensures the attachment manager and scoped ADS bridges are wired
3. reapplies the saved attachment ids into the live view runtime
4. leaves the active sight anchor and scoped ADS bridge in sync before the first RMB ADS

`give test` should use that same authoritative path. It should grant only the rifle and ammo, then seed the rifle from its authored default optic state instead of granting a duplicate loose scope item.

### Eyebrows as Appearance Data

The STYLE `brous*` meshes are currently overloaded into `OutfitBottomId` in the NPC appearance pipeline. That is why the prefab can contain eyebrow meshes while runtime civilians still render without eyebrows: there is no actual eyebrow field in `CivilianPopulationRecord`, the generator, or the applicator. The current data model is simply missing that lane.

The fix is to add a dedicated eyebrow id through the civilian appearance stack:

- `CivilianAppearanceLibrary` should expose eyebrow ids
- `CivilianAppearanceGenerator` and seeded appearance creation should pick one
- `CivilianPopulationRecord` and save-module validation should persist it
- `MainTownNpcAppearanceApplicator` should map eyebrow ids to the STYLE `brous*` child names independently of beard, hair, and clothing

This keeps facial features and clothing separated and makes eyebrows available everywhere the same record is used, including MainTown and review/demo scenes.

## Testing

### Projectile Trace

- add projectile-level tests that prove exact curved path reporting and terminal reporting on lifetime expiry
- update dev trace runtime tests so they assert rendered polylines with more than two points and no fabricated fallback segment
- verify concurrent projectiles with the same item id do not collapse into one trace

### Scoped ADS Resync

- add `PlayerWeaponController` coverage proving seeded/restored scoped state produces a live active optic without going through manual swap
- add travel coverage proving a scoped Kar98k still ADSes through the scope after arrival in `IndoorRangeInstance`
- update `DevGiveItemCommand` tests so `give test` grants rifle + ammo only and still equips a scoped, loaded rifle

### NPC Eyebrows

- add applicator tests that eyebrow ids activate the correct `brous*` child on male and female STYLE roots
- add generator / persistence tests proving eyebrow ids are generated, copied, and serialized through the civilian population module


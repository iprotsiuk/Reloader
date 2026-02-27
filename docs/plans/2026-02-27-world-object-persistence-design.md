# World Object Persistence Design (Unified Store)

## Goal
Build a modular, policy-driven world persistence architecture where every scene chooses its own behavior, while gameplay objects keep stable identity and exact state restore.

## Non-Negotiable Product Behavior
1. World should feel alive and continuous, not regenerated.
2. Scene behavior is per-scene configurable, not hardcoded by scene type.
3. `Persistent` scenes keep object state indefinitely.
4. `DailyReset` scenes keep same-day changes, then cleanup at day boundary.
5. Daily cleanup sends cleaned items to reclaim/lost-and-found (not hard delete).
6. Future cleaners can selectively clean by rules (for example trash only).

## Policy Model (Per Scene)
Each scene path maps to a persistence policy asset.

`WorldScenePersistencePolicy`
- `scenePath` (canonical key, for example `Assets/_Project/World/Scenes/MainTown.unity`)
- `mode` (`Persistent`, `DailyReset`, later extensible)
- `retentionDays` (for modes that need TTL semantics)
- `cleanupRuleSetId` (future selective cleaner behavior)
- `trackConsumed`
- `trackDestroyed`
- `trackTransforms`
- `trackSpawnedObjects`

Important: both `Persistent` and `DailyReset` are generic scene policies. Any scene can use either mode.

## Unified Runtime State Store
Create one canonical runtime service that tracks mutable world-object state:

`WorldObjectStateService`
- Key: `scenePath + objectId`
- State fields:
  - `consumed` (pickup taken)
  - `destroyed`
  - `hasTransformOverride`
  - `position`
  - `rotation`
  - `lastUpdatedDay`
  - `itemInstanceId` (optional link when object represents an item)

This replaces ad-hoc heuristics (for example hiding pickups by owned `itemId`).

## Stable Identity
Add stable identity component for mutable world objects:

`WorldObjectIdentity`
- `objectId` (GUID-like, serialized, stable)
- `objectKind` (pickup, prop, dropped-item, etc.)
- optional tags (future cleanup filters)

All pickups and future movable/destructible world objects use this identity.

## Save Integration
Add a new save module in save pipeline:

`WorldObjectState` module
- Module key: `WorldObjectState`
- Module version: `1`
- Stored in `SaveEnvelope.Modules` like other modules

Payload shape (logical):
- `sceneStates[]`
  - `scenePath`
  - `objectStates[]`
    - `objectId`
    - state flags (`consumed`, `destroyed`, `hasTransformOverride`)
    - `position?`, `rotation?`
    - `itemInstanceId?`
    - `lastUpdatedDay`
- `reclaimStorage[]` (items moved by cleanup)

## Runtime Flows
### 1. Scene Load Apply
1. Scene loads.
2. Load policy by scene path.
3. Find `WorldObjectIdentity` instances in scene.
4. Apply `WorldObjectStateService` entries:
- consumed/destroyed -> disable/remove runtime presence
- transform override -> apply position/rotation

### 2. Pickup Success
1. Inventory confirms pickup success.
2. Mark object consumed in `WorldObjectStateService` via `scenePath + objectId`.
3. Call target visual/runtime deactivation.

### 3. Day Boundary Cleanup (DailyReset)
1. On day advance, evaluate each loaded/known scene policy.
2. For `DailyReset` scenes, cleanup changed objects by policy.
3. Move recoverable items into reclaim storage.
4. Prune/reset scene object state entries according to policy.

### 4. Persistent Scenes
No automatic cleanup. State remains indefinitely until explicitly changed.

## Reclaim / Lost-and-Found
Add reclaim service for cleaned items:

`ReclaimStorageService`
- Receives item instances removed by cleanup.
- Preserves identity and runtime payload.
- Exposes retrieval flow for player (UI/interaction can come in later phase).

## Extensibility: Selective Cleaners
`cleanupRuleSetId` allows future rule-driven cleanup without reworking core persistence.
Examples:
- clean `trash` only
- keep player-owned tagged objects
- clean ammo casings but keep weapons

## Assembly Boundaries
- Save module stays Core-domain and payload-only.
- Scene scanning and object apply live in persistence runtime bridge layer under `Core/Scripts/Persistence/**`.
- Avoid introducing direct cross-assembly references from Core save modules to scene-specific components.

## Migration and Compatibility
Current save runtime requires all registered modules. Adding one new required module requires schema migration.

Plan:
1. Bump schema version from `1` to `2`.
2. Add migration `v1 -> v2` that injects default empty `WorldObjectState` module block.
3. Register new module in bootstrap after migration support is in place.
4. Keep restore defensive for missing/empty optional fields in payload internals.

## Testing Strategy
1. EditMode
- module capture/restore roundtrip
- migration `v1 -> v2`
- malformed payload safety

2. PlayMode
- pickup persists consumed state across scene travel
- `DailyReset` scene keeps same-day state then resets next day with reclaim transfer
- `Persistent` scene keeps moved/dropped object state across day changes
- no owned-item heuristic side effects

3. Contract/Validation
- scene objects intended for mutation have `WorldObjectIdentity`
- policies exist for all runtime scenes (or have deterministic fallback policy)

## Rollout Phases
1. Foundation
- identity + unified state store + save module + scene apply
2. Pickup migration
- replace current pickup-only behavior and remove ownership workaround
3. Day-boundary cleanup + reclaim
- activate `DailyReset` semantics
4. Future systems
- moved props/destructibles integration
- cleaner rule sets

## Out of Scope for This Phase
1. Full UI for reclaim retrieval.
2. Full NPC cleaner simulation.
3. Advanced conflict resolution for multiple external systems mutating same object in same frame.

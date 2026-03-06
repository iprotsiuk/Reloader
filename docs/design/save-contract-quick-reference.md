# Save Contract Quick Reference

> **Purpose:** Fast routing doc for agents touching save/load contracts without loading every domain design document.
> **Use with:** [save-and-progression.md](save-and-progression.md) for full schema details and progression context.

## Canonical Terms [v0.1]

- `SaveCoordinator` = current save orchestration service.
- `SaveEnvelope` = versioned top-level container.
- `ModuleSaveBlock` = per-domain payload block (`moduleVersion` + `payloadJson`).

## Current Implemented Save Slice [v0.1]

Current repository runtime requires these registered module blocks:

- `CoreWorld` (`dayCount`, `timeOfDay`)
- `Inventory` (`carriedItemIds`, `beltSlotItemIds`, `backpackItemIds`, `backpackCapacity`, `selectedBeltIndex`)
- `Weapons` (`itemId`, `chamberLoaded`, `magCount`, `reserveCount`, `chamberRound`, `magazineRounds[]`)
- `WorldObjectState` (`sceneObjectStates[]`, `reclaimEntries[]`; scene-path + object-id keyed world object records)
- `ContainerStorage` (`containers[]` keyed by `containerId`)
- `PlayerDevice` (`selectedTarget`, `activeGroupShots[]`, `savedGroups[]`, `notesText`, `installedHooks[]`)
- `WorkbenchLoadout` (`workbenches[]` with nested `slotNodes[]`)
- `ContractState` (`contractId`, `targetId`, `distanceBand`, `payout`, `generatedContractIds[]`, `completedContractIds[]`)
- `PoliceHeatState` (`level`, `lastCrimeType`, `searchTimeRemainingSeconds`, `hasLineOfSightToPlayer`)

Weapons ammo snapshot fields are: `ammoSource`, `muzzleVelocityFps`, `velocityStdDevFps`, `projectileMassGrains`, `ballisticCoefficientG1`, `dispersionMoa`.
In-flight projectiles are intentionally excluded from v0.1 save scope.

Runtime schema note: baseline schema is `v6`, with migrations adding default blocks in sequence for `WorldObjectState`, `ContainerStorage`, `PlayerDevice`, `WorkbenchLoadout`, `ContractState`, and `PoliceHeatState` when older saves are loaded.

The broader `SaveData` tree in `save-and-progression.md` is the target schema contract. Blocks become required only after module registration + migration support land in runtime.

Current limitation:
- `ContractState` and `PoliceHeatState` now exist as registered schema blocks and module contracts.
- Live runtime capture/restore for those systems still needs dedicated save bridges; default save capture does not yet pull active gameplay state into those blocks automatically.

### Feature Flag / Module Coherence [v0.1]

- `SaveFeatureFlags` may only enable systems that have registered domain modules in `SaveBootstrapper`.
- If a feature flag exists without module registration support, it must remain disabled.
- Enabling a new flag requires, in the same change: module registration, payload contract, and migration notes.

## Load/Restore Guarantees [v0.1]

- Unknown module keys are ignored safely.
- Missing required registered module blocks fail before any restore.
- Corrupted module payload JSON fails before any restore.
- Baseline deterministic order: `CoreWorld`, `Inventory`, `Weapons`, `WorldObjectState`, `ContainerStorage`, `PlayerDevice`, `WorkbenchLoadout`, `ContractState`, `PoliceHeatState`.

## Unified World-Object Policy Contract [v0.2]

- Runtime state key: `scenePath + objectId`.
- Policy mode:
  - `Persistent`: day boundary does not clear records.
  - `DailyReset`: same-day state is retained; day increment clears old records and moves reclaimable entries into reclaim storage.
- Pickup flows capture consumption via the unified state store; travel must not use ownership-based hide workarounds.

## Scene Policy Authoring + Validation Workflow [v0.2]

- Policy assets: `WorldScenePersistencePolicyAsset` under `Assets/_Project/World/Data/SceneContracts`.
- Validation gate: `Reloader/World/Validate Scene Persistence Policies` (editor menu).
- Validator guarantees:
  - every required world scene has a policy
  - no duplicate `scenePath` policies

## Data Ownership Rules (Target Canonical Model) [v0.1]

- `ItemRegistry` holds full item-instance payloads (single source of truth).
- `ItemLocation` is canonical ownership/location mapping.
- System blocks (`InventoryState`, `WorldItemState`, `WeaponState`, `VehicleState`) store IDs + slot/location metadata only.
- Do not duplicate full item payloads outside `ItemRegistry`.

## Save Size Budget Policy [v0.1]

Uncompressed `SaveEnvelope` JSON thresholds:

- `500 KB` soft warning threshold.
- `1 MB` hard failure threshold.
- Any single module growing more than `10%` versus baseline must be explicitly noted in design/plan updates.

## Compactness Rules [v0.1]

- Persist authoritative state only; do not persist derived/runtime-recomputable fields.
- Keep references ID-based across modules.
- Quantize high-volume floats only when gameplay-safe.
- Keep module payloads JSON-first for migration/debuggability.
- If space pressure rises, prefer file-level compression at repository I/O boundary over per-module format changes.

## When To Load Full Docs [v0.1]

Load [save-and-progression.md](save-and-progression.md) when changing:

- `SaveEnvelope` schema/versioning semantics
- migration chain behavior
- module restore ordering/validation contracts
- exact-restore guarantees

Load domain docs only for the modules you are editing.

## Verification Sweep (Expected Suites) [v0.2]

- Core save EditMode: `WorldObjectStateSaveModuleTests` (+ save coordinator/module registration invariants).
- Core persistence PlayMode: `WorldObjectPersistenceRuntimeBridgePlayModeTests`.
- World travel PlayMode: `RoundTripTravelPlayModeTests`.
- Pickup/identity coverage: `PlayerInventoryControllerPlayModeTests`, `PickupTargetWorldIdentityEditModeTests`, `PickupTargetWorldIdentityPlayModeTests`.

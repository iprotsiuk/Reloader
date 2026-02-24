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

The broader `SaveData` tree in `save-and-progression.md` is the target schema contract. Blocks become required only after module registration + migration support land in runtime.

## Load/Restore Guarantees [v0.1]

- Unknown module keys are ignored safely.
- Missing required registered module blocks fail before any restore.
- Corrupted module payload JSON fails before any restore.
- Baseline deterministic order: `CoreWorld` then `Inventory`, then explicit registered order.

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

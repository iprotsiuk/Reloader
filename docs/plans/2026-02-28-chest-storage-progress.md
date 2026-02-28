# 2026-02-28 Chest Storage Progress

## Scope Completed
- Added world storage chest foundation with runtime registry/session and transfer engine.
- Added `WorldStorageContainer` in MainTown and renamed scene object to `StorageChest`.
- Added chest UI (`ChestInventory`) with side-by-side container/player slots and drag/drop transfer flow.
- Added chest content persistence module (`ContainerStorage`) to save pipeline with schema migration v2 -> v3.
- Added workbench container-link plumbing (`WorkbenchContainerLink`, `StorageWorkbenchLinkQuery`) without enabling gameplay wiring.

## UX/Input Behaviors Implemented
- `ESC` closes open interactive UIs (storage, trade, workbench, tab inventory).
- `TAB` behavior:
  - closes currently open interactive UI(s) if any are open,
  - opens Tab inventory only when no interactive UI is open.
- Cursor unlocks while storage UI is open.

## Bug Fixes Implemented During Integration
- Fixed chest opening workbench UI (removed chest -> workbench visibility coupling).
- Fixed interaction conflicts around shared pickup/menu inputs between storage, vendor, and workbench.
- Improved storage interaction tolerance by changing chest resolver from thin ray to sphere-cast (`_interactionRadius`).
- Fixed post-travel interaction regression by resetting travel UI state, including `StorageUiSession`, on scene travel completion.
- Hardened resolver/controller reference recovery after travel-related object lifecycle changes.
- Added chest icon rendering via `ItemIconCatalogProvider` (previously labels-only).

## Tests and Validation
- Added/updated focused tests:
  - `WorldTravelCoordinatorUiResetEditModeTests`
  - `ChestInventoryUiToolkitPlayModeTests`
  - save/storage and workbench-link tests created during implementation.
- Touched scripts validated with MCP `validate_script` (no diagnostics on validated files).

## Known Notes
- Storage runtime uses static bridge/session state at runtime; in-editor playmode behavior depends on domain reload settings.
- Workbench storage linkage remains intentionally unwired at gameplay level for now.

## Next Recommended Follow-ups
- Add explicit runtime bootstrap reset policy for storage static bridge/session when entering play if desired.
- Add end-to-end playmode coverage for town -> range -> town storage interaction and UI open/close flow.
- Implement gameplay wiring for workbench linked containers when design is finalized.

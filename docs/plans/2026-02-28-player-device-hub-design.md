# Player Device Hub Design

**Date:** 2026-02-28
**Status:** Approved

## Goals

- Add a permanent player device available in TAB menu from game start.
- Start with notes capability at T0 and include rangefinder flow for dev/testing in this slice.
- Support attachment install/uninstall plumbing (not permanent):
  - install consumes selected inventory item into device attachment state,
  - uninstall returns item to belt first, backpack fallback.
- Support target binding from device UI (`Choose Target`) with click-to-bind flow.
- Track active shot group on selected target and show:
  - shots count,
  - group spread,
  - true geometric MOA.
- Provide `Save Group` and `Clear Group` actions.
- `Clear Group` must also clear persistent hit markers on the bound target.

## Non-Goals (This Slice)

- No final UX polish for attachment lifecycle flows.
- No advanced notes system (rich formatting, search, categories).
- No full unlock/economy restrictions for all future attachments.
- No hotkey bindings now (only architecture seam for future keybind integration).

## Architecture

### Module Layout

- Introduce `PlayerDevice` as a dedicated module (runtime/controller/state/contracts) to avoid overloading existing TAB inventory code.
- Keep `TabInventory` thin: render device section, route user intents to device controller.

### Device Runtime Ownership

- `PlayerDeviceController` is the single owner of device runtime state.
- `TabInventoryController` does not hold business logic for attachments, target selection, or group computation.

### Attachment Model

- Device is always present.
- Attachments are represented by item-backed installable capabilities.
- LMB install flow:
  - when a valid attachment item is selected, install command consumes it into device state.
- Uninstall plumbing:
  - command removes installed attachment and returns corresponding item to inventory (`belt -> backpack` fallback order).

### Target Metrics Contract

- Targets expose authoritative metrics via `IRangeTargetMetrics`:
  - stable target id,
  - display name,
  - authoritative distance value at impact/select time.
- Existing range targets (for example dummy target) get a component implementing this contract.

### Group Tracking

- Track group as shot impacts tied to currently selected target id.
- Store per-shot data at impact time:
  - local target-plane coordinates,
  - authoritative distance at impact,
  - optional source metadata (weapon/ammo snapshot ids).
- Show derived metrics in device UI (count, spread, MOA).

### Future Hotkey Seam

- Expose command-style API from `PlayerDeviceController`:
  - `OpenDeviceSection`
  - `BeginTargetSelection`
  - `SaveCurrentGroup`
  - `ClearCurrentGroup`
  - `TryInstallSelectedAttachment`
  - `TryUninstallAttachment`
- Current TAB UI uses this API; future keybind system can call same commands directly.

## Interaction Flows

### Device Access

- Player opens TAB menu.
- Selects `Device` tab/section.
- Notes panel is available by default.

### Attachment Install (Dev Flow)

- Player selects attachment item in belt/backpack workflow.
- On LMB install trigger, controller validates item as installable.
- Item is consumed from inventory and attachment is marked installed.

### Target Selection

- Player clicks `Choose Target` in device UI.
- TAB closes and controller enters pending target selection mode.
- Next click raycast on valid target binds target to device.
- Show transient confirmation text (`Target selected`) via existing hint/toast-capable path.

### Group Session

- On each shot impact:
  - if hit belongs to selected target, append shot sample.
  - recompute spread + MOA.
- Player may:
  - `Save Group` to session log,
  - `Clear Group` to reset active session and clear target markers.

## MOA Calculation Contract

### Requirement

- Use true geometric calculation (not 1" @ 100 yd approximation).
- Support moving targets correctly.

### Algorithm (Angular Space)

For each shot impact i:
- Compute target-local planar coordinates `(x_i, y_i)` at impact time.
- Capture `d_i` = authoritative distance at impact.
- Compute shot angular coordinates:
  - `thetaX_i = atan(x_i / d_i)`
  - `thetaY_i = atan(y_i / d_i)`

Group angular spread:
- For every shot pair `(i, j)`:
  - `delta_ij = sqrt((thetaX_j - thetaX_i)^2 + (thetaY_j - thetaY_i)^2)`
- Group angle = `max(delta_ij)` (angular extreme spread).

Convert to MOA:
- `MOA = groupAngleRadians * (180 / pi) * 60`

### Display/Validation Rules

- `shotCount < 2` => MOA unavailable.
- Invalid/non-positive distance shot samples are ignored or flagged (implementation chooses deterministic guard).
- UI rounds for display only; internal computation remains full precision.

## Persistence and Logging

- Keep active session in runtime memory for this slice.
- Save-group entries stored in device runtime log structure for current run.
- Future save-module integration can persist notes/log history.

## Testing Strategy

- EditMode:
  - attachment install/uninstall state + inventory fallback behavior.
  - target binding state transitions.
  - MOA angular-space math correctness (static and moving distance cases).
- PlayMode:
  - choose-target flow from TAB to click bind.
  - confirmation message appears then clears.
  - shot logging only for selected target.
  - clear resets calculations and removes impact markers from bound target.

## Future Extensions

- Attachment registry + unlock-gating data assets.
- Additional attachment handlers (binos, kestrel, weather tools).
- Hotkey commands wired to device controller API.
- Persistent notes/DOPE/session logs through save pipeline.
- Optional world displays that subscribe to device session state.

# Dev Console Starter Kit, Trace TTL, and Enter Autocomplete Design

## Goal

Tighten the developer console workflow by making `Enter` accept highlighted autocomplete without executing, adding a `give test` starter-kit shortcut that fully equips a scoped Kar98k with ammo, and replacing the old persistent-trace toggle with a single `trace <seconds>` TTL command.

## Scope

- Console autocomplete acceptance:
  - `Enter` and `NumpadEnter` accept the highlighted suggestion exactly like `Tab`.
  - Accepting a suggestion must not execute the command on that same keypress.
  - After applying a suggestion, the caret should move to end-of-line.
- Starter kit:
  - `give test` grants `weapon-kar98k`, `att-kar98k-scope-remote-a`, and `ammo-308 x500`.
  - It auto-selects/equips the rifle, applies the scope attachment to runtime weapon state, and seeds the weapon as loaded.
- Trace command:
  - Replace `traces persistent ...` with `trace <seconds>`.
  - `trace 0` disables traces and clears visible segments.
  - `trace -1` enables permanent traces.
  - `trace N` enables traces for `N` seconds, where `N > 0`.

## Architecture

### Console Acceptance Flow

Keep the behavior inside `DevConsoleController` rather than pushing it into the binder. The controller already owns suggestion refresh, autocomplete consumption, and command submission, so it is the correct place to decide whether a submit keypress should be treated as autocomplete acceptance or execution. Caret placement should be updated immediately after the selected suggestion is applied.

### Starter Kit Wiring

Build `give test` inside `DevGiveItemCommand` instead of adding a separate command. The command context should expose `PlayerWeaponController` so the give path can reuse the existing inventory + weapon runtime contracts. The command should:

1. Grant the rifle, scope, and ammo definitions through the existing inventory controller.
2. Ensure the rifle is selected on the belt so normal weapon equip flow picks it up.
3. Apply scope attachment state through `PlayerWeaponController.ApplyRuntimeAttachments`.
4. Apply loaded ammo counts through `PlayerWeaponController.ApplyRuntimeState`.

This keeps the logic on top of approved systems rather than inventing a dev-only equip mechanism.

### Trace TTL Model

Move dev trace state from a bool to a float TTL value on `DevToolsState`. `DevTraceRuntime` should interpret TTL directly:

- `<= 0` with exact `0`: disabled, clear pending and visible traces.
- `-1`: enabled permanently, no auto-hide timeout for visible segments.
- `> 0`: enabled, visible segments expire after the configured lifetime.

`DevTracesCommand` becomes a single numeric command parser and `DevCommandCatalog` should advertise the singular `trace` command.

## Testing

- `DevConsoleScreenPlayModeTests`:
  - `Enter` accepts a highlighted suggestion without executing.
  - Caret moves to end-of-line after acceptance.
- `DevGiveItemCommandPlayModeTests`:
  - `give test` grants the expected items.
  - The Kar98k becomes the equipped weapon.
  - The scope attachment is present in runtime state.
  - The weapon is loaded and reserve ammo reflects the starter kit.
- `DevTraceRuntimePlayModeTests` and nearby edit/runtime tests:
  - `trace 0` disables and clears.
  - `trace -1` persists segments.
  - `trace 15` sets lifetime-driven expiration.
  - Command catalog/runtime tests recognize `trace` instead of `traces`.

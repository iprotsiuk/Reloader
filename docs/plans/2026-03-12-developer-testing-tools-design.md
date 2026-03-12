# Developer Testing Tools Design

**Date:** 2026-03-12
**Status:** Approved

## Goals

- Add a runtime developer console for gameplay testing during Play Mode.
- Support autocomplete and autosuggest while typing commands.
- Allow item grants by either item definition id or display name.
- Expose the same command backend through a lightweight Unity editor surface.
- Keep the first gameplay command slice narrow:
  - `noclip on|off|toggle`
  - `noclip speed <value>`
  - `give item <item-id-or-name> [quantity]`
  - `traces persistent on|off`
  - `spawn npc <spawn-id>`

## Non-Goals (First Slice)

- No production-ready cheat system for shipping release builds.
- No broad command coverage across every domain system.
- No scrolling terminal emulator or large log console UI.
- No save persistence for developer toggle state.
- No complex argument quoting, piping, or shell syntax.

## Recommended Architecture

### Layered Surface Model

Use one shared runtime command backend with two frontends:

- Runtime console overlay for fast Play Mode and standalone testing.
- Editor window for quick buttons and direct command execution from Unity.

The runtime overlay is the primary workflow. The editor window is a secondary convenience layer that forwards into the same backend.

### Runtime Ownership

Introduce a dedicated `DevTools` feature module under `Reloader/Assets/_Project/DevTools/`.

Core runtime pieces:

- `DevToolsRuntime`
  - owns enablement, open state, command history, and persistent debug toggles
- `DevCommandCatalog`
  - registers commands, aliases, help text, argument schemas, and suggestion providers
- `DevCommandContext`
  - resolves runtime dependencies such as player movement, inventory, weapon events, and NPC spawn services
- `DevToolsState`
  - stores current toggles such as noclip enabled, noclip speed, and persistent traces enabled

This keeps command execution testable without depending on the UI layer.

### UI Integration

The runtime console should integrate into the existing UI Toolkit runtime bridge rather than creating a separate canvas stack.

Required integration points:

- new `DevConsole` screen id in `UiRuntimeCompositionIds`
- new UXML + USS for the console overlay
- new controller + view binder pair bound by `UiToolkitScreenRuntimeBridge`
- new UI visibility signal so gameplay input and cursor lock treat the console like other menus

### Access Control

- `Unity Editor`: always available
- `Development build`: available only after explicit unlock
- `Release build`: disabled by policy

Recommended unlock policy for development builds:

- compile the runtime module in
- keep the console hidden unless enabled via launch argument such as `--devtools`

That allows external playtesters to use the tools in standalone development builds without exposing the feature by default.

## Console UX

### Command Palette Behavior

The runtime surface should behave like a command palette, not a fake terminal.

- single focused input row
- live suggestion list under the input
- keyboard-first navigation
- compact recent-result feedback area
- compact active-toggle strip for `NOCLIP` and `TRACES`

### Autocomplete Contract

Suggestions must be token-aware.

Examples:

- at token 1: suggest command names and aliases
- after `give item`: suggest item definitions
- after `traces persistent`: suggest `on`, `off`, `toggle`
- after `spawn npc`: suggest configured spawnable NPC ids

Suggestion rows should display both the stable token and the friendly label:

```text
ammo-308        | .308 Winchester FMJ
npc.police      | Police Officer
```

Acceptance behavior:

- `Up` / `Down`: move highlighted suggestion
- `Tab`: accept highlighted suggestion
- `Enter`: execute current command
- `Esc`: close console

### Item Lookup Source of Truth

Item suggestions and resolution should use authored `ItemDefinition` entries already exposed by `PlayerInventoryController.GetItemDefinitionRegistrySnapshot()`.

Matching rules:

1. exact `definitionId`
2. `definitionId` prefix
3. `displayName` prefix
4. case-insensitive contains fallback

Resolved execution should always use the stable `definitionId`.

## First-Slice Command Set

### Movement

- `noclip on`
- `noclip off`
- `noclip toggle`
- `noclip speed 12`

`noclip` should disable collision-constrained movement and gravity while preserving standard look + move input. `noclip speed` should affect noclip locomotion only and must not overwrite authored walk/sprint tuning.

### Inventory

- `give item ammo-308`
- `give item ".308 Winchester FMJ" 50`

The command should resolve the target item definition, then route the grant through `PlayerInventoryController` / `PlayerInventoryRuntime` semantics rather than mutating UI state directly.

### Traces

- `traces persistent on`
- `traces persistent off`

Persistent traces should be runtime-visible in Game view and standalone builds. `Debug.DrawLine` is not sufficient. Use a visible runtime representation such as pooled `LineRenderer` segments or an equivalent lightweight world-space trace visualizer.

### NPC Spawning

- `spawn npc npc.police`
- `spawn npc npc.front-desk-clerk`

The first slice should not try to spawn arbitrary authored NPC assets by filesystem discovery. Instead, use a small runtime spawn catalog asset that explicitly lists spawnable prefabs and stable spawn ids for the console.

Default placement:

- spawn a short distance in front of the player or at the crosshair hit point when valid

## Editor Surface

The editor surface should stay small and operationally useful:

- command text field
- execute button
- quick toggle buttons for `noclip` and `traces`
- numeric field for noclip speed
- optional quick-pick buttons for a few spawn ids

The editor window must call the same command backend used by the runtime console.

## Testing Strategy

### EditMode

- command parsing and tokenization
- autocomplete ranking and selection behavior
- item resolution by id and display name
- command catalog metadata coverage
- NPC spawn catalog lookup

### PlayMode

- opening the console blocks gameplay input and unlocks cursor state
- closing the console restores gameplay input
- `noclip on` bypasses gravity/collision-driven locomotion
- `noclip speed` changes noclip motion only
- `give item` grants the resolved authored item definition
- `traces persistent on` produces visible runtime trace objects after weapon fire
- `spawn npc` instantiates a configured NPC prefab near the player

## Initial File Touchpoints

Expected integration points for implementation:

- `Reloader/Assets/_Project/Core/Scripts/Runtime/IUiStateEvents.cs`
- `Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs`
- `Reloader/Assets/_Project/Player/Scripts/IPlayerInputSource.cs`
- `Reloader/Assets/_Project/Player/Scripts/PlayerInputReader.cs`
- `Reloader/Assets/_Project/Player/Scripts/PlayerCursorLockController.cs`
- `Reloader/Assets/_Project/Player/Scripts/PlayerMover.cs`
- `Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs`
- `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiRuntimeCompositionIds.cs`
- `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitRuntimeInstaller.cs`
- `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs`
- new `Reloader/Assets/_Project/DevTools/**` runtime/editor/test assets

## Acceptance Summary

The first approved slice is successful when:

- the console opens in editor Play Mode and development builds
- the console offers autocomplete while typing
- items can be granted by id or display name
- noclip and noclip speed work without corrupting normal player movement tuning
- persistent traces can be toggled on and remain visible in runtime
- configured NPC prefabs can be spawned from a stable runtime catalog
- the editor window can drive the same commands without a separate execution path

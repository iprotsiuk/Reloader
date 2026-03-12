# Dev Console Input Capture Design

**Date:** 2026-03-12
**Status:** Approved

## Goal

Make the developer console reliably readable and prevent gameplay keyboard input from leaking through while the console is open and focused for typing.

## Approach

- Set an explicit high-contrast dark text color on the runtime console UI through `DevConsoleViewBinder`, because the current playmode tests construct the UI tree in code and do not load the console stylesheet.
- Mirror the same text color in `DevConsole.uss` so authored UI and runtime behavior stay aligned.
- Treat a visible developer console as an input-capture state in `PlayerInputReader` for keyboard-driven gameplay actions.

## Scope

- Prompt, command input, suggestion rows, and status text should all render with the same dark readable color.
- While the console is visible, keyboard movement and keyboard gameplay actions should not queue gameplay behavior.
- Console-specific controls such as submit, cancel, autocomplete, suggestion navigation, and the console toggle remain active.

## Non-Goals

- No refactor of the broader input architecture.
- No change to mouse-driven gameplay behavior unless existing tests show it is also leaking through this path.
- No console theme system or reusable color token framework.

## Verification

- Extend `DevConsoleScreenPlayModeTests` to assert readable console text rendering.
- Extend `PlayerControllerPlayModeTests` to assert keyboard gameplay input is suppressed while the console is visible.

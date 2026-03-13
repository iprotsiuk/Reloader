# Dev Console Backquote And Noclip Flight Design

**Date:** 2026-03-12
**Status:** Approved

## Goal

Prevent the console-opening backquote from becoming the first command character, and make noclip fly in the direction the player camera is looking with a default speed of `5x` walk speed.

## Approach

- Suppress the first opening backquote in the console UI layer so the toggle key opens the console without seeding the command field.
- Make noclip movement view-relative by using the active camera orientation when available instead of flattening movement to the player root's XZ plane.
- Resolve noclip default speed from player walk speed when no explicit noclip speed override has been set.

## Scope

- Opening the console with `` ` `` must leave the command field empty.
- Noclip forward movement should climb or descend based on camera pitch.
- Explicit `noclip speed <value>` still overrides the default.
- Default noclip speed should be `walkSpeed * 5`.

## Non-Goals

- No new noclip keybinds or dedicated up/down fly controls.
- No wider refactor of devtools state persistence.
- No changes to normal grounded movement behavior.

## Verification

- Add a console UI test that proves the opening backquote is suppressed.
- Add noclip playmode tests for camera-relative vertical flight and the default `5x` walk-speed behavior.

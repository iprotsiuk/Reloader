---
name: unity-ui-toolkit-design
description: Designs modern Unity UI Toolkit screens for the Reloader project. Use when creating or redesigning runtime UI Toolkit screens, UXML/USS layouts, or view binders under Reloader/Assets/_Project/UI or feature-local UI Toolkit folders. Excludes legacy uGUI work.
---

# Unity UI Toolkit Design

Design and implement modern runtime UI for this project using Unity UI Toolkit only. Work inside the existing `UXML` + `USS` + C# binder architecture and keep the result aligned with the current Reloader visual language unless the user asks for a deliberate departure.

## When to Use

- Creating a new runtime UI Toolkit screen
- Redesigning an existing `UXML` or `USS` screen
- Adding or updating a binder that implements `IUiViewBinder`
- Wiring a new screen into the runtime installer and screen bridge flow
- Not appropriate for legacy `uGUI`, editor tooling UI, or docs-only tasks

## Project Pattern

Default structure:

- `Reloader/Assets/_Project/UI/Toolkit/UXML/`
- `Reloader/Assets/_Project/UI/Toolkit/USS/`
- `Reloader/Assets/_Project/UI/Scripts/Toolkit/<Feature>/`
- `Reloader/Assets/_Project/UI/Tests/EditMode/`
- `Reloader/Assets/_Project/UI/Tests/PlayMode/`

Feature-local exceptions are acceptable when an existing feature already owns its UI, but stay within the same runtime pattern:

- `VisualTreeAsset` / `UXML`
- `USS`
- binder implementing `IUiViewBinder`
- runtime composition through `UiToolkitRuntimeInstaller` and `UiToolkitScreenRuntimeBridge` when a new screen is introduced

## Workflow

1. Inspect one existing screen, its `USS`, binder, and tests before changing anything.
2. Decide the screen's purpose, information hierarchy, input modes, and success criteria.
3. Reuse the current project language first:
   - `UiKit.uss` utility classes
   - restrained dark surfaces
   - compact spacing
   - readable small-text hierarchy
   - focused blue-accent interactive states
4. Build or revise `UXML` structure first, then `USS`, then binder behavior.
5. Add or update tests for structure and runtime behavior before claiming success.

## Design Rules

- Use a clear visual hierarchy. The player should understand the primary action and current state immediately.
- Favor deliberate composition over web-style filler. UI Toolkit screens here are dense, game-facing interfaces, not marketing pages.
- Keep labels short and state-rich.
- Design for gameplay readability:
  - readable at distance
  - stable at common game resolutions
  - clear focus and selected states
  - strong disabled and unavailable states
- Preserve asymmetry or stronger layout moves only when they improve scanning and action speed.
- Reuse utility classes from `UiKit.uss` before inventing near-duplicates.

## UI Toolkit Constraints

- Do not design as if this were HTML/CSS in a browser.
- Avoid assumptions about:
  - web fonts
  - DOM scripting patterns
  - unsupported CSS features
  - heavy animation systems not already used in the project
- Prefer UI Toolkit primitives that are already common in the repo:
  - `VisualElement`
  - `Label`
  - `Button`
  - `DropdownField`
- Keep naming explicit and query-friendly:
  - element names: `feature__element`
  - classes: `feature__modifier` or shared `ui-kit__*`

## Implementation Expectations

- New screens should usually include:
  - one `UXML`
  - one `USS`
  - one binder under `Scripts/Toolkit/<Feature>/`
- If behavior changes, update or add targeted EditMode and PlayMode coverage.
- If a new screen joins runtime composition, update the relevant runtime installer or bridge path.
- Keep logic in C# binders/controllers, not in ad hoc visual tree hacks.
- Do not add `uGUI` fallback paths. This skill is UI Toolkit only.

## Verification

Before finishing:

- run `xmllint --noout` on any edited `UXML`
- run `bash .agent/skills/creating-skills/scripts/validate-skill.sh .agent/skills/unity-ui-toolkit-design` when this skill changes
- run the relevant focused Unity EditMode or PlayMode tests for the touched UI flow
- confirm naming and layout still match the existing runtime binder pattern

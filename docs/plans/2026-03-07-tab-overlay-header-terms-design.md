# TAB Overlay Header And Terms Design

## Goal

Extend the redesigned in-world `TAB` overlay with two missing pieces:

- a persistent top-right header line showing live game-world date/time and player balance
- a contracts-only right-side terms pane that surfaces payout logic, restrictions, and failure conditions

This slice should make the overlay feel more like an actual player organizer instead of a shell with empty chrome.

---

## Decision Summary

### Header format

The top-right header will render as a single compact right-aligned line:

- `<dayOfWeek> • <timeOfDay> • <balance>`

Example:

- `Monday • 18:40 • $2,450`

Rules:

- `dayOfWeek` must be a full weekday name (`Monday`, `Tuesday`, etc.)
- `timeOfDay` must use military `HH:mm` formatting
- `balance` must use full currency formatting, not abbreviated `k` notation

### Data sources

- `balance` comes from the live `EconomyController.Runtime.Money`
- `dayOfWeek` and `timeOfDay` come from a new lightweight live `CoreWorld` runtime

### Right-pane scope

For this PR, the right pane becomes a real `Contracts` terms pane only when the active TAB section is `Contracts`.

Non-Contracts sections can keep lightweight contextual copy for now.

---

## Approaches Considered

### 1. Add a lightweight live `CoreWorld` runtime now

Add a minimal always-on runtime/controller that owns:

- `dayCount`
- `timeOfDay`

Then let the TAB header format those values.

Pros:

- keeps the header honest and game-world-driven
- aligns with the existing save/schema contract that already persists `CoreWorld.dayCount` and `timeOfDay`
- gives future day/night and calendar work a real runtime seam

Cons:

- introduces a new runtime/controller in this PR

### 2. Fake the date/time in the TAB controller

Derive or hardcode temporary values only for UI.

Pros:

- faster

Cons:

- wrong architecture
- guaranteed rework once the day/night cycle lands

### 3. Show only money for now

Pros:

- cheapest implementation

Cons:

- misses the approved UI requirement

### Recommendation

Use approach `1`.

The repo already acknowledges `CoreWorld.dayCount` and `timeOfDay` as runtime-save contracts, so adding a tiny live runtime now is the cleanest path.

---

## Runtime Design

Add a minimal world-clock runtime/controller pair.

### Runtime responsibilities

The new runtime only needs to:

- store `dayCount`
- store `timeOfDay` as fractional hours in `[0, 24)`
- expose a simple snapshot/value API for read-only UI consumers

This slice does not need:

- visible sky/day-night presentation
- time progression authored into gameplay loops
- sleep/day-advance UX
- save/load bridge wiring beyond future compatibility planning

### Controller responsibilities

A lightweight scene/controller component should:

- initialize the runtime with authored default values
- expose the runtime to UI/runtime consumers
- optionally tick forward later, but this slice does not require active time progression if gameplay is not yet driving it

This keeps the header binding real without forcing full time-system scope.

---

## UI Design

### Header

The `TAB` overlay header row keeps the title on the left and adds a compact metadata line on the right.

Visual rules:

- right-aligned
- compact enough to stay unobtrusive
- `balance` can be slightly brighter or bolder than date/time, but still remains on one line

### Contracts terms pane

When `Contracts` is active, the right pane should show structured contract terms instead of placeholder copy.

Initial sections:

- base payout
- bonus conditions
- restrictions
- failure conditions
- current reward / claim state

For the current authored slice, much of this content can be static or derived from the current contract status until richer authored modifiers exist.

Examples for the first slice:

- `Base payout`
- `Restrictions: none`
- `Failure conditions: wrong target, manual cancel`
- `Reward state: active / escape search / ready to claim`

This is enough to make the right pane meaningful now without inventing a full modifier authoring system.

### Non-contract sections

When `Contracts` is not active:

- keep the right pane present
- show lightweight contextual copy or section-specific placeholder text

Do not attempt to fully redesign every tab’s right pane in this slice.

---

## Formatting Rules

### Day-of-week mapping

Use a stable deterministic mapping from `dayCount` to weekday name.

For this slice:

- `dayCount = 0` can map to `Monday`
- subsequent days wrap every `7`

This is acceptable as long as the mapping is consistent and test-covered.

### Time formatting

Render `timeOfDay` using:

- 24-hour clock
- zero-padded `HH:mm`

Examples:

- `6.5` -> `06:30`
- `18.6667` -> `18:40`

### Balance formatting

Render using full currency:

- `$500`
- `$2,450`

No abbreviated notation.

---

## Testing Strategy

Use TDD for both new behaviors.

### World time header tests

Add UI/controller/runtime coverage for:

- formatting weekday from `dayCount`
- formatting military time from fractional `timeOfDay`
- formatting balance from economy runtime
- rendering the header text on the live `TAB` document

### Contracts terms pane tests

Add UI coverage for:

- terms pane visible and populated when `Contracts` is active
- claim state reflected in the pane during `Ready to claim`
- non-Contracts sections keep the right pane but do not render contract-terms content

### Verification

Targeted verification for this slice should include:

- relevant EditMode UI tests
- relevant PlayMode TAB tests
- world/contract smoke tests if the terms pane depends on live runtime state
- `bash scripts/verify-docs-and-context.sh`
- `xmllint --noout` on `TabInventory.uxml`
- `git diff --check`

---

## Out Of Scope

This slice does not need to implement:

- full time progression gameplay
- day/night lighting changes
- sleep/day-advance actions
- a universal right-pane redesign for every TAB section
- authored contract modifier assets beyond what is needed to populate the current terms pane

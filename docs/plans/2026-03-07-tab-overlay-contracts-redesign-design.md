# TAB Overlay Contracts Redesign Design

## Goal

Redesign the in-world `TAB` overlay so it can carry denser information without feeling cramped, starting with the `Contracts` surface and a reusable three-region shell that scales to future inventory, device, journal, and portrait/intel needs.

This slice should make the existing assassination-contract loop feel readable and intentional instead of technically functional but visually congested.

---

## Decision Summary

### Recommended direction

Adopt a fixed-size immersive overlay shell with three stable regions:

- icon-first left navigation rail
- dense center workspace
- persistent right-side detail / terms pane

Within that shell:

- posted contracts render as a scrollable feed of dense message-like rows
- accepting a contract switches the center pane into a focused mission workspace
- contract completion moves to an explicit `Ready to Claim` state
- reward collection becomes a deliberate `Claim Reward` action instead of silently collapsing into empty state

### Why this direction wins

- It keeps the overlay immersive instead of replacing it with a full-screen modal scene.
- It solves the current density problem by improving zoning and scale, not by adding more screen area.
- It gives future portrait, intel, restriction, and bonus content a home without another layout rewrite.
- It supports the current one-active-contract loop while staying compatible with future multiple-posted-contract feeds.

### Rejected directions

- `Separate full-screen menu`: breaks immersion and goes against the intended in-world feel.
- `Resizable MMO panel chrome`: adds complexity and still does not guarantee good defaults.
- `Current stacked card approach with smaller fonts only`: shrinks the problem without fixing the weak layout hierarchy.

---

## Layout Shell

The overlay remains an in-world `TAB` panel with the world visible behind it.

Shell rules:

- Keep overall panel size roughly stable across desktop resolutions.
- Do not stretch the interface to fill ultrawide or 4K displays.
- Use extra screen space as margin, not as uncontrolled panel growth.

Region proportions:

- left rail: `14-16%`
- center pane: `52-58%`
- right pane: `28-32%`

Region responsibilities:

- left rail: section switching only
- center pane: primary active workspace
- right pane: contextual detail, payout logic, restrictions, bonuses

---

## Navigation Rail

The left rail becomes icon-first.

Rules:

- use icons instead of oversized text buttons
- active section receives stronger highlight treatment
- show tiny label or tooltip on hover/focus if needed
- keep width narrow and visually quiet

Initial sections:

- `Inventory`
- `Contracts`
- `Device`
- `Journal`
- `Calendar`

Icon sourcing:

- first pass should inspect repo-tracked UI icon candidates first, then optionally evaluate external source packs by pack name only before importing any selected icons into a repo-tracked folder
- if those assets do not fit, fall back to simple, readable vector-style glyphs or lightweight placeholder icons

---

## Posted Contracts State

When no contract is active, the center pane becomes a clearly scrollable posted-contract feed.

Each contract uses a fixed-height dense row:

- portrait / silhouette block on the left
- target name and `1-3` line summary in the middle
- payout right-aligned
- compact accept action on the far right

The row should feel more like a forum post or a Slack message than a large card.

### Posted contract row sketch

```text
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│ [IMG]   Viktor Hale                                                                        │
│         Grey coat, smoker, exits the cafe at dusk.                                         │
│         Confirm target, take the shot, clear the area.                       $1,500 [ACC]  │
└─────────────────────────────────────────────────────────────────────────────────────────────┘
```

Feed behavior:

- list region is explicitly scrollable
- scrollbar remains visible enough to teach overflow
- rows keep a consistent height
- selected / hovered row can drive the right pane

For the current slice, one posted contract is acceptable, but the list should still be built as a feed from day one.

---

## Active Contract State

Once accepted, the center pane stops being a feed and becomes a single mission workspace.

Header order:

1. mission status
2. payout
3. target name
4. target identity line
5. briefing and intel blocks

Mission status must be visually above the target name.

Allowed states:

- `Active`
- `Escape Search`
- `Ready to Claim`

Actions:

- before kill: `Cancel Contract`
- after search clears following a successful kill: `Claim Reward`
- no cancel after successful kill

### Active contract sketch

```text
┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ PLAYER ORGANIZER                                                                               money / time │
├──────┬────────────────────────────────────────────────────────────────────────┬──────────────────────────────┤
│      │ ACTIVE CONTRACT                                                        │ CONTRACT TERMS               │
│  ◉   │                                                                        │                              │
│  ■   │ Mission Status: ACTIVE                                 Payout: $1,500 │ Base payout                  │
│  △   │                                                                        │ $1,500                       │
│  ○   │ Viktor Hale                                                            │                              │
│  ◇   │ Grey coat, smoker, exits the cafe at dusk.                            │ Bonus conditions             │
│      │                                                                        │ +$500 Headshot               │
│      │ ┌────────────────────────────────────────────────────────────────────┐ │ +$500 Kill > 500m           │
│      │ │ Briefing                                                           │ │                              │
│      │ │ Confirm target identity before firing.                             │ │ Restrictions                │
│      │ │ Exit the area before payout is released.                           │ │ No civilian casualties      │
│      │ └────────────────────────────────────────────────────────────────────┘ │ No alarm before shot         │
│      │                                                                        │                              │
│      │ ┌────────────────────────────────────────────────────────────────────┐ │ Failure conditions          │
│      │ │ Intel                                                              │ │ Wrong target                │
│      │ │ - Last seen leaving cafe at dusk                                   │ │ Contract cancelled          │
│      │ │ - Grey coat, rooftop smoker                                        │ │                              │
│      │ │ - Public route, witnesses likely                                   │ │ Current reward              │
│      │ └────────────────────────────────────────────────────────────────────┘ │ $1,500                       │
│      │                                                                        │ Bonus earned: none          │
│      │ ┌────────────────────────────────────────────────────────────────────┐ │                              │
│      │ │ Notes / mission log / later dynamic updates                        │ │                              │
│      │ └────────────────────────────────────────────────────────────────────┘ │                              │
│      │                                                                        │                              │
│      │                                                   [ Cancel Contract ] │                              │
├──────┴────────────────────────────────────────────────────────────────────────┴──────────────────────────────┤
│ hints / prompts / context help                                                                             │
└──────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## Right Pane Role

The right pane should not be wasted on weak metadata.

It exists to show:

- base payout
- bonus conditions
- restrictions
- failure conditions
- live reward resolution

This keeps mission rules visible and gives future contract modifiers a durable home.

Portraits can still exist, but they should not crowd out terms and payout logic in the current direction.

---

## Completion Flow

Current behavior in the merged slice is technically correct but unclear:

- active contract
- kill target
- wait out search
- tab drops to empty / no-offer state

The redesign changes that to:

1. accept contract
2. complete kill objective
3. survive search
4. center pane changes to `Ready to Claim`
5. player clicks `Claim Reward`
6. interface returns to posted contract feed or `No contracts currently posted`

This makes reward collection feel intentional and readable.

---

## Density Rules

The redesign should increase information density by reducing wasted space, not by shrinking everything arbitrarily.

Rules:

- reduce base body size from current
- use only a few consistent text scales
- reduce vertical padding aggressively
- use stronger hierarchy via weight, contrast, and grouping instead of bigger text
- keep card borders and dividers subtle but explicit
- preserve readable row height and line clamp behavior

Explicitly avoid:

- giant tab labels
- oversized action buttons
- large empty gutters inside cards
- freeform stretching on wide displays

---

## Responsiveness / Resolution Contract

Primary design target:

- `1920x1080`

Scale behavior:

- keep layout proportions stable at `1080p`
- keep overall readable overlay size roughly stable on larger screens
- use additional screen area as margin or modest internal breathing room
- do not rely on mobile/web-style fluid responsiveness
- do not design only for ultrawide

---

## Validation Strategy

Validation must not rely on code review alone.

Required validation loop:

- implement shell layout incrementally
- capture in-editor screenshots at each major milestone
- compare against the approved sketch
- iterate on spacing, type scale, and proportions with actual screenshots

Recommended screenshot checkpoints:

- base shell + icon rail
- posted contract feed row density
- active contract workspace
- ready-to-claim state
- inventory tab inside the new shell once contracts are stable

---

## Scope Boundary

This slice is primarily a UI / flow redesign.

Required in this slice:

- shell redesign
- icon-first navigation rail
- contracts posted-feed mode
- active-contract workspace mode
- explicit `Cancel Contract`
- explicit `Ready to Claim` -> `Claim Reward` flow
- clearer empty-state copy

Deferred unless trivial:

- multiple active contracts
- full procedural contract feed
- advanced portrait rendering pipeline
- deep journal/calendar redesign beyond shell adoption
- optional modifier generation beyond UI-ready placeholders

---

## Process / PR Workflow

This redesign should be executed in a fresh branch with an early non-draft PR to `main`.

Required workflow:

- commit design / plan docs first
- open PR immediately
- tag `@codex`
- use Unity MCP heavily for screenshots and verification
- use parallel subagents once implementation begins
- commit frequently in narrow checkpoints
- update progress doc after each meaningful milestone

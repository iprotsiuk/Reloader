# Long-Range Sniper Scope Framework Design

## Goal

Define a production-ready long-range scope framework that:

- keeps the current camera-authoritative authored-optic contract
- treats the current Kar98k optic as a real fixed `10x` legacy scope
- supports future `5-25x` FFP scopes without re-architecting
- keeps long-range observation performant by making expensive rendering scope-only
- introduces real optic-instance zeroing with click-based `MOA` / `MRAD` behavior

This is a framework design, not a prototype shortcut. The first shipped implementation is intentionally smaller in feature count, but it must use the same runtime ownership, data model, and rendering path that future variable scopes will use.

## Roadmap Placement

This scope framework is a supporting subsystem for the assassination-contract pivot, not the top-level product milestone by itself.

Long-range contracts are intended to be the premium contract tier, so this document should be read as precision-contract infrastructure under the broader sandbox roadmap.

---

## Decision Summary

### Rendering mode

Use `RenderTexturePiP` as the authoritative runtime path for magnified scopes.

Do not pivot the main production architecture to old-style full-screen fake scopes.

Reason:

- the current `AdsPivot` + `WeaponAimAligner` + authored `SightAnchor` contract already assumes a physically mounted optic
- PiP keeps scope behavior compatible with future `FFP`, zeroing, eye relief, and per-optic authored content
- full-screen replacement is cheaper short-term, but it would create a second scope architecture and push the project toward fake-scope tuning instead of authored reusable optics

### First shipped scope family

Start with a real fixed-power legacy scope:

- `Kar98k` current optic becomes a `fixed 10x` scope
- no zoom input
- PiP path remains active
- zeroing is click-based from day one

This is the smallest production slice that still validates:

- optic-instance state
- reticle contract
- PiP scope rendering
- long-range observation rules
- future extension to variable `FFP`

### Long-range observation model

Long-range visual promotion must be scope-only.

Simulation authority is always active where gameplay requires it, but expensive long-range readability must only turn on through the magnified scope path.

### Zeroing model

Zero is a property of the optic instance, not the weapon.

The same optic keeps its elevation and windage state while being moved between rifles.

### Unit consistency

Reticle subtensions and turret clicks must share the same angular unit system.

- `MOA` scopes: `0.25 MOA` clicks, `MOA` reticle subtensions
- `MRAD` scopes: `0.1 mil` clicks, `mil` reticle subtensions

No mixed-unit shortcuts.

---

## Scope Runtime Architecture

### Existing runtime to preserve

The current implemented runtime already has the correct high-level ownership split:

- `AttachmentManager` owns optic mount lifecycle and active `SightAnchor`
- `AdsStateController` owns ADS state, visual mode state, and magnification state
- `WeaponAimAligner` owns final camera-authoritative scoped eye alignment
- `RenderTextureScopeController` owns PiP scope rendering
- `WeaponViewPoseTuningHelper` owns coarse presentation only

That architecture should remain intact.

The long-range scope framework extends it rather than replacing it.

### Runtime principle

The camera remains source of truth for eye alignment, but the scope remains the source of truth for aiming reference.

That means:

- `WeaponAimAligner` lines the optic up to the camera
- optic runtime state defines the reticle/bore relationship
- projectile flight remains independent of scope presentation

### New/expanded runtime responsibilities

#### `OpticRuntimeState` or `WeaponScopeRuntimeState`

Extend the runtime optic state surface so it owns:

- optic instance identity
- visual mode policy
- fixed vs variable power behavior
- current magnification state
- angular unit (`MOA` / `MRAD`)
- elevation click count
- windage click count
- zero distance metadata where needed
- future per-optic persisted adjustments

This state must be decoupled from scene-only presentation helpers.

#### `RenderTextureScopeController`

This becomes the real magnified-scope rendering owner, not just a PiP stub.

Responsibilities:

- own the scope camera
- own render texture allocation and assignment to `ScopeLensDisplay`
- read magnification from optic runtime state
- read reticle configuration from optic definition/runtime state
- keep `Viewmodel` excluded
- expose scoped-ADS state needed by peripheral and long-range presentation systems

It must remain compatible with fixed-power and variable-power optics.

#### `ScopeReticleController`

This owns reticle presentation rules, not ballistic simulation.

Responsibilities:

- render the reticle using the optic's authored reticle definition
- interpret subtensions in the optic's angular unit system
- handle `FFP` / `SFP` behavior
- apply zeroing offsets to reticle presentation

For the first `10x` legacy scope:

- fixed magnification
- reticle behavior is simpler
- but the controller still uses the same angular-unit and click-offset pipeline

#### `LongRangeObservationController`

Add a dedicated runtime seam for scope-only long-range visibility rules.

This system should decide when the player is entitled to long-range readability through a magnified optic.

It should not own AI simulation. It should own render-side observation policy only.

Responsibilities:

- determine whether scoped PiP is active enough to promote long-range targets
- publish current observation band based on active optic and magnification
- allow future scope-only silhouette/impostor rendering systems to subscribe

This can start small, but it should exist as a real runtime surface rather than hidden heuristics inside rendering code.

---

## Optic Data Model

### Existing `OpticDefinition`

Keep `OpticDefinition` as the authored source for:

- optic prefab
- visual mode policy
- fixed or variable zoom limits
- eye relief
- render profile
- reticle definition

### New authored fields

Extend the authored optic definition contract with explicit scope metadata:

- `scopeFamily`
  - `FixedPowerLegacy`
  - `VariablePowerFFP`
  - `VariablePowerSFP`
- `angularUnit`
  - `MOA`
  - `MRAD`
- `elevationClickStep`
  - e.g. `0.25 MOA`
  - e.g. `0.1 mil`
- `windageClickStep`
- `defaultZeroDistanceMeters`
- `supportsUserZeroing`
- `reticleFocalPlane`
  - `FFP`
  - `SFP`
- `fixedMagnification`
  - used by fixed scopes
- `minimumReadableContrastCueDistance`
- `maximumReadableContrastCueDistance`
  - optional content hint for future long-range rendering

The first implementation can use a subset at runtime, but the schema should be authored as the real production contract.

### Reticle definition contract

Reticle definitions must be unit-aware.

They should carry:

- authored sprite/material/prefab data
- focal plane mode
- subtension unit
- subtension scale data

This allows reticle math to stay unit-correct for both `MOA` and `MRAD` optics.

---

## Zeroing Model

### Non-negotiable rule

Zeroing must not move the weapon rig or camera.

It must not be implemented through:

- `WeaponViewPoseTuningHelper`
- `AdsPivot`
- `SightAnchor` authoring edits at runtime
- scope camera transform offsets

Those belong to authored presentation and alignment only.

### What zeroing actually changes

The scope does not change projectile physics.

Projectile simulation is still driven by:

- bore direction
- muzzle velocity
- drag / BC
- gravity
- later wind and atmospheric factors

Zeroing changes the optic's aiming reference relative to the bore.

In practical gameplay terms:

- the reticle's centerline is offset angularly relative to the bore
- the player's point of aim changes because the optic's reference changed
- the bullet's flight model itself does not depend on the optic

### Persistent ownership

Zero state belongs to the optic instance.

Persisted fields should be integer click counts:

- `ElevationClicks`
- `WindageClicks`

Derived angular offsets should be computed from:

- `ElevationClicks * ElevationClickStep`
- `WindageClicks * WindageClickStep`

This avoids drift and preserves correct scope semantics.

### Unit contract

#### `MOA`

- click step: usually `0.25 MOA`
- reticle subtensions: `MOA`
- holdovers and turret adjustments speak the same unit system

#### `MRAD`

- click step: usually `0.1 mil`
- reticle subtensions: `mil`

The reticle controller and zeroing controller must consume the same angular-unit abstraction.

### Weapon swap behavior

When an optic is moved between rifles:

- the optic keeps its click state
- the mounting geometry is authored on the weapon + optic content side
- the resulting effective zero may differ because a different rifle/ammo system is in use

That is correct and should not be hidden.

Future work can add:

- zero presets
- remembered rifle-specific dope
- mount repeatability loss

None of that changes the ownership model.

---

## Long-Range Rendering and Simulation

### Principle

Simulation and rendering must be decoupled.

The player may be able to hit a target that is not being rendered in full character detail. The system must never require a fully animated high-detail NPC at `2-3 km` just to keep ballistics valid.

### Observation bands

Recommended visual bands:

- `0m-200m`
  - full character readability
- `200m-600m`
  - body shape plus strong clothing blocks
- `600m-1200m`
  - silhouette plus high-contrast markers
- `1200m-2000m`
  - silhouette-first, one or two exaggerated cues at most
- `2000m-3000m`
  - shot-solving range, not reliable identity range

These are gameplay-facing contracts, not necessarily exact engine LOD thresholds. Final thresholds can be tuned by optic quality and conditions later.

### Scope-only promotion

The expensive long-range visual path should activate only while looking through a magnified PiP scope.

That means:

- base camera stays cheap
- hip fire and non-scoped aim do not promote far actors
- scope PiP camera gets the long-range observation representation

### Long-range render representations

Future long-range targets should use dedicated representations:

- normal NPC render near the player
- aggressive character LOD at medium range
- far-target silhouette/impostor representation at long range

At `1000m+`, the target should be largely silhouette-driven, with optional high-contrast cue overlays if the mission depends on them.

### Simulation authority

Even when far targets are not rendered as full characters, the following remain authoritative:

- position
- velocity
- stance / posture class
- alive/dead state
- mission identity metadata
- hittable target volumes

This supports long-range assassination gameplay without requiring full-detail character rendering everywhere.

### Mission-design implication

Mission-critical clothing reads should generally be reliable up to around `1200m`, and unreliable beyond that unless content intentionally exaggerates the cue.

At `2000m+`, mission play should mostly be:

- confirming known target position
- observing movement
- solving the shot

Not newly discovering identity from fine detail.

---

## Fixed 10x Legacy Scope First Slice

### Concrete target

The current Kar98k scope becomes the first real production scope.

Contract:

- magnification fixed at `10x`
- `RenderTexturePiP`
- older scope behavior
- no variable zoom input
- authored anchor and lens display required
- click-based zeroing supported

### Why this is the right first slice

It is simpler than a `5-25x FFP`, but still proves:

- authored PiP optic content
- optic-instance zero state
- unit-correct reticle/turret contract
- scope-only long-range observation path
- future compatibility with variable zoom

### What it intentionally does not shortcut

Even though the scope is fixed-power, it must still use:

- runtime optic state
- reticle controller
- persistent click-based zeroing
- PiP scope camera
- long-range observation policy hooks

No separate "legacy fixed scope" mini-system should be created.

---

## Future 5-25x FFP Extension

Once the `10x` fixed scope is solid, the next scope family should be:

- `5-25x`
- `FFP`
- PiP
- unit-consistent reticle and turret model

That extension should require:

- authored variable magnification ranges
- magnification input path
- `FFP` reticle scaling behavior
- possibly richer long-range observation tuning by magnification

It should not require:

- replacing the scope camera system
- replacing optic-instance state
- replacing zero persistence
- replacing reticle unit math

That is the main success criterion for the fixed `10x` foundation.

---

## Integration With Current Scopes System

### Preserve

- `AttachmentManager` authored optic mount contract
- `SightAnchor`
- `ScopeLensDisplay`
- `WeaponAimAligner`
- `WeaponViewAttachmentMounts.AdsPivot`
- `RenderTexturePiP` policy

### Tighten

- current Kar98k optic definition should be explicit fixed `10x`, not a generic future-variable placeholder
- reticle definitions should become unit-aware
- optic runtime state should own click-based zeroing
- `RenderTextureScopeController` should expose the scope state needed for long-range observation systems

### Avoid

- full-screen fake scope fallback
- scene-only zeroing hacks
- weapon pose offsets used as pseudo-zeroing
- controller-side heuristic scope behavior based on prefab names
- a separate codepath for "simple fixed scopes"

---

## Testing Strategy

### Runtime tests

Add tests that prove:

- fixed `10x` scope does not expose zoom changes
- PiP remains active for the fixed `10x` scope
- optic runtime state persists click values
- reticle unit and click unit match the authored optic contract
- zeroing changes aiming reference state without moving camera/weapon rig
- scope-only observation policy activates only during scoped PiP ADS

### Asset contract tests

Add tests that prove:

- Kar98k optic asset is authored as fixed `10x`
- PiP lens-display contract exists
- authored `SightAnchor` exists
- zeroing-compatible runtime state is wired for the optic

### Long-range framework tests

The first slice does not need final impostor rendering, but it does need contract tests that prove:

- the observation system can detect "magnified PiP scope active"
- observation-band logic is driven by scope state, not by global camera mode

---

## Risks and Guardrails

### Risk: scope rendering becomes a second camera spaghetti system

Guardrail:

- keep `RenderTextureScopeController` as the sole owner of magnified PiP rendering
- keep optic state explicit and data-driven

### Risk: zeroing leaks into viewmodel pose logic

Guardrail:

- forbid zeroing via pose helpers, camera offsets, or authored pivot changes

### Risk: full-detail far NPC rendering destroys performance

Guardrail:

- make long-range readability scope-only
- decouple far-target simulation from render detail

### Risk: fixed `10x` implementation hardcodes itself into the architecture

Guardrail:

- encode fixed vs variable power in the same optic schema
- keep the fixed `10x` scope on the same PiP and runtime-state path that future `FFP` scopes use

---

## Recommended Rollout Order

1. Formalize the authored data contract for fixed-power magnified scopes.
2. Convert current Kar98k optic to explicit fixed `10x`.
3. Add optic-instance click-based zero state and persistence seam.
4. Make reticle/turret units explicit and consistent.
5. Add a scope-only observation policy surface for future long-range visual promotion.
6. Validate Kar98k `10x` as the first production scope.
7. Extend the same framework to `5-25x FFP`.

---

## Out of Scope For This Design Slice

- final UI/interaction flow for dialing turrets
- full atmospheric ballistics
- final far-target impostor renderer implementation
- rifle-specific dope cards / saved per-rifle zero profiles
- SFP production rollout

Those should be layered on top of this framework, not mixed into the initial architecture decision.

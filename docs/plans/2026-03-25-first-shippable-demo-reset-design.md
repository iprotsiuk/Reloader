# First Shippable Demo Reset Design

## Goal

Reset `Reloader` onto one first shippable assassination demo that is small enough to finish, strong enough to prove the fantasy, and narrow enough to stop the current expansion-driven brittleness.

---

## Problem Summary

The repo already has the core fantasy pieces:

- first-person movement
- one rifle / one caliber path
- scoped PiP optics
- basic ballistics
- reloading bench acceptance coverage
- shop and inventory flow
- contract intake and payout gating
- police heat seams
- an authored `MainTown` contract slice

The current pain is not "wrong genre." The pain is that the project reopened too many fronts at once:

- `MainTown` shell growth and planning-map churn
- floating-origin follow-on ambition
- broader world/travel hardening
- more simulation breadth than the first slice can justify

That makes the repo feel entangled because a small change can now cross authored scene state, runtime contracts, save state, UI binding, world travel, and long-range combat support.

---

## Approach Options

### Option A: Keep pushing the broad sandbox

Keep building the current full-scope direction: bigger `MainTown`, more systems, more world support, more progression surfaces.

Why reject it:

- it preserves the current frustration pattern
- it delays a finishable proof point
- it keeps every change crossing too many seams

### Option B: Pivot the repo into zombie hill-defense

Treat the current repo as a failed direction and reuse only FPS/combat tech for a simpler zombie mode.

Why reject it:

- it throws away the repo's differentiator
- it replaces one unfinished game with another
- it creates new content, pacing, and AI demands instead of reducing scope

### Option C: Ruthlessly cut to one authored assassination micro-slice

Keep the direction, keep the strongest implemented systems, and freeze almost everything else until one narrow demo is reproducible.

Why recommend it:

- it matches the canonical product thesis
- it reuses the most real, distinctive work already in the repo
- it gives the project a finish line instead of another expansion cycle

Recommended: `Option C`

---

## Recommended Slice

The first shippable demo should be:

1. Boot into the current playable flow.
2. Accept one contract from the `Contracts` tab.
3. Use one known-good rifle / optic / `.308` ammo path.
4. Optionally validate the setup with the `PlayerDevice`.
5. Travel or walk to one stable authored target-exposure setup with multiple plausible firing lanes/sightlines, including at least one known-good reference setup for deterministic tests.
6. Kill one correct target.
7. Break line of sight and survive the search timer.
8. Claim payout.
9. End or cleanly restart.

Important constraint:

- this is an authored demo path built on the current contract runtime
- it does not need broad target variety, a huge map, or new simulation layers

---

## Keep / Freeze / Later

### Keep Now

- `Contracts` tab intake and current contract runtime
- `ContractEscapeResolutionRuntime` and `StaticContractRuntimeProvider`
- `PoliceHeatRuntime` payout gate
- authored `MainTown` contract slice and smoke tests
- `StarterRifle`, `.308`, scoped PiP optics, and no-wind ballistics
- reloading bench acceptance path
- shop, inventory, and pickup/drop persistence
- `PlayerDevice` as optional prep instrumentation
- floating-origin slice-one runtime exactly as already implemented

### Freeze Now

- more `MainTown` shell growth, district expansion, terrain beautification, and planning-map churn
- floating-origin Task 6 style follow-on work
- ELR projectile authority, bullet-cam, and stable-target follow-up
- vehicles
- hunting
- competitions
- black-market or underworld progression expansion
- deep procedural population work beyond what the current contract slice already needs
- "finish everything" save/load scope creep beyond the demo path

### Later

- one specialty-ammo / contract-modifier slice
- arrest/death confiscation and respawn consequence
- real in-world police responders
- broader authored contract variety
- deeper progression and social simulation

---

## Active Working Set

Until the first demo is stable, the active working set should stay small:

- `Reloader/Assets/_Project/Core/Scripts/Runtime/ContractEscapeResolutionRuntime.cs`
- `Reloader/Assets/_Project/Core/Scripts/Runtime/StaticContractRuntimeProvider.cs`
- `Reloader/Assets/_Project/Core/Scripts/Runtime/PoliceHeatRuntime.cs`
- `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsBridgePlayModeTests.cs`
- `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryContractsSectionPlayModeTests.cs`
- `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryDeviceSectionPlayModeTests.cs`
- `Reloader/Assets/_Project/Weapons/Scripts/World/ContractTargetDamageable.cs`
- `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`
- `Reloader/Assets/_Project/World/Tests/PlayMode/RoundTripTravelPlayModeTests.cs`
- `Reloader/Assets/_Project/Reloading/Tests/PlayMode/WorkbenchMountFlowAcceptancePlayModeTests.cs`
- `Reloader/Assets/_Project/Reloading/Tests/PlayMode/WorkbenchLoadoutControllerPlayModeTests.cs`
- `Reloader/Assets/_Project/Reloading/Tests/PlayMode/ReloadingBenchInteractionPlayModeTests.cs`
- `Reloader/Assets/_Project/Economy/Tests/PlayMode/EconomyControllerCheckoutPlayModeTests.cs`
- `Reloader/Assets/_Project/Core/Tests/EditMode/WorldObjectStateSaveModuleTests.cs`
- `Reloader/Assets/_Project/Core/Tests/EditMode/PlayerDeviceSaveModuleTests.cs`

Everything else is allowed to exist, but it should not control near-term priorities.

---

## Exit Criteria

The reset slice is done when:

- one contract path is playable without editor intervention
- the player can prep, shoot, escape, and cash out on purpose
- the contract path has targeted smoke coverage
- the required prep/runtime seams are deterministic enough that small edits stop breaking unrelated work
- the next task is "add one expressive modifier/ammo slice," not "keep rebuilding the world"

---

## Anti-Goals

Do not use this reset to:

- restart the whole architecture
- rewrite contracts from scratch
- replace the existing authored contract slice with a new mission system
- treat floating origin as the next headline feature
- chase a bigger map because the current map feels incomplete
- pivot the repo into a different genre

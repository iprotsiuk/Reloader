# Floating Origin Design

**Date:** 2026-03-21

## Goal

Remove visible far-from-origin jitter in the playable world while preserving exact long-range ballistic truth, smooth bullet-cam presentation, and moving-target hit accuracy over multi-second projectile flight.

## Problem

The current runtime uses a single local Unity float world with no implemented floating-origin controller. When the player travels far from local `0,0,0`, visible jitter shows up while turning and aiming. That is unacceptable for:

- first-person weapon presentation
- long-range target identification
- ELR bullet follow camera
- realistic wind/drop/lead simulation
- moving targets at distances greater than one mile

The repo already contains planning references for dynamic origin rebasing, but no runtime implementation exists yet.

## Requirements

Non-negotiable outcomes:

- no visible rifle/arms/world jitter when the player is far from the original scene origin
- bullet flight remains smooth in bullet cam with no visible pops/snaps during rebasing
- moving shootable NPCs remain hittable during long projectile flight
- ballistic truth remains precise at ELR, including wind effects
- no hidden repair, no dual-path fallback, no “temporary near-zero” patch

Additional constraints:

- all NPCs are shootable
- targets can move during multi-second bullet flight
- the system must be tunable later without architecture churn

## Explored Approaches

### Option 1: Local Floating Origin Only

Rebase the Unity scene when the player is far from local zero, but keep bullets and targets as ordinary local-scene simulation.

Pros:

- smallest first implementation
- likely reduces visible player/view jitter quickly

Cons:

- does not solve ELR truth cleanly
- moving-target hit resolution still depends on local float precision
- highest risk of bullet-cam pops and precision glitches during long flight

### Option 2: Split Authority With Rebased Local Scene

Keep a rebased local Unity scene for play/rendering, but move ballistic truth and moving-target truth into stable world coordinates that never rebase.

Pros:

- best fit for ELR accuracy, moving targets, and smooth bullet cam
- solves player jitter and ballistic precision as one coherent architecture
- rebasing becomes a presentation concern, not a hit-truth concern

Cons:

- more up-front design and integration work

### Option 3: Hybrid Deferral

Add local floating origin now, promote bullets to stable authority later, and defer stable moving-target authority further.

Pros:

- faster initial delivery than full split authority

Cons:

- likely redesign of bullet/target seams later
- highest chance of rework
- poor fit for the stated ELR goals

## Decision

Use **Option 2**.

The runtime should separate:

- **rebased local scene space** for player, cameras, world rendering, NPC GameObjects, and presentation
- **stable world space** for projectile truth and moving target truth
- **projection bridges** that map stable truth into the current rebased local scene

This is the cleanest route to:

- fix visible jitter
- preserve smooth bullet-cam motion
- keep long-range ballistics precise
- support moving ELR targets without float drift

## Architecture

### 1. Stable World Authority

This is the canonical truth layer.

It owns:

- projectile state
- shootable NPC target state
- long-range hit evaluation
- wind/drop/drag integration

This layer never rebases.

### 2. Rebased Local Unity Scene

This is the playable/rendered layer.

It owns:

- canonical runtime player root
- world camera and viewmodel camera stacks
- world geometry
- NPC presentation objects
- local FX and scene interactions

When the player gets too far from local zero, the local scene is shifted as one coherent translation so the player stays near zero.

### 3. Projection Bridges

These are the only approved seams between stable authority and local scene space.

They provide:

- `LocalToStable`
- `StableToLocal`
- `LocalDirectionToStable`

Targets and bullet-cam proxies use these bridges to project stable truth into the current rebased local scene.

## Rebase Rules

The runtime will use one canonical rebase trigger:

- measure horizontal distance from the canonical runtime player root to the current local origin
- ignore `y`
- rebase when `distance >= RebaseDistanceMeters`

Initial tuning:

- `RebaseDistanceMeters = 500`
- `RebaseCooldownSeconds` present and tunable

Important rules:

- no ADS-specific rebase path
- no second micro-correction or near-zero patch
- rebasing is one global local-scene translation, not per-object catch-up logic

## Visual Continuity Rule

A rebase is allowed to change local coordinates, but it must not visibly teleport props, NPCs, anchors, or the player relative to one another.

That means:

- the local scene shifts coherently in one frame
- relative offsets around the player remain unchanged
- systems that cache world-space values must opt into an explicit rebase-participant seam instead of relying on hidden repair

## Data Flow

### Firing

- sample muzzle position/direction from the authored runtime weapon view in local scene space
- immediately promote launch data into stable world coordinates
- projectile truth lives in stable authority from that point forward

### Targets

- every shootable NPC has stable-world position and velocity state
- local NPC transforms are projections of that stable state into the current local scene
- hit evaluation compares stable bullet state against stable target state

### Bullet Cam

- bullet cam follows a local proxy projected from stable projectile state
- if a rebase occurs during flight, the local proxy remains continuous under the new origin mapping

### Travel

- stable coordinate state must remain coherent through scene travel
- local travel/entry anchors project against the active stable/local mapping

## Phased Delivery

The framework should be designed now for full split authority, but rollout should be phased.

### Slice 1

Introduce:

- stable/local coordinate runtime state
- dynamic origin rebase controller
- explicit rebase participant seam
- player/world/camera integration
- coordinate conversion API

Do **not** introduce yet:

- stable projectile simulation
- stable NPC target authority
- bullet-cam rewrite
- stable ELR hit-truth migration

Slice 1 acceptance:

- visible far-from-origin jitter is removed
- multiple rebases do not accumulate drift
- canonical runtime player root remains authoritative
- props/NPCs/anchors do not appear to teleport relative to the player

### Later Slices

- stable ballistic authority
- stable target authority for all shootable NPCs
- ELR hit truth and bullet cam running entirely from stable authority plus projection

## Verification Strategy

Primary verification should be targeted and evidence-driven:

- EditMode/runtime tests for coordinate conversion and threshold math
- EditMode tests for rebase participation contracts
- targeted PlayMode tests for:
  - player/world continuity across rebase
  - unchanged relative offsets to props/NPCs/anchors
  - repeated rebases without drift accumulation

Later slices add:

- stable projectile continuity tests
- moving-target ELR hit tests
- rebase-during-flight bullet-cam continuity tests

## Summary

The floating-origin solution should not be a visual patch. It should be the first layer of a stable-world combat architecture:

- rebased local Unity scene for play/rendering
- stable authority for projectile and target truth
- explicit projection between them

That keeps the near-player experience stable while making ELR combat precise and rebase-safe.

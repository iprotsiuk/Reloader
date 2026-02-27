# Main Town + Indoor Range Travel Design

**Date:** 2026-02-27
**Status:** Approved for implementation

## Goal
Define the first production world slice where the player can live in a `MainTown` hub, enter an `IndoorRange` gameplay instance, and return while preserving character state.

## Chosen Approach
Use a **hub + reusable instance** model with a **persistent player root** and **additive scene travel**.

## Decision Summary

- World model: `MainTown` hub plus reusable activity instances.
- First instance: `IndoorRangeInstance`.
- Player lifetime: one persistent player object survives scene transitions (`DontDestroyOnLoad`).
- Time model: **hybrid** progression (clock/date + lightweight scheduled systems advance; no background NPC simulation while away).
- Access model: **data-driven unlock rules** (not hardcoded scene checks).

## Why This Approach

- Extensible: supports future outdoor range, competitions, and hunting instances without reworking core travel architecture.
- Reusable: same scene templates can host multiple activity variants via config/context payload.
- Stable for saves: central travel contracts and world-state updates are easier to persist/restore.
- MCP-friendly: scene authoring and wiring can be largely handled with Unity MCP object/component operations and read-back checks.

## Core Contracts

### Scene Topology
- `Bootstrap` scene (persistent systems + persistent player root)
- `MainTown` scene (hub gameplay)
- `IndoorRangeInstance` scene (first reusable activity instance)

### Runtime Contracts
- `TravelContext` payload including destination scene, entry point ID, return point ID, activity type, and time policy.
- `WorldTravelManager` as single orchestration path for scene transitions.
- `TravelAccessEvaluator` to validate unlock rules before entering instances.

### Data Contracts
- `UnlockRuleDefinition` + `TravelAccessProfile` ScriptableObject assets.
- Minimal v1 rule set: required progression flags and/or required inventory token IDs.

## Non-Goals (v1)

- No live NPC simulation while the player is inside activity instances.
- No full outdoor range/competition/hunting travel implementation in this slice.
- No advanced travel UI/map or cinematic loading transitions.

## Success Criteria

- Player enters indoor range from town and returns to town with same runtime player identity and preserved state.
- Access to indoor range is controlled via data-driven rules.
- Time advances per travel policy with no background NPC movement simulation.
- Travel pipeline is validated by targeted tests and Unity-side read-back checks.

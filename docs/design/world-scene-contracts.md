# World Scene Contracts

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) and [world-and-scenes.md](world-and-scenes.md) first.

## Why This Exists

MainTown exposed a scaling risk: scene content could be partially functional (pickup works) while critical gameplay wiring (equip/ADS/reload/fire) was missing. For a project with many future town/instance scenes, this must be validated by contract, not by manual memory.

This document defines the scalable foundation for world-scene authoring.

## Incident Summary (February 27, 2026)

- Symptom: rifle/ammo pickup worked, but ADS, reload, and firing did not.
- Root cause: `MainTown` had inventory/pickup chain wired, but missing/empty combat chain:
  - missing or unwired `PlayerWeaponController`
  - `WeaponRegistry` with no weapon definitions
  - missing camera/muzzle/link dependencies for combat runtime
- Fix: scene wiring was standardized and scripted via:
  - `Reloader/World/Wire MainTown Combat Setup`
  - `Assets/_Project/World/Editor/MainTownCombatWiring.cs`

## Architecture Direction

Use **declarative scene contracts + automated validators + deterministic scene templates**.

### Contract Model

For each runtime scene, define a `WorldSceneContract` (initially in docs/tests, then as data assets):

- identity:
  - scene path
  - scene role (`Bootstrap`, `TownHub`, `ActivityInstance`)
- required object paths
- required component types per object
- required serialized references (non-null and/or exact target)
- required entry point IDs (`SceneEntryPoint`)
- forbidden objects/components (for example bootstrap-only systems inside instances)

### Standardization Boundaries

Global/persistent systems (one per game lifetime):
- runtime kernel/event hub roots
- persistent player lifetime root
- global UI shell

Scene-local systems:
- geometry, props, nav data, local triggers, local NPC placement
- scene-specific interactables and authored points

Reusable prefabs/templates:
- scene entry marker prefab
- travel trigger prefab
- optional scene service root prefab

## Validation Gates (Required)

### Gate 1: EditMode Contract Tests (fast)

Every world scene change must pass contract tests that assert:
- required object paths exist
- required components exist
- required references are non-null/valid
- required entry point IDs are present and unique

### Gate 2: PlayMode Flow Tests (behavior)

World-scene flows must pass:
- topology smoke (build settings order contract)
- travel round-trip contract
- pickup -> equip -> ADS/reload/fire happy path in key hub scenes

### Gate 3: MCP Read-Back Verification

After scene mutations through MCP/editor tooling:
- read back changed objects/components
- verify exact references and key serialized values
- only then run targeted tests

## Authoring Workflow for New Scenes

1. Create scene from approved template.
2. Apply scene wiring tool (or equivalent deterministic setup command).
3. Run contract read-back checks.
4. Run EditMode + PlayMode targeted tests.
5. Commit only scoped world files.

For current workflow details, use:
- `docs/plans/2026-02-27-main-town-indoor-range-mcp-authoring-checklist.md`

## Rollout Plan

Phase 1 (now):
- document contract
- enforce checklist discipline
- keep deterministic wiring tools per scene family

Phase 2:
- add `WorldSceneContract` ScriptableObject assets
- add `Validate All Scene Contracts` editor command
- add contract suite to CI for world-scene touching PRs

Phase 3:
- add scene templates that satisfy baseline contracts by default
- minimize scene-specific hand wiring

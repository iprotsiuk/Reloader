# World and Scenes Design

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) and [world-and-vehicles.md](world-and-vehicles.md) first.

## Scene Topology Contract [v0.1]

Build Settings first-three order is a runtime contract for world travel bootstrap:

| Build Index | Scene Path | Role |
|---|---|---|
| 0 | `Assets/Scenes/Bootstrap.unity` | Persistent runtime/bootstrap entrypoint |
| 1 | `Assets/_Project/World/Scenes/MainTown.unity` | Primary world hub scene |
| 2 | `Assets/_Project/World/Scenes/IndoorRangeInstance.unity` | First instanced activity scene |

Contract rules:
- Indices `0..2` are reserved and must stay in the order above.
- Additional scenes may be appended after index `2`.
- Topology validation is enforced by PlayMode smoke tests under `_Project/World/Tests/PlayMode`.

## Travel Slice Status Snapshot [v0.1]

Status as of 2026-02-28:
- MainTown <-> IndoorRange round-trip travel is implemented and wired in scenes.
- Bootstrap runtime entry flow targets MainTown spawn through travel coordinator bootstrap wiring.
- Scene trigger objects and entry-point IDs are authored for both travel directions.
- Regression coverage exists in PlayMode via `RoundTripTravelPlayModeTests`.

Implementation references:
- Runtime travel pipeline: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/WorldTravelCoordinator.cs`
- Trigger component: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/TravelSceneTrigger.cs`
- Bootstrap handoff: `Reloader/Assets/_Project/World/Scripts/Runtime/BootstrapWorldRoot.cs`
- Round-trip regression tests: `Reloader/Assets/_Project/World/Tests/PlayMode/RoundTripTravelPlayModeTests.cs`

Out-of-slice note:
- Data-driven travel unlock evaluation and hybrid travel-time advancement remain separate follow-up slices; their absence does not block the current MainTown/IndoorRange round trip.

## Temporary MainWorld Coexistence Gate [v0.1]

`Assets/Scenes/MainWorld.unity` remains a temporary compatibility scene during migration.

Gate policy:
- Do not place `MainWorld.unity` in Build Settings indices `0..2`.
- `MainWorld.unity` may coexist in the repository for rollback/manual checks until travel cutover is accepted.
- New world-travel wiring targets `Bootstrap -> MainTown -> IndoorRangeInstance`, not `MainWorld`.

Exit criteria for removing coexistence gate:
- MainTown and IndoorRange travel loop is stable in PlayMode test coverage.
- Runtime bootstrap and persistence contracts no longer depend on MainWorld-only scene objects.
- Migration sign-off explicitly removes `MainWorld.unity` from active runtime entry flows.

## Forward Scope [v0.2]

When new instance destinations are added, append scenes after index `2` and preserve the first-three topology contract.

## Scene Contract Guardrail [v0.2]

World scenes must satisfy explicit wiring contracts, not ad-hoc/manual assumptions.

Required references:
- [world-scene-contracts.md](world-scene-contracts.md)
- [world-scene-wiring-incident-2026-02-27.md](world-scene-wiring-incident-2026-02-27.md)

Policy:
- Scene mutations must be followed by MCP read-back verification of changed objects/components.
- Scene mutations must pass targeted EditMode/PlayMode tests before completion claims.
- Deterministic editor wiring tools are preferred over one-off inspector editing for repeatable setup.

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

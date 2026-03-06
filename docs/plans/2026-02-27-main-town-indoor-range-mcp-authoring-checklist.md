# MainTown + IndoorRange MCP Authoring Checklist

> Status Pointer (2026-02-28): This is a planning/execution artifact. For live implemented-vs-planned status, use `docs/design/v0.1-demo-status-and-milestones.md`.


Use this checklist for scene/world MCP sessions in this slice.

## 1) Preflight

- Confirm branch/working policy (this project: `main`, no worktrees unless explicitly requested).
- Read MCP instance list: `mcpforunity://instances`.
- Set active instance explicitly when needed.
- Confirm project: `mcpforunity://project/info`.
- Confirm editor ready: `mcpforunity://editor/state` (`ready_for_tools=true`).
- Confirm target scene path before mutation.

## 2) Mutation Order

- Apply deterministic wiring tools first when available.
- Apply focused scene object/component edits second.
- Avoid broad/manual rewiring across unrelated objects.
- Save scene after mutation.

## 3) Mandatory Read-Back

After every scene mutation set:

- Read changed objects/components and verify key references.
- Verify required runtime chains are complete (not partial).
- For MainTown combat specifically:
  - `WeaponRegistry._definitions` includes only the supported authored weapons (`Kar98k`, `Canik TP9`).
  - vendor and storage authority is intact: no seeded starter floor spawns, starter kit seeded in `StorageChest`, supported weapon/ammo catalogs present.
  - `PlayerWeaponController` refs set: input, inventory, registry, muzzle, camera defaults, projectile.
  - `PlayerLookController._cameraDefaults` set.
  - `CameraPivot/CameraLookTarget` and `CameraPivot/WeaponMuzzle` present.

## 4) Verification Gates

Run targeted tests before completion claim:

- `Reloader.World.Tests.PlayMode.SceneTopologySmokeTests`
- `Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests`
- `Reloader.World.Tests.EditMode.TravelContextEditModeTests`
- world-flow tests touched by the change (for combat/acquisition/storage: relevant weapon + storage playmode tests)

## 5) Completion Rules

- Do not claim done without:
  - read-back evidence
  - targeted test evidence
- Commit only scoped files (ignore unrelated repository changes).

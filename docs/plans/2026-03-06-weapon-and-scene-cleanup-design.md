# Weapon And Scene Cleanup Design

## Goal

Clean the current `feature/assassin-sandbox-pivot` branch so the supported weapon/content base is explicit, strict, and expandable without hidden runtime fallbacks.

This pass keeps the recent good systems, but removes permissive lookup and placeholder behavior that can hide content bugs or make scene behavior drift.

---

## Supported Arsenal

Authored weapon/content support after this pass:

- `Kar98k (.308)`
- `Canik TP9 (9mm)`

Authoring rules:

- Remove authored starter/support content for other weapons in `_Project`.
- Keep only real authored attachment content that exists and is intentionally supported.
- Current authored attachment content remains `Kar98k` scope + muzzle.
- Pistol attachment framework support stays intact, but fake/unshipped pistol attachment content is not part of this cleanup.

---

## Strict Runtime Contract

The weapon/content runtime should stop masking bad data.

New strictness rules:

- `WeaponRegistry` resolves only from its assigned serialized definitions in live runtime.
- UI/runtime code must not scan other registries to rescue bad scene wiring.
- Editor asset-database fallback must not rescue live runtime lookups.
- Missing weapon definitions fail clearly instead of resolving from unrelated content.
- Missing attachment mount/anchor data fails clearly instead of silently substituting a different gun/content path.
- Missing drop/pickup visuals fail validation and log loud runtime errors instead of using generic grey cubes in live gameplay.

This is a deliberate anti-patch-on-patch cleanup. The point is to surface bad content/wiring quickly, not hide it.

---

## Scene Parity

`MainTown` and `IndoorRange` should use the same authoritative weapon rig contract.

Scene parity requirements:

- Same supported weapon definitions
- Same weapon view prefab bindings
- Same attachment metadata expectations
- Same player-arms / animator binding expectations
- Same dropped-item visual behavior

Differences between scenes should come from authored world layout, not separate weapon-system behavior or stale scene overrides.

---

## Acquisition Authority

`MainTown` should stop seeding starter weapons on the floor.

Authoritative acquisition rules for this pass:

- supported weapons and ammo are sold through the authored weapon/ammo vendor catalogs
- the player's `StorageChest` remains general-purpose storage, but starts seeded once with the grandpa rifle kit:
  - `Kar98k`
  - `Canik TP9`
  - `Kar98k` scope
  - `Kar98k` muzzle device
  - `50` rounds of `.308`
- no authored starter floor spawns for guns, ammo, or attachments remain in `MainTown`
- dropped items can still exist later from real gameplay events, but the scene no longer depends on seeded world pickups for starter access

---

## Drop Visual Contract

Dropping guns/ammo with `G` must create authored world visuals, not generic cubes.

Contract:

- Runtime drop visuals come from authoritative item-definition visual sources.
- Scene-authoring pickup visuals come from authoritative spawn/item visual sources.
- Grey cube fallback is removed from live runtime drop creation.
- Grey cube fallback is removed from scene authoring/wiring paths that currently hide missing visual sources.
- Validation/tests must catch missing visual sources before they reach normal gameplay.

---

## Included In This Pass

- Canonical pistol naming cleanup to `Canik TP9`
- Strict registry resolution cleanup
- Tab inventory attachment-lookup cleanup
- Scene wiring cleanup for `MainTown` and `IndoorRange`
- Dropped-item visual cleanup
- Removal of stale authored non-supported weapon content in `_Project`
- Active PR feedback fix for repeated `ReportLineOfSightLost` timer reset behavior
- Doc/plan/progress updates for future agent clarity

---

## Deferred To A Later Targeted Refactor

This pass does **not** try to solve the whole assassination-sandbox runtime architecture.

Deferred:

- Contract runtime aggregate / execution-state rewrite
- Law-response coordinator and full police consequence pipeline
- Rich NPC target / witness / patrol capability stack
- Semantic world-scene contract anchors for contract gameplay

Those need a dedicated architecture pass. They should not be smuggled into this cleanup branch.

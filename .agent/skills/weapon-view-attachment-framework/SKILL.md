---
name: weapon-view-attachment-framework
description: Use when adding or modifying weapon view prefabs, attachment slots, attachment definitions, or runtime attachment mounting for rifles, pistols, optics, muzzles, magazines, slides, bipods, and other weapon upgrades.
---

# Weapon View Attachment Framework

Use this skill whenever a task touches:

- `Reloader/Assets/_Project/Weapons/Prefabs/**`
- `Reloader/Assets/_Project/Weapons/Data/**` for weapon/attachment content
- `Reloader/Assets/_Project/Weapons/Scripts/**` that bridge weapon views to runtime attachments
- `Reloader/Assets/Game/Weapons/**` attachment runtime or ADS/optic mounting
- player/scene wiring that binds weapon item ids to first-person view prefabs

## Required Reading

Read these first:

1. `docs/design/core-architecture.md`
2. `docs/design/weapons-and-ballistics.md`
3. `docs/design/ads-optics-framework.md`
4. `docs/design/weapon-view-attachment-runtime.md`
5. `.agent/skills/unity-project-conventions/SKILL.md`

## Non-Negotiable Contract

1. Every weapon item id binds to an explicit authored runtime view prefab.
2. Runtime weapon view prefabs are weapon-specific. Do not reuse a generic placeholder rifle/pistol as a fallback.
3. Runtime weapon view prefabs expose mount points through `WeaponViewAttachmentMounts`.
4. Runtime attachment slots start visually empty. Do not seed runtime state from authored scope/muzzle art in the prefab.
5. Attachment meshes mount only from equipped runtime state and explicit attachment definitions.
6. New attachment families extend slot-driven config and runtime ownership. Do not add controller-side transform-name heuristics.
7. Do not add fallback weapons, fallback attachments, or editor-only substitute prefabs to hide broken content wiring.
8. Pose tuning is data/config per weapon and per attachment item where needed, not hard-coded special cases.

## Required Workflow

Copy this checklist and complete it for substantial weapon/attachment work:

```
Weapon View Attachment Progress:
- [ ] Confirm the weapon item id and explicit runtime view prefab binding
- [ ] Confirm `WeaponViewAttachmentMounts` exposes all required mount points/slots
- [ ] Confirm attachment definitions point to explicit attachment prefabs
- [ ] Keep runtime spawn empty for attachment slots
- [ ] Mount visuals only from equipped runtime state
- [ ] Add/update per-weapon and per-attachment pose tuning only through helper/config
- [ ] Add or update targeted tests for mount success/failure and no-fallback behavior
- [ ] Verify scenes/prefabs bind the intended view prefab, not third-party source art
```

## Implementation Rules

### Weapon View Prefabs

- One authored runtime view prefab per weapon.
- Include only that weapon's base mesh and explicit reference points.
- Required references live on `WeaponViewAttachmentMounts`.
- Do not rely on child names like `OpticSlot`, `WWII_Recon_A_Sight`, or asset-pack naming unless that name is serialized into the mounts component.

### Attachment Definitions

- Every attachment item id resolves to one explicit definition asset.
- Every definition asset resolves to one explicit mount prefab.
- Fix broken prefab references at the asset source; do not compensate with runtime fallback lookup.

### Runtime Ownership

- `PlayerWeaponController` owns equipped state and view binding.
- `AttachmentManager` owns scope and muzzle child lifecycle for mounted visuals.
- Additional slot runtimes must follow the same pattern: explicit slot owner, explicit prefab, explicit failure path.
- If a mount fails, surface the failure and keep state consistent. Do not silently report success.

### Pose Tuning

- Base pose is for irons/no matching override.
- Scoped or attachment-specific ADS differences go through `WeaponViewPoseTuningHelper` attachment overrides keyed by slot + attachment item id.
- Do not disable pose tuning wholesale for “any scope present”.

## Forbidden Fixes

Do not introduce any of the following:

- fallback to another weapon view prefab
- fallback to another attachment prefab
- controller-side discovery of attachments from authored visuals
- deterministic project-wide “first compatible asset” lookup
- re-seeding attachment state from prefab visuals on each equip
- silently mounting to arbitrary roots because a proper slot is missing

## Verification Targets

At minimum, verify:

- the intended weapon view prefab is what actually spawns in-hand
- equipping a supported attachment mounts the correct mesh under the correct slot
- unsupported/misconfigured attachments fail loudly without consuming state incorrectly
- the weapon still spawns with empty attachment slots when nothing is equipped
- pose tuning distinguishes base irons from attachment override when relevant

If the change touches docs or agent guardrails, also run:

- `bash scripts/verify-docs-and-context.sh`
- `bash scripts/verify-extensible-development-contracts.sh`
- `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`

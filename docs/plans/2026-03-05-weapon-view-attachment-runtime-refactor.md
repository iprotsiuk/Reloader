# Weapon View Attachment Runtime Refactor

## Goal

Make weapon view attachment mounting simple and extensible:

- weapon view prefabs spawn empty
- each weapon binds to an explicit authored view prefab, not a generic default rifle
- prefabs expose mount points, not pre-equipped attachment art
- runtime equips and unequips attachment meshes under explicit slots
- controller owns weapon state, not attachment mesh discovery
- future slots can expand beyond scope and muzzle (`magazine`, `slide`, `trigger`, `bipod`, etc.)

## Problem Summary

The Kar98k scope flow regressed because the runtime path was split across too many responsibilities:

- `PlayerWeaponController` was scanning prefab art to infer whether a weapon "already had" a scope
- controller code destroyed authored visuals by name
- controller bridged to `AttachmentManager` through reflection
- `AttachmentManager` tried to harden around broken instantiate results with editor fallback and object-type probing

That turned a simple requirement, "mount the equipped optic prefab under the rifle scope slot", into a multi-system heuristic flow.

## Bugs Observed

### Live symptoms

- `AttachmentManager: Used editor fallback optic prefab ...`
- `AttachmentManager: Equipped optic has no SightAnchor. Falling back to optic root.`
- `AttachmentManager: Optic instantiate threw InvalidCastException ...`
- `AttachmentManager: Optic instantiate returned unsupported type 'UnityEngine.Object' ...`
- `PlayerWeaponController: EquipOptic returned failure ...`

### Root causes

1. The controller was not using a stable view contract.
   It inferred slots from names like `WWII_Recon_A_Sight`, `OpticSlot`, authored mesh names, and visual keywords.

2. Optic mounting was not owned by one runtime system.
   The controller destroyed visuals before and after mount, while `AttachmentManager` also managed slot children.

3. The explicit Kar98k view binding was no longer falling back, but `RifleView.prefab` itself still embedded an AR mesh from another asset pack.
   The binding was correct, the authored view prefab content was wrong.

4. Hardening patches masked the real problem.
   Fallback prefabs and unsupported-object handling kept moving the failure point instead of removing the architectural ambiguity.

5. Weapon spawn still auto-seeded muzzle and magazine runtime bridges.
   `PlayerWeaponController` scanned authored runtime defaults, then fell back to deterministic project-wide attachment assets before the real equipped attachment state was applied.

## Failed Attempts Logged

### Attempt 1: editor fallback optic prefab

Intent:
- keep optic equip alive if the real prefab path failed

Why it failed:
- it hid the missing explicit slot contract
- fallback prefab also lacked an authored `SightAnchor`
- runtime behavior no longer represented the real equipped attachment

### Attempt 2: broad object instantiate hardening

Intent:
- accept `UnityEngine.Object` instantiate results and recover by detecting new slot children

Why it failed:
- it still depended on side effects under the slot
- it did not solve the missing explicit view contract
- the equip path remained brittle and hard to reason about

### Attempt 3: authored-visual seeding and destruction

Intent:
- infer default attachments from prefab art and remove conflicting visuals during equip

Why it failed:
- prefab art became runtime state
- removing one heuristic exposed another
- "empty spawn + runtime attach" was impossible to trust

## Target Runtime Contract

### Weapon view prefab

Weapon view prefabs should contain:

- the weapon mesh for exactly one weapon
- stable reference points like `Muzzle`, `IronSightAnchor`
- stable attachment slots like `ScopeSlot`, `MuzzleAttachmentSlot`
- optional runtime mount component: `WeaponViewAttachmentMounts`

Weapon view prefabs should not contain:

- pre-equipped scope meshes used as live runtime attachments
- pre-equipped muzzle or magazine runtime defaults used as live runtime attachments
- name-based surrogates for mount points
- another weapon's mesh as a stand-in default rifle

### Controller responsibilities

`PlayerWeaponController` should:

- spawn the explicitly bound view prefab for the selected weapon item id
- keep runtime ammo and equipped attachment state
- resolve attachment definitions
- ask the attachment runtime to equip or unequip
- wire scoped ADS to the active optic definition and sight anchor

`PlayerWeaponController` should not:

- discover attachments from authored visuals
- seed scope, muzzle, or magazine state from prefab art
- scan project attachment assets to invent a runtime default
- destroy attachment meshes by keyword heuristics

### Attachment runtime responsibilities

`AttachmentManager` should:

- own the scope slot child lifecycle
- own the muzzle slot child lifecycle
- expose `ActiveOpticDefinition`, `ActiveMuzzleDefinition`, and active sight anchor
- create a default zeroed `SightAnchor` child when an optic prefab does not author one

`AttachmentManager` should not:

- use editor fallback prefabs as normal runtime behavior
- guess attachment data from scene art

## Refactor Notes For This Pass

### Code direction

- keep cross-assembly interop narrow: `_Project/Weapons` still talks to `Game/Weapons` through a small reflection bridge because the assemblies are split
- the controller now expects explicit mount ownership rather than authored-visual heuristics
- scoped ADS pose bypass stays tied to magnified optics only
- temporary compatibility is acceptable for explicit child names like `ScopeSlot` and `MuzzleAttachmentSlot`, but not for authored attachment art inference

### Asset direction

- `RifleView.prefab` is currently the explicit Kar98k runtime view for `weapon-kar98k`
- it must contain the Kar98k mesh, not a generic/default rifle mesh from another pack
- it must provide explicit reference points and slots through `WeaponViewAttachmentMounts`
- future weapons should follow the same contract: one explicit view prefab per weapon binding

## Implemented In This Pass

- removed controller-side deterministic muzzle and magazine attachment fallback seeding during weapon spawn
- kept spawned weapon views empty until runtime attachment state explicitly equips scope or muzzle meshes
- replaced the authored AR mesh inside `RifleView.prefab` with the Kar98k mesh contract
- updated stale Kar98k item/icon source references to the explicit runtime view prefab so visual consumers stop pointing at legacy rifle assets
- extended `WeaponViewPoseTuningHelper` so one helper can carry a base irons pose plus per-attachment ADS overrides, keyed by slot + attachment item id
- seeded MainTown with an explicit override entry for `att-kar98k-scope-remote-a` so runtime tuning can compare irons vs current scope without code changes

## Runtime Pose Tuning Workflow

Use the `WeaponViewPoseTuningHelper` on `PlayerRoot` as the live tuning surface:

1. Tune `_hipLocalPosition`, `_hipLocalEuler`, `_adsLocalPosition`, `_adsLocalEuler`, `_blendSpeed`, and `_rifleLocalEulerOffset` for the base irons pose.
2. Tune `_attachmentPoseOverrides[]` entries for scoped variants. Each entry is matched by `WeaponAttachmentSlotType` plus the equipped attachment item id.
3. Watch `_activePoseSource` and `_activeAttachmentItemId` at runtime to confirm whether the helper is currently driving the base pose or an attachment override.
4. When a pose feels correct, copy the serialized values back into source control instead of relying on runtime-only scene state.

Current seeded example:

- base pose: `weapon-kar98k` irons
- attachment override: `Scope` + `att-kar98k-scope-remote-a`

## Framework Contract Going Forward

1. Each weapon item id must bind to its own authored view prefab.
2. Every runtime view prefab must expose `WeaponViewAttachmentMounts`.
3. Runtime view prefabs start visually empty for attachment slots.
4. Attachment meshes are mounted only from equipped runtime state.
5. New attachment families extend the slot enum plus mount declarations, not fallback heuristics.

## Open Follow-Ups

1. Add dedicated authored runtime view prefabs for the next weapons instead of reusing `RifleView.prefab` as a generic template.
2. Extend `WeaponViewAttachmentMounts` coverage to future slots like `Magazine`, `Slide`, `Trigger`, and `Bipod`.
3. Add a focused PlayMode test that proves custom mount components can remap non-standard slot names.
4. If future weapons need authored defaults for save/load or preset content, store them in weapon data, not prefab mesh presence.

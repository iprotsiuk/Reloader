# Weapon View Attachment Runtime [v0.1]

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.
> **Purpose:** Runtime contract for weapon view prefabs and attachment mesh mounting.

## Intent [v0.1]

Weapon view prefabs must spawn as empty runtime shells. They expose named mount points through explicit serialized references, and runtime systems attach visuals based on weapon state rather than whatever art happened to be authored into the prefab.

Target outcome:
- Rifle spawns with no runtime attachments mounted.
- Equipped attachment state lives in `WeaponRuntimeState`.
- Runtime view only reflects that state.
- Future attachment types add new slot definitions instead of new controller heuristics.

## Implemented Problems Encountered [v0.1]

This refactor was triggered by live Kar98k scope mounting regressions:

| Problem | Where it showed up | Why it happened |
|---|---|---|
| Editor fallback optic path activated | `AttachmentManager` warning logs | Real optic instantiation path was hardened around `UnityEngine.Object` handling instead of staying on the typed prefab path |
| Optic mount failed with `InvalidCastException` / unsupported `UnityEngine.Object` | `AttachmentManager.EquipOptic(...)` | Main path depended on reflection and generic object instantiation quirks instead of a direct typed contract |
| Scope anchor fell back to optic root | `AttachmentManager` warning logs | Fallback optic/view path did not guarantee an authored `SightAnchor` |
| Runtime rifle spawned with attachment art already present | MainTown / IndoorRange weapon view bindings | Kar98k was still bound to the third-party `WWII_Recon_A` prefab instead of the empty `RifleView` runtime shell |
| Attachment state was inferred from visuals | `PlayerWeaponController` | Controller seeded state from authored child names and destroyed matching visuals at runtime |

## Rejected / Temporary Attempts [v0.1]

The following approaches were tried or present during debugging and are intentionally not the long-term contract:

- Reflection-driven `AttachmentManager` invocation from `PlayerWeaponController`
- Name-based discovery of `ScopeSlot`, `OpticSlot`, `WWII_Recon_A_Sight`, `MuzzleAttachmentSlot`, and similar transforms
- Creating attachment slots from authored visuals during runtime
- Seeding runtime attachment state from whichever compatible prefab name already existed in the view
- Editor-only optic prefab fallback to mask failed real-asset mounting

These approaches hid source-of-truth problems and made healthy runtime views behave differently across scenes.

## Runtime Contract [v0.1]

Runtime weapon views must expose a single mount descriptor component with:

- `MuzzleFirePoint`
- `IronSightAnchor`
- `MagazineSocket`
- `MagazineDropSocket`
- attachment slot entries keyed by `WeaponAttachmentSlotType`

Current required attachment slots:
- `Scope`
- `Muzzle`

Future slot expansion is expected for:
- `Magazine`
- `Bipod`
- `Trigger`
- `Slide`
- other weapon-specific upgrade points

The contract rule is stable:
- add a new slot type
- add the matching slot entry to the runtime view prefab
- keep runtime state and swap logic data-driven
- do not add more controller-side transform discovery heuristics

## Agent Extension Workflow [v0.1]

Future agents extending weapons or attachments must follow this workflow:

1. Bind the weapon item id to one explicit first-person runtime view prefab.
2. Add or update `WeaponViewAttachmentMounts` on that prefab so every required slot/reference is serialized explicitly.
3. Keep the prefab visually empty for runtime attachment slots.
4. Add or update attachment definition assets so each attachment item id resolves to one explicit mount prefab.
5. Extend slot-driven runtime ownership instead of adding new transform-name heuristics or fallback lookups.
6. Add pose tuning through `WeaponViewPoseTuningHelper` base pose plus per-attachment overrides when ADS differs by optic/attachment.
7. Verify the in-hand spawned weapon, mount success path, mount failure path, and no-fallback behavior.

This is mandatory for:

- new rifles
- new pistols
- new scopes / optics
- new muzzle devices
- new magazines
- new slides
- new triggers
- new bipods
- any future weapon upgrade family

Do not treat Kar98k as a special-case template beyond being the first complete example.

## Ownership Boundaries [v0.1]

`PlayerWeaponController`
- owns equipped weapon state
- spawns the runtime view prefab
- resolves attachment definitions from item ids
- forwards mount requests into runtime owners

`AttachmentManager`
- owns `Scope` and `Muzzle` slot content
- destroys previous mounted content for those slots
- instantiates the new attachment prefab under the explicit slot
- exposes the active optic definition and active sight anchor

Weapon view prefab
- exposes only mount points and base weapon art
- does not encode runtime attachment state

## Migration Notes [v0.1]

Kar98k runtime wiring must point to `Assets/_Project/Weapons/Prefabs/RifleView.prefab`, not `Assets/Low Poly Weapon Pack 4_WWII_1/Prefabs/Weapons/WWII_Recon_A.prefab`, in:

- `PlayerRoot_MainTown.prefab`
- `MainTown.unity`
- `IndoorRangeInstance.unity`
- `FPArmsTuning.unity`
- `MainTownCombatWiring.cs`

This keeps the runtime rifle empty on spawn and removes the need to strip third-party authored scope art during play.

## Verification Focus [v0.1]

Any change to this flow should re-check:

- real asset-backed Kar98k scope mount succeeds
- mounted optic resolves a deterministic `SightAnchor`
- initial rifle view spawns without pre-mounted scope visuals
- `WeaponRuntimeState` remains the only attachment source of truth
- 1x optics do not disable pose tuning unless scoped ADS is actually required
- new weapons/attachments follow the same explicit view-prefab + explicit-slot + explicit-definition contract without introducing fallback behavior

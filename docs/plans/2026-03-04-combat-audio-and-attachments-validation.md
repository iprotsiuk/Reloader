# Combat Audio + Attachments Validation

Status: Implemented on branch `feat/combat-audio-attachments-2026-03-04` as of 2026-03-04 (America/Los_Angeles).

Plan source: `docs/plans/2026-03-04-combat-audio-and-attachments-implementation-plan.md`

## Task Status

| Task | Status | Evidence |
|---|---|---|
| Task 1 - Import + catalog external combat audio assets | Completed | `40b7b5b` |
| Task 2 - Weapon combat audio runtime emitter | Completed | `e837df7`, follow-up hardening `6f2555f`, `6159b0f` |
| Task 3 - Muzzle attachment definition + runtime hooks | Completed | `f082ba3`, follow-up correctness `6f2555f` |
| Task 4 - Detachable magazine runtime visuals | Completed | `504759e` |
| Task 5 - Scope attachment ADS hot-swap integration | Completed | `478ff05` |
| Task 6 - Footstep + impact audio routing | Completed | `6b634ad`, runtime-source hardening `6159b0f` |
| Task 7 - Prefab + scene demo wiring | Completed (prefab wiring landed; scene-level runtime already consumes prefab updates) | `215b3b5` |
| Task 8 - Final verification + docs sync | Completed with known external blocker | `78fdb6c`, `6159b0f`, this docs update |

## Commit Log (this feature)

- `40b7b5b` feat(audio): import and catalog combat sfx assets
- `e837df7` feat(weapons): add combat audio emitter for fire and reload
- `f082ba3` feat(attachments): add data-driven muzzle attachment runtime
- `78fdb6c` docs: sync combat audio and attachment runtime contracts
- `6f2555f` Fix weapon runtime bridges and accurate muzzle equip failures
- `b1516c9` Remove unrelated staged changes from weapon runtime fix commit
- `6b634ad` feat(audio): add footstep and impact audio routing
- `504759e` feat(attachments): add detachable magazine reload visuals
- `478ff05` feat(ads): integrate scope attachment hot-swap with mask/zoom
- `215b3b5` chore(weapons): wire combat audio and attachment demo prefabs
- `03ed9c5` test(weapons): decouple combat emitter test from catalog dependency
- `6159b0f` fix(audio): bootstrap default catalog and runtime impact router
- `41e2a61` fix(audio): remove gunshot clips from reload audio mappings
- `43667f1` fix(audio): bootstrap footsteps runtime and stabilize one-shot playback
- `09c86d1` fix(attachments): skip unsafe prefabs with missing scripts at runtime
- `5e4127b` fix(audio): prevent emitter host transform movement during one-shot playback (shoot jump regression)
- `b54d15e` fix(weapons): resolve emitter catalog and attachment bridge review regressions
- `a3473ae` fix(audio): stabilize clip selection and tighten combat catalog pools
- `6f59041` chore(audio): prune unreferenced sfx and add curation script
- `d06bbae` fix(audio): make weapon fire clip stable per weapon by default
- `16bea33` fix(audio,weapons): make clip selection deterministic and address review regressions
- `1be12bc` fix(audio): rebind footstep router to active mover with regression test
- `43c4218` fix(scripts): make audio curation clip-size lookup portable across BSD and GNU stat
- `f34083c` test(audio): return from footstep rebind coroutine
- `1a72f2b` fix(audio): restore generic collections import for combat catalog

## Post-Plan Project Hygiene (Imported Asset Curation)

Large imported packs were curated to reduce demo/package bloat without touching protected vendor roots (`Assets/ThirdParty/**`, `Assets/Infima Games/**`).

- Script added: `scripts/assets/curate_unused_imported_assets.sh`
- Curation mode: GUID-reference based from active project assets/settings (excluding candidate pack self-references)
- Candidate roots:
  - `Assets/Cartoon_Texture_Pack`
  - `Assets/Free Wood Door Pack`
  - `Assets/YughuesFreeConcreteMaterials`
  - `Assets/Low Poly Weapon Pack 4_WWII_1`
  - `Assets/Low Poly Optic Pack 1`
  - `Assets/LowPoly Environment Pack`
  - `Assets/EasyRoads3D scenes`
- Safety exclusions: `Resources/**`, `StreamingAssets/**`, `Editor/**`, script/asmdef/shader/native plugin extensions.

Applied result:

- Moved out of project: `1042` files
- Reclaimed size: `1,660,812,529` bytes (~`1.55 GiB`)
- External dump: `/Users/ivanprotsiuk/Documents/SOUNDS/project-asset-dump/2026-03-04-imported-packs-prune`
- Manifest: `tmp/asset-curation-manifest-2026-03-04-164113.csv`

Additional conservative `ThirdParty` demo cleanup:

- Script added: `scripts/assets/curate_thirdparty_demo_folders.sh`
- Scope: demo/example/profile/test folders only, removed only when no external references exist.
- Moved: `4` folders
- Reclaimed size: `59,601,027` bytes (~`56.8 MiB`)
- External dump: `/Users/ivanprotsiuk/Documents/SOUNDS/project-asset-dump/2026-03-04-thirdparty-demo-prune`
- Manifest: `tmp/thirdparty-demo-curation-manifest-2026-03-04-164736.csv`

## Verification Evidence

### Guardrails

| Command | Result | Evidence |
|---|---|---|
| `bash scripts/verify-docs-and-context.sh` | Failed (pre-existing unrelated guardrail) | `RuntimeDroppedObjectPersistenceTracker.cs` placement policy violation under inventory world scripts |
| `bash scripts/verify-extensible-development-contracts.sh` | Passed | `All extensible development contract checks passed.` |

### Unity PlayMode runs (batchmode)

All targeted and aggregate batchmode runs were blocked by an existing open Unity editor session on the same project path.

Common failure text:

- `Aborting batchmode due to fatal error:`
- `It looks like another Unity instance is running with this project open.`
- `Multiple Unity instances cannot open the same project.`

Commands attempted included:

- `./scripts/run-unity-tests.sh playmode "Reloader.Weapons.Tests.PlayMode.DetachableMagazineRuntimePlayModeTests" ...`
- `./scripts/run-unity-tests.sh playmode "Reloader.Weapons.Tests.PlayMode.ScopeAttachmentAdsIntegrationPlayModeTests" ...`
- `./scripts/run-unity-tests.sh playmode "Reloader.Weapons.Tests.PlayMode.PlayerWeaponControllerPlayModeTests" ...`
- `./scripts/run-unity-tests.sh playmode "Reloader.Audio.Tests.PlayMode.FootstepAndImpactAudioPlayModeTests" ...`
- `Unity -batchmode -runTests -testFilter "Reloader.Weapons.Tests.PlayMode|Reloader.UI.Tests.PlayMode" ...`

## PR Review Thread Resolution Log

PR: https://github.com/iprotsiuk/Reloader/pull/21

| Thread | Status | Resolution |
|---|---|---|
| https://github.com/iprotsiuk/Reloader/pull/21#discussion_r2886425108 | Resolved | Runtime emitter auto-resolution + auto-add path (`6f2555f`) |
| https://github.com/iprotsiuk/Reloader/pull/21#discussion_r2886450569 | Resolved | Muzzle bridge now equips discovered/fallback definition (`6f2555f`) |
| https://github.com/iprotsiuk/Reloader/pull/21#discussion_r2886450571 | Resolved | `AttachmentManager.EquipMuzzle` runtime failure reporting corrected (`6f2555f`) |
| https://github.com/iprotsiuk/Reloader/pull/21#discussion_r2886519681 | Resolved | Default catalog bootstrap for runtime-created emitters (`6159b0f`) |
| https://github.com/iprotsiuk/Reloader/pull/21#discussion_r2886519682 | Resolved | Runtime `ImpactAudioRouter` source guaranteed from `WeaponProjectile` (`6159b0f`) |
| https://github.com/iprotsiuk/Reloader/pull/21#discussion_r2886627784 | Resolved | Preserve custom emitter catalog when controller discovers emitter dynamically (`b54d15e`) |
| https://github.com/iprotsiuk/Reloader/pull/21#discussion_r2886627788 | Resolved | Initialize bridged detachable magazine runtime with transferred/default attachment (`b54d15e`) |
| https://github.com/iprotsiuk/Reloader/pull/21#discussion_r2886627792 | Resolved | Keep runtime muzzle state synchronized on rejected equip by clearing mounted runtime attachment (`b54d15e`) |
| https://github.com/iprotsiuk/Reloader/pull/21#discussion_r2886682598 | Resolved | `CombatAudioCatalogResolver` no longer caches non-null caller-provided catalogs globally; added resolver regression test (`16bea33`) |
| https://github.com/iprotsiuk/Reloader/pull/21#discussion_r2886682601 | Resolved | Runtime bridge now attempts existing runtime `_defaultAttachment` before giving up when no fallback definition assets resolve (`16bea33`) |
| https://github.com/iprotsiuk/Reloader/pull/21#discussion_r2886748129 | Resolved | Reload cancel path now re-notifies view magazine insertion to restore detachable mag visual (`16bea33`) |
| https://github.com/iprotsiuk/Reloader/pull/21#discussion_r2886812814 | Resolved | Footstep router now prefers active mover rebinding over stale inactive cache; regression coverage added (`1be12bc`) |
| https://github.com/iprotsiuk/Reloader/pull/21#discussion_r2887020916 | Resolved | Restored `Wall_Brick_Basecolor_B/C` textures (+ original GUID metas) used by IndoorRange wall materials after asset-prune regression (`pending commit`) |

## Known Residual Risks

- Batchmode test XML evidence is still missing for this branch due Unity-instance lock; rerun required after closing the active editor project.
- `scripts/verify-docs-and-context.sh` currently fails because of a pre-existing persistence script location issue not introduced by this feature branch.
- Existing vendor-derived weapon visual prefabs still report missing script references in play mode; runtime attachment modules now skip unsafe prefab instantiation paths to reduce reload/fire crash risk from those assets.
- Unity MCP test runner is currently reporting `tests_running` busy state and blocking new jobs; rerun the new emitter-regression playmode test after runner recovers:
  - `Reloader.Weapons.Tests.PlayMode.WeaponCombatAudioEmitterPlayModeTests.EmitWeaponFire_DoesNotMoveEmitterHostTransform`
- Deterministic traceability mode now removes random clip selection across fire/reload/impact/footstep catalog APIs; if per-shot variation is desired later, it should be reintroduced explicitly as opt-in with deterministic seed logging.
- Audio asset curation moved unused SFX out of project to external dump path:
  - `/Users/ivanprotsiuk/Documents/SOUNDS/project-audio-dump/2026-03-04-153834`
  - Curated by `scripts/audio/curate_project_audio_assets.sh` (dry-run default, `--apply` to move).

## Ongoing Kar98k Iteration Notes (2026-03-04 late session)

- `22724f3c` fixes scoped ADS zoom input to use `IPlayerInputSource.ConsumeZoomInput()` (Input System compatible) and removes runtime dependency on legacy `UnityEngine.Input.mouseScrollDelta`.
- `WeaponViewPoseTuningHelper` now preserves inspector-authored hip/ADS values by default and only seeds from current pose when `Seed Offsets From Current Pose On Equip` is explicitly enabled.
- `22b6eb2d` hardens restore + ADS behavior:
  - `ApplyRuntimeAttachments` now clears all attachment slots before applying restored snapshot keys, preventing stale attachments from surviving partial/legacy saves.
  - Scoped ADS bridge now only overrides pack FOV when an active optic is actually equipped (`AttachmentManager.ActiveOpticDefinition != null`), preserving baseline ADS FOV without scope.
- `1808348d` hardens attachment removal + PR #22 review regressions:
  - Kar98k scope/muzzle remove now clears authored attachment visuals using compatible attachment prefab names (not only generic keyword matching), fixing cases where `Remove` kept scope mesh visible on the equipped weapon view.
  - Added PlayMode coverage for authored scope visual teardown in `PlayerWeaponControllerPlayModeTests.TrySwapEquippedWeaponAttachment_RemoveScope_DestroysAuthoredScopeVisual`.
  - Closed PR #22 unresolved review items by:
    - raising inventory changed events after successful attachment swaps,
    - destroying stale `AdsStateController` bridge on equipped weapon view teardown,
    - null/whitespace guarding `WeaponRegistry.TryGetWeaponDefinition`,
    - restoring attachments during travel restore when ballistic chamber payload is unavailable.
- `2026-03-05 hotfix` resolves two Kar98k follow-up regressions from live playtest:
  - Scope swap apply no-op in `MainTown` caused by scene-level `PlayerWeaponController._attachmentItemMetadata` override set to empty array; scene now mirrors prefab metadata entries for Kar98k scope/muzzle definitions.
  - Severe FPS drop when selecting non-weapon belt items (for example scope attachment) caused by per-frame weapon registry miss path hitting editor asset scans; `WeaponRegistry` now short-circuits non-weapon IDs and caches negative lookup misses.
  - Attachment swaps now fail-fast with rollback if runtime mount is not possible (for example missing scope slot on active view), so inventory/state are not consumed when visual/runtime apply fails.
- `2026-03-05 scene parity hotfix` removes location-dependent attachment behavior:
  - `IndoorRangeInstance` now maps `weapon-kar98k` to the same WWII Kar98k view prefab used by `MainTown` (removed stale AR `RifleView` mapping).
  - `IndoorRangeInstance` now includes Kar98k attachment metadata wiring (`att-kar98k-scope-remote-a`, `att-kar98k-muzzle-device-c`) on `PlayerWeaponController`.
  - Added `EventSystem` + `InputSystemUIInputModule` to `IndoorRangeInstance` so TAB right-click attachment context works in range scene.
  - Broadened animation event receiver auto-wiring to cover `PlayerArms` animator hosts and added explicit receiver ensure in `PlayerWeaponAnimationBinder` to prevent `OnAnimationEndedHolster` no-receiver errors.
- `2026-03-05 attachment mount recovery hotfix`:
  - `PlayerWeaponController` now auto-creates synthetic `ScopeSlot` / `MuzzleAttachmentSlot` runtime anchors from authored attachment visuals when source prefab lacks explicit slot transforms.
  - On first equip, runtime state now seeds attachment IDs from authored visuals if slots were unset, preventing default prefab attachments from disappearing immediately on pickup.
- `2026-03-05 simplification`:
  - Kar98k default view/source switched from `WWII_Recon_A_PreSet` to base `WWII_Recon_A` (no pre-mounted scope/muzzle) across `StarterRifle`, `PlayerRoot_MainTown`, `MainTown`, and `IndoorRangeInstance` mappings, so attachment lifecycle is fully player-driven.
- `2026-03-05 scope-visual follow-up`:
  - Fixed `Wire MainTown Combat Setup` / content builder fallback to stop re-introducing preset-scoped Kar98k by switching editor constants to base `WWII_Recon_A.prefab`.
  - `PlayerWeaponController` now aligns mounted scope/muzzle instance layers to the runtime slot layer recursively after equip, so optics render in the held viewmodel camera pass (previously FOV changed but optic mesh could remain culled on Default layer).
  - Kar98k scope fallback anchor resolution now uses `WWII_Recon_A_Sight` when explicit `ScopeSlot` / `OpticSlot` / `SightAnchor` mounts are absent on the base prefab.

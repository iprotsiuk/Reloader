# Weapon Pose Framework Manager Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to execute this plan task-by-task.

**Goal:** Restore clean targeted compilation, land the remaining scoped housing stabilization fix, implement real peripheral downscale plus blur for scoped PiP ADS, and keep PR #47 moving with small verified commits.

**Architecture:** Treat the effort as three tightly scoped runtime-local slices: first restore the player/UI compile seam so targeted verification is available, then finish the scoped root-pose stabilization path, then add a URP 17 renderer feature for peripheral-only blur/downscale while preserving PiP and viewmodel sharpness. Manage all implementation through isolated subagents and targeted tests only.

**Tech Stack:** Unity 6, URP 17.3.0, Unity Test Framework, Unity MCP, GitHub CLI, project-local runtime/test seams under player, UI, and weapons.

---

## Constraints

- Do not touch unrelated local edits in `Reloader/Assets/_Project/World/Scenes/MainTown.unity`.
- Do not touch unrelated local edits in `Reloader/Assets/_Project/NPCs/Prefabs/NpcFoundation.prefab`.
- Do not run full regression unless a focused failure proves coupling.
- Do not make PiP blurry.
- Do not make the viewmodel blurry.
- Preserve `Scoped PiP Resolution %` semantics: `100 = native/current game baseline`, `400 = 4x edge scale`.
- Use the existing `Peripheral Blur` slider for the new blur/downscale path.
- Resolve already-resolved PR comments in GitHub as they are confirmed.

## Manager Status

- [x] Repo instructions loaded from `.cursor/agents.md` and relevant runtime-local routers.
- [x] Old detached worker worktrees pruned.
- [x] Unity started and editor connection confirmed.
- [x] Console errors triaged and assigned.
- [x] UI compile seam fixed.
- [x] Housing stabilization fix integrated, verified, committed.
- [x] ADS sensitivity / PiP precision slice verified and committed.
- [ ] Peripheral blur/downscale feature implemented, verified, committed.
- [ ] PR #47 comments reviewed, addressed, and resolved where appropriate.

## Workstreams

### Task 1: Restore targeted compile and console cleanliness

**Scope:** unblock verification before any new feature slice proceeds.

**Files likely in scope:**
- `Reloader/Assets/_Project/UI/Tests/PlayMode/EscMenuUiToolkitPlayModeTests.cs`
- `Reloader/Assets/_Project/UI/Scripts/Toolkit/EscMenu/EscMenuUiState.cs`
- other adjacent ESC menu settings files only if the signature mismatch requires it

**Execution notes:**
- Start Unity and collect current console errors first.
- Fix the known `EscMenuUiState.Create(...)` seam around `lookSensitivity`.
- Re-run only the smallest compile-relevant / ESC menu focused verification after the fix.

**Target verification:**
- targeted compile succeeds
- focused ESC menu PlayMode tests or nearest test filter succeeds

**Owner:** Worker A (`Bacon`)

### Task 2: Integrate and verify scoped housing stabilization

**Scope:** finish the remaining scoped root-pose hold fix without reopening the old scoped vibration bug.

**Files likely in scope:**
- `Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponViewPoseTuningHelper.cs`
- `Reloader/Assets/_Project/Core/Tests/EditMode/WeaponViewPoseTuningHelperScopedRootPoseTests.cs`

**Execution notes:**
- Pull in the already-developed worker patch from the current branch worktree state.
- Keep the change local to the scoped root-pose hold path.
- Verify with the focused edit-mode regression first, then the nearest scoped stabilization seam only if needed.

**Target verification:**
- focused scoped root-pose regression passes
- no new compile/runtime errors introduced by the slice

**Owner:** Worker H (`Lorentz`) completed in `c453ae71`

### Task 3: Verify the ADS sensitivity / PiP precision slice in live Unity

**Scope:** runtime validation of the already-landed precision architecture and settings seam.

**Files likely in scope if tuning is needed:**
- `Reloader/Assets/_Project/Player/Scripts/PlayerInputReader.cs`
- `Reloader/Assets/_Project/Player/Scripts/PlayerLookController.cs`
- `Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs`
- `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- `Reloader/Assets/_Project/UI/Scripts/Toolkit/EscMenu/*.cs`
- nearest playmode tests already touched on branch

**Execution notes:**
- Only proceed after Task 1 restores compile.
- Run focused PlayMode filters for player look, ESC menu, and scope PiP precision.
- Perform in-editor feel check for hipfire, ADS, high-mag PiP precision, and FOV scaling restoration.
- Tune only if the current `0.05` baseline proves materially off.

**Target verification:**
- focused player look tests
- focused ESC menu tests
- focused scope PiP precision tests
- manual runtime check recorded

**Owner:** Worker P (`Hilbert`) completed in `d2cc025a`

### Task 4: Implement peripheral-only downscale + blur for scoped PiP ADS

**Scope:** add real blur/downscale on peripheral world only, keeping PiP and viewmodel sharp.

**Files likely in scope:**
- `Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs`
- `Reloader/Assets/Game/Weapons/Runtime/PeripheralScopeEffects.cs`
- `Reloader/Assets/Game/Weapons/Runtime/PeripheralScopeScreenMask.cs`
- new URP feature/pass files under a project-local runtime/rendering path
- `Reloader/Assets/Settings/PC_Renderer.asset`
- `Reloader/Assets/Settings/Mobile_Renderer.asset`

**Execution notes:**
- Use a URP 17 RenderGraph-era `ScriptableRendererFeature` + `ScriptableRenderPass`.
- Drive intensity from the existing `Peripheral Blur` slider.
- Make the effect active only during scoped PiP ADS.
- Keep the viewmodel and PiP excluded from blur/downscale.

**Target verification:**
- focused runtime / playmode coverage where practical
- Unity manual visual verification in scoped ADS
- renderer assets wired correctly on PC and Mobile

**Owner:** Worker B (`Ampere`)

### Task 5: PR #47 comment handling and branch hygiene

**Scope:** keep PR feedback moving alongside implementation without losing context.

**Execution notes:**
- Fetch current PR review comments after each landed slice or when new feedback appears.
- Resolve comments in GitHub only after the corresponding fix is verified.
- Keep commits small and scoped to one slice.
- Push commits to `codex/weapon-pose-framework`.

**Target verification:**
- comment state in GitHub matches completed work
- no unrelated files included in commits

**Owner:** Explorer B (`Maxwell`)

## Progress Log

### 2026-03-19

- Initialized manager plan.
- Pruned stale detached worker worktrees left from older runs.
- Confirmed current worktree still contains in-progress precision, UI, and housing edits plus unrelated dirty scene/prefab files that must remain untouched.
- Fixed the ESC menu compile blocker in `EscMenuUiToolkitPlayModeTests.cs` and verified the focused PlayMode slice passed.
- Committed the compile-restoration slice as `c4694947` (`fix: restore esc menu playmode compile`).
- Assigned fresh workers for compile restoration, PR review intake, and blur readiness analysis.
- PR #47 review intake completed at `2026-03-19 14:06:14 PDT`.
- Recorded still-actionable review threads:
  - `PRRT_kwDORWf7vs51ZGMa` (`PlayerWeaponController.cs` scope unequip early-return bug)
  - `PRRT_kwDORWf7vs51Z8Id` (`Reloader.Game.Weapons.asmdef` missing `Unity.ugui` reference)
  - `PRRT_kwDORWf7vs51jp9W` (`PlayerWeaponController.cs` destroys `_peripheralScopeEffectsRuntimeBridge`)
- Recorded already-likely-resolved review threads ready for GitHub resolution after corresponding verification:
  - `PRRT_kwDORWf7vs51XE-h`
  - `PRRT_kwDORWf7vs51XZdt`
  - `PRRT_kwDORWf7vs51XZdu`
  - `PRRT_kwDORWf7vs51Yga7`
- Deferred judgment thread `PRRT_kwDORWf7vs51Y7yA` until fresh housing verification evidence exists.
- Blur readiness analysis completed:
  - confirmed current branch still uses `ScalableBufferManager.ResizeBuffers(...)` fallback rather than real blur
  - confirmed existing slider flow already reaches `PeripheralScopeEffects.SetState(...)`
  - confirmed current camera layering can keep PiP and viewmodel sharp if the pass runs on the base camera before overlay composition
  - recommended implementation sequence is new URP 17 RenderGraph renderer feature/pass plus runtime blur state, with renderer asset hookup done in Unity after compile
  - explicit merge hotspot is limited to `AdsStateController.cs`, so blur remains sequenced after compile/precision stabilization
- Dispatched fresh implementation workers:
  - `Lorentz` for scoped housing/root-pose stabilization
  - `Hilbert` for ADS sensitivity / PiP precision verification and minimal tuning
- Housing stabilization landed as `c453ae71` (`fix: reassert scoped root pose every stable frame`).
- Focused housing verification recorded:
  - live-editor Unity MCP run for `Reloader.Core.Tests.EditMode.WeaponViewPoseTuningHelperScopedRootPoseTests`
  - result: `3/3` tests green
  - `git diff --check` clean on touched files
- ADS sensitivity / PiP precision slice landed as `d2cc025a` (`feat: add scoped ADS sensitivity and precision controls`).
- Focused precision verification recorded:
  - `bash scripts/run-unity-tests.sh playmode EscMenuUiToolkitPlayModeTests tmp/test-results-escmenu.xml tmp/test-escmenu.log`
  - `bash scripts/run-unity-tests.sh playmode LookInputNormalization_NormalizeLookDelta_AppliesMouseScale tmp/test-results-look-mouse.xml tmp/test-look-mouse.log`
  - `bash scripts/run-unity-tests.sh playmode LookInputNormalization_NormalizeLookDelta_DoesNotScaleGamepadInput tmp/test-results-look-gamepad.xml tmp/test-look-gamepad.log`
  - `bash scripts/run-unity-tests.sh playmode PlayerLookController_Tick_PipScopedAds_CanDisableFovSensitivityScaling tmp/test-results-look-pipfov.xml tmp/test-look-pipfov.log`
  - `bash scripts/run-unity-tests.sh playmode RealKar98kOpticAsset_PipPrecisionCurve_BridgesDedicatedScopedMultiplier tmp/test-results-scope-pipprecision.xml tmp/test-scope-pipprecision.log`
  - `git diff --check` clean
- Residual precision risk remains manual runtime feel only; targeted automated coverage passed and baseline `0.05` was accepted.
- Dispatched `Ampere` for the scoped peripheral blur/downscale implementation slice, including the adjacent `_peripheralScopeEffectsRuntimeBridge` PR fix.
- Already-fixed PR threads were resolved in GitHub during the blur workstream pass; remaining actionable PR threads are the blur/peripheral-bridge thread and the separate scope-unequip thread.
- Blur implementation hit a narrow technical blocker:
  - the detached worker workspace did not have the local URP package sources it wanted for reference
  - no renderer code or asset hookup landed from that pass yet
  - next step is a focused URP 17 implementation-pattern lookup from the main project package sources or official docs, then immediate restart of the blur worker
- URP 17 implementation-pattern lookup completed from the main project package cache:
  - local sample/reference files confirmed under `Reloader/Library/PackageCache/com.unity.render-pipelines.universal@a6e30d1f59f5/...`
  - confirmed project should use `ScriptableRendererFeature.Create()` + `AddRenderPasses(...)` plus `ScriptableRenderPass.RecordRenderGraph(RenderGraph, ContextContainer)`
  - confirmed `UniversalResourceData`, `UniversalCameraData`, `requiresIntermediateTexture`, `renderGraph.GetTextureDesc(...)`, and `RenderGraphUtils.BlitMaterialParameters` are the intended APIs
  - confirmed `RenderPassEvent.BeforeRenderingPostProcessing` remains the correct injection point
  - confirmed renderer-feature hookup should still be done in Unity rather than by hand-editing renderer YAML
- Restarted blur implementation with the exact local URP 17 sample paths and worker-ready implementation brief.
- Current blur branch-state patch now includes:
  - renderer/runtime code under `Reloader/Assets/Game/Weapons/Runtime/Rendering/`
  - shader under `Reloader/Assets/Game/Weapons/Shaders/`
  - runtime integration in `AdsStateController`, `PeripheralScopeEffects`, `PeripheralScopeScreenMask`, and `PlayerWeaponController`
  - focused tests already passing for peripheral bridge preservation, peripheral blur runtime-state updates, and scope unequip
- Remaining blur blocker narrowed to Unity-only work:
  - import new files / generate meta files
  - attach the renderer feature to `PC_Renderer.asset` and `Mobile_Renderer.asset`
  - perform real scoped-ADS visual verification
  - commit the finished slice
- Dispatched `Godel` to finish the Unity hookup, visual verification, and commit.
- Unity import/hookup finished:
  - new renderer/shader files were imported and `.meta` files generated
  - renderer feature is wired into both `PC_Renderer.asset` and `Mobile_Renderer.asset`
  - renderer assets reimported cleanly
- Remaining blur gate is now only credible live scoped-ADS visual verification.
- Dispatched `Erdos` to find an existing in-project path to a real scoped PiP ADS state for final visual confirmation without adding debug code.
- URP 17 implementation references were recovered from the main project package cache:
  - `Library/PackageCache/com.unity.render-pipelines.universal@a6e30d1f59f5/Samples~/URPRenderGraphSamples/BlitWithMaterial/BlitAndSwapColorRendererFeature.cs`
  - `Library/PackageCache/com.unity.render-pipelines.universal@a6e30d1f59f5/Samples~/URPRenderGraphSamples/BlitWithFrameData/BlitRendererFeature.cs`
  - `Library/PackageCache/com.unity.render-pipelines.universal@a6e30d1f59f5/Samples~/URPRenderGraphSamples/UnsafePass/UnsafePassRenderFeature.cs`
  - `Library/PackageCache/com.unity.render-pipelines.universal@a6e30d1f59f5/Runtime/FrameData/UniversalResourceData.cs`
  - `Library/PackageCache/com.unity.render-pipelines.universal@a6e30d1f59f5/Runtime/FrameData/UniversalCameraData.cs`
  - `Library/PackageCache/com.unity.render-pipelines.core@d6dfd1e235d5/Runtime/RenderGraph/RenderGraphUtilsBlit.cs`
- Restart brief for blur now has the concrete URP 17 rules:
  - use `RecordRenderGraph(...)` with `UniversalResourceData` and early-out when `isActiveTargetBackBuffer` is true
  - read/write camera color through transient `TextureHandle`s and `RenderGraphUtils.AddBlitPass(...)`
  - enqueue only for base game cameras, not overlay / preview / reflection / scene cameras
  - set the pass event to `BeforeRenderingPostProcessing`
- Blur/peripheral path is now visually accepted by the user:
  - blur no longer contaminates the scoped PiP image
  - peripheral downscale is active and ADS performance recovered to acceptable levels
  - blur kernel was adjusted away from ghosted/double-vision artifacts toward a smoother low-res peripheral image
- Remaining open issue is the last scoped ADS micro-movement:
  - user reports the whole rifle/scope/hands stack shifts on the smallest possible turn input, not just the PiP image
  - screenshots indicate the symptom tracks tiny point-of-aim yaw steps while scoped
  - user explicitly believes this is an architectural/update-order seam, not a tuning/constants problem
- Runtime MCP probing is not trustworthy enough to decide root cause:
  - live probing did confirm `PlayerRoot/CameraPivot/Camera` stays identity-relative to `CameraPivot` during tiny yaw change, so the simplest `WorldCamera`-relative drift hypothesis is weakened
  - however repeated MCP-for-Unity serializer/socket errors (`TransformHandle` null, `ScriptableRenderer.cameraDepth` serialization failure, disposed `NetworkStream`) make transform time-series capture unreliable
  - manager decision: stop treating live MCP transform sampling as authoritative evidence for this issue
- New evidence-gathering strategy:
  - static ownership/update-order audit of `WeaponViewPoseTuningHelper`, `WeaponAimAligner`, `PlayerWeaponController`, `PlayerLookController`, and `PlayerCameraDefaults`
  - parallel PR/history audit for prior review concerns touching scoped housing movement, eye relief, image shift, or aligner behavior
- PR/history audit result:
  - prior scoped-vibration regression on PR #47 was explicitly attributed to `WeaponAimAligner` continuously rewriting a held ADS pose every `LateUpdate`
  - that history matters because the current symptom again looks like a held scoped pose still being actively recomputed/written every frame, only now likely from the root-pose helper path rather than the old aligner path
- Static ownership audit result:
  - best current root-cause hypothesis is that stable scoped ADS freezes only the inner `AdsPivot` solve, while the outer rifle view root remains a live writer in `WeaponViewPoseTuningHelper.LateUpdate()`
  - ownership chain indicates the weapon view is mounted under `CameraPivot/WeaponPresentationRoot`, `PlayerLookController` still updates the parent presentation chain every frame, and `WeaponAimAligner` no longer continuously compensates once hold mode engages
  - this explains why the whole rifle/scope body moves on tiny ADS turns rather than a PiP-only image shift
  - VSync is now considered a low-confidence explanation relative to the transform-ownership seam
- Next execution step opened:
  - dispatch a single implementation worker to make stable scoped ADS a true single-owner state for the outer rifle root
  - worker scope is limited to `WeaponViewPoseTuningHelper`, `PlayerWeaponController`, and the focused scoped-root-pose regression tests
  - targeted verification only; no blur/peripheral code changes and no unrelated scene/prefab churn
- Scoped ownership follow-up landed and pushed:
  - commit `87f2f301` (`Fix scoped ADS root pose ownership`)
  - targeted red/green evidence recorded on `WeaponViewPoseTuningHelperScopedRootPoseTests.LateUpdate_StableScopedAds_SnapsOnceThenStopsRewritingRootPose`
  - this fixes a real ADS-only ownership bug but does not close the broader movement issue
- New live user evidence broadened the bug:
  - whole-rifle micro-step movement is visible even outside ADS
  - user screenshots suggest the entire rifle presentation stack moves on tiny turns, with obviously wrong viewmodel placement in the reproduced non-ADS state
  - this lowers confidence in ADS-specific explanations as the full story
- Common-path audit result:
  - `WeaponViewPoseTuningHelper.LateUpdate()` remains the top suspect because it still writes the equipped weapon view root `localPosition` / `localRotation` every frame in both hipfire and ADS
  - hand IK / `WeaponHandRigController` is not the primary seam; it adjusts IK targets/weights rather than owning the rifle root
  - `FpsViewmodelAnimatorDriver` is also not the best common explanation because its locking path is tied to scoped stabilization rather than generic hipfire runtime
  - `PlayerLookController` remains a secondary universal parent-chain writer, but current evidence points to the helper’s root overwrite as the first fix target
- Current active execution step:
  - dispatch a second focused implementation worker to stop or appropriately gate `WeaponViewPoseTuningHelper` as a perpetual runtime root writer in hipfire as well as ADS
  - preserve legitimate authored/tuning/setup behavior while removing the runtime ownership leak
  - require focused regression coverage and targeted verification only

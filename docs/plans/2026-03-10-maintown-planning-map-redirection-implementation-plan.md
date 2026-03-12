# MainTown Planning-Map Redirection Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Redirect `MainTown` from the recent slope-heavy terrain pass into a clearer `2km x 2km` planning map with readable grayboxed districts, added landmarks, distant outer ridges, and preserved scene contracts.

**Architecture:** Keep the existing `MainTownWorldShell`, district roots, landmark roots, and town core, but disable the added slope/ramp masses and reposition the shell to a larger footprint. Add new district and landmark blockouts as simple gray primitive masses under the existing world-shell structure so the scene reads clearly from a zoomed-out editor camera.

**Tech Stack:** Unity 6.3 scene authoring, Unity MCP when stable for optional label helpers, direct `.unity` YAML edits for deterministic scene patching, existing EditMode layout tests, repo docs/context guardrails.

---

### Task 1: Document the redirected planning-map pass

**Files:**
- Create: `docs/plans/2026-03-10-maintown-planning-map-redirection-design.md`
- Create: `docs/plans/2026-03-10-maintown-planning-map-redirection-implementation-plan.md`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Save the approved redirection**

- Record the new direction:
  - no slope focus
  - larger shell
  - more grayboxed districts
  - clearer zoomed-out readability

**Step 2: Update the progress log before scene mutation**

- Add a log entry for the rollback / planning-map pass.

### Task 2: Roll back the recent slope/ramp layer

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Disable or remove the second-pass slope helpers**

- Target:
  - `RidgeNorth_InnerSlope`
  - `RidgeSouth_InnerSlope`
  - `RidgeEast_InnerSlope`
  - `RidgeWest_InnerSlope`
  - `NorthValleyApproach`
  - `EastValleyApproach`
  - `PlayerOverlookApproach`
  - `WaterTowerApproach`
  - `QuarrySouthApproach`
  - `QuarryWestRamp`

**Step 2: Keep the town-core and base landmark contract intact**

- Do not rename or remove required world-shell / runtime objects.

**Step 3: Update the progress log**

- Record the rollback of the slope/ramp layer.

### Task 3: Expand the world shell to `2km x 2km`

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Verify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Enlarge `BasinFloor`**

- Increase the floor footprint to roughly `2000 x 2000`.

**Step 2: Move the outer ridges outward**

- Reposition the existing perimeter ridge masses so they frame the larger footprint.

**Step 3: Preserve test compatibility**

- Keep the scene still satisfying the current minimum scale assertions.

**Step 4: Update the progress log**

- Record the new footprint and ridge positions.

### Task 4: Add readable grayboxed districts and landmark masses

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Add several new grayboxed district masses**

- Add simple blockouts for:
  - industrial yard
  - trailer park
  - service / depot zone
  - utility yard
  - municipal / school block
  - truck stop / roadside zone

**Step 2: Keep district silhouettes obvious**

- Use simple large blocks and pads with clear spacing between them.
- Favor editor readability over realism.

**Step 3: Add or reposition landmark masses as needed**

- Ensure the new districts are easy to identify from above.

**Step 4: Update the progress log**

- Record the added districts and landmark masses.

### Task 5: Add planning-view markers and attempt readable labels

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Add large marker pads / plaques**

- Create broad flat markers for key districts.

**Step 2: Attempt overhead district labels if reliable**

- Prefer in-world readable text if Unity tooling supports it cleanly.
- Fallback to explicit marker-object naming and large differentiated marker geometry.

**Step 3: Update the progress log**

- Record whether text labels were added or whether the fallback marker system was used.

### Task 6: Keep forest presence and re-verify

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Verify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownLayoutEditModeTests.cs`
- Modify: `docs/plans/progress/2026-03-10-maintown-literal-mile-rebuild-progress.md`

**Step 1: Keep or expand forest coverage**

- Preserve the recent added tree density and add more only where the larger shell would otherwise read empty.

**Step 2: Re-run the dedicated layout test**

- Run `Reloader.World.Tests.EditMode.MainTownLayoutEditModeTests` if Unity MCP is healthy.

**Step 3: Re-run repo guardrails**

- `bash scripts/verify-docs-and-context.sh`
- `bash scripts/verify-extensible-development-contracts.sh`
- `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`

**Step 4: Record the final truth**

- Document what passed, what was rolled back, and any remaining editor-visual gaps.

# Shared NPC Dialogue Face Anchor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace runtime-derived dialogue look targets with one shared local-space face point per STYLE archetype so all civilians and vendors resolve to a stable nose/face anchor during dialogue.

**Architecture:** `MainTownNpcAppearanceApplicator` becomes the single owner of dialogue face anchor placement for STYLE rigs by mapping `StyleMaleRoot` and `StyleFemaleRoot` to explicit serialized local-space points. Existing dialogue consumers keep their current flow; only the applicator’s common-path anchor resolution changes, while unsupported layouts still use the generic bounds fallback.

**Tech Stack:** Unity, C#, NUnit EditMode tests, Unity PlayMode tests, existing NPC dialogue/runtime stack.

---

### Task 1: Document and lock the shared-anchor contract

**Files:**
- Create: `docs/plans/2026-03-10-shared-npc-dialogue-face-anchor-design.md`
- Create: `docs/plans/2026-03-10-shared-npc-dialogue-face-anchor.md`

**Step 1: Write the design document**

Capture:
- why the current head-derived target is insufficient
- why a shared archetype-local point is preferred
- that the point must be inside the head volume, approximately nose / center-face level
- that the change applies to civilians and vendors through the shared applicator

**Step 2: Save the implementation plan**

List exact touched files, TDD sequence, verification commands, and cleanup expectations.

**Step 3: Commit**

Run:
```bash
git add docs/plans/2026-03-10-shared-npc-dialogue-face-anchor-design.md docs/plans/2026-03-10-shared-npc-dialogue-face-anchor.md
git commit -m "docs: plan shared npc dialogue face anchor"
```

### Task 2: Write the failing EditMode tests for shared local face points

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/MainTownNpcAppearanceApplicatorEditModeTests.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/MainTownNpcAppearanceApplicatorEditModeTests.cs`

**Step 1: Write the failing tests**

Add two focused tests:
- male STYLE root resolves `DialogueFaceAnchorRuntime` at the configured male local point
- female STYLE root resolves `DialogueFaceAnchorRuntime` at the configured female local point

Each test should:
- build a minimal `VisualRoot` with the relevant STYLE root name
- create the applicator
- invoke the public dialogue focus resolution path when possible
- assert the returned transform exists and matches the exact expected local position

**Step 2: Run test to verify it fails**

Run:
```bash
Unity EditMode: Reloader.NPCs.Tests.EditMode
```

Expected:
- failure because current production code still uses head-bone-derived positioning

**Step 3: Commit**

Run:
```bash
git add Reloader/Assets/_Project/NPCs/Tests/EditMode/MainTownNpcAppearanceApplicatorEditModeTests.cs
git commit -m "test: cover shared dialogue face points"
```

### Task 3: Implement shared local-space face points in the applicator

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/MainTownNpcAppearanceApplicator.cs`

**Step 1: Write minimal implementation**

Add serialized shared local-space fields:
- one for `StyleMaleRoot`
- one for `StyleFemaleRoot`

Update `ResolveDialogueFocusAnchor()` / helper flow to:
- detect `StyleMaleRoot` and `StyleFemaleRoot`
- place/update `DialogueFaceAnchorRuntime` at the configured local point
- remove forward-bias and head-direction-derived logic from the common STYLE path
- preserve the generic bounds fallback for unsupported layouts

**Step 2: Clean up debugging leftovers**

Remove:
- forward-projection code
- obsolete helper assumptions tied to head-facing direction
- any stale comments/assertions that describe “in front of face” behavior

**Step 3: Run test to verify it passes**

Run:
```bash
Unity EditMode: Reloader.NPCs.Tests.EditMode
```

Expected:
- all NPC EditMode tests pass

**Step 4: Commit**

Run:
```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime/MainTownNpcAppearanceApplicator.cs Reloader/Assets/_Project/NPCs/Tests/EditMode/MainTownNpcAppearanceApplicatorEditModeTests.cs
git commit -m "fix: use shared npc dialogue face points"
```

### Task 4: Verify the live MainTown dialogue slice still works

**Files:**
- Test: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownPopulationInfrastructurePlayModeTests.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/PlayerNpcInteractionPlayModeTests.cs`

**Step 1: Run focused PlayMode coverage**

Run:
```bash
Unity PlayMode: Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests
Unity PlayMode: Reloader.NPCs.Tests.PlayMode
```

Expected:
- procedural civilian interaction still opens dialogue
- vendor/civilian dialogue flows remain green

**Step 2: Run guardrail scripts**

Run:
```bash
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
```

Expected:
- both scripts pass cleanly

**Step 3: Commit**

Run:
```bash
git add -A
git commit -m "test: verify shared npc dialogue anchor coverage"
```

### Task 5: Push and request review

**Files:**
- Modify: current branch only

**Step 1: Push the branch**

Run:
```bash
git push
```

**Step 2: Trigger review**

Run:
```bash
gh pr comment 32 --body "@codex review"
```

If Codex review remains rate-limited, run an internal subagent review pass before declaring the slice ready.

**Step 3: Confirm no stray debugging junk remains**

Run:
```bash
git status --short
```

Expected:
- clean working tree

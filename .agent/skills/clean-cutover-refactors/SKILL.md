---
name: clean-cutover-refactors
description: Drives pre-MVP refactors toward single-path replacements and deletion of superseded code. Use when replacing architecture, resetting a subsystem, simplifying an overgrown implementation, or changing behavior before compatibility guarantees are required.
---

# Clean Cutover Refactors

Use this skill when the goal is to replace or simplify a system cleanly, not preserve every historical path.

## When to Use

- Replacing an existing architecture or subsystem before MVP/demo
- Refactoring code that has accumulated fallback paths, adapters, shims, wrappers, or rescue logic
- Simplifying behavior where old and new paths should not coexist
- Resetting ownership so one canonical runtime path remains
- NOT appropriate when: the user explicitly asks for backward compatibility, migration support, dual-path rollout, preserved save compatibility, or public API stability

## Workflow

```
Clean Cutover Progress:
- [ ] Identify the canonical replacement path
- [ ] List old paths, helpers, fallbacks, and compatibility scaffolding to delete
- [ ] Update tests to assert the new single-path contract
- [ ] Remove superseded code instead of keeping both paths alive
- [ ] Run targeted verification for the cutover seam
- [ ] Confirm no hidden fallback or silent repair behavior remains
```

## Rules

- Default to one canonical runtime path.
- Prefer deletion, collapse, and direct replacement over adapters, bridges, wrappers, and compatibility layers.
- Do not preserve old and new architecture in parallel unless the user explicitly asks for that tradeoff.
- Do not add hidden fallback lookup, best-effort repair, silent error suppression, or dual-write behavior.
- Fail loudly on broken wiring or missing required data rather than masking the issue.
- Remove stale tests that assert legacy behavior once the new contract is verified.

## Decision Rule

When considering a compatibility layer, ask:

1. Did the user explicitly request compatibility, migration, or staged rollout?
2. Is there a real external contract that must be preserved right now:
   - shipped save files
   - public API
   - external integration
   - already-authored content that the task explicitly must preserve
3. Is the repo still before MVP/demo for this surface?

If the answer is "no" to 1 and 2, prefer the clean cutover.

## Preferred Implementation Pattern

### 1. Name the new owner

- Pick the one runtime owner or seam that should survive.
- Route all behavior through that owner.
- Remove alternate entrypoints after the new path is proven.

### 2. Delete before patching around

Delete:
- legacy helper methods
- compatibility-only fields
- fallback branches
- wrapper classes that only translate old shape to new shape
- silent rescue paths
- dead tests for removed behavior

Avoid:
- keeping old method signatures "just in case"
- preserving old serialized fields without a current contract reason
- adding adapters because deletion feels risky

### 3. Make failure explicit

- If required data is missing, surface the error
- If a mount, lookup, or dependency resolution fails, keep state consistent and report failure
- Do not silently substitute a "close enough" object or path

### 4. Verify the cutover seam

- Run the smallest tests that prove the new path works
- Run adjacent seam tests next
- Widen only if those tests expose coupling

## Smells That Mean "Delete It"

- "Temporary" fallback branch with no removal plan
- Adapter whose only job is keeping a removed shape alive
- Wrapper that forwards everything to the new owner
- Best-effort lookup when explicit references should exist
- Error suppression that hides broken state instead of fixing it
- Old field, old helper, and new helper all coexisting after the cutover

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Keeping both old and new paths "for safety" | Verify the new path, then delete the old one |
| Adding an adapter to avoid touching callers | Update callers and remove the obsolete seam |
| Silent fallback when data is missing | Fail loudly and fix the authoring/wiring issue |
| Leaving old tests around after the contract changed | Rewrite tests to assert the new single-path behavior |
| Treating pre-MVP code like a live migration problem | Cut over cleanly unless the user explicitly says otherwise |

## Verification Checklist

- [ ] One canonical owner/path remains
- [ ] Superseded helpers/fallbacks were deleted
- [ ] No hidden compatibility branch remains
- [ ] No silent repair or suppressed failure hides bad state
- [ ] Tests assert the new contract, not the legacy one

## Integration

- `.agent/skills/refactoring-and-test-hygiene/SKILL.md` - Use alongside this for local hotspot cleanup
- `.agent/skills/unity-project-conventions/SKILL.md` - Keep project architecture contracts consistent while cutting over

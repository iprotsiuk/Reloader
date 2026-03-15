---
name: refactoring-and-test-hygiene
description: Performs bounded runtime cleanup that reduces LOC, deletes obsolete paths, splits oversized tests, and lowers future agent context cost. Use when the user asks for refactoring, simplification, cleanup, LOC reduction, test-bloat reduction, or context-hygiene work.
---

# Refactoring and Test Hygiene

Use this skill for bounded runtime cleanup and test-hygiene work in Reloader. Tests are part of the cleanup scope, not optional follow-up work.

## When to Use

- Refactor or simplify existing runtime code
- Reduce LOC or delete obsolete logic
- Split or shrink oversized tests
- Reduce future agent context load
- NOT appropriate when: the task is primarily docs, plans, rules, or skills work

## Workflow

```
Refactor Hygiene Progress:
- [ ] Pick exactly one hotspot cluster
- [ ] Load only the local context needed for that cluster
- [ ] Write or identify the narrow failing test first
- [ ] Refactor toward deletion and explicit ownership
- [ ] Run the narrowest useful verification
- [ ] Stop without expanding into adjacent systems
```

## Instructions

### Step 1: Bound the work

- Work on exactly one hotspot cluster per run.
- A hotspot cluster is one primary runtime file or tightly related local group, plus directly related tests or support code.
- Do not broaden into adjacent systems just because nearby cleanup is available.

### Step 2: Load minimal context

1. Start with touched file metadata, symbol search, and direct references.
2. Load the matching local router or skill for the touched domain.
3. Read docs only if:
   - the task crosses domains
   - save, events, or persistence contracts change
   - local code intent is unclear
4. Do not auto-load `docs/design/core-architecture.md` for local refactor or test-cleanup work.

### Step 3: Rank hotspots

Prioritize clusters with:

- large runtime files
- giant paired test files
- duplicated fixture setup
- PlayMode tests that could be EditMode
- reflection usage
- scene-wide lookup usage
- mixed responsibilities
- dead optionality

### Step 4: Refactor toward less code

- Prefer deletion over abstraction.
- Prefer explicit ownership over scene-wide discovery.
- Prefer local helpers over new manager or service layers.
- Do not replace one god object with five fake abstractions.
- Remove obsolete branches and compatibility paths.
- Keep public and internal surface area small.

### Step 5: Apply test hygiene

- EditMode by default.
- Use PlayMode only when frame timing, physics, rendering, cameras, or scene or runtime behavior are required.
- Split test files that mix unrelated behaviors.
- Extract assembly-local fixtures or builders after repeated setup appears 3 or more times.
- Avoid reflection-heavy tests when a narrow explicit seam is cheaper.
- Never run unfiltered full suites in the inner loop.

### Step 6: Verify with the smallest useful ladder

Use `scripts/run-unity-tests.sh` with `-testFilter` via the script's filter argument.

1. Narrowest method or class filter
2. Local subsystem filter
3. Broader smoke only if needed

## Success Criteria

- Net negative LOC
- Fewer branches and fallback paths
- Smaller touched test files
- Less duplicated setup
- Lower future context cost
- No regressions in the touched cluster

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Turning cleanup into a cross-system rewrite | Stop at one hotspot cluster |
| Adding new abstractions to justify refactoring | Delete code first and prefer local helpers |
| Using PlayMode for convenience | Prove EditMode is insufficient first |
| Running broad suites in the inner loop | Start with the narrowest filter that exercises the changed cluster |

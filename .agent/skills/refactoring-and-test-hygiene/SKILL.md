---
name: refactoring-and-test-hygiene
description: Keeps local runtime refactors and test cleanup scoped to one hotspot cluster with minimal context load. Use when cleaning up a single subsystem's runtime code or tests, restoring targeted regression coverage, or reducing local complexity without changing scenes, prefabs, or cross-domain contracts.
---

# Refactoring And Test Hygiene

Use this skill for local runtime cleanup. Keep the run small, test-backed, and inside one hotspot cluster.

## When to Use

- Local refactor in one runtime subsystem
- Test-local cleanup or targeted regression repair
- Tightening one controller, binder, runtime helper, or adjacent test seam
- Not for cross-domain feature work, save/event/persistence changes, scene/prefab/editor-state work, or data-content authoring

## Rules

- One hotspot cluster per run: one touched code seam plus its nearest tests.
- Start from touched code and touched tests first.
- Do not begin with repo-wide scans, docs sweeps, or rules/skills/plans for runtime-local work.
- Tests are in scope. If no nearby test exists, add the smallest local regression coverage that matches the seam.
- Prefer deletion, inlining, and simplification over new abstraction.
- If the work crosses save, events, persistence, or another domain, stop and reclassify the task.

## Discovery

- First pass stays inside the touched domain roots only.
- Read the nearest implementation files and nearest tests before any broader search.
- Load docs, rules, or other skills only if local evidence is insufficient or the touched seam crosses a shared contract boundary.

## Filtered Verification Ladder

1. Run the touched test file or smallest relevant filter first.
2. Run the nearest subsystem tests next if the first check passes.
3. Widen only when failures or dependencies prove coupling.
4. Do not jump to repo-wide suites by default.

## Completion Check

- Scope stayed local
- Verification stayed filtered
- Tests covered the touched seam
- No scene, prefab, or editor-state changes slipped in
- No new abstraction replaced a simpler deletion or collapse

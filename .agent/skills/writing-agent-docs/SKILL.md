---
name: writing-agent-docs
description: Writes and updates design/planning docs for agent-first workflows with strict scope control, concise structure, and minimal context bloat. Use when the user asks to create or update architecture/design/routing docs in this repository.
---

# Writing Agent Docs

Write docs optimized for coding agents: concise, scoped, and contract-safe.

## When to Use

- User asks to create/update docs under `docs/`, `.cursor/rules/`, or `.cursor/agents.md`
- User asks to improve architecture/design clarity for future agent work
- User asks to reduce context load or remove doc drift
- NOT appropriate when: user asks for runtime feature implementation

## Workflow

Copy this checklist and keep it updated in your response for substantial doc work:

```
Docs Writing Progress:
- [ ] Confirm scope and target files
- [ ] Load only required source docs/contracts
- [ ] Draft concise updates using repo schema
- [ ] Sync related routers/skills if contracts changed
- [ ] Run validation checks
- [ ] Report changes and residual risks
```

## Instructions

### 1. Confirm Scope First

- Work only in requested domain(s).
- Do not edit unrelated docs for completeness.
- Do not infer code implementation tasks from doc requests.

### 2. Load Minimal Context

- Always load `docs/design/core-architecture.md` first for shared contracts.
- Then load only the domain doc(s) directly tied to the task.
- Avoid broad context loading across all design docs.

### 3. Follow Repository Doc Schema

- For `docs/design/*.md` except `README.md`, use scope-tagged section headers:
  - `## ... [v0.1]`
  - `## ... [v0.2]`
  - `## ... [v1+]`
- Keep top-level intro short and actionable.
- Prefer contract tables and compact bullet lists over long prose.
- Keep terminology aligned with canonical contracts (for example `SaveCoordinator`, EventBus pattern with `GameEvents` implementation).

### 4. Concision and Context-Bloat Rules

- Avoid repeating large reference blocks that already exist elsewhere.
- Prefer canonical paths over duplicated text.
- Keep sections scoped to one concern (routing, contract, workflow, policy).
- Remove speculative text that does not affect implementation decisions.

### 5. Relevance and Sync Rules

When changing a contract or naming convention, update all affected surfaces in the same pass:

- Domain doc in `docs/design/`
- Affected router file(s) in `.cursor/rules/`
- Affected local skill(s) in `.agent/skills/`
- `.cursor/agents.md` only when agent behavior expectations changed

### 6. Validation (Required)

Run from repo root:

```bash
bash scripts/verify-docs-and-context.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

If a skill file was edited/created, also run:

```bash
bash .agent/skills/creating-skills/scripts/validate-skill.sh path/to/skill-folder
```

### 7. Output Contract

Return:

1. What changed (files)
2. Why (contract/routing/context reason)
3. Validation results
4. Residual risks or follow-ups

## Writing Patterns

### New Domain Doc Pattern

- Short purpose statement
- `Prerequisites` line (`core-architecture.md` first)
- 3-6 scoped sections with version tags
- Cross-doc references only where required for behavior/contracts

### Existing Doc Update Pattern

- Preserve structure unless the structure itself is the problem
- Edit the smallest section that satisfies the request
- Keep terminology and examples consistent with current repository phase

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Loading every design doc | Load core + directly relevant domain docs only |
| Mixing roadmap with current contracts | Use explicit version tags and scope-specific text |
| Duplicating the same contract in many docs | Keep one source of truth and reference it |
| Updating docs without router/skill sync | Update `.cursor/rules/` and `.agent/skills/` when contracts move |
| Verbose explanation | Keep only implementation-relevant content |

## Resources

- `docs/design/README.md`
- `docs/design/core-architecture.md`
- `.cursor/agents.md`
- `.agent/skills/reviewing-design-docs/SKILL.md`

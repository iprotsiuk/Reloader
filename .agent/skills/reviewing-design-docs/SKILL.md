---
name: reviewing-design-docs
description: Reviews design docs, agent rules, and skills for contract drift, routing overlap, broken links, and context-bloat risks. Use when the user asks for architecture/doc reviews, agent-framework consistency checks, or context-load audits before implementation.
---

# Reviewing Design Docs

Use this skill to run deterministic checks first, then produce a findings-first review.

## When to Use

- User asks to review docs/architecture/rules/skills quality or consistency
- User asks to find conflicts or future weirdness in planning/design artifacts
- User asks whether structure is suitable for coding agents with minimal context load
- User asks to audit Unity-specific context hygiene (generated/third-party scope)
- NOT appropriate when: user asks to implement gameplay/runtime features

## Workflow

Copy this checklist and keep it updated in your response when a review is substantial:

```
Docs Review Progress:
- [ ] Confirm scope and phase constraints
- [ ] Run deterministic audit checks
- [ ] Read only files connected to flagged checks
- [ ] Report findings first by severity with exact file references
- [ ] Report residual risks or testing gaps
```

## Instructions

### 1. Confirm Current Delivery-Phase Constraints

- Follow `.cursor/agents.md` current phase contract.
- Verify live implemented-vs-planned status via `docs/design/v0.1-demo-status-and-milestones.md`.
- Do not propose implementation work unless user asks.

### 2. Run Deterministic Audit First

Run:

```bash
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

This script checks:
- existing docs/context guardrails
- local markdown link validity in `docs/`, `.agent/skills/`, `.cursor/rules/`
- protected context-router overlap pairs
- Unity context ignore hygiene in `.codexignore`, `.cursorignore`, `.ignore`

### 3. Read Only What Is Needed

- Start from failing/warning checks.
- Open only files directly related to each potential issue.
- Avoid loading broad or generated Unity folders unless the task explicitly needs them.

### 4. Evaluate Conflict Types

- Contract drift between docs and skills
- Router-glob overlap that pulls unrelated context
- Broken or stale path references
- Missing ignore patterns likely to bloat agent context
- Terminology drift in architectural contracts (for example save orchestration names)

### 5. Required Output Contract

Report findings first, highest severity to lowest.

For each finding include:
- Severity (`P1`, `P2`, `P3`)
- What conflicts
- Why it matters for agent behavior
- Exact file reference(s)
- Fix direction (unless user asked for direct edits)

If no findings:
- State explicitly: `No critical conflicts found.`
- Still include residual risks/testing gaps.

## Quick Commands

- Full audit:

```bash
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

- Existing repo guardrail only:

```bash
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
```

- Validate this skill:

```bash
bash .agent/skills/creating-skills/scripts/validate-skill.sh .agent/skills/reviewing-design-docs
```

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Returning only a summary | List concrete findings with exact file references |
| Loading all docs by default | Use audit output to target only relevant files |
| Treating overlap as harmless | Explain whether overlap raises context-load risk |
| Ignoring Unity context hygiene | Verify ignore files in every audit |

## Resources

- `scripts/audit-docs-context.sh` — deterministic audit entrypoint for this skill

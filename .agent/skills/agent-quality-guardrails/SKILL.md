---
name: agent-quality-guardrails
description: Applies repo-local quality guardrails for autonomous coding, manager/subagent coordination, and quality review. Use when doing autonomous implementation, orchestrating subagents, reviewing work quality, or resolving uncertainty while the user is unavailable.
---

# Agent Quality Guardrails

Use this skill as a lightweight quality layer, not as a replacement for repository rules or global superpowers.

## Precedence

Follow these rules in order:

1. User instructions in the current task.
2. `.cursor/agents.md`, `.cursor/rules/*.mdc`, and domain local skills under `.agent/skills/*/SKILL.md`.
3. Mandatory global superpowers when triggered, including TDD, verification-before-completion, systematic-debugging, managing-subagents, and requesting-code-review.
4. This skill's general guardrails.

If this skill conflicts with repo-specific rules, domain local skills, or mandatory global superpowers, those rules win.

## When to Use

- Autonomous coding, especially when the user is AFK or asked not to be interrupted.
- Manager/subagent work, including task decomposition and quality review.
- Code, docs, or skill review where scope creep, overengineering, or weak verification is likely.
- Ambiguous tasks where a safe default can move work forward.

## Operating Rules

- State assumptions before acting when they affect behavior, scope, or risk.
- Prefer the smallest change that satisfies the explicit request.
- Keep edits surgical: touch only files needed for the task and do not clean up unrelated code.
- Match local style and existing repository patterns before introducing new patterns.
- Define concrete verification before claiming completion.
- If uncertainty is safe and bounded, choose a reasonable default, document it, and continue.
- Block only on truly unsafe ambiguity, destructive operations, credential/security risk, irreversible data loss, or mutually exclusive requirements.

## Autonomy Rule

Do not blindly follow an upstream "ask when uncertain" habit. If the user explicitly says not to ask questions, is AFK, or asks for autonomous execution:

- Proceed with safe, reversible assumptions.
- Keep scope narrow and record the assumptions in the final response.
- Ask only when continuing would be unsafe or likely to waste significant work.

## Manager/Subagent Rule

If `managing-subagents` is active, the manager coordinates and reviews but does not perform worker implementation. "Think before coding" means clarify scope, risks, and verification; it does not override the manager/worker boundary.

## Quality Checks

Before finishing:

- Verify every changed line traces to the user's request.
- Confirm mandatory triggered skills were followed.
- Run the narrowest meaningful validation available, then widen only if the repo rules or task require it.
- Report validation evidence and residual conflict risks.

## Attribution

Adapted for this repository from `forrestchang/andrej-karpathy-skills` (`karpathy-guidelines`, MIT-marked source). This version is intentionally repo-local and defers to Reloader's `.cursor` rules, local skills, and global superpowers.

# Local Agent Guidance

This file is the canonical local guidance entrypoint for agents working in this repository.

## Context Routing

- `.cursor/rules/*.mdc` files are context routers.
- Treat each router's `globs` as the trigger for domain context.
- `game-design-docs.mdc` is always-on and points agents to the modular design docs under `docs/design/`.
- Gameplay routing is intentionally split by domain to reduce context load:
  - `inventory-economy-context.mdc` → inventory/economy/shopping workflows
  - `hunting-competitions-context.mdc` → hunting loops and competition flows
  - `npcs-quests-context.mdc` → NPC definitions, dialogue, quest systems
  - `law-enforcement-context.mdc` → legal rules, policing, black-market behavior
- Save-related routing is intentionally split to reduce context load:
  - `inventory-save-context.mdc` → pipeline orchestration under `Core/Scripts/Save/**`
  - `save-schema-context.mdc` → persisted DTO/schema contract changes
  - `scene-persistence-context.mdc` → scene/world placement restore behavior
  - `core-events-context.mdc` → cross-domain runtime event contract changes under `Core/Scripts/Events/**` and `Core/Scripts/Runtime/*Events*.cs`
- Prefer modular docs and avoid the superseded monolithic design plan.

## Skill Sources

- `.agent/skills/*/SKILL.md` files are the project skill sources.
- Use the skill that matches the active task domain before making changes.
- Use each skill's referenced `resources/` and `scripts/` files as supporting material.
- For architecture/docs/rules/skills audits, use `.agent/skills/reviewing-design-docs/SKILL.md` first.
- For writing/updating architecture or design docs, use `.agent/skills/writing-agent-docs/SKILL.md` first.

## Skill / Rule Precedence

- Global superpowers are the default workflow baseline.
- If any global superpower guidance conflicts with repository-local guidance, local guidance wins.
- Local guidance includes:
  - `.cursor/agents.md` and `.cursor/rules/*.mdc`
  - `.agent/skills/*/SKILL.md`
- Practical rule: use global superpowers unless a local rule/skill says otherwise. In conflicts, follow local rule/skill.

## Current Phase Contract

- Historical contract phrase retained for guardrails: this repository started in design/planning/doc-framework phase.
- Current delivery phase is `v0.1` demo implementation + hardening.
- Treat `docs/design/v0.1-demo-status-and-milestones.md` as the canonical implemented-vs-planned source of truth.
- Treat `docs/design/prototype-scope.md` as version target scope, not runtime completion truth.
- Unless the user explicitly requests runtime implementation, default to docs/rules/skills/plan updates.
- Do not infer game-feature coding work from architecture/design requests alone.

## Working Rules

- Follow `docs/design/core-architecture.md` as the shared contract before domain docs.
- Keep changes data-driven and consistent with ScriptableObject + runtime-instance patterns.
- Keep docs and skills synchronized when contracts or naming conventions change.
- For cross-domain extension work, load `docs/design/extensible-development-contracts.md` in addition to domain docs.
- Do not load generated Unity directories for context (`Reloader/Library/**`, `Reloader/Temp/**`, `Reloader/Logs/**`) unless a task explicitly targets build/runtime diagnostics.
- For Unity discovery, start with code/docs filters (for example `rg --files -g '*.cs' -g '*.asmdef' -g '*.md' Reloader/Assets/_Project docs .cursor .agent`) before broad file scans.
- Do not load Unity `*.meta` files unless the task explicitly requires GUID/reference/import diagnostics.
- If event contracts are added or changed under `Reloader/Assets/_Project/Core/Scripts/Events/**` or `Reloader/Assets/_Project/Core/Scripts/Runtime/*Events*.cs`, update routing in `.cursor/rules/core-events-context.mdc` in the same change.
- Place scene/world-item persistence orchestration scripts under `Reloader/Assets/_Project/Core/Scripts/Persistence/**` and keep `scene-persistence-context.mdc` globs aligned with that folder.
- If a referenced workflow tool is unavailable in the current agent runtime (for example `Skill` tool or `TodoWrite`), follow the same workflow manually: open the referenced skill file directly and track checklist progress in plain text updates.

# Local Agent Guidance

This file is the canonical local guidance entrypoint for agents working in this repository.

## Context Routing

- `.cursor/rules/*.mdc` files are context routers.
- Treat each router's `globs` as the trigger for domain context.
- `game-design-docs.mdc` is always-on and points agents to the modular design docs under `docs/design/`.
- Gameplay routing is intentionally split by domain to reduce context load:
  - `assassination-contracts-context.mdc` → assassination contracts, target generation, premium long-range job flow
  - `inventory-economy-context.mdc` → inventory/economy/shopping workflows
  - `hunting-competitions-context.mdc` → deferred optional side systems only
  - `npcs-quests-context.mdc` → NPC definitions, dialogue, quest systems
  - `law-enforcement-context.mdc` → police heat, search, arrest, confiscation, black-market behavior
- Save-related routing is intentionally split to reduce context load:
  - `inventory-save-context.mdc` → pipeline orchestration under `Core/Scripts/Save/**`
  - `save-schema-context.mdc` → persisted DTO/schema contract changes
  - `scene-persistence-context.mdc` → scene/world placement restore behavior
  - `core-events-context.mdc` → cross-domain runtime event contract changes under `Core/Scripts/Events/**` and `Core/Scripts/Runtime/*Events*.cs`
- Weapons runtime routing is intentionally split:
  - `reloading-context.mdc` → reloading systems, weapon data definitions, and ballistics-content work
  - `weapons-runtime-local-context.mdc` → local weapons runtime/controller/ADS/test cleanup under weapons script/test roots
- Prefer modular docs and avoid the superseded monolithic design plan.

## Task Classification

- `runtime-local` → one subsystem or hotspot cluster; start from touched code/tests only and keep verification filtered.
- `runtime-cross-domain` → shared runtime contracts, save/events/persistence boundaries, or changes that clearly span multiple systems; load shared docs and guardrails.
- `scene/prefab/editor-state` → Unity-authored state, scene wiring, prefabs, components, or editor workflows; verify via Unity-aware tooling/state checks.
- `data-content` → ScriptableObject assets, authored content, balancing data, or data-definition work.
- `docs/rules/skills` → markdown/routing/guardrail updates only; no gameplay/runtime edits.

For `runtime-local` and `test-local` work:

- start from touched code/tests only
- do NOT default to docs/rules/skills/plan work
- do NOT auto-load `docs/design/core-architecture.md`
- use `.agent/skills/refactoring-and-test-hygiene/SKILL.md` for local refactor/test cleanup

## Skill Sources

- `.agent/skills/*/SKILL.md` files are the project skill sources.
- Use the skill that matches the active task domain before making changes.
- Use each skill's referenced `resources/` and `scripts/` files as supporting material.
- For local runtime refactor/test cleanup inside one hotspot cluster, use `.agent/skills/refactoring-and-test-hygiene/SKILL.md` first.
- For weapon view prefab, attachment mounting, optic/muzzle runtime, or first-person weapon pose work, use `.agent/skills/weapon-view-attachment-framework/SKILL.md` before changing code/assets.
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
- Runtime-local and test-local requests are implementation work. Do not redirect them into docs/rules/skills by default.
- Do not infer game-feature coding work from architecture/design requests alone.

## Working Rules

- Load `docs/design/core-architecture.md` only for new features, cross-domain changes, save/events/persistence work, or when local intent is unclear.
- Keep changes data-driven and consistent with ScriptableObject + runtime-instance patterns.
- Keep docs and skills synchronized when contracts or naming conventions change.
- MCP endpoints (Codex runtime defaults):
  - Unity MCP server is configured in `~/.codex/config.toml` under `[mcp_servers.unityMCP]`.
  - Blender MCP server is configured in `~/.codex/config.toml` under `[mcp_servers.blender]` and is expected at `localhost:9876` by default.
  - For Blender MCP smoke checks, call `get_polyhaven_status` or `get_scene_info`.
- For cross-domain extension work, load `docs/design/extensible-development-contracts.md` in addition to domain docs.
- Do not load generated Unity directories for context (`Reloader/Library/**`, `Reloader/Temp/**`, `Reloader/Logs/**`) unless a task explicitly targets build/runtime diagnostics.
- For runtime-local discovery, first pass should stay inside touched domain roots only.
  - Weapons example: `rg --files -g '*.cs' Reloader/Assets/_Project/Weapons/Scripts Reloader/Assets/_Project/Weapons/Tests Reloader/Assets/Game/Weapons`
  - UI example: `rg --files -g '*.cs' -g '*.uxml' -g '*.uss' Reloader/Assets/_Project/UI/Scripts Reloader/Assets/_Project/UI/Tests Reloader/Assets/_Project/UI/Toolkit`
- For runtime-local cleanup/refactor work, use a filtered test-first ladder:
  - run the touched test file or smallest relevant filter first
  - then run the nearest subsystem tests
  - widen only when failures prove coupling
- For multi-step Unity/editor workflows, strongly prefer batched Unity MCP commands (`batch_execute` or equivalent batched operations) because repeated single MCP calls often run into Unity reload/time-out churn.
- Do not load Unity `*.meta` files unless the task explicitly requires GUID/reference/import diagnostics.
- If event contracts are added or changed under `Reloader/Assets/_Project/Core/Scripts/Events/**` or `Reloader/Assets/_Project/Core/Scripts/Runtime/*Events*.cs`, update routing in `.cursor/rules/core-events-context.mdc` in the same change.
- Place scene/world-item persistence orchestration scripts under `Reloader/Assets/_Project/Core/Scripts/Persistence/**` and keep `scene-persistence-context.mdc` globs aligned with that folder.
- If a referenced workflow tool is unavailable in the current agent runtime (for example `Skill` tool or `TodoWrite`), follow the same workflow manually: open the referenced skill file directly and track checklist progress in plain text updates.

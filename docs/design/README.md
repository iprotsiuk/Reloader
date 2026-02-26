# Reloader Design Docs — Agent Routing

> **For agents:** Use this index to find the right docs for your task. Load ONLY what you need.

## Always Read First

**[core-architecture.md](core-architecture.md)** (~300 lines) — shared patterns, project structure, SO system, event bus, design principles. Every agent reads this regardless of task.

## Implementation Status Contract

Design docs can intentionally describe both:

- **Implemented in repository** — APIs/types currently present under `Reloader/Assets/_Project/**`.
- **Target design** — planned architecture/contracts for upcoming milestones.

Agent behavior:
- Prefer **implemented** contracts for coding tasks unless the task explicitly asks for forward-design work.
- Treat `SaveCoordinator` as the canonical current save orchestration term. If a future save facade is added, it must be documented as a thin wrapper over `SaveCoordinator`.

## Then Read Your Domain

| Working on... | Read these docs | Related skills |
|---------------|----------------|----------------|
| **Reloading bench, press interactions, ammo assembly** | [reloading-system.md](reloading-system.md) | `reloading-domain-knowledge` (accuracy calc, real-world reference), `adding-game-content` (creating SO assets) |
| **Weapons, ballistics, shooting, accuracy model** | [weapons-and-ballistics.md](weapons-and-ballistics.md) | `reloading-domain-knowledge` (accuracy calc, ballistics reference), `adding-game-content` (creating weapon/part assets) |
| **Driving, world layout, vehicles, scene transitions** | [world-and-vehicles.md](world-and-vehicles.md) | — |
| **Player controls/input/camera, shared UI shell, shared audio** | [core-architecture.md](core-architecture.md), [prototype-scope.md](prototype-scope.md), plus affected domain doc | `unity-project-conventions` |
| **Inventory, item persistence, shops, economy, money** | [inventory-and-economy.md](inventory-and-economy.md) | `adding-game-content` (for shop inventory items) |
| **Hunting, animal AI, competitions, scoring** | [hunting-and-competitions.md](hunting-and-competitions.md) | — |
| **NPCs, dialogue, quests, relationships** | [npcs-and-quests.md](npcs-and-quests.md) | — |
| **Police, wardens, legal system, black market** | [law-enforcement.md](law-enforcement.md) | — |
| **Save/load, game loop, progression, day cycle** | [save-and-progression.md](save-and-progression.md) | — |
| **Quick save contract checks (schema/load order/size policy)** | [save-contract-quick-reference.md](save-contract-quick-reference.md) | — |
| **Scoping work, prioritizing features** | [prototype-scope.md](prototype-scope.md) | — |
| **Adding new data assets (weapons, ammo, equipment)** | Depends on asset type — check skill | `adding-game-content`, `unity-project-conventions` |
| **Any new C# script or Unity feature** | [core-architecture.md](core-architecture.md) | `unity-project-conventions` |

## Cross-Domain Work

If your task spans multiple domains (e.g., "hunting competition that awards money"), load each relevant domain doc. The core architecture doc covers how systems communicate via runtime event ports/hub (`IGameEventsRuntimeHub`). The EventBus pattern formerly exposed a static `GameEvents` facade; that facade is retired. You should NOT need to understand another domain's internals, only what events it fires/listens to.

If your change touches runtime state (item ownership/location, transforms, inventories/containers, weapon/vehicle/NPC/player state), also load [save-and-progression.md](save-and-progression.md) and preserve the exact-restore save contract.

## Skills Reference

| Skill | When to use | Location |
|-------|------------|----------|
| `adding-game-content` | Creating new SO data assets (weapons, ammo, equipment, etc.) | `.agent/skills/adding-game-content/SKILL.md` |
| `reloading-domain-knowledge` | Implementing reloading mechanics, ballistics, accuracy model | `.agent/skills/reloading-domain-knowledge/SKILL.md` |
| `unity-project-conventions` | Writing any C# code, creating scripts/prefabs/scenes | `.agent/skills/unity-project-conventions/SKILL.md` |

## Historical Note

The original monolithic design draft has been retired from active routing. Use modular docs only. Historical versions remain available in git history.

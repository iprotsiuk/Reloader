# Extensible Development Contracts

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.
> **Purpose:** Canonical cross-domain implementation guardrails for agents adding/extending systems.

## Scope and Status [v0.1]

This contract defines how to safely extend runtime systems without breaking existing behavior.

Status markers used in this doc:
- **Implemented now**: behavior/types currently present in repository runtime.
- **Target contract**: approved shape for upcoming implementation slices.

## Runtime Event Hub Contract [v0.1]

Cross-domain communication must go through `IGameEventsRuntimeHub` and its composed ports.

Implemented now port set:
- `IRuntimeEvents`
- `IInventoryEvents`
- `IWeaponEvents`
- `IShopEvents`
- `IUiStateEvents`
- `IInteractionHintEvents`

Contract rules:
- Add/modify event contracts only under `Core/Scripts/Runtime/*Events*.cs` and payload types under `Core/Scripts/Events/**`.
- `RuntimeKernelBootstrapper.Events` is the runtime access point.
- Domain systems must not directly call other domains for gameplay state changes; publish events and consume ports.
- Any event contract change must be mirrored in docs/rules/tests in the same change.

## Wiring Conventions [v0.1]

### Runtime wiring

- Keep domain wiring in runtime controllers/adapters, not view binders.
- Dependency resolution order must be deterministic and null-safe.
- Do not apply expensive or state-mutating wiring work every frame when stable state can be cached.
- Input consumption must be explicit on all terminal paths to avoid queued carry-over actions.

### Scene wiring

- World scene setup must follow declarative contracts in [world-scene-contracts.md](world-scene-contracts.md).
- Prefer deterministic editor wiring tools over manual inspector-only edits.
- After MCP/editor mutations, always perform read-back verification before claiming completion.

## UI Toolkit Runtime Bridge Contract [v0.1]

The runtime UI bridge is built around screen ids and explicit dependency injection.

Implemented now:
- Runtime bridge: `UiToolkitScreenRuntimeBridge`
- Runtime root/installer: `UiToolkitRuntimeRoot`, `UiToolkitRuntimeInstaller`
- Screen composition config type: `UiScreenCompositionConfig`
- Action map config type: `UiActionMapConfig`
- Dumb view contracts: `IUiViewBinder`, `IUiController`, `UiIntent`, `UiRenderState`
- Dialogue overlay runtime seam: `IDialogueOverlayBridge` resolved by `UiToolkitScreenRuntimeBridge`

Contract rules:
- View binders emit intents only; they do not mutate gameplay state.
- Controllers map intents to domain/runtime calls.
- Stable naming conventions (`inventory__*`, `trade__*`) are required for safe UXML/USS rewiring.
- New runtime screens must declare: screen id, dependency contract, composition policy, and intent mapping policy.

## Interaction and Inventory Flow Contracts [v0.1]

### Interaction arbitration

Implemented now:
- `PlayerInteractionCoordinator` resolves winner by priority, stable tie-breaker, provider order.
- Interaction hints are published through `IInteractionHintEvents` with `InteractionHintPayload(contextId, actionText, subjectText)`.

Guardrails:
- Provider mode synchronization must not clear hints every frame.
- Pickup input must be consumed when no winner is present to avoid stale deferred interactions.

### Inventory UI item movement

Implemented now:
- TAB inventory drag events route through `TabInventoryViewBinder` -> `TabInventoryController` -> `PlayerInventoryController` / `PlayerInventoryRuntime`.

Guardrails:
- Keep transfer semantics deterministic (`move`, `merge`, `swap`) and validated through runtime APIs.
- Keep UI action dispatch decoupled from inventory data structure internals.

### Reload interactions

Implemented now:
- Reload lifecycle is driven from `ConsumeReloadPressed()` and weapon controller state transitions.

Guardrails:
- Reload start/cancel/complete must emit runtime events and preserve weapon-state save contract compatibility.

### Dropped-world-item persistence

Current status:
- **Target contract** is exact restore of dropped item transforms and scene placement.
- Full runtime module support is still partial in current implemented save slice.

Guardrails:
- Any new dropped-item runtime implementation must ship with matching save module payload contracts and current-schema documentation updates.

## Save and Persistence Contract [v0.1]

Canonical save runtime entrypoint is `SaveCoordinator` configured via `SaveBootstrapper`.

Implemented now required modules:
- `CoreWorld`
- `Inventory`
- `Weapons`

Persistence rules:
- Update payload contract + module implementation + current-schema docs together.
- Keep deterministic restore ordering stable unless explicitly versioned and documented.
- Keep save sizes within policy (`500 KB` soft, `1 MB` hard).

## World/Scene/Checkpoint/NPC Integration Workflow [v0.1]

When touching travel, scene contracts, interactables, checkpoints, or NPC interaction entrypoints:

1. Update/confirm scene contract expectations (`WorldSceneContract`, required object paths, required references).
2. Verify travel context validity (`scene identifiers`, `entryPointId`, return-link coherence).
3. Validate interactor gates (`required tag`, interaction source, controller references).
4. Validate NPC interaction wiring (`NpcAgent` capabilities, interaction controller links, optional vendor hooks). If dialogue is present, also validate `DialogueCapability` has a valid `DialogueDefinition` and that conversation mode / dialogue overlay wiring can resolve at runtime.
5. Validate checkpoint contract (entry point existence, transition target, vehicle anchor assumptions).
6. Run targeted world tests (topology + round-trip + relevant interaction tests).

## Enforcement and Guardrails [v0.1]

Required checks before claiming completion for cross-domain changes:
- `bash scripts/verify-docs-and-context.sh`
- `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`
- `bash scripts/verify-extensible-development-contracts.sh`

If Unity is available and runtime behavior changed, also run targeted Unity EditMode/PlayMode tests for touched contracts.

## Change Checklist [v0.1]

Use this checklist for cross-domain extensions:

- [ ] Event hub contract updated (if event shape changed)
- [ ] UI bridge contract updated (if screen/wiring changed)
- [ ] Save/persistence contract updated (if runtime state shape changed)
- [ ] World/scene/integration workflow docs updated (if travel/interactable/checkpoint/NPC changed)
- [ ] `.cursor` routing updated for any moved/added contract files
- [ ] `.agent` skills updated when workflow expectations changed
- [ ] Guardrail scripts updated/green

# 2026-03-08 Dialogue Orchestration Progress

## Shipped In This Slice

- Added a shared `DialogueOrchestrator` contract so player-interacted, nearby NPC-initiated, and scripted conversations all enter the same runtime/UI path.
- Added `DialogueProximityInitiator` and `DialogueScriptStarter` as the first reusable non-player conversation-start seams.
- Routed existing `DialogueCapability` and `LawEnforcementInteractionCapability` through the orchestrator instead of opening the runtime directly.
- Tightened `DialogueRuntimeOverlayBridge` so it resolves the player-host dialogue runtime instead of observing the first global runtime it finds.
- Guarded dialogue confirm input on the first observed frame of a new conversation so queued pickup/confirm input cannot auto-submit a newly opened conversation.
- Tightened `DialogueOverlayViewBinder` so the fullscreen dialogue shell disables hit testing when hidden.

## Verification Snapshot

- `DialogueOverlayBridgePlayModeTests`: bridge binding, rebind cleanup, player-host runtime affinity, and first-observed-frame input guard.
- `DialogueOverlayUiToolkitPlayModeTests`: hidden overlay hit-target guard plus real authored outcome submission paths.
- `DialogueInitiationPlayModeTests`: nearby NPC initiation still enters the shared conversation mode/runtime path cleanly.

## Resulting Contract

- All first shipped conversations use one runtime authority and one live overlay path.
- Police stop, quest/script conversations, vendors, and future fixers can initiate dialogue without bypassing camera/input/UI rules.
- The current system remains one-node in shipped authored content, but the orchestration/runtime shape is now in place for future multi-step dialogue without rewrite.

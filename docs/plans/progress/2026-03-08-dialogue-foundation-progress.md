# 2026-03-08 Dialogue Foundation Progress

## Shipped Runtime Seams
- `DialogueCapability` is the NPC-facing action capability for conversations.
- `DialogueDefinition` assets carry node/reply data and structured outcome ids/payloads.
- `DialogueRuntimeController` owns active conversation state and confirmation outcomes.
- `DialogueConversationModeController` owns focus-lock / movement-lock / cursor-unlock behavior while a conversation is active.
- `DialogueRuntimeOverlayBridge` feeds the live UI state into the shared UI Toolkit binding path.
- `UiToolkitScreenRuntimeBridge` binds the dialogue overlay screen through `DialogueOverlayController` + `DialogueOverlayViewBinder`.

## Authored Proof Point
- Added `Dialogue_FrontDeskClerk.asset` as the first real dialogue asset.
- Wired `Npc_FrontDeskClerk.prefab` with `DialogueCapability` referencing that asset.
- Added tests that prove:
  - the authored asset is valid
  - the prefab is wired to it
  - interacting with the prefab opens the shared dialogue runtime and conversation mode
  - the rendered overlay button click submits the authored reply outcome through the real UI binding path

## Actual Contract vs Initial Plan
- The runtime does not expose a separate `IDialogueRuntime` abstraction in this slice.
- Conversation focus does not use a separate `IDialogueFocusTarget` abstraction in this slice.
- Keyboard navigation lives in `DialogueRuntimeOverlayBridge`; the runtime itself stays focused on conversation state and outcomes.
- The current contract is still extendable for multi-step dialogue, gated replies, police stops, and vendor/fixer outcomes.

## Remaining Follow-Through
- Add real multi-step authored dialogue once a character needs it.
- Add domain consumers for dialogue outcomes (police stop logic, vendor/fixer response, quest hooks).
- Decide whether the current polling-based conversation-mode synchronization should become event-driven in a later hardening pass.

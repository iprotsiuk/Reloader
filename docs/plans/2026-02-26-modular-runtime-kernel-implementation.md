# Modular Runtime Kernel Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Introduce a scalable modular runtime kernel and domain event hub while preserving existing gameplay behavior and fixing stale interaction input consumption.

**Architecture:** Add a kernel layer that owns lifecycle modules (`IGameModule`) and a replaceable runtime event hub service. Runtime integrations are canonical through `RuntimeKernelBootstrapper.Events` and typed event ports. Fix interaction controllers to consume pickup input at frame boundary so queued input cannot trigger delayed interactions.

**Tech Stack:** Unity 6.3, C#, NUnit EditMode + PlayMode tests, existing `Reloader.Core` asmdef.

---

### Task 1: Add Runtime Kernel Primitives

**Files:**
- Create: `Reloader/Assets/_Project/Core/Scripts/Runtime/IGameModule.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Runtime/RuntimeKernel.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Runtime/RuntimeModuleRegistration.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Runtime/IRuntimeEvents.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Runtime/RuntimeKernelBootstrapper.cs`
- Create: `Reloader/Assets/_Project/Core/Tests/EditMode/RuntimeKernelTests.cs`

**Step 1: Write failing tests for module order + event replacement + lifecycle guards**
- Add tests asserting:
  - modules initialize/start/stop in deterministic registration order.
  - duplicate module keys are rejected.
  - kernel exposes replaceable runtime events implementation.

**Step 2: Run targeted test file and confirm RED**
- Run: `dotnet test` equivalent is unavailable for Unity tests; use Unity test runner if available.
- Expected: new tests fail due to missing runtime kernel types.

**Step 3: Implement minimal runtime kernel types**
- Create interfaces/types and deterministic kernel orchestration with explicit state transitions.
- Keep implementation independent from scene/runtime object references.

**Step 4: Re-run tests and confirm GREEN**
- Run the same targeted tests.

**Step 5: Commit**
- `feat(core): add modular runtime kernel foundation`

### Task 2: Harden Runtime Event Hub Contract + Legacy Bridge Coverage

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/IGameEventsRuntimeHub.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/RuntimeKernelBootstrapper.cs`
- Create: `Reloader/Assets/_Project/Core/Tests/EditMode/RuntimeEventHubBehaviorTests.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/InventoryEventContractsTests.cs` (only if needed for compatibility assertions)

**Step 1: Write failing tests for compatibility facade behavior**
- Add tests asserting runtime hub channels forward through `RuntimeKernelBootstrapper.Events` consistently.
- Add test asserting menu visibility state flags stay correct via the bridge.

**Step 2: Run tests to confirm RED**
- Expected failures because runtime hub bridge coverage is incomplete.

**Step 3: Implement bridge**
- Keep runtime event contracts backward-compatible for existing consumers.
- Route event access through `RuntimeKernelBootstrapper.Events` (or equivalent static accessor).
- Preserve menu-open state behavior and all current public API.

**Step 4: Re-run tests and confirm GREEN**
- Run bridge tests plus existing inventory event contract tests.

**Step 5: Commit**
- `refactor(core): route legacy event access through runtime hub`

### Task 3: Fix Delayed Pickup Interaction Consumption

**Files:**
- Modify: `Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/World/PlayerShopVendorController.cs`
- Modify: `Reloader/Assets/_Project/Reloading/Scripts/World/PlayerReloadingBenchController.cs`
- Modify: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerInventoryControllerPlayModeTests.cs`
- Create or Modify: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/ShopVendorInteractionPlayModeTests.cs` (only if needed for stale-input regression)

**Step 1: Write failing regression tests**
- Add test: pressing pickup without target should consume input and not trigger interaction later when target appears.
- Add vendor/bench coverage if current tests do not already enforce this frame-boundary behavior.

**Step 2: Run tests and confirm RED**
- Expected: current code fails due to late consumption path.

**Step 3: Implement minimal fixes**
- Consume pickup input once per tick before target-resolution branch in each interaction controller.
- Keep existing gameplay semantics (only interact when target present this frame).

**Step 4: Re-run tests and confirm GREEN**
- Run updated interaction PlayMode test set.

**Step 5: Commit**
- `fix(interaction): consume pickup input at frame boundary`

### Final Verification

**Files:**
- No new files required.

**Steps:**
1. Run all modified EditMode tests.
2. Run all modified PlayMode tests.
3. Verify no API breakage in compile errors for runtime event consumers and any retained legacy bridge call sites.
4. Capture `git status` and summarize changed files.

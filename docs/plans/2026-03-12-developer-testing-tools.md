# Developer Testing Tools Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a layered developer testing tools slice with a runtime console, autocomplete, and a shared command backend that supports `noclip`, `noclip speed`, `give item`, `traces persistent`, and `spawn npc`.

**Architecture:** Add a new `DevTools` feature module that owns command parsing, suggestion generation, runtime state, and gameplay command execution. Integrate the runtime console into the existing UI Toolkit screen bridge, extend UI-state events so cursor lock and gameplay input treat the console like other menus, and keep the editor surface as a thin adapter that forwards into the same runtime backend.

**Tech Stack:** Unity 6.3, UI Toolkit, Input System, existing runtime event hub (`IUiStateEvents`), `PlayerMover`, `PlayerInventoryController`, NPC prefabs/catalog assets, EditMode + PlayMode NUnit tests.

---

### Task 1: Create the DevTools Runtime Contracts and Catalog

**Files:**
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Reloader.DevTools.asmdef`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevToolsRuntime.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevToolsState.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandCatalog.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandDefinition.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandContext.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandParseResult.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevConsoleSuggestion.cs`
- Create: `Reloader/Assets/_Project/DevTools/Tests/EditMode/Reloader.DevTools.Tests.EditMode.asmdef`
- Create: `Reloader/Assets/_Project/DevTools/Tests/EditMode/DevCommandCatalogTests.cs`
- Create: `Reloader/Assets/_Project/DevTools/Tests/EditMode/DevCommandParsingTests.cs`

**Step 1: Write the failing tests**

Add EditMode tests that prove:

```csharp
[Test]
public void Parse_GiveItemCommand_PreservesCommandAndArguments()
{
    var result = DevCommandLineParser.Parse("give item ammo-308 50");

    Assert.That(result.CommandName, Is.EqualTo("give"));
    Assert.That(result.Arguments, Is.EqualTo(new[] { "item", "ammo-308", "50" }));
}

[Test]
public void Catalog_BuildsDefaultCommands_ForApprovedFirstSlice()
{
    var catalog = DevCommandCatalog.CreateDefault();

    Assert.That(catalog.Contains("noclip"), Is.True);
    Assert.That(catalog.Contains("give"), Is.True);
    Assert.That(catalog.Contains("traces"), Is.True);
    Assert.That(catalog.Contains("spawn"), Is.True);
}
```

**Step 2: Run test to verify it fails**

Run: `Unity Test Runner EditMode -> DevTools tests` or targeted MCP EditMode run for `DevCommandCatalogTests` and `DevCommandParsingTests`

Expected: FAIL because the `DevTools` runtime types do not exist yet.

**Step 3: Write minimal implementation**

Implement the new runtime contracts and default command registration. Keep the parser intentionally small:

```csharp
public static class DevCommandLineParser
{
    public static DevCommandParseResult Parse(string input)
    {
        var tokens = Tokenize(input);
        if (tokens.Count == 0)
        {
            return DevCommandParseResult.Empty;
        }

        return new DevCommandParseResult(tokens[0], tokens.Skip(1).ToArray(), tokens.ToArray());
    }
}
```

`DevToolsRuntime` should expose small entry points only:

```csharp
public bool TryExecute(string input, out string resultMessage);
public IReadOnlyList<DevConsoleSuggestion> GetSuggestions(string input, int highlightedIndex);
public void SetConsoleVisible(bool isVisible);
public bool IsConsoleVisible { get; }
public DevToolsState State { get; }
```

**Step 4: Run test to verify it passes**

Run: targeted EditMode tests for `DevCommandCatalogTests` and `DevCommandParsingTests`

Expected: PASS

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/DevTools/Scripts \
        Reloader/Assets/_Project/DevTools/Tests/EditMode
git commit -m "feat: add dev tools command runtime contracts"
```

### Task 2: Add Console Visibility State, Input Plumbing, and Runtime UI Screen

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/IUiStateEvents.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/IPlayerInputSource.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerInputReader.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerCursorLockController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiRuntimeCompositionIds.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitRuntimeInstaller.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/UI/DevConsoleController.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/UI/DevConsoleViewBinder.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/UI/DevConsoleUiState.cs`
- Create: `Reloader/Assets/_Project/DevTools/UI/UXML/DevConsole.uxml`
- Create: `Reloader/Assets/_Project/DevTools/UI/USS/DevConsole.uss`
- Create: `Reloader/Assets/_Project/UI/Tests/PlayMode/DevConsoleScreenPlayModeTests.cs`

**Step 1: Write the failing tests**

Add PlayMode tests that prove:

```csharp
[UnityTest]
public IEnumerator OpeningConsole_RaisesUiStateAndBlocksGameplayInput()
{
    // Arrange runtime root + console controller
    // Open console through the controller
    // Assert IUiStateEvents reports console visible and cursor/gameplay input are blocked
}

[UnityTest]
public IEnumerator ClosingConsole_ClearsUiStateAndRestoresGameplayInput()
{
    // Arrange open console
    // Close console
    // Assert gameplay input is restored
}
```

**Step 2: Run test to verify it fails**

Run: targeted UI PlayMode test for `DevConsoleScreenPlayModeTests`

Expected: FAIL because there is no console screen or UI-state event support yet.

**Step 3: Write minimal implementation**

Extend `IUiStateEvents` and `DefaultRuntimeEvents` with console visibility:

```csharp
bool IsDevConsoleVisible { get; }
event Action<bool> OnDevConsoleVisibilityChanged;
void RaiseDevConsoleVisibilityChanged(bool isVisible);
```

Extend `IPlayerInputSource` / `PlayerInputReader` with dev-console specific input:

```csharp
bool ConsumeDevConsoleTogglePressed();
bool ConsumeAutocompletePressed();
int ConsumeSuggestionDelta();
```

Add the new UI Toolkit screen id and bind it through the runtime bridge. Keep the controller thin:

```csharp
public void Configure(DevToolsRuntime runtime, IUiStateEvents uiStateEvents = null)
{
    _runtime = runtime;
    _uiStateEvents = uiStateEvents;
}
```

The controller should own open/close, suggestion selection, and command submission. `Esc` should close the console before it reaches the general escape-unlock path.

**Step 4: Run test to verify it passes**

Run: targeted UI PlayMode test for `DevConsoleScreenPlayModeTests`

Expected: PASS

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Runtime/IUiStateEvents.cs \
        Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs \
        Reloader/Assets/_Project/Player/Scripts/IPlayerInputSource.cs \
        Reloader/Assets/_Project/Player/Scripts/PlayerInputReader.cs \
        Reloader/Assets/_Project/Player/Scripts/PlayerCursorLockController.cs \
        Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiRuntimeCompositionIds.cs \
        Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitRuntimeInstaller.cs \
        Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs \
        Reloader/Assets/_Project/DevTools/Scripts/UI \
        Reloader/Assets/_Project/DevTools/UI/UXML/DevConsole.uxml \
        Reloader/Assets/_Project/DevTools/UI/USS/DevConsole.uss \
        Reloader/Assets/_Project/UI/Tests/PlayMode/DevConsoleScreenPlayModeTests.cs
git commit -m "feat: add runtime dev console screen and input plumbing"
```

### Task 3: Implement Autocomplete and Item Resolution for `give item`

**Files:**
- Modify: `Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevItemLookupService.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandExecutors/DevGiveItemCommand.cs`
- Create: `Reloader/Assets/_Project/DevTools/Tests/EditMode/DevItemLookupServiceTests.cs`
- Create: `Reloader/Assets/_Project/DevTools/Tests/PlayMode/Reloader.DevTools.Tests.PlayMode.asmdef`
- Create: `Reloader/Assets/_Project/DevTools/Tests/PlayMode/DevGiveItemCommandPlayModeTests.cs`

**Step 1: Write the failing tests**

Add EditMode coverage for item resolution and suggestion ranking:

```csharp
[Test]
public void Suggestions_RankDefinitionIdPrefixBeforeDisplayNameContains()
{
    // ammo-308 should rank above unrelated contains matches
}

[Test]
public void Resolve_ByDisplayName_ReturnsStableDefinitionId()
{
    var resolved = lookup.Resolve("308 Winchester FMJ");
    Assert.That(resolved.DefinitionId, Is.EqualTo("ammo-308"));
}
```

Add PlayMode coverage for inventory grant:

```csharp
[UnityTest]
public IEnumerator GiveItemCommand_StoresResolvedItemInInventory()
{
    // Arrange inventory controller with item definition registry
    // Execute: give item ammo-308 50
    // Assert runtime inventory quantity changed
}
```

**Step 2: Run test to verify it fails**

Run: targeted EditMode and PlayMode tests for `DevItemLookupServiceTests` and `DevGiveItemCommandPlayModeTests`

Expected: FAIL because item lookup and the command executor do not exist yet.

**Step 3: Write minimal implementation**

Use `PlayerInventoryController.GetItemDefinitionRegistrySnapshot()` as the lookup source. The lookup service should normalize both id and display name:

```csharp
public bool TryResolve(string rawToken, out ItemDefinition definition)
{
    // exact id
    // id prefix
    // display-name prefix
    // contains fallback
}
```

The give-item executor should route through inventory APIs instead of mutating UI state:

```csharp
for (var i = 0; i < quantity; i++)
{
    if (!_inventoryController.TryStoreItem(definition.DefinitionId))
    {
        return DevCommandResult.Failure("Inventory full.");
    }
}
```

If stackable semantics are available for the target item, prefer the stack-aware runtime path over one-by-one inserts.

**Step 4: Run test to verify it passes**

Run: targeted EditMode and PlayMode tests for `DevItemLookupServiceTests` and `DevGiveItemCommandPlayModeTests`

Expected: PASS

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs \
        Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevItemLookupService.cs \
        Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandExecutors/DevGiveItemCommand.cs \
        Reloader/Assets/_Project/DevTools/Tests/EditMode/DevItemLookupServiceTests.cs \
        Reloader/Assets/_Project/DevTools/Tests/PlayMode
git commit -m "feat: add dev console item autocomplete and give item command"
```

### Task 4: Add `noclip` and `noclip speed` to Player Movement

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerMover.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevPlayerMovementOverride.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandExecutors/DevNoclipCommand.cs`
- Create: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerDevNoclipPlayModeTests.cs`

**Step 1: Write the failing tests**

Add PlayMode tests that prove:

```csharp
[Test]
public void Tick_NoclipEnabled_IgnoresGravityAndGroundSnap()
{
    // Arrange noclip on
    // Tick mover
    // Assert vertical velocity not forced by normal gravity path
}

[Test]
public void Tick_NoclipEnabled_UsesDevNoclipSpeedInsteadOfWalkSprintSettings()
{
    // Arrange noclip speed 12
    // Tick mover with move input
    // Assert displacement matches noclip speed path
}
```

**Step 2: Run test to verify it fails**

Run: targeted Player PlayMode tests for `PlayerDevNoclipPlayModeTests`

Expected: FAIL because the mover does not have a noclip override path yet.

**Step 3: Write minimal implementation**

Add a narrow movement override seam to `PlayerMover`:

```csharp
public void SetDevNoclip(bool isEnabled, float speed)
{
    _devNoclipEnabled = isEnabled;
    _devNoclipSpeed = Mathf.Max(0.1f, speed);
}
```

In `Tick`, branch early into a noclip path:

```csharp
if (_devNoclipEnabled)
{
    var moveInput = Vector2.ClampMagnitude(_inputSource.MoveInput, 1f);
    var move = (transform.right * moveInput.x) + (transform.forward * moveInput.y);
    transform.position += move.normalized * _devNoclipSpeed * deltaTime;
    VerticalVelocity = 0f;
    _horizontalVelocity = Vector3.zero;
    PublishLocomotionFrame(transform.position, move * _devNoclipSpeed, false, deltaTime);
    return;
}
```

Do not overwrite `_settings.WalkSpeed` or `_settings.SprintSpeed`; noclip speed must remain a separate developer override.

**Step 4: Run test to verify it passes**

Run: targeted Player PlayMode tests for `PlayerDevNoclipPlayModeTests`

Expected: PASS

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Player/Scripts/PlayerMover.cs \
        Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevPlayerMovementOverride.cs \
        Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandExecutors/DevNoclipCommand.cs \
        Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerDevNoclipPlayModeTests.cs
git commit -m "feat: add dev noclip movement commands"
```

### Task 5: Add Persistent Bullet Traces

**Files:**
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevTraceRuntime.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevTraceSegmentView.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandExecutors/DevTracesCommand.cs`
- Create: `Reloader/Assets/_Project/DevTools/Tests/PlayMode/DevTraceRuntimePlayModeTests.cs`

**Step 1: Write the failing tests**

Add PlayMode coverage:

```csharp
[UnityTest]
public IEnumerator PersistentTracesEnabled_WeaponFireCreatesVisibleTraceSegment()
{
    // Arrange traces on and runtime listening to weapon events
    // Raise weapon fired + projectile hit
    // Assert one visible runtime trace object exists
}

[UnityTest]
public IEnumerator PersistentTracesDisabled_DoesNotCreateTraceSegments()
{
    // Arrange traces off
    // Raise events
    // Assert no trace objects exist
}
```

**Step 2: Run test to verify it fails**

Run: targeted PlayMode tests for `DevTraceRuntimePlayModeTests`

Expected: FAIL because no runtime trace service exists yet.

**Step 3: Write minimal implementation**

Subscribe to `IWeaponEvents` or the runtime hub and create visible world-space segments. Keep the first slice simple:

```csharp
public void RecordShot(Vector3 origin, Vector3 endPoint)
{
    if (!_state.PersistentTracesEnabled)
    {
        return;
    }

    var segment = _pool.Get();
    segment.Show(origin, endPoint, _traceColor, _traceLifetimeSeconds);
}
```

Use the `OnWeaponFired` origin with the later `OnProjectileHit` point to complete the segment. If no hit arrives within the frame budget, use a configurable fallback forward distance so the trace still appears for misses.

**Step 4: Run test to verify it passes**

Run: targeted PlayMode tests for `DevTraceRuntimePlayModeTests`

Expected: PASS

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevTraceRuntime.cs \
        Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevTraceSegmentView.cs \
        Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandExecutors/DevTracesCommand.cs \
        Reloader/Assets/_Project/DevTools/Tests/PlayMode/DevTraceRuntimePlayModeTests.cs
git commit -m "feat: add persistent dev bullet traces"
```

### Task 6: Add Spawnable NPC Catalog and `spawn npc`

**Files:**
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Data/DevNpcSpawnCatalog.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevNpcSpawnService.cs`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandExecutors/DevSpawnNpcCommand.cs`
- Create: `Reloader/Assets/_Project/DevTools/Data/DevNpcSpawnCatalog.asset`
- Create: `Reloader/Assets/_Project/DevTools/Tests/EditMode/DevNpcSpawnCatalogTests.cs`
- Create: `Reloader/Assets/_Project/DevTools/Tests/PlayMode/DevSpawnNpcCommandPlayModeTests.cs`

**Step 1: Write the failing tests**

Add EditMode coverage:

```csharp
[Test]
public void Catalog_ResolvesStableSpawnIdToPrefab()
{
    Assert.That(catalog.TryResolve("npc.police", out var prefab), Is.True);
}
```

Add PlayMode coverage:

```csharp
[UnityTest]
public IEnumerator SpawnNpcCommand_InstantiatesConfiguredPrefabInFrontOfPlayer()
{
    // Arrange player transform + spawn catalog
    // Execute: spawn npc npc.police
    // Assert prefab instance exists near the player
}
```

**Step 2: Run test to verify it fails**

Run: targeted EditMode and PlayMode tests for `DevNpcSpawnCatalogTests` and `DevSpawnNpcCommandPlayModeTests`

Expected: FAIL because no spawn catalog or command exists yet.

**Step 3: Write minimal implementation**

Keep spawn data explicit and runtime-safe:

```csharp
[CreateAssetMenu(fileName = "DevNpcSpawnCatalog", menuName = "Reloader/DevTools/NPC Spawn Catalog")]
public sealed class DevNpcSpawnCatalog : ScriptableObject
{
    [SerializeField] private Entry[] _entries;
}
```

The spawn service should place prefabs a fixed distance in front of the player, with an optional raycast-to-ground correction:

```csharp
var spawnPosition = player.position + player.forward * 3f;
var instance = Object.Instantiate(prefab, spawnPosition, Quaternion.identity);
```

Autocomplete for `spawn npc` should come directly from this catalog.

**Step 4: Run test to verify it passes**

Run: targeted EditMode and PlayMode tests for `DevNpcSpawnCatalogTests` and `DevSpawnNpcCommandPlayModeTests`

Expected: PASS

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/DevTools/Scripts/Data/DevNpcSpawnCatalog.cs \
        Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevNpcSpawnService.cs \
        Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandExecutors/DevSpawnNpcCommand.cs \
        Reloader/Assets/_Project/DevTools/Data/DevNpcSpawnCatalog.asset \
        Reloader/Assets/_Project/DevTools/Tests/EditMode/DevNpcSpawnCatalogTests.cs \
        Reloader/Assets/_Project/DevTools/Tests/PlayMode/DevSpawnNpcCommandPlayModeTests.cs
git commit -m "feat: add dev npc spawn command"
```

### Task 7: Add the Unity Editor Dev Tools Window

**Files:**
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Editor/Reloader.DevTools.Editor.asmdef`
- Create: `Reloader/Assets/_Project/DevTools/Scripts/Editor/DevToolsWindow.cs`
- Create: `Reloader/Assets/_Project/DevTools/Tests/EditMode/DevToolsWindowTests.cs`

**Step 1: Write the failing tests**

Add EditMode coverage that proves the window forwards to the runtime backend:

```csharp
[Test]
public void ExecuteButton_ForwardsCommandStringToRuntime()
{
    // Arrange fake runtime
    // Enter command text
    // Click execute
    // Assert runtime.TryExecute was called
}
```

**Step 2: Run test to verify it fails**

Run: targeted EditMode tests for `DevToolsWindowTests`

Expected: FAIL because the editor window does not exist yet.

**Step 3: Write minimal implementation**

Keep the window thin and backend-driven:

```csharp
if (GUILayout.Button("Execute"))
{
    runtime.TryExecute(_commandText, out _lastResult);
}
```

Add only the approved quick controls:

- command input
- execute button
- `noclip` toggle button
- `traces` toggle button
- noclip speed field

**Step 4: Run test to verify it passes**

Run: targeted EditMode tests for `DevToolsWindowTests`

Expected: PASS

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/DevTools/Scripts/Editor \
        Reloader/Assets/_Project/DevTools/Tests/EditMode/DevToolsWindowTests.cs
git commit -m "feat: add editor window for dev tools"
```

### Task 8: Verify the End-to-End Slice

**Files:**
- Verify only

**Step 1: Run focused EditMode suites**

Run: targeted EditMode tests for:

- `DevCommandCatalogTests`
- `DevCommandParsingTests`
- `DevItemLookupServiceTests`
- `DevNpcSpawnCatalogTests`
- `DevToolsWindowTests`

Expected: PASS

**Step 2: Run focused PlayMode suites**

Run: targeted PlayMode tests for:

- `DevConsoleScreenPlayModeTests`
- `DevGiveItemCommandPlayModeTests`
- `PlayerDevNoclipPlayModeTests`
- `DevTraceRuntimePlayModeTests`
- `DevSpawnNpcCommandPlayModeTests`

Expected: PASS

**Step 3: Perform runtime smoke check in Unity**

Manual smoke checklist:

1. Open Play Mode in editor.
2. Open console.
3. Type `gi` and confirm `give item` appears in suggestions.
4. Type part of an item display name and accept an autocomplete suggestion.
5. Run `noclip on`, move through geometry, then run `noclip speed 12`.
6. Run `traces persistent on`, fire a shot, and confirm a visible trace remains.
7. Run `spawn npc npc.police` and confirm the prefab appears in front of the player.
8. Open the editor window and repeat one command through the shared backend.

Expected: all checklist items succeed without breaking cursor lock or normal menu behavior.

**Step 4: Commit**

```bash
git add .
git commit -m "test: verify developer testing tools slice"
```

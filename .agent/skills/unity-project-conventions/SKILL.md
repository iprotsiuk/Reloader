---
name: unity-project-conventions
description: Enforces Reloader project coding standards, folder structure, and Unity patterns. Use when creating new scripts, ScriptableObjects, MonoBehaviours, prefabs, or adding any new feature to the Unity project.
---

# Unity Project Conventions

## When to Use

- Creating any new C# script in the project
- Adding a new feature or system
- Creating ScriptableObject definitions
- Adding prefabs or scenes
- NOT appropriate when: editing docs, plans, or non-Unity files

## Project Architecture

Read `docs/design/core-architecture.md` for shared patterns, SO system, and project structure. For domain-specific design, check `docs/design/README.md` to find the right module doc for your task.

## Folder Rules

All custom code goes under `Reloader/Assets/_Project/<FeatureName>/`:

```
Reloader/Assets/_Project/<Feature>/
├── Scripts/          # C# MonoBehaviours, SOs, interfaces
├── Data/             # ScriptableObject asset instances (.asset files)
├── Prefabs/          # Prefab assets
├── UI/               # UI-specific prefabs and scripts (if feature has UI)
└── Scenes/           # Feature-specific scenes (if applicable)
```

- NEVER put custom code in `Reloader/Assets/ThirdParty/`
- NEVER put scripts in the root `Reloader/Assets/` folder
- NEVER create a new top-level folder under `Reloader/Assets/_Project/` without checking the design doc first
- Implemented exception: FPS ADS/optics framework currently lives in `Reloader/Assets/Game/Weapons/**` and should be extended in-place for optics/sights/aiming work.

## Naming Conventions

| Type | Convention | Example |
|------|-----------|---------|
| Scripts | PascalCase | `PowderDefinition.cs` |
| ScriptableObject definitions | `<Thing>Definition.cs` | `WeaponDefinition.cs` |
| Runtime instances | `<Thing>Instance.cs` | `AmmoInstance.cs` |
| Managers | `<System>Manager.cs` | `GameManager.cs` |
| Interfaces | `I<Name>.cs` | `ISaveable.cs` |
| Events | Runtime port interfaces (`I*Events.cs`) + payload types (`*EventsTypes.cs`) | `IInventoryEvents.cs` |
| SO asset files | PascalCase descriptive | `Hodgdon_Varget.asset` |
| Prefabs | PascalCase | `BoltActionRifle.prefab` |
| Scenes | PascalCase | `MainTown.unity` |
| Enums | PascalCase, singular | `PrimerType`, `JamState` |
| Private fields | `_camelCase` with underscore prefix | `_currentCharge` |
| Public properties | PascalCase | `CurrentCharge` |
| Serialized private fields | `_camelCase` with `[SerializeField]` | `[SerializeField] private float _powderCharge;` |

## Coding Patterns

### ScriptableObject Definitions

Every game item type has a SO definition. Always include the extensibility field:

```csharp
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Powder", menuName = "Reloader/Components/Powder")]
public class PowderDefinition : ComponentDefinition
{
    [Header("Powder Properties")]
    [SerializeField] private float _burnRate;
    [SerializeField] private float _packageCount; // pounds per package
    [SerializeField] private float _packagePrice;
    [SerializeField] private float _temperatureSensitivity;

    [Header("Factory Deviation (drives accuracy simulation)")]
    [SerializeField] private float _chargeWeightVariance;
    [SerializeField] private float _lotToLotVariance;
    [SerializeField] private float _batchBurnRateDeviation;

    [Header("Load Data")]
    [SerializeField] private List<LoadDataEntry> _loadData;

    public float BurnRate => _burnRate;
    public float PackageCount => _packageCount;
    public float PackagePrice => _packagePrice;
    public float UnitPrice => _packagePrice / Mathf.Max(_packageCount, 0.0001f);
    public float TemperatureSensitivity => _temperatureSensitivity;
    public float ChargeWeightVariance => _chargeWeightVariance;
    public float LotToLotVariance => _lotToLotVariance;
    public float BatchBurnRateDeviation => _batchBurnRateDeviation;

    [Header("Extensibility")]
    [SerializeField] private List<CustomProperty> _customProperties;
}
```

Always use `[CreateAssetMenu]` so new assets can be created from Unity's right-click menu.

See `adding-game-content` skill for complete required fields per SO type. Every component definition includes factory deviation fields that drive the accuracy simulation — see `reloading-domain-knowledge` skill for how these map to accuracy variables.

### Manager Pattern

Managers use `DontDestroyOnLoad` and provide a static Instance:

```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

Save orchestration terminology contract: use `SaveCoordinator` (service) as the canonical save pipeline entrypoint. Do not introduce legacy save-manager naming in new docs/examples unless it is explicitly a thin wrapper over `SaveCoordinator`.

### Event Bus Usage

Terminology contract: "EventBus" is the architecture pattern. Runtime implementation contract is `IGameEventsRuntimeHub` + bounded event ports.

Use runtime ports/hub for cross-system communication. Never call another domain's manager directly from gameplay code:

```csharp
// GOOD - runtime port/hub access
var events = RuntimeKernelBootstrapper.Events;
events?.InventoryEvents?.RaiseInventoryChanged();

// BAD - direct coupling between domains
InventoryManager.Instance.AddItem(newAmmo);
```

When extending contracts, update these together:
- Runtime interfaces under `Core/Scripts/Runtime/*Events*.cs`
- Payload types under `Core/Scripts/Events/**`
- Routing/docs guardrails (`.cursor/rules/core-events-context.mdc`, `docs/design/extensible-development-contracts.md`)

### Definition vs. Instance

- **Definition (SO):** What something IS. "A .308 Win case by Lapua." Lives in `Data/` folder as `.asset`.
- **Instance (runtime):** A specific physical one. "This case, fired 3 times in rifle #A7F2." Created at runtime, serialized by the save pipeline (`SaveCoordinator` + modules).

```csharp
[System.Serializable]
public class CasingInstance : ItemInstance
{
    // Do NOT redeclare "definition" in subclasses.
    // Use a typed accessor backed by ItemInstance.definition.
    public CaseDefinition CaseDefinition => definition as CaseDefinition;
    public int timesFired;
    public string lastFiredInChamber; // weapon.barrel.chamberID for fire-forming
    public bool neckSizedOnly;
    public float currentLength;
    public float shoulderDatum; // shoulder position relative to case head (inches)
    public bool annealed;
    public bool lubed;
    public bool cleaned;
    public PrimerInstance primerInstance; // null if unprimed
    public CaseCondition condition; // Good / Cracked / Stretched / Crushed

    // Per-instance factory deviation (sampled from definition on spawn)
    public float actualWeight;
    public float actualNeckThickness;
}
```

Contract rule: `ItemInstance.definition` is the canonical definition field. Subclasses must not declare another `definition` field.

Note: every component instance carries per-instance values sampled from its definition's deviation fields at spawn time. A box of Lapua brass spawns 50 CasingInstances with tight weight spread; budget brass spawns with wide spread. The player can sort/measure to find the best ones.

### Persistence Change Checklist

When changing runtime fields that must persist across save/load, update all three in the same change:

1. Domain payload contract (`SaveEnvelope` module payload shape or equivalent state DTO)
2. Save/load module implementation (capture + restore + validation)
3. Migration note/step for schema evolution (`schemaVersion` path)

Never add persisted runtime fields without updating the save contract at the same time.

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Putting scripts in ThirdParty/ | Always use `_Project/<Feature>/Scripts/` |
| Hard-coding item data in MonoBehaviours | Use ScriptableObject definitions |
| Systems calling each other directly | Use runtime event ports/hub (`IGameEventsRuntimeHub`) |
| Single weapon condition float | Track per-part condition on WeaponPartInstance |
| Preventing player from making mistakes | Simulate the consequence instead |
| Creating scenes without checking design doc | Follow world topology contract (`Bootstrap -> MainTown -> IndoorRangeInstance`); `MainWorld` is compatibility-only |

## Baked Lighting Safety

- If a scene relies on baked lighting, version both `LightingData.asset` and `LightingData.asset.meta` for that scene.
- Never delete or rename baked `LightingData` files outside Unity without moving their `.meta` pair.
- After lighting-related merges, run a headless Unity open (`-batchmode -quit`) and check logs for missing `LightingData.asset` warnings before claiming success.

## Travel + Viewmodel Invariants

When adding new travel-connected scenes (or editing existing player rigs), preserve these runtime invariants:

- Keep first-person arms under `PlayerRoot/CameraPivot/PlayerArms` in every travel destination scene.
- `PlayerArms` must stay in canonical local pose:
  - position `(0, -0.24, 1.56)`
  - rotation `(-90, 0, 0)`
  - scale `(0.42, 0.42, 0.42)`
- `PlayerArms` `Animator` must not drive root motion for travel rigs (`applyRootMotion=false`).
- For persistent player roots (`DontDestroyOnLoad`), re-entry validation must include:
  - `PlayerArms` active in hierarchy
  - child renderers enabled
  - animator culling safe for first-person updates (`AlwaysAnimate`)
- Do not add scene-specific "hide owned pickup" travel workarounds; rely on unified persistence apply.

## Quick Reference

| Aspect | Rule |
|--------|------|
| Design docs | `docs/design/README.md` → routes to modular docs |
| Cross-domain extension guardrails | `docs/design/extensible-development-contracts.md` |
| Custom code root | `Reloader/Assets/_Project/` |
| Third-party assets | `Reloader/Assets/ThirdParty/` (read-only) |
| New items | ScriptableObject definition + CreateAssetMenu |
| Cross-system comms | Runtime event ports/hub (`IGameEventsRuntimeHub`) |
| Singletons | DontDestroyOnLoad + static Instance |
| Save/load | `SaveCoordinator` orchestrates module serializers and migrations |
| Render pipeline | URP (Universal Render Pipeline) |
| Unity version | 6.3 |

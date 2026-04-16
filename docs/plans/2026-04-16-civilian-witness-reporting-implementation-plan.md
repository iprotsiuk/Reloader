# Civilian Witness Reporting Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make eligible civilian deaths raise police heat through a dedicated inbound crime-reporting seam.

**Architecture:** Add a source-agnostic `ILawEnforcementCrimeReporter.ReportCrime(CrimeType crimeType)` contract that delegates to the existing `PoliceHeatRuntime.ReportCrime(...)` ingress. Add a small civilian-side death reporter wired by `CivilianPopulationRuntimeBridge`, using `HumanoidDamageReceiver.Died` for the v0.1 MVP and excluding cops, dispatch reserves, and active contract targets by default.

**Tech Stack:** Unity, C#, EditMode tests, PlayMode tests, existing `IGameEventsRuntimeHub` output ports, existing NPC population bridge

---

## Implementation Status [v0.1]

Status: Complete.

Implemented contract:
- `ILawEnforcementCrimeReporter.ReportCrime(CrimeType)` is the inbound crime-reporting seam and delegates into `PoliceHeatRuntime.ReportCrime(...)`.
- `CivilianPopulationRuntimeBridge` injects `CivilianWitnessReporter` into eligible spawned civilians and uses explicit serialized/configured contract runtime provider and crime reporter dependencies.
- `MainTown` authors `MainTownPopulationRuntime` with `MainTownContractRuntime` as both the contract runtime provider and crime reporter.
- Active-target witness exclusion resolves through the bridge's explicit contract runtime provider dependency and no longer relies on scene-global `StaticContractRuntimeProvider` lookup.
- Eligible civilian deaths report `CrimeType.Murder` once through `HumanoidDamageReceiver.Died`.
- Cops, dispatch-only reserve police, and active contract targets are excluded from witness reporter injection by default.
- Witness reporter wiring does not use a scene-wide fallback lookup; late `ConfigureCrimeReporter(...)` calls refresh existing spawned eligible witnesses to avoid stale/null reporter wiring.

Completion evidence:
- Contract/EditMode coverage verifies the reporter seam stays separate from `ILawEnforcementEvents` and `IGameEventsRuntimeHub`, and that `PoliceHeatRuntime`, `PoliceHeatController`, and `StaticContractRuntimeProvider` route murder reports into heat.
- Bridge/EditMode coverage verifies eligible spawned civilians receive `CivilianWitnessReporter`, excluded records do not, and late reporter configuration refreshes existing witnesses.
- PlayMode integration coverage verifies an eligible spawned civilian death with `StaticContractRuntimeProvider` raises murder heat and wanted level 3.
- Production `MainTown` witness-kill coverage verifies an eligible spawned civilian death reports murder heat through the authored `MainTownPopulationRuntime -> MainTownContractRuntime` scene wiring.

---

## Scope Guardrails

- Do not add a full perception, panic, fleeing, or investigation system.
- Do not overload `ILawEnforcementEvents`; it remains output-only heat broadcasting.
- Do not bypass `PoliceHeatRuntime.ReportCrime(...)`; all witness reports must land there.
- Report only `CrimeType.Murder` for the first MVP.
- Report once per eligible civilian death.
- Exclude police records, dispatch-only reserves, and active contract targets from witness reporter injection unless a later slice explicitly opts them in.

### Task 1: Add The Inbound Crime Reporter Contract

**Files:**
- Create: `Reloader/Assets/_Project/Core/Scripts/Runtime/ILawEnforcementCrimeReporter.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/PoliceHeatRuntime.cs`
- Modify: `Reloader/Assets/_Project/LawEnforcement/Scripts/Runtime/PoliceHeatController.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/ContractEscapeResolutionRuntime.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/StaticContractRuntimeProvider.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/RuntimeKernelTests.cs`
- Test: `Reloader/Assets/_Project/LawEnforcement/Tests/EditMode/PoliceHeatControllerTests.cs`

**Step 1: Write the failing contract tests**

Add assertions that:
- `ILawEnforcementEvents` is not assignable to `ILawEnforcementCrimeReporter`.
- `IGameEventsRuntimeHub` is not assignable to `ILawEnforcementCrimeReporter`.
- `PoliceHeatRuntime` implements `ILawEnforcementCrimeReporter` and `ReportCrime(CrimeType.Murder)` raises wanted heat.
- `PoliceHeatController` implements `ILawEnforcementCrimeReporter`.
- `StaticContractRuntimeProvider.ReportCrime(CrimeType.Murder)` updates `CurrentHeatState` through its internal runtime.

**Step 2: Run the focused red tests**

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.RuntimeKernelTests|Reloader.LawEnforcement.Tests.EditMode.PoliceHeatControllerTests|Reloader.Core.Tests.EditMode.StaticContractRuntimeProviderTests" "$(pwd)/tmp/civilian-witness-contract-red.xml" "$(pwd)/tmp/civilian-witness-contract-red.log"
```

Expected: FAIL because `ILawEnforcementCrimeReporter` and provider/controller implementations do not exist yet.

**Step 3: Implement the minimal contract**

Create:

```csharp
using Reloader.Core.Events;

namespace Reloader.Core.Runtime
{
    public interface ILawEnforcementCrimeReporter
    {
        void ReportCrime(CrimeType crimeType);
    }
}
```

Then:
- Make `PoliceHeatRuntime` implement `ILawEnforcementCrimeReporter`.
- Make `PoliceHeatController` implement `ILawEnforcementCrimeReporter`.
- Add `ContractEscapeResolutionRuntime.ReportCrime(CrimeType crimeType)` delegating to `_policeHeatRuntime.ReportCrime(crimeType)`.
- Make `StaticContractRuntimeProvider` implement `ILawEnforcementCrimeReporter` and delegate `ReportCrime` to `EnsureRuntime().ReportCrime(crimeType)`.
- Do not add this reporter to `IRuntimeEvents`, `IGameEventsRuntimeHub`, `DefaultRuntimeEvents`, or `RuntimeKernelBootstrapper`.

**Step 4: Run the focused green tests**

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.RuntimeKernelTests|Reloader.LawEnforcement.Tests.EditMode.PoliceHeatControllerTests|Reloader.Core.Tests.EditMode.StaticContractRuntimeProviderTests" "$(pwd)/tmp/civilian-witness-contract-green.xml" "$(pwd)/tmp/civilian-witness-contract-green.log"
```

Expected: PASS.

### Task 2: Add The Civilian Death Reporter Component

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Combat/CivilianWitnessReporter.cs`
- Create: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/CivilianWitnessReporterPlayModeTests.cs`

**Step 1: Write failing PlayMode coverage**

Cover:
- A reporter attached to a normal civilian subscribes to `HumanoidDamageReceiver.Died`.
- A lethal hit reports `CrimeType.Murder` once to an injected `ILawEnforcementCrimeReporter`.
- Repeated `Died`/damage after death does not report twice.
- Missing reporter dependency does not throw and does not report.

**Step 2: Run the focused red test**

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.CivilianWitnessReporterPlayModeTests" "$(pwd)/tmp/civilian-witness-reporter-red.xml" "$(pwd)/tmp/civilian-witness-reporter-red.log"
```

Expected: FAIL because `CivilianWitnessReporter` does not exist yet.

**Step 3: Implement the minimal component**

Implementation shape:
- `[DisallowMultipleComponent] public sealed class CivilianWitnessReporter : MonoBehaviour`
- Resolve `HumanoidDamageReceiver` on the same GameObject.
- Allow `CivilianPopulationRuntimeBridge` or tests to inject `ILawEnforcementCrimeReporter` through a public `Configure(ILawEnforcementCrimeReporter reporter)` method.
- Do not perform any scene-wide interface scan or fallback lookup; if the reporter is not configured, the component remains inert and does not report.
- Subscribe in `OnEnable`, unsubscribe in `OnDisable`.
- On first `Died`, call `ReportCrime(CrimeType.Murder)` and latch `_hasReported`.
- Keep any range/LOS check local to this component if added; do not create shared perception registries, senses, or civilian AI reactions.

**Step 4: Run the focused green test**

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.CivilianWitnessReporterPlayModeTests" "$(pwd)/tmp/civilian-witness-reporter-green.xml" "$(pwd)/tmp/civilian-witness-reporter-green.log"
```

Expected: PASS.

### Task 3: Inject Witness Capability From Civilian Population Bridge

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`
- Optionally modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`

**Step 1: Write failing bridge coverage**

Add tests that:
- Normal townsfolk spawned by `RebuildScenePopulation()` receive `CivilianWitnessReporter`.
- Spawned cops do not receive `CivilianWitnessReporter`.
- Dispatch-only reserve police do not receive `CivilianWitnessReporter` when spawned for dispatch.
- Active contract targets do not receive `CivilianWitnessReporter` by default.
- A civilian without a configured reporter stays inert and does not report murder.
- Normal spawned civilians still keep `HumanoidDamageReceiver`, ragdoll, corpse loot, metadata, and `AmbientCitizenCapability`.

**Step 2: Run the focused red tests**

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests" "$(pwd)/tmp/civilian-witness-bridge-red.xml" "$(pwd)/tmp/civilian-witness-bridge-red.log"
```

Expected: FAIL because the bridge does not inject/exclude `CivilianWitnessReporter` yet.

**Step 3: Implement the bridge injection**

In `CivilianPopulationRuntimeBridge.InitializeSpawnedCivilian(...)`, after `EnsureCivilianActorComponents(civilian)` and metadata initialization, add a small helper such as `ConfigureCivilianWitnessReporter(civilian, record)`.

Eligibility:
- include when `record != null`, `record.IsAlive`, and `record.PoolId != "cops"`.
- exclude when `IsDispatchReservePoliceRecord(record)` is true.
- exclude when `IsActiveContractTarget(record)` is true.
- configure the reporter explicitly from the bridge's known `ILawEnforcementCrimeReporter` dependency when the record is eligible.
- remove an existing reporter component if the record is not eligible.

Do not add this to `NpcAgent` capabilities; it is a passive combat/death hook, not a decision/action provider.

**Step 4: Run the focused green tests**

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests" "$(pwd)/tmp/civilian-witness-bridge-green.xml" "$(pwd)/tmp/civilian-witness-bridge-green.log"
```

Expected: PASS.

### Task 4: Verify Police Heat Integration

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/CivilianWitnessReporterPlayModeTests.cs`
- Optionally modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`

**Step 1: Write focused integration coverage**

Use a `StaticContractRuntimeProvider` wired directly as the reporter implementation, via bridge/test configuration rather than discovery. Kill an eligible normal civilian with `HumanoidDamageReceiver.ApplyDamage(...)` and assert:
- `provider.CurrentHeatState.Level` is no longer `Clear`.
- `provider.CurrentHeatState.LastCrimeType == CrimeType.Murder`.
- `provider.CurrentHeatState.WantedLevel == 3`.

If `MainTownContractSlicePlayModeTests` is touched, only add one assertion that a spawned non-target civilian has witness reporting while the active target does not.

**Step 2: Run the focused PlayMode integration test**

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.CivilianWitnessReporterPlayModeTests|Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests" "$(pwd)/tmp/civilian-witness-integration.xml" "$(pwd)/tmp/civilian-witness-integration.log"
```

Expected: PASS.

### Task 5: Widen Verification And Commit

**Files:**
- Modify only the files touched in Tasks 1-4.

**Step 1: Run nearest EditMode coverage**

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.RuntimeKernelTests|Reloader.Core.Tests.EditMode.GameEventsRuntimeBridgeTests|Reloader.Core.Tests.EditMode.StaticContractRuntimeProviderTests|Reloader.LawEnforcement.Tests.EditMode.PoliceHeatControllerTests|Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests" "$(pwd)/tmp/civilian-witness-edit-wide.xml" "$(pwd)/tmp/civilian-witness-edit-wide.log"
```

Expected: PASS.

**Step 2: Run nearest PlayMode coverage**

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.CivilianWitnessReporterPlayModeTests|Reloader.NPCs.Tests.PlayMode.PoliceDispatchCoordinatorPlayModeTests|Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests|Reloader.UI.Tests.PlayMode.CompassHudRuntimeBridgePlayModeTests" "$(pwd)/tmp/civilian-witness-play-wide.xml" "$(pwd)/tmp/civilian-witness-play-wide.log"
```

Expected: PASS.

**Step 3: Run docs/context validation if docs changed**

```bash
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

Expected: PASS.

**Step 4: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Runtime/ILawEnforcementCrimeReporter.cs \
        Reloader/Assets/_Project/Core/Scripts/Runtime/PoliceHeatRuntime.cs \
        Reloader/Assets/_Project/LawEnforcement/Scripts/Runtime/PoliceHeatController.cs \
        Reloader/Assets/_Project/Core/Scripts/Runtime/ContractEscapeResolutionRuntime.cs \
        Reloader/Assets/_Project/Core/Scripts/Runtime/StaticContractRuntimeProvider.cs \
        Reloader/Assets/_Project/NPCs/Scripts/Combat/CivilianWitnessReporter.cs \
        Reloader/Assets/_Project/Core/Tests/EditMode/RuntimeKernelTests.cs \
        Reloader/Assets/_Project/LawEnforcement/Tests/EditMode/PoliceHeatControllerTests.cs \
        Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs \
        Reloader/Assets/_Project/NPCs/Tests/PlayMode/CivilianWitnessReporterPlayModeTests.cs
git commit -m "feat: report civilian witness deaths into police heat"
```

# Ragdoll Hitboxes And Blood Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add an NPC-wide humanoid combat stack with zone hitboxes, energy-aware lethality, immediate directional ragdoll takeover on lethal hits, impact blood bursts, and death puddles.

**Architecture:** Extend the ballistic impact contract first so projectiles report direction and energy-driving metadata, then route hits through a shared NPC combat pipeline (`BodyZoneHitbox` -> `HumanoidDamageReceiver` -> ragdoll/blood controllers). Keep contract-target elimination as a thin bridge on top of the shared death event instead of a separate health system. Use prefab-builder tooling to make the NPC foundation and role prefabs auto-bind against a humanoid rig contract rather than hand-wiring every NPC.

**Tech Stack:** Unity 6.3 C#, NUnit EditMode and PlayMode tests, prefab authoring under `Reloader/Assets/_Project/NPCs/**`, ballistic runtime under `Reloader/Assets/_Project/Weapons/**`, existing `scripts/run-unity-tests.sh` verification flow

---

### Task 1: Add Red Tests For Impact Metadata And Energy Resolution

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Tests/EditMode/HumanoidImpactResolutionEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/Reloader.NPCs.Tests.EditMode.asmdef`
- Modify: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/Reloader.NPCs.Tests.PlayMode.asmdef`
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponProjectilePlayModeTests.cs`

**Step 1: Write the failing tests**

Add coverage that proves:
- high delivered energy to `Head` or `Neck` resolves lethal while the same energy to `ArmL` does not
- low delivered energy to `Torso` stays non-lethal
- `WeaponProjectile` forwards projectile travel direction and current speed/impact-driving metadata into the hit payload

Example editmode assertion:

```csharp
[Test]
public void Resolve_WhenHeadHitCarriesRifleEnergy_ReturnsLethal()
{
    var result = HumanoidImpactResolution.Resolve(
        bodyZone: HumanoidBodyZone.Head,
        deliveredEnergyJoules: 900f);

    Assert.That(result.IsLethal, Is.True);
}
```

**Step 2: Run tests to verify they fail**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.HumanoidImpactResolutionEditModeTests" "$(pwd)/tmp/humanoid-impact-red.xml" "$(pwd)/tmp/humanoid-impact-red.log"
bash scripts/run-unity-tests.sh playmode "Reloader.Weapons.Tests.PlayMode.WeaponProjectilePlayModeTests" "$(pwd)/tmp/weapon-projectile-impact-red.xml" "$(pwd)/tmp/weapon-projectile-impact-red.log"
```

Expected:
- the new resolution tests fail because the resolver and zone enum do not exist yet
- the projectile test fails because `ProjectileImpactPayload` does not yet expose direction/speed/energy-driving metadata

**Step 3: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Tests/EditMode/HumanoidImpactResolutionEditModeTests.cs Reloader/Assets/_Project/NPCs/Tests/EditMode/Reloader.NPCs.Tests.EditMode.asmdef Reloader/Assets/_Project/NPCs/Tests/PlayMode/Reloader.NPCs.Tests.PlayMode.asmdef Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponProjectilePlayModeTests.cs
git commit -m "test: add red coverage for humanoid impact resolution"
```

### Task 2: Implement The Shared Impact Contract And Resolver

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidBodyZone.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidImpactResolution.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidImpactResolutionResult.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/ProjectileImpactPayload.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/WeaponProjectile.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`

**Step 1: Write minimal implementation**

- add the shared body-zone enum and resolution result type in `NPCs`
- implement an initial static resolver that maps delivered joules + zone multiplier to `IsLethal`, semantic severity, and recommended ragdoll impulse scalar
- extend `ProjectileImpactPayload` to carry:
  - projectile direction at impact
  - impact speed in meters per second
  - projectile mass in grains
  - delivered energy in joules
- pass ballistic mass from `PlayerWeaponController` into `WeaponProjectile.Initialize`
- have `WeaponProjectile` compute delivered energy from current speed and projectile mass before calling `IDamageable.ApplyDamage`

Example payload shape:

```csharp
public Vector3 Direction { get; }
public float ImpactSpeedMetersPerSecond { get; }
public float ProjectileMassGrains { get; }
public float DeliveredEnergyJoules { get; }
```

**Step 2: Run focused tests to verify green**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.HumanoidImpactResolutionEditModeTests" "$(pwd)/tmp/humanoid-impact-green.xml" "$(pwd)/tmp/humanoid-impact-green.log"
bash scripts/run-unity-tests.sh playmode "Reloader.Weapons.Tests.PlayMode.WeaponProjectilePlayModeTests" "$(pwd)/tmp/weapon-projectile-impact-green.xml" "$(pwd)/tmp/weapon-projectile-impact-green.log"
```

Expected:
- both suites pass with the new payload fields and resolver in place

**Step 3: Refactor only if needed**

- keep energy math centralized in the shared resolver
- do not bury lethality thresholds inside `WeaponProjectile`

**Step 4: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidBodyZone.cs Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidImpactResolution.cs Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidImpactResolutionResult.cs Reloader/Assets/_Project/Weapons/Scripts/Ballistics/ProjectileImpactPayload.cs Reloader/Assets/_Project/Weapons/Scripts/Ballistics/WeaponProjectile.cs Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs
git commit -m "feat: add energy-aware projectile impact contract"
```

### Task 3: Add Red Tests For Zone Hitboxes And Shared Death Routing

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/HumanoidHitboxRigPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ContractTargetDamageablePlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`

**Step 1: Write the failing tests**

Add coverage that proves:
- a projectile hitting the `Head` zone collider resolves through the shared humanoid receiver, not a generic root collider
- lethal shared receiver death notifies `ContractTargetDamageable` and still reports the procedural contract elimination sink
- spawned civilians keep the contract-target bridge narrow while still allowing the shared combat receiver to exist on all NPCs

Example playmode assertion:

```csharp
Assert.That(receiver.LastZone, Is.EqualTo(HumanoidBodyZone.Head));
Assert.That(receiver.LastResult.IsLethal, Is.True);
```

**Step 2: Run tests to verify they fail**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.HumanoidHitboxRigPlayModeTests|Reloader.Weapons.Tests.PlayMode.ContractTargetDamageablePlayModeTests" "$(pwd)/tmp/humanoid-hitbox-red.xml" "$(pwd)/tmp/humanoid-hitbox-red.log"
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests" "$(pwd)/tmp/civilian-combat-bridge-red.xml" "$(pwd)/tmp/civilian-combat-bridge-red.log"
```

Expected:
- tests fail because the zone hitbox components and shared death routing do not exist yet

**Step 3: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Tests/PlayMode/HumanoidHitboxRigPlayModeTests.cs Reloader/Assets/_Project/Weapons/Tests/PlayMode/ContractTargetDamageablePlayModeTests.cs Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs
git commit -m "test: add red coverage for humanoid hitbox routing"
```

### Task 4: Implement Hitbox Rig, Shared Damage Receiver, And Contract Target Bridge

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Combat/BodyZoneHitbox.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidHitboxRig.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidDamageReceiver.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/World/ContractTargetDamageable.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`

**Step 1: Write minimal implementation**

- add a hitbox component that reports semantic body zone and owning rig
- add a rig component that resolves/validates the standard humanoid bones and exposes zone lookup helpers
- add a shared damage receiver that:
  - implements `IDamageable`
  - resolves the struck zone from `BodyZoneHitbox`
  - runs `HumanoidImpactResolution`
  - raises shared death/result events
- convert `ContractTargetDamageable` from “private health pool” to “contract elimination bridge” that subscribes to lethal shared receiver events
- keep `CivilianPopulationRuntimeBridge` responsible only for deciding which civilian is the contract target, not for owning separate ballistics health logic

Example receiver seam:

```csharp
public event Action<HumanoidImpactResolutionResult> LethalImpactResolved;
```

**Step 2: Run focused tests to verify green**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.HumanoidHitboxRigPlayModeTests|Reloader.Weapons.Tests.PlayMode.ContractTargetDamageablePlayModeTests" "$(pwd)/tmp/humanoid-hitbox-green.xml" "$(pwd)/tmp/humanoid-hitbox-green.log"
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests" "$(pwd)/tmp/civilian-combat-bridge-green.xml" "$(pwd)/tmp/civilian-combat-bridge-green.log"
```

**Step 3: Refactor only if needed**

- keep contract elimination as an observer of shared combat death, not a second damage system

**Step 4: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Combat/BodyZoneHitbox.cs Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidHitboxRig.cs Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidDamageReceiver.cs Reloader/Assets/_Project/Weapons/Scripts/World/ContractTargetDamageable.cs Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs
git commit -m "feat: add humanoid hitbox rig and shared damage receiver"
```

### Task 5: Add Red Tests For Ragdoll Impulse And Blood Semantics

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/HumanoidRagdollControllerPlayModeTests.cs`
- Create: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/HumanoidBloodControllerPlayModeTests.cs`

**Step 1: Write the failing tests**

Add coverage that proves:
- lethal hit disables animator/AI dependencies and enables ragdoll rigidbodies
- lethal hit applies force in projectile travel direction to the struck body or torso fallback
- blood controller maps `Head`, `Neck`, `Torso`, `Arm`, `Leg` hits to semantic effect requests
- lethal hits request both impact blood and a death puddle

Example playmode assertion:

```csharp
Assert.That(struckRigidbody.linearVelocity.z, Is.GreaterThan(0f));
Assert.That(effectSink.Requests, Does.Contain(BloodEffectKind.DeathPuddle));
```

**Step 2: Run tests to verify they fail**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.HumanoidRagdollControllerPlayModeTests|Reloader.NPCs.Tests.PlayMode.HumanoidBloodControllerPlayModeTests" "$(pwd)/tmp/humanoid-ragdoll-blood-red.xml" "$(pwd)/tmp/humanoid-ragdoll-blood-red.log"
```

Expected:
- tests fail because the ragdoll/blood controllers and semantic effect catalog do not exist yet

**Step 3: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Tests/PlayMode/HumanoidRagdollControllerPlayModeTests.cs Reloader/Assets/_Project/NPCs/Tests/PlayMode/HumanoidBloodControllerPlayModeTests.cs
git commit -m "test: add red coverage for humanoid ragdoll and blood"
```

### Task 6: Implement Immediate Ragdoll Takeover And Blood Adapter Layer

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidRagdollController.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidBloodController.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Combat/BloodEffectKind.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Combat/BloodVfxCatalog.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcAiController.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/ContractTargetPatrolMotion.cs`

**Step 1: Write minimal implementation**

- implement a ragdoll controller that keeps bodies/joints disabled until lethal hit
- on lethal hit:
  - disable animator, `NpcAiController`, patrol motion, and any registered runtime dependencies
  - enable ragdoll rigidbodies/colliders
  - apply force at the hit point in projectile direction using the recommended impulse scalar
- implement a blood controller that converts impact results into project-owned semantic effect requests
- create a `BloodVfxCatalog` ScriptableObject seam so the purchased blood pack can be mapped later without hardcoding package prefab paths
- if the blood asset pack is still not imported, keep the catalog valid with empty references so tests cover semantics rather than package assets

Example semantic enum:

```csharp
public enum BloodEffectKind
{
    LightImpact,
    HeavyImpact,
    NeckImpact,
    DeathPuddle
}
```

**Step 2: Run focused tests to verify green**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.HumanoidRagdollControllerPlayModeTests|Reloader.NPCs.Tests.PlayMode.HumanoidBloodControllerPlayModeTests" "$(pwd)/tmp/humanoid-ragdoll-blood-green.xml" "$(pwd)/tmp/humanoid-ragdoll-blood-green.log"
```

**Step 3: Refactor only if needed**

- keep blood logic semantic and package-agnostic
- keep dependency shutdown list explicit instead of relying on `GetComponents<MonoBehaviour>()` magic

**Step 4: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidRagdollController.cs Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidBloodController.cs Reloader/Assets/_Project/NPCs/Scripts/Combat/BloodEffectKind.cs Reloader/Assets/_Project/NPCs/Scripts/Combat/BloodVfxCatalog.cs Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcAiController.cs Reloader/Assets/_Project/NPCs/Scripts/Runtime/ContractTargetPatrolMotion.cs
git commit -m "feat: add humanoid ragdoll takeover and blood semantics"
```

### Task 7: Auto-Bind The NPC Foundation And Role Prefabs To The Shared Combat Stack

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Editor/NpcVendorPrefabBuilder.cs`
- Create: `Reloader/Assets/_Project/NPCs/Editor/NpcCombatPrefabValidator.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/NpcFoundation.prefab`
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/Roles/Npc_Police.prefab`
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/Roles/Npc_BankWorker.prefab`
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/Roles/Npc_FrontDeskClerk.prefab`
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/Roles/Npc_PostWorker.prefab`
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/Roles/Npc_WeaponVendor.prefab`
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/Roles/Npc_AmmoVendor.prefab`
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/Roles/Npc_ReloadingSuppliesVendor.prefab`
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/Roles/Npc_RangeSafetyOfficer.prefab`
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/Roles/Npc_GameWarden.prefab`
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/Roles/Npc_CompetitionOrganizer.prefab`
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/Roles/Npc_Competitor.prefab`
- Create: `Reloader/Assets/_Project/NPCs/Tests/EditMode/NpcCombatPrefabValidationEditModeTests.cs`

**Step 1: Write the failing test**

Add editmode validation coverage that proves the rebuilt NPC foundation/role prefabs:
- include the shared combat stack components
- can resolve a humanoid `Animator` / avatar source for auto-binding
- expose the required seven body zones
- fail validation if a prefab lacks the required rig contract

**Step 2: Run test to verify it fails**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.NpcCombatPrefabValidationEditModeTests" "$(pwd)/tmp/npc-combat-prefabs-red.xml" "$(pwd)/tmp/npc-combat-prefabs-red.log"
```

**Step 3: Write minimal implementation**

- update `NpcVendorPrefabBuilder` so the foundation prefab builder adds the shared combat components
- make the builder resolve the humanoid rig source from the approved visual root prefab; if the current nested STYLE root does not expose a usable `Animator`, switch the foundation to the approved rigged humanoid source before proceeding
- auto-create or validate zone collider anchors against the standard humanoid bones
- add an editor validator menu command for batch-checking the role prefabs after rebuild
- rebuild `NpcFoundation` and the role prefab variants so spawned civilians and authored roles inherit the same combat contract

**Step 4: Run focused tests to verify green**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.NpcCombatPrefabValidationEditModeTests|Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests" "$(pwd)/tmp/npc-combat-prefabs-green.xml" "$(pwd)/tmp/npc-combat-prefabs-green.log"
```

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Editor/NpcVendorPrefabBuilder.cs Reloader/Assets/_Project/NPCs/Editor/NpcCombatPrefabValidator.cs Reloader/Assets/_Project/NPCs/Prefabs/NpcFoundation.prefab Reloader/Assets/_Project/NPCs/Prefabs/Roles Reloader/Assets/_Project/NPCs/Tests/EditMode/NpcCombatPrefabValidationEditModeTests.cs
git commit -m "feat: auto-bind npc prefabs to humanoid combat stack"
```

### Task 8: Verify Regressions, Hook Real Blood Assets, And Clean The Diff

**Files:**
- Verify only

**Step 1: Run the focused regression suites**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.HumanoidImpactResolutionEditModeTests|Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests|Reloader.NPCs.Tests.EditMode.NpcCombatPrefabValidationEditModeTests" "$(pwd)/tmp/npc-combat-editmode-full.xml" "$(pwd)/tmp/npc-combat-editmode-full.log"
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.HumanoidHitboxRigPlayModeTests|Reloader.NPCs.Tests.PlayMode.HumanoidRagdollControllerPlayModeTests|Reloader.NPCs.Tests.PlayMode.HumanoidBloodControllerPlayModeTests|Reloader.Weapons.Tests.PlayMode.WeaponProjectilePlayModeTests|Reloader.Weapons.Tests.PlayMode.ContractTargetDamageablePlayModeTests" "$(pwd)/tmp/npc-combat-playmode-full.xml" "$(pwd)/tmp/npc-combat-playmode-full.log"
```

**Step 2: Run repository guardrails**

Run:

```bash
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
git diff --check
```

**Step 3: Hook the purchased blood package into the catalog**

- if the realistic blood pack is imported by this point, create and assign the concrete `BloodVfxCatalog.asset` mappings for impact bursts and death puddles
- if the package is still absent, leave the semantic catalog in place and document the missing asset hookup as the only remaining manual step

**Step 4: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Combat Reloader/Assets/_Project/NPCs/Editor Reloader/Assets/_Project/NPCs/Tests Reloader/Assets/_Project/Weapons/Scripts/Ballistics Reloader/Assets/_Project/Weapons/Scripts/Controllers Reloader/Assets/_Project/Weapons/Scripts/World
git commit -m "feat: ship npc ragdoll hitboxes and blood foundation"
```

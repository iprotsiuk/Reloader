# Bullet Energy Health, Hit Zones, And Blood VFX Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.
> **Status (2026-04-16):** Stages A/B/C are implemented. Keep this plan as historical execution record plus regression guidance; the final docs sync is complete when the domain docs and status board reflect the shipped contracts.

**Goal:** Cut humanoid bullet damage over to impact-energy health damage with zone-specific lethality, 100 baseline health, live NPC hit-zone selection, shared player/NPC death handling, and project-owned red blood VFX.

**Architecture:** Keep projectile energy in `ImpactEnergyMath` and `ProjectileImpactPayload`; make `HumanoidImpactResolution` consume impact joules through explicit per-zone rules. Preserve `HumanoidDamageReceiver.Died` as the only death authority for ragdoll, corpse loot, witness reporting, contract elimination, and player death recovery. Add project-owned blood wrapper/catalog assets so gameplay code stays decoupled from third-party `RealisticBloodVFX` internals.

**Tech Stack:** Unity 6.3 C#, NUnit EditMode/PlayMode tests, `scripts/run-unity-tests.sh`, ScriptableObject/prefab authoring under `Reloader/Assets/_Project/**`.

## Subagent Ownership Matrix And Stage Contract

Implement this as three reviewable slices. Each slice should be independently green before the next slice starts.

| Stage | Commit/slice purpose | Primary owner | File ownership |
|---|---|---|---|
| Stage A | Health + exact zone damage + contract target cutover | Health owner | `HumanoidImpactResolution`, `HumanoidImpactResolutionResult`, `HumanoidDamageReceiver`, `ContractTargetDamageable`, health/default tests, `NpcFoundation.prefab` and `PlayerRoot.prefab` health values, procedural health setup. |
| Stage B | NPC live hitbox selection/authoring + player/death regressions | Hitbox owner + Integrator | `BodyZoneHitbox`, `HumanoidHitboxRig`, `NpcFoundationRagdollAuthoringUtility`, NPC live collider prefab state, projectile hit-selection tests. Integrator owns player/death regression verification and routes any health-file fixes back to the Health owner. |
| Stage C | Blood VFX wrapper + red placeholders/catalog + docs sync | Blood owner + Integrator | `HumanoidBloodController`, `BloodEffectKind`, `BloodVfxCatalog`, project-owned VFX prefabs/materials/tests. Integrator owns docs final sync and final verification. |

Avoid overlapping file ownership unless the previous stage is complete and the handoff is explicit. In particular, do not let Stage B or C casually rework `HumanoidDamageReceiver`; route any required changes through the Health owner or make them as a sequenced follow-up with focused regression tests.

## Exact v0.1 Damage Constants

Use these exact values in code and tests. Do not replace them with approximate comments, hidden global multipliers, or a single global lethal threshold.

```text
if zone.lethalEnergyJ != null and impactEnergyJ >= zone.lethalEnergyJ:
    instant lethal
else:
    damage = min(zone.maxDamage, impactEnergyJ * zone.damagePerJoule)
```

| Zone | Instant lethal threshold | `damagePerJoule` | Max per-hit health damage | Expected result from 100 health at `3500 J` |
|---|---:|---:|---:|---|
| Head | `200 J` | `0.50` | `100` | Instant lethal. |
| Neck | `200 J` | `0.50` | `100` | Instant lethal. |
| Torso | `1800 J` | `0.045` | `100` | Instant lethal. |
| Pelvis | none | `0.030` | `80` | 80 damage; one hit does not kill, two can. |
| ArmL / ArmR | none | `0.010` | `35` | 35 damage; one hit never kills, three can. |
| LegL / LegR | none | `0.015` | `45` | 45 damage; one and two hits do not kill, three can. |

Additional deterministic locks:

- `ImpactEnergyMath.ComputeDeliveredEnergyJoules(847.344f, 147f)` should be approximately `3419.6 J`.
- Use `3500 J` as the full-power rifle test reference for hit-count assertions.
- Pelvis has no immediate lethal threshold in v0.1.
- Arm and leg zones have no immediate lethal threshold in v0.1.

## Review Gate 0: Spec And Constants Review

Before runtime edits, review `docs/plans/2026-04-16-bullet-energy-health-hit-zones-blood-vfx-design.md`.

Confirm:

- The exact constants above match the design doc.
- No global lethal threshold applies to all zones.
- Arms, legs, and pelvis have no v0.1 instant-lethal threshold.
- Health baseline is exactly `100`.
- `ContractTargetDamageable` legacy `_maxHealth = 1f` / reset behavior is not preserved as a contract-target kill path.
- Player remains on `HumanoidDamageReceiver` + `PlayerDeathContractBridge`.
- Blood uses project-owned wrappers/catalogs before any third-party prefab references.

STOP if the constants or contract-target cutover are disputed. Do not start Stage A until this gate is accepted.

## Stage A: Health, Zone Damage, And Contract Target Cutover

Stage A owns all health semantics. Complete it before touching NPC live hitbox authoring or blood.

### Task A1: Energy Contract Lock

**Files:**

- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/ImpactEnergyMathEditModeTests.cs` or create it if missing.
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponProjectilePlayModeTests.cs`.
- Modify: `Reloader/Assets/_Project/Weapons/Tests/EditMode/ProjectileImpactPayloadEditModeTests.cs`.
- Modify only if tests expose drift: `Reloader/Assets/_Project/Core/Scripts/Runtime/ImpactEnergyMath.cs`.
- Modify only if tests expose drift: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/WeaponProjectile.cs`.
- Modify only if tests expose drift: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/ProjectileImpactPayload.cs`.

**Steps:**

1. Add or update tests proving the authored .308 default computes about `3419.6 J`, ammo defaults match 147 gr / 2780 fps / G1 BC 0.398 / 55 fps SD, projectile drag reduces impact speed/energy, and humanoid damage consumes `ProjectileImpactPayload.DeliveredEnergyJoules` when positive.
2. Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.ImpactEnergyMathEditModeTests|Reloader.Weapons.Tests.EditMode.ProjectileImpactPayloadEditModeTests" "$(pwd)/tmp/bullet-energy-contract-editmode.xml" "$(pwd)/tmp/bullet-energy-contract-editmode.log"
bash scripts/run-unity-tests.sh playmode "Reloader.Weapons.Tests.PlayMode.WeaponProjectilePlayModeTests" "$(pwd)/tmp/bullet-energy-contract-playmode.xml" "$(pwd)/tmp/bullet-energy-contract-playmode.log"
```

3. Implement only minimal energy-contract fixes if the tests expose drift.
4. Re-run the same tests and require PASS.

### Task A2: 100 Health Cutover Including Contract Targets

**Files:**

- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidDamageReceiver.cs`.
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/CivilianPopulationRuntimeBridge.cs`.
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/World/ContractTargetDamageable.cs`.
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/NpcFoundation.prefab`.
- Modify: `Reloader/Assets/_Project/Player/Prefabs/PlayerRoot.prefab`.
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/NpcFoundationPrefabEditModeTests.cs`.
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/CivilianPopulationRuntimeBridgeTests.cs`.
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/PlayerRootDeathHookEditModeTests.cs`.
- Modify: `Reloader/Assets/_Project/Player/Tests/EditMode/PlayerStateRuntimeBridgeEditModeTests.cs`.
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ContractTargetDamageablePlayModeTests.cs`.
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`.

**Steps:**

1. Write failing tests asserting new `HumanoidDamageReceiver` instances start with `MaxHealth == 100`, `NpcFoundation.prefab` and `PlayerRoot.prefab` serialize 100 max health, procedural civilian/target setup does not override the receiver down to 10 or 15, and player state restore still uses `HumanoidDamageReceiver.SetHealthStateForRuntime`.
2. Add contract-target tests proving active contract targets expose/use the shared receiver with `MaxHealth == 100`, `ContractTargetDamageable` no longer owns an effective `_maxHealth = 1f` kill/reset path, and reset/re-arm behavior restores the shared receiver to 100 rather than one-shot health.
3. Add contract-target hit tests using full-power `.308`/`3500 J` limb damage: one arm hit does not eliminate the contract, one leg hit does not eliminate the contract, and contract elimination occurs only after `HumanoidDamageReceiver.Died`.
4. Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.NpcFoundationPrefabEditModeTests|Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests|Reloader.NPCs.Tests.EditMode.PlayerRootDeathHookEditModeTests|Reloader.Player.Tests.EditMode.PlayerStateRuntimeBridgeEditModeTests" "$(pwd)/tmp/humanoid-health-100-editmode.xml" "$(pwd)/tmp/humanoid-health-100-editmode.log"
bash scripts/run-unity-tests.sh playmode "Reloader.Weapons.Tests.PlayMode.ContractTargetDamageablePlayModeTests|Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests" "$(pwd)/tmp/contract-target-health-cutover-playmode.xml" "$(pwd)/tmp/contract-target-health-cutover-playmode.log"
```

5. Cut over runtime defaults, serialized prefab values, and procedural target setup to 100. Remove, ignore, or replace legacy `ContractTargetDamageable._maxHealth = 1f` / reset logic so it cannot decide damage or elimination.
6. Re-run the same tests and require PASS.

### Task A3: Zone-Specific Damage Tuning

**Files:**

- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidImpactResolution.cs`.
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidImpactResolutionResult.cs`.
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidDamageReceiver.cs`.
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/HumanoidImpactResolutionEditModeTests.cs`.
- Modify: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/HumanoidRagdollControllerPlayModeTests.cs`.
- Modify if constructor expectations need explicit energy payloads: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/PlayerDeathContractBridgePlayModeTests.cs`.

**Steps:**

1. Add table-driven tests for every exact constant in the table above.
2. Add hit-count tests from 100 health: head/neck at `200 J` kill, torso at `1800 J` kills, pelvis at `3500 J` deals `80` and does not kill from full health, arm at `3500 J` deals `35` and does not kill, leg at `3500 J` deals `45`, two full-power leg hits leave the target alive, and three full-power leg hits can kill through accumulated health.
3. Add tests proving one arm hit never kills because arm max damage is `35`, and repeated arm hits can kill by accumulated health once enough land.
4. Add a `HumanoidDamageReceiver.Died` edge test proving death fires once when health reaches 0 through accumulated damage.
5. Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.HumanoidImpactResolutionEditModeTests" "$(pwd)/tmp/zone-damage-editmode.xml" "$(pwd)/tmp/zone-damage-editmode.log"
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.HumanoidRagdollControllerPlayModeTests|Reloader.NPCs.Tests.PlayMode.PlayerDeathContractBridgePlayModeTests" "$(pwd)/tmp/zone-damage-playmode.xml" "$(pwd)/tmp/zone-damage-playmode.log"
```

6. Replace the global threshold/multiplier model with a small per-zone rule table using the exact constants.
7. Re-run the same tests and require PASS.

### Stage A Review And Verification Gate

STOP after Stage A. Do not start Stage B until review confirms:

- Runtime and serialized humanoid health defaults are 100.
- Active contract targets use shared 100 health.
- `ContractTargetDamageable` cannot eliminate a contract through legacy `_maxHealth = 1f`, reset behavior, or direct hit counting.
- Exact damage constants are present as a simple rule table, not hidden behind a global multiplier.
- One full-power .308 arm hit and one full-power .308 leg hit do not kill or eliminate a contract target from full health.
- One and two full-power leg hits from full health do not kill; three can.
- `HumanoidDamageReceiver.Died` remains the only death authority.

Stage A verification command:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.ImpactEnergyMathEditModeTests|Reloader.Weapons.Tests.EditMode.ProjectileImpactPayloadEditModeTests|Reloader.NPCs.Tests.EditMode.HumanoidImpactResolutionEditModeTests|Reloader.NPCs.Tests.EditMode.NpcFoundationPrefabEditModeTests|Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests|Reloader.NPCs.Tests.EditMode.PlayerRootDeathHookEditModeTests|Reloader.Player.Tests.EditMode.PlayerStateRuntimeBridgeEditModeTests" "$(pwd)/tmp/stage-a-health-zone-editmode.xml" "$(pwd)/tmp/stage-a-health-zone-editmode.log"
bash scripts/run-unity-tests.sh playmode "Reloader.Weapons.Tests.PlayMode.WeaponProjectilePlayModeTests|Reloader.Weapons.Tests.PlayMode.ContractTargetDamageablePlayModeTests|Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests|Reloader.NPCs.Tests.PlayMode.HumanoidRagdollControllerPlayModeTests|Reloader.NPCs.Tests.PlayMode.PlayerDeathContractBridgePlayModeTests" "$(pwd)/tmp/stage-a-health-zone-playmode.xml" "$(pwd)/tmp/stage-a-health-zone-playmode.log"
```

## Stage B: NPC Live Hitbox Selection And Player/Death Regressions

Stage B owns live hitbox authoring and selection. Do not change damage constants in this stage.

### Task B1: NPC Live Hit-Zone Authoring And Selection

**Files:**

- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Combat/BodyZoneHitbox.cs`.
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidHitboxRig.cs`.
- Modify: `Reloader/Assets/_Project/NPCs/Editor/NpcFoundationRagdollAuthoringUtility.cs`.
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/NpcFoundation.prefab`.
- Modify representative role prefabs under `Reloader/Assets/_Project/NPCs/Prefabs/Roles/` only if they override or break foundation collider state.
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/NpcFoundationPrefabEditModeTests.cs`.
- Modify: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/HumanoidHitboxRigPlayModeTests.cs`.
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponProjectilePlayModeTests.cs` if projectile hit filtering needs a regression.
- Modify `HumanoidDamageReceiver.cs` only as an explicitly sequenced Health-owner follow-up if Stage B tests prove the receiver is the real fault.

**Steps:**

1. Add tests proving `NpcFoundation.prefab` has live, enabled, non-trigger `BodyZoneHitbox` colliders for head, neck, torso, pelvis, arms, and legs.
2. Add tests proving the root/alive capsule cannot consume a head/limb shot before the body-zone collider.
3. Add projectile/routing tests proving a projectile hit on a head collider records `LastZone == HumanoidBodyZone.Head`, and a leg collider records `LegL` or `LegR` and does not kill from full health.
4. Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.NpcFoundationPrefabEditModeTests" "$(pwd)/tmp/npc-hit-zone-authoring-editmode.xml" "$(pwd)/tmp/npc-hit-zone-authoring-editmode.log"
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.HumanoidHitboxRigPlayModeTests|Reloader.Weapons.Tests.PlayMode.WeaponProjectilePlayModeTests" "$(pwd)/tmp/npc-hit-zone-authoring-playmode.xml" "$(pwd)/tmp/npc-hit-zone-authoring-playmode.log"
```

5. Make zone colliders participate in live physics. If the root capsule must remain for movement/interactions, ensure projectile layer/mask or collider layout prevents it from swallowing zone hits.
6. Keep fallback torso behavior only for non-zone objects.
7. Re-run the same tests and require PASS.

### Task B2: Player Health Persistence And Death Recovery Regressions

**Files:**

- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Combat/PlayerDeathContractBridge.cs` only if tests expose a bug.
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerStateRuntimeBridge.cs` only if tests expose health drift.
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/PlayerStateSaveModuleTests.cs`.
- Modify: `Reloader/Assets/_Project/Player/Tests/EditMode/PlayerStateRuntimeBridgeEditModeTests.cs`.
- Modify: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/PlayerDeathContractBridgePlayModeTests.cs`.
- Modify: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/PoliceHostileShooterPlayModeTests.cs`.
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponProjectilePlayModeTests.cs`.

**Steps:**

1. Assert player health persists/restores current and max health values on the shared receiver.
2. Assert player death recovery is invoked exactly once per death edge.
3. Assert police hostile shooter damage reaches the player through `WeaponProjectile` -> `HumanoidDamageReceiver` -> `PlayerDeathContractBridge`.
4. Assert player hits still default to torso/default zone in v0.1 when no player limb/head hitbox exists.
5. Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.PlayerStateSaveModuleTests|Reloader.Player.Tests.EditMode.PlayerStateRuntimeBridgeEditModeTests" "$(pwd)/tmp/player-health-persistence-editmode.xml" "$(pwd)/tmp/player-health-persistence-editmode.log"
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.PlayerDeathContractBridgePlayModeTests|Reloader.NPCs.Tests.PlayMode.PoliceHostileShooterPlayModeTests|Reloader.Weapons.Tests.PlayMode.WeaponProjectilePlayModeTests" "$(pwd)/tmp/player-health-persistence-playmode.xml" "$(pwd)/tmp/player-health-persistence-playmode.log"
```

6. Fix only verified drift. Do not add a player-only health component.

### Task B3: Contract, Witness, Ragdoll, And Corpse Regressions

**Files:**

- Modify only if tests expose drift: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidRagdollController.cs`.
- Modify only if tests expose drift: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidCorpseLootController.cs`.
- Modify only if tests expose drift: `Reloader/Assets/_Project/NPCs/Scripts/Combat/CivilianWitnessReporter.cs`.
- Existing tests: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/HumanoidRagdollControllerPlayModeTests.cs`.
- Existing tests: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/NpcCorpseLootPlayModeTests.cs`.
- Existing tests: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/CivilianWitnessReporterPlayModeTests.cs`.
- Existing tests: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ContractTargetDamageablePlayModeTests.cs`.
- Existing tests: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownContractSlicePlayModeTests.cs`.

**Steps:**

1. Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.HumanoidRagdollControllerPlayModeTests|Reloader.NPCs.Tests.PlayMode.NpcCorpseLootPlayModeTests|Reloader.NPCs.Tests.PlayMode.CivilianWitnessReporterPlayModeTests|Reloader.Weapons.Tests.PlayMode.ContractTargetDamageablePlayModeTests|Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests" "$(pwd)/tmp/humanoid-death-regression-playmode.xml" "$(pwd)/tmp/humanoid-death-regression-playmode.log"
```

2. If a regression fails, fix the specific broken listener while preserving `HumanoidDamageReceiver.Died` as the only death authority.

### Stage B Review And Verification Gate

STOP after Stage B. Do not start Stage C until review confirms:

- `NpcFoundation.prefab` and representative role prefabs have live body-zone colliders.
- Projectile selection resolves NPC head/neck/torso/pelvis/arm/leg colliders before any broad root/default torso fallback.
- Root/default fallback remains only for non-rigged or malformed NPCs.
- Player damage still uses shared receiver/default torso behavior.
- Player death recovery, police shooter, contract target, witness, ragdoll, and corpse-loot regressions pass.
- Stage B did not change Stage A damage constants or reintroduce `ContractTargetDamageable` as a health authority.

Stage B verification command:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.NPCs.Tests.EditMode.NpcFoundationPrefabEditModeTests|Reloader.Core.Tests.EditMode.PlayerStateSaveModuleTests|Reloader.Player.Tests.EditMode.PlayerStateRuntimeBridgeEditModeTests" "$(pwd)/tmp/stage-b-hitbox-player-editmode.xml" "$(pwd)/tmp/stage-b-hitbox-player-editmode.log"
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.HumanoidHitboxRigPlayModeTests|Reloader.Weapons.Tests.PlayMode.WeaponProjectilePlayModeTests|Reloader.NPCs.Tests.PlayMode.PlayerDeathContractBridgePlayModeTests|Reloader.NPCs.Tests.PlayMode.PoliceHostileShooterPlayModeTests|Reloader.NPCs.Tests.PlayMode.HumanoidRagdollControllerPlayModeTests|Reloader.NPCs.Tests.PlayMode.NpcCorpseLootPlayModeTests|Reloader.NPCs.Tests.PlayMode.CivilianWitnessReporterPlayModeTests|Reloader.Weapons.Tests.PlayMode.ContractTargetDamageablePlayModeTests|Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests" "$(pwd)/tmp/stage-b-hitbox-player-playmode.xml" "$(pwd)/tmp/stage-b-hitbox-player-playmode.log"
```

## Stage C: Blood VFX Wrapper, Red Defaults, And Docs Sync

Stage C owns blood feedback and docs. It must not depend on third-party package mappings to pass gameplay tests.

### Task C1: Blood VFX Project Wrapper And Catalog

**Files:**

- Create: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidBloodController.cs`.
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Combat/BloodEffectKind.cs`.
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Combat/BloodVfxCatalog.cs`.
- Create or modify tests: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/HumanoidBloodControllerPlayModeTests.cs`.
- Create project-owned prefabs under `Reloader/Assets/_Project/NPCs/Prefabs/VFX/`.
- Create project-owned materials under `Reloader/Assets/_Project/NPCs/Materials/VFX/`.
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Combat/HumanoidDamageReceiver.cs` only through a sequenced Health-owner handoff if an event seam is missing.
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/WeaponProjectile.cs` only if humanoid hits need to suppress generic impact VFX.

**Steps:**

1. Add PlayMode tests proving non-lethal humanoid hits request `LightImpact` or `HeavyImpact`, neck hits request `NeckImpact`, and death requests `DeathPuddle` once.
2. Add validation tests proving catalogued defaults use project-owned prefab paths and red/readable project-owned materials.
3. Add tests proving missing optional third-party mappings fail softly with no exception.
4. Add validation tests proving missing project-owned default entries fail validation.
5. Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.HumanoidBloodControllerPlayModeTests" "$(pwd)/tmp/humanoid-blood-playmode.xml" "$(pwd)/tmp/humanoid-blood-playmode.log"
```

6. Implement `HumanoidBloodController` as an observer of humanoid impact/death results. Keep it semantic: it asks `BloodVfxCatalog` for a prefab by `BloodEffectKind`, spawns it at the impact/death point, and does not know third-party package paths.
7. Create safe project-owned red placeholder spray, splatter, and puddle prefabs/materials.
8. Re-run the same tests and require PASS.

Required acceptance for Stage C:

- Project-owned red default spray/splatter/puddle prefabs and materials exist.
- Tests and/or manual smoke prove these defaults spawn for humanoid bullet hits/death.
- Missing project-owned defaults fail validation.
- Missing `Assets/HIVEMIND/RealisticBloodVFX/**` mappings do not block gameplay.

### Review Gate C1: Before Third-Party Blood Mapping

STOP before mapping any `RealisticBloodVFX` package asset.

Review must confirm:

- The project-owned defaults pass tests and manual smoke without package mappings.
- No gameplay code references `Assets/HIVEMIND/RealisticBloodVFX/**` directly.
- The candidate package prefab/material has been opened in Unity and validated for missing scripts, material color, opacity, scale, and black-only puddle regressions.
- If validation fails or the package is absent, leave the project-owned defaults in place and skip package mapping.

### Task C2: Docs Final Sync

**Files:**

- Modify: `docs/design/weapons-and-ballistics.md`.
- Modify: `docs/design/npcs-and-quests.md`.
- Modify: `docs/design/law-enforcement.md`.
- Modify: `docs/design/assassination-contracts.md`.
- Modify: `docs/design/v0.1-demo-status-and-milestones.md`.
- Modify if still misleading: `docs/plans/2026-03-13-ragdoll-hitboxes-blood-design.md`.
- Modify if still misleading: `docs/plans/2026-03-13-ragdoll-hitboxes-blood-implementation-plan.md`.

**Steps:**

1. Update docs minimally with the final contracts only: energy stays in `ImpactEnergyMath`/projectile payload, 100 health default, exact zone-specific damage/lethality, NPC live zone collider authoring, player shared receiver/default-zone v0.1, contract target shared-health cutover, and project-owned blood wrapper/catalog.
2. Mark older ragdoll/hitbox/blood docs partial/superseded for damage tuning and blood restoration only if they still imply old behavior.
3. Run:

```bash
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

Expected: PASS.

### Stage C Review And Verification Gate

STOP after Stage C. Review must confirm:

- Blood is semantic and project-owned by default.
- Project-owned defaults are red/readable and validated.
- Third-party package mappings are optional and validation-gated.
- Docs are synchronized without duplicating large plan content into domain docs.
- No Stage C change altered Stage A damage constants or Stage B live hitbox selection.

Stage C verification command:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.NPCs.Tests.PlayMode.HumanoidBloodControllerPlayModeTests" "$(pwd)/tmp/stage-c-blood-playmode.xml" "$(pwd)/tmp/stage-c-blood-playmode.log"
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

## Final Verification

Run the focused Unity suites first:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.ImpactEnergyMathEditModeTests|Reloader.Weapons.Tests.EditMode.ProjectileImpactPayloadEditModeTests|Reloader.NPCs.Tests.EditMode.HumanoidImpactResolutionEditModeTests|Reloader.NPCs.Tests.EditMode.NpcFoundationPrefabEditModeTests|Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests|Reloader.NPCs.Tests.EditMode.PlayerRootDeathHookEditModeTests|Reloader.Player.Tests.EditMode.PlayerStateRuntimeBridgeEditModeTests|Reloader.Core.Tests.EditMode.PlayerStateSaveModuleTests" "$(pwd)/tmp/bullet-health-final-editmode.xml" "$(pwd)/tmp/bullet-health-final-editmode.log"
bash scripts/run-unity-tests.sh playmode "Reloader.Weapons.Tests.PlayMode.WeaponProjectilePlayModeTests|Reloader.NPCs.Tests.PlayMode.HumanoidHitboxRigPlayModeTests|Reloader.NPCs.Tests.PlayMode.HumanoidRagdollControllerPlayModeTests|Reloader.NPCs.Tests.PlayMode.HumanoidBloodControllerPlayModeTests|Reloader.NPCs.Tests.PlayMode.PlayerDeathContractBridgePlayModeTests|Reloader.NPCs.Tests.PlayMode.PoliceHostileShooterPlayModeTests|Reloader.NPCs.Tests.PlayMode.NpcCorpseLootPlayModeTests|Reloader.NPCs.Tests.PlayMode.CivilianWitnessReporterPlayModeTests|Reloader.Weapons.Tests.PlayMode.ContractTargetDamageablePlayModeTests|Reloader.World.Tests.PlayMode.MainTownContractSlicePlayModeTests" "$(pwd)/tmp/bullet-health-final-playmode.xml" "$(pwd)/tmp/bullet-health-final-playmode.log"
```

Then run the docs guardrails again:

```bash
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

Expected final state:

- All focused tests pass.
- A .308 head/neck hit kills.
- A .308 close/mid torso hit kills at or above `1800 J`.
- Pelvis has no immediate lethal threshold and caps at `80` damage.
- A single .308 arm/leg hit from full health does not kill.
- One and two full-power leg hits from full health do not kill; three can.
- Active contract targets use shared `100` health and are not eliminated by one full-power .308 arm/leg hit.
- Player death recovery still runs once through `PlayerDeathContractBridge`.
- Humanoid hits spawn red project-owned blood effects.
- Missing project-owned blood defaults fail validation.
- Missing third-party blood package mappings do not block gameplay.
- No runtime code calls third-party blood package prefabs directly.

# Ragdoll Hitboxes And Blood Design

## Goal

Add a shared humanoid combat feedback stack for all NPCs that supports body-zone hitboxes, energy-aware lethality, immediate ragdoll takeover on lethal hits, directional hit impulse, impact blood bursts, and blood puddles on death.

This slice should make NPC gunplay feel physically readable and satisfying without yet taking on penetration, exit wounds, wound-state AI reactions, or blood trails.

---

## Scope Decisions

### NPC Coverage

- apply the new combat stack to all humanoid NPCs
- do not scope the first pass only to contract targets
- do not include player damage/ragdoll in this slice

### Hit Zone Model

Author semantic body zones as:

- `Head`
- `Neck`
- `Torso`
- `ArmL`
- `ArmR`
- `LegL`
- `LegR`

The gameplay model still treats those as five regional classes for tuning:

- head
- neck
- torso
- arms
- legs

### Damage Model

- lethality is driven by bullet energy at impact, not hardcoded instant-kill zones
- each body zone applies vulnerability multipliers and threshold tuning
- first-pass gameplay distinction only needs `non-lethal` versus `lethal`
- the internal result contract should preserve richer outcome categories so future work can add panic, limp, bleed, and trails without reworking the hit pipeline

### Bullet Behavior Boundary

- no bullet penetration or pass-through in this slice
- keep enough information in the impact-resolution contract to add residual energy / exit wound logic later

### Death Presentation

- immediate ragdoll takeover on lethal hit
- disable animator and AI/runtime interaction dependencies immediately
- apply a tuned directional impulse in the bullet travel direction at the struck ragdoll body / impact point
- if the specific struck limb rigidbody cannot be resolved, fall back to the nearest torso or root rigidbody

### Blood Scope

Ship:

- impact blood bursts on hit
- ground puddle on death

Do not ship yet:

- blood trails from moving wounded NPCs
- arterial fountain specialization
- exit wound blood logic

---

## Recommended Architecture

Use a shared humanoid combat stack with standardized prefab authoring.

### Runtime Components

- `HumanoidHitboxRig`
  - lives on the NPC root
  - resolves required humanoid bones from the `Animator` / avatar
  - binds or validates the seven zone colliders
- `BodyZoneHitbox`
  - lives on each hit collider
  - reports semantic zone, owning rig, and struck rigidbody/bone context
- `HumanoidDamageReceiver`
  - authoritative entrypoint for ballistic impacts on that NPC
  - converts projectile impact data into a structured resolution result
- `HumanoidRagdollController`
  - owns idle-vs-ragdoll state
  - enables ragdoll rigidbodies/colliders and disables authored runtime dependencies on lethal hits
  - applies directional hit impulse at the struck body
- `HumanoidBloodController`
  - receives semantic blood effect requests from the damage result
  - spawns impact burst at wound location
  - spawns death puddle through a delayed death/collapse hook
- `HumanoidRigValidator`
  - editor/runtime validation helper for missing bones, colliders, or ragdoll bodies

### Projectile Hit Flow

1. `WeaponProjectile` raycast hits a `BodyZoneHitbox`.
2. The hitbox provides the semantic body zone plus struck rigidbody context.
3. `HumanoidDamageReceiver` evaluates delivered bullet energy and zone vulnerability.
4. A shared `ImpactResolutionResult` is produced.
5. `HumanoidBloodController` spawns the impact burst.
6. If the result is lethal, `HumanoidRagdollController` takes over immediately and applies directional impulse.
7. A death puddle is spawned after the lethal transition through the blood controller.

### Result Contract Requirements

The shared impact result payload should carry at least:

- hit point
- hit normal
- projectile travel direction
- delivered impact energy
- semantic body zone
- lethal / non-lethal result
- impulse magnitude recommendation
- optional placeholder for residual energy or pass-through metadata

This keeps the runtime extensible without shipping penetration yet.

---

## Damage And Feedback Rules

### Zone Tuning Direction

- `Head`
  - highest vulnerability
  - lethal at relatively moderate rifle energy
- `Neck`
  - very high vulnerability
  - strong bias for heavier blood impact presentation
- `Torso`
  - high vulnerability
  - center-mass rifle hits should often be decisive
- `ArmL` / `ArmR`
  - low vulnerability
  - generally wound-only in first pass unless damage stacking is extreme
- `LegL` / `LegR`
  - low-to-medium vulnerability
  - treated as wound-oriented in first pass

### Ragdoll Force Rule

The ragdoll should not simply replace animation and collapse.

On lethal hits:

- use projectile direction at impact as the push vector
- scale force from delivered energy
- clamp force into tuned gameplay-friendly bounds so hits feel punchy but not comical
- apply force at the impact point or closest struck ragdoll rigidbody

This is required to preserve the visual payoff of accurate rifle hits.

### Blood Integration Rule

Gameplay code should not call the asset-store package directly.

Wrap the blood package behind project-owned semantics such as:

- `LightImpact`
- `HeavyImpact`
- `NeckImpact`
- `DeathPuddle`

The wrapper chooses which effect prefab/system from the purchased package to spawn.

This avoids coupling combat logic to package-specific prefab names or folder layout.

---

## Prefab Authoring Contract

All humanoid NPC prefabs that participate in ballistic damage should share the same root-level combat stack.

Each supported NPC prefab should contain:

- `Animator` with humanoid avatar
- `HumanoidHitboxRig`
- `HumanoidDamageReceiver`
- `HumanoidRagdollController`
- `HumanoidBloodController`
- authored ragdoll rigidbodies and joints
- seven body-zone colliders bound to the standard bones

Auto-binding rule:

- prefer resolving standard humanoid bones from the `Animator`
- bind required zones by standard bone targets, not hand-authored string conventions
- validation should fail if a humanoid prefab cannot satisfy the required rig contract

Dependency shutdown rule:

- AI, patrol motion, interaction controllers, and other authored NPC runtime components should be registered as dependencies to disable on lethal ragdoll takeover
- lethal transition must be deterministic and not leave locomotion or dialogue runtime fighting physics

---

## Validation And Testing

### Validation

Fail loudly when:

- required humanoid bones are missing
- a required zone collider is missing or assigned to the wrong bone
- ragdoll rigidbodies/joints required for lethal impulse are absent
- a combat-enabled NPC prefab does not satisfy the shared humanoid rig contract

Validation should exist both:

- in editor tooling / validation helpers
- in play mode smoke paths for representative prefabs

### Automated Testing

EditMode coverage:

- energy-to-outcome resolution
- zone vulnerability mapping
- rig validation and required-zone completeness

PlayMode coverage:

- projectile hits the correct zone collider and reaches shared damage resolution
- lethal hit disables animator/AI and enables ragdoll rigidbodies
- lethal hit applies directional impulse to the expected ragdoll body
- blood controller receives correct semantic request for body zone and lethal death puddle

### Manual Validation

Manual Unity checks are still required for:

- ragdoll feel
- blood VFX choice and placement
- puddle timing / placement
- prefab authoring consistency across representative NPC roles

---

## Explicit Non-Goals For This Slice

- bullet penetration / pass-through
- exit wounds
- moving blood trails
- AI flee/panic/stagger behavior
- corpse persistence/save integration
- player damage/ragdoll
- asset-pack-specific direct gameplay dependencies

---

## Follow-On Work Enabled By This Design

Once this slice ships, the same contracts can be extended to support:

- wounded flee/panic and limp states
- blood trails from moving wounded NPCs
- arterial/neck-specific blood behavior
- penetration and exit wounds
- armor/material-specific damage reduction
- law-enforcement/witness reactions keyed off hit severity

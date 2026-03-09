# Contract Policy Extensibility Design

> **Prerequisites:** Read [../design/core-architecture.md](../design/core-architecture.md), [../design/assassination-contracts.md](../design/assassination-contracts.md), and [../design/law-enforcement.md](../design/law-enforcement.md) first.

---

## Goal

Replace the current global "wrong target always fails contract" behavior with a structured contract policy model that:
- keeps ordinary procedural contracts permissive
- lets special authored contracts opt into strict failure rules
- keeps failure and restriction text easy to surface in the Contracts tab UI
- leaves room for future mission rules without another runtime rewrite

---

## Problem

The current assassination flow hard-codes wrong-target elimination as a contract failure for every active contract. That conflicts with the intended sandbox contract loop:
- random murder is allowed, even when it is a bad idea
- only some higher-value or specialty contracts should fail on collateral or rule violations
- the Contracts tab should show the player what restrictions apply before they act

The current failure path also lacks an explicit player-owned clear action, which risks progression friction after a failure.

---

## Design Summary

Introduce two structured policy surfaces on assassination contracts:
- `ContractFailurePolicy`
- `ContractObjectivePolicy`

`AssassinationContractDefinition` becomes the source of mission rules, while runtime systems read those rules rather than hard-coding contract behavior.

Initial v1 implementation only activates one policy rule:
- `WrongTargetKill`

The structure is intentionally broader than the first rule so later rules can be added without changing the contract/runtime/UI integration shape.

---

## Data Contract

### `ContractFailurePolicy`

Owns a list of failure rules for the contract.

Initial rule model:
- `AssassinationContractFailureRuleType`
  - `WrongTargetKill`
  - reserved for later: `AlarmRaised`, `WrongWeapon`, `HeadshotRequired`, `EscapeRequired`, `RangeTooClose`, `RangeTooFar`
- `AssassinationContractFailureRule`
  - `RuleType`
  - future parameter payload slots

Contract rule semantics:
- no rule present => no failure for that condition
- `WrongTargetKill` present => killing a non-target can fail the contract

### `ContractObjectivePolicy`

Owns objective/restriction descriptors for the contract.

For this slice it exists mainly as the stable UI/runtime seam:
- it can expose human-readable restriction text to the Contracts tab
- it does not yet enforce new completion mechanics beyond current assassination flow

This keeps future rules like `HeadshotRequired`, `SpecificWeapon`, or `EscapeRequired` aligned with the same data contract instead of being introduced ad hoc later.

---

## Runtime Contract

### Wrong-target elimination

When a non-target NPC is killed while a contract is active:
- law-enforcement/crime consequences still fire
- the contract only fails if the active contract definition includes `WrongTargetKill`
- otherwise the contract remains active

This keeps sandbox violence consequences in the police system while moving mission strictness into contract data.

### Failed contracts

Failed contracts keep the visible failed snapshot introduced in the current branch. The player can clear/cancel a failed contract at any time, including during police search/pursuit aftermath.

Cancel semantics:
- remove the failed snapshot
- clear active/failed contract state
- leave law-enforcement state untouched

This keeps mission progression unblocked without letting contract UI control police behavior.

---

## Procedural Contract Defaults

Procedural MainTown assassination contracts should default to:
- no strict failure rules
- no `WrongTargetKill` rule

That makes ordinary procedural contracts match the intended sandbox design immediately.

Strict failure behavior is reserved for authored or higher-level contracts that explicitly opt in through their definition data.

---

## Contracts Tab UI

The Contracts tab should render policy-derived mission context directly from the active or failed contract definition.

Visible surfaces:
- mission status
- tracking text
- restrictions / failure conditions text
- clear/cancel action when the contract is failed

UI rule:
- do not hard-code restriction prose in the screen bridge
- derive display text from the policy model so adding a new rule later automatically has a place in the UI

For v1, the visible restriction text can be compact:
- `RESTRICTIONS: wrong target fails contract`
- or omitted when the contract has no special restrictions

---

## Extensibility Contract

This design intentionally separates:
- law-enforcement consequences
- mission failure rules
- mission objective/restriction display

That separation keeps future growth straightforward:
- new rule type added to policy enum/data
- runtime evaluates only the rules it understands
- UI renders the rule text from the same policy source

Future rules can be implemented incrementally without breaking existing relaxed contracts.

---

## Validation Scope For This Slice

Implement now:
- policy types on assassination contracts
- opt-in `WrongTargetKill`
- procedural contracts defaulting to relaxed behavior
- failed-contract clear/cancel action
- Contracts-tab restriction text sourced from policy

Defer:
- headshot enforcement
- weapon-specific enforcement
- escape-required enforcement
- alarm-based failure
- range-based failure


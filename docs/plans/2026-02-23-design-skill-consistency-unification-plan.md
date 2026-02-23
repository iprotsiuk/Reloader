# Design/Skill Consistency Unification Plan

## Summary
This plan aligns design docs and skills around a single item-persistence ownership model, a single `definition` contract, explicit scope tags, consistent asset paths, explicit powder-lot runtime modeling, rimfire buy-only policy, normalized component pricing fields, and fixed skill self-validation.  
It is documentation-and-skill-only work (no runtime code edits in this pass), but all type/schema contracts are updated as implementation guidance.

## Locked Decisions
- Scope tags format: heading suffixes like `## Section [v0.1]`.
- Scope coverage: all `docs/design/*.md` except `docs/design/README.md`.
- `definition` contract: base field only (`ItemInstance.definition`), no subclass field redefinition.
- Agents file location: `.cursor/agents.md`.
- Powder pricing unit: `packageCount` for powder is pounds.
- Rimfire policy: all rimfire calibers are buy-only now; no reload workflow.
- `CasingInventory` handling in save schema: keep as ID-only index.

## Public APIs / Interfaces / Type Contracts To Update
1. Save schema in `docs/design/save-and-progression.md`.
`ItemRegistry: uniqueID -> full ItemInstance` becomes the only source of full item instance data.
`ItemLocation: uniqueID -> owner/location metadata` becomes canonical ownership/location map.
`InventoryState`, `WorldItemState`, `WeaponState`, `VehicleState`, and `CasingInventory` store IDs plus location/slot metadata only.

2. Runtime item contract in `docs/design/core-architecture.md` and skill examples.
Only `ItemInstance.definition` exists as storage.
Subclasses must not declare `definition` again.

3. Reloading runtime contracts in `docs/design/reloading-system.md`.
Add `PowderLotInstance` with `lotID` and `lotOffset`.
Add `PowderCanisterInstance` linked to lot.
`AmmoInstance` references `powderLotID` (not just powder definition).

4. Component pricing schema in `docs/design/inventory-and-economy.md` and skill refs.
For bullets/cases/primers/powder: `packageCount`, `packagePrice`, derived `unitPrice`.

5. Rimfire policy in docs/skills.
Add `CaliberDefinition.isReloadable` contract guidance and set rimfire policy to `false` in docs.
Remove rimfire implication from case/primer assembly docs (no rimfire primer-pocket assembly path).

6. Equipment field applicability in `.agent/skills/adding-game-content/SKILL.md`.
`calibersSupported`, `precision`, `speed` become nullable/optional when not applicable (especially diagnostic tools).

## File-by-File Change Plan
1. `docs/design/save-and-progression.md`
Rewrite the `Save Data Structure` block to introduce `ItemRegistry` and `ItemLocation` at top level.
Refactor `InventoryState`, `WorldItemState`, `WeaponState`, `VehicleState`, and `CasingInventory` to ID/location-only representation.
Keep semantic ownership of full instances in `ItemRegistry` only.

2. `docs/design/core-architecture.md`
In `Runtime Instances vs. Definitions`, add explicit "no subclass redefinition of `definition`" rule.
Keep `ItemInstance.definition` as canonical storage and clarify typed access is via cast/helper, not duplicated fields.

3. `.agent/skills/unity-project-conventions/SKILL.md`
Update `Definition vs. Instance` example so `CasingInstance` no longer declares `public CaseDefinition definition;`.
Update notes to align with base-field-only contract.
Update any pricing snippet terminology if it still uses old `pricePer*` component fields.

4. `docs/design/reloading-system.md`
Add `CaliberDefinition.isReloadable`.
Add explicit rimfire buy-only rule and remove reload-path implication for rimfire case/primer assembly.
Replace component pricing fields (`pricePerPound`, `pricePerBox`, `pricePerBag`) with `packageCount`, `packagePrice`, `unitPrice` for powder/bullets/cases/primers.
Add `PowderLotInstance` and `PowderCanisterInstance` runtime contracts with `lotOffset`.
Change `AmmoInstance` contract to include `powderLotID`.

5. `docs/design/inventory-and-economy.md`
Add a `Component Pricing Schema [v0.1]` section documenting canonical `packageCount + packagePrice + derived unitPrice` fields and unit expectations (powder per pound).

6. `.agent/skills/adding-game-content/SKILL.md`
Change weapon part asset path from `.../Weapons/Data/Parts/...` to `.../Weapons/Data/WeaponParts/...`.
Add `isReloadable` guidance for calibers and explicit rimfire non-reloadable/buy-only rules.
Remove `Rimfire` from `primerPocketSize` assembly guidance.
Normalize powder/bullet/case/primer pricing field guidance to `packageCount/packagePrice/unitPrice`.
Change equipment-field requirements to applicability-driven optional/nullable semantics for diagnostic tools.

7. `.agent/skills/reloading-domain-knowledge/resources/caliber-reference.md`
Update rimfire notes/table guidance to reflect buy-only non-reloadable policy.
Add a short note mapping rimfire to `isReloadable=false`.

8. `.agent/skills/adding-game-content/SKILL.md`
Point caliber/burn-rate references to canonical shared resources under
`../reloading-domain-knowledge/resources/` and remove duplicate local copies.

9. `.cursor/rules/world-vehicles-context.mdc`
Scope scene globs to MainWorld files (`Reloader/Assets/Scenes/MainWorld*`) instead of all scenes.

10. `.agent/skills/creating-skills/SKILL.md`
Keep one-level-deep reference behavior explicit and simplify resource-link wording if needed to match validator constraints.

11. `.agent/skills/creating-skills/resources/skill-template.md`
Replace nested markdown links with plain inline code path references so validator no longer detects second-level links.

12. `.cursor/agents.md`
Create canonical local agent guidance file referenced by your AGENTS instruction chain.
Document that `.cursor/rules/*.mdc` are context routers and `.agent/skills/*` are skill sources.

## Scope Tag Matrix (Major `##` Headings)
| File | Heading tags to apply |
|---|---|
| `docs/design/core-architecture.md` | Project Structure `[v0.1]`; ScriptableObject Data Assets `[v0.1]`; Manager Singletons `[v0.1]`; Event Bus `[v0.1]`; Runtime Instances vs. Definitions `[v0.1]`; Design Principles `[v0.1]`; Asset Store Packages in Use `[v0.1]`; Domain Design Docs `[v0.1]` |
| `docs/design/prototype-scope.md` | Must Have `[v0.1]`; Should Have `[v0.2]`; Could Have `[v1+]` |
| `docs/design/save-and-progression.md` | Save Triggers `[v0.1]`; Save Data Structure `[v0.1]`; Game Loop `[v0.1]`; Equipment Progression `[v1+]`; Weapon Progression `[v1+]`; World Progression `[v1+]`; Reputation Progression `[v1+]` |
| `docs/design/inventory-and-economy.md` | Item Persistence `[v0.1]`; Performance Management `[v1+]`; Storage `[v0.1]`; Currency `[v0.1]`; Component Pricing Schema `[v0.1]`; Income Sources `[v0.2]`; Expenses `[v0.2]`; Shops `[v0.2]` |
| `docs/design/reloading-system.md` | The Real Reloading Process `[v1+]`; Sandbox Philosophy `[v0.1]`; Data Model `[v0.1]`; Fire-Formed Brass `[v0.2]`; Equipment Progression `[v1+]`; Interaction Model `[v0.1]`; Core Precision Systems `[v1+]`; Future Expansion Hooks `[v1+]` |
| `docs/design/weapons-and-ballistics.md` | Modular Weapon System `[v0.1]`; Ballistics Model `[v1+]`; Precision / Benchrest Factors `[v1+]`; Shooting Mechanics `[v0.1]`; Competition Spectrum `[v0.2]` |
| `docs/design/world-and-vehicles.md` | Main World `[v0.1]`; Instanced Scenes `[v0.2]`; Building Interiors `[v0.1]`; Workshop Evolution `[v1+]`; Vehicles `[v0.2]` |
| `docs/design/hunting-and-competitions.md` | Hunting `[v1+]`; Competitions `[v0.2]` |
| `docs/design/npcs-and-quests.md` | NPC Types `[v0.2]`; Quest Types `[v1+]`; Relationship System `[v1+]`; Data Model `[v1+]` |
| `docs/design/law-enforcement.md` | Legal System `[v1+]`; Black Market `[v1+]`; Law Enforcement `[v1+]`; Consequences `[v1+]`; Data Model `[v1+]` |

## Verification Cases and Commands
1. Creating-skills validator must pass.
Run: `bash .agent/skills/creating-skills/scripts/validate-skill.sh .agent/skills/creating-skills`
Expected: exit code `0`, no nested-link error.

2. Weapon-part path canonicalization check.
Run: `rg -n "Weapons/Data/Parts" .agent/skills`
Expected: no matches.
Run: `rg -n "Weapons/Data/WeaponParts" .agent/skills/adding-game-content/SKILL.md`
Expected: at least one match.

3. Definition contract check.
Run: `rg -n "public\\s+\\w+Definition\\s+definition;" .agent/skills docs/design`
Expected: no subclass redeclaration examples remain.

4. Rimfire reload-path conflict check.
Run: `rg -n "primerPocketSize.*Rimfire" docs/design .agent/skills`
Expected: no assembly-schema matches.
Run: `rg -n "isReloadable" docs/design/reloading-system.md .agent/skills/adding-game-content/SKILL.md`
Expected: policy present.

5. Pricing schema normalization check.
Run: `rg -n "packageCount|packagePrice|unitPrice" docs/design/inventory-and-economy.md docs/design/reloading-system.md .agent/skills/adding-game-content/SKILL.md`
Expected: schema present in all three.

6. Scope-tag coverage check.
Run: `for f in docs/design/*.md; do [[ "$f" == *README.md ]] && continue; rg '^## ' "$f" | rg -v '\\[(v0\\.1|v0\\.2|v1\\+)\\]' && echo "MISSING TAGS: $f"; done`
Expected: no output.

7. Cursor context updates check.
Run: `rg -n "Reloader/Assets/Scenes/MainWorld\\*" .cursor/rules/world-vehicles-context.mdc`
Expected: one match.
Run: `test -f .cursor/agents.md && echo OK`
Expected: `OK`.

## Assumptions and Defaults
- This pass edits docs/skills/config only, not C# source or assets.
- `unitPrice` is documented as derived, not entered manually.
- Rimfire remains available as factory ammo/shop inventory only.
- Existing non-README design doc structure is retained unless a heading rename is needed for the new tag format.

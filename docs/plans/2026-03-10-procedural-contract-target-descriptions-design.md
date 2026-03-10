# Procedural Contract Target Descriptions Design

## Goal

Improve procedural contract target descriptions so the Contracts tab shows fast, readable visual intel for the current live civilian occupant, instead of a thin or arbitrary tag list.

## Decision

Use persisted appearance fields already stored on `CivilianPopulationRecord` as the primary source of contract target descriptions.

Do not add new save-schema fields for this slice.

## Why This Approach

- The data is already persistent across save/load, sleep, travel, and Monday replacements.
- Contract intel should describe the current occupant, not a slot or a preview-only authoring artifact.
- Existing `GeneratedDescriptionTags` are too weak and inconsistent to be the primary briefing signal.
- The repo already contains conservative module ids for body, hair, beard, tops, and outerwear that can be mapped into readable clue text without guessing at fine fashion detail.

## Output Shape

Contract descriptions should use terse clue fragments, ordered for scanability:

1. sex
2. primary clothing clue
3. hair clue
4. beard clue only when present and useful

Examples:

- `male, gray coat, short black hair, clean-shaven`
- `female, red jacket, long brown hair`
- `male, dark hoodie, wavy hair, thick beard`

The description should usually contain 3-4 clues, not a sentence.

## Clue Sources

### Sex

Derive from `BaseBodyId` and `PresentationType`.

- male-like ids/presentation -> `male`
- female-like ids/presentation -> `female`
- unknown -> omit

### Clothing

Prefer `OuterwearId` when present because it is the most visible contract clue.
Fallback to `OutfitTopId` when outerwear is empty or unhelpful.

Use conservative garment labels only:

- `coat`
- `jacket`
- `open jacket`
- `hoodie`
- `t-shirt`

Combine garment with a broad color from `MaterialColorIds` when one is recognizable:

- `gray coat`
- `red jacket`
- `dark hoodie`

### Hair

Use `HairId` + `HairColorId` to build a broad clue:

- `short black hair`
- `long brown hair`
- `wavy hair`
- `parted hair`
- `bob haircut`

If color is unknown, omit it and keep the shape clue.

### Beard

Use `BeardId` only for male-compatible beard ids.

Map to broad readable labels:

- `clean-shaven`
- `short beard`
- `trim beard`
- `full beard`
- `thick beard`

If beard is empty or `beard.none`, emit `clean-shaven` only when there is room and it adds value.

## Fallback Rules

Description priority:

1. derived appearance clues from persisted fields
2. existing `GeneratedDescriptionTags`
3. `poolId in areaTag`
4. `areaTag`

This keeps old or manually authored records readable even if some appearance fields are sparse.

## Scope Boundaries

- No save schema bump for this change.
- No contract UI layout redesign.
- No authored natural-language briefing prose yet.
- No preview/authoring NPC description overhaul.
- No attempt to infer subtle clothing style beyond safe labels supported by current module ids.

## Testing

Add tests that prove:

- contract descriptions prefer derived appearance clues over saved tag lists
- sex is present in the description
- clothing and hair clues are built deterministically from persisted fields
- Monday replacements publish descriptions that match the replacement occupant, not the dead predecessor
- fallback behavior still works for sparse records

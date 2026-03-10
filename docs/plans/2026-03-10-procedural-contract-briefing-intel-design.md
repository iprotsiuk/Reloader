# Procedural Contract Briefing Intel Design

## Goal

Make procedural assassination contracts read like actual target briefs by keeping the fast visual clue line in `TargetDescription` and generating a richer `BriefingText` from the civilian's existing persisted world-role data.

## Constraints

- Do not add new save-schema fields for this slice.
- Keep contract summary text compact in the Contracts tab.
- Keep briefing intel stable across save/load and civilian replacement.
- Replacement civilians must produce briefing text that matches the new occupant, not the vacated civilian.

## Current State

- `CivilianPopulationRuntimeBridge` already builds procedural offers from live `CivilianPopulationRecord` data.
- `TargetDescription` now derives visual clues from persisted appearance fields.
- `BriefingText` is still a generic placeholder.
- Existing civilian records already persist `PoolId`, `AreaTag`, and `SpawnAnchorId`.
- The Contracts UI already renders `TargetDescription` as the compact summary and `BriefingText` as the longer body copy.

## Approaches Considered

### 1. Merge all intel into `TargetDescription`

- Pros: smallest implementation.
- Cons: bloats the summary line and duplicates location information already surfaced elsewhere.

### 2. Keep visual summary in `TargetDescription`, generate role/location intel in `BriefingText`

- Pros: fits the current UI contract, avoids schema churn, keeps the summary readable, and lets the briefing carry slightly richer copy.
- Cons: target intel is split across two fields.

### 3. Add new structured contract intel fields

- Pros: cleaner long-term data model.
- Cons: unnecessary scope for this slice because the current runtime/UI contract already has the right display seams.

## Decision

Use approach 2.

- `TargetDescription` remains the terse appearance-led clue string.
- `BriefingText` becomes a generated line built from:
  - a readable pool/role label derived from `PoolId`
  - a readable location label derived from `AreaTag`
- The generated copy should be conservative and reusable, for example:
  - `Contractor notes: quarry worker, usually found around the quarry. Confirm the visual match before taking the shot.`

## Data Mapping

### Role labels

Map known procedural `PoolId` values to compact, believable labels:

- `townsfolk` -> `local resident`
- `quarry_workers` -> `quarry worker`
- `hobos` -> `drifter`
- `cops` -> `police officer`

Fallback for unknown pool ids:

- normalize underscore/dot-separated ids into lower-case readable text
- if normalization still produces nothing useful, omit the role phrase

### Area labels

Use `AreaTag` as the source of location intel, but normalize authored ids into readable text:

- `maintown.square` -> `the town square`
- `maintown.watch` -> `the watch post`
- `maintown.alley` -> `the alleys`
- `quarry` -> `the quarry`

Fallback behavior:

- split on `.`, `_`, and `-`
- prefer the most specific non-empty tail tokens
- add `the` where it reads naturally

## Runtime Behavior

- Procedural offers should always build briefing intel from the currently live occupant record.
- If a civilian is killed and replaced on Monday, the next posted offer must rebuild both `TargetDescription` and `BriefingText` from the replacement record.
- No old briefing text should survive if the target id changes.

## Testing

- Add an EditMode test in `CivilianPopulationRuntimeBridgeTests` that publishes a procedural offer and asserts:
  - `TargetDescription` still uses appearance clues
  - `BriefingText` uses readable role/location intel derived from `PoolId` and `AreaTag`
- Keep UI verification implicit for this slice because the existing contract status bridge already passes `BriefingText` through unchanged.


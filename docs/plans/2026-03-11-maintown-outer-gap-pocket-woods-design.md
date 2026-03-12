# MainTown Outer Gap Pocket Woods Design

## Summary

Add a targeted forest-shaping pass to `MainTown` that fills the empty gaps between outer districts with dense wooded pockets instead of just increasing overall map-wide tree count. The goal is to make the outer districts feel separated by believable woods while preserving the planning-map readability built in earlier passes.

## Goals

- Thicken the large empty gaps between outer districts.
- Keep the current perimeter density layers and roads intact.
- Create irregular wooded pockets with openings rather than uniform blanket coverage.
- Preserve key reads for district labels, major road junctions, `ChurchHill`, `UtilityLandmarks`, and the quarry bowl.

## Placement Direction

### Pocket Woods Zones

- West-side outer gap:
  - between trailer park / motel / truck stop approaches
- East-side outer gap:
  - between quarry / industrial yard / roadside market approaches
- North-side outer gap:
  - between storage yard / municipal block / service depot approaches

### Shape Direction

- Use compact cluster roots rather than another full-map density layer.
- Favor dense local pockets with clear lanes between them.
- Keep the woods off the main district pads and out of the most important sightline windows.

## Contract Impact

- Extend the focused `MainTown` layout test with a few stable gap-cluster root names under `MainTownWorldShell`.
- Extend the road/forest contract note with those cluster roots.
- Record the targeted gap-filling pass in the progress log so later art passes understand these as intentional separation pockets.

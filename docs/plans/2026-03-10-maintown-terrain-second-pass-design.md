# MainTown Terrain Second Pass Design

**Context:** `MainTown` already has a literal-mile shell, district roots, landmark roots, a first road skeleton, and scale-aware blockout tests. The town core is acceptable for now, but the larger landscape still reads as flat basin floor plus boxy ridge masses, sparse forest cover, and unskinned raised road strips.

**Problem:** The outskirts do not yet sell the fantasy of a climbable Appalachian sniper town. High ground should be tactically useful and traversable, the quarry should read as a deep extraction valley, the forest should provide concealment and route variety, and roads should look like cohesive surfaced infrastructure rather than placeholders.

## Approved Direction

Use the `Hybrid` approach.

- Keep the town core readable and mostly stable.
- Rebuild the outskirts into more natural, climbable ridges, hills, and valleys.
- Make every named hill traversable through sloped approaches.
- Turn the map-edge ridge into a mostly continuous climbable ring with controlled valley cuts and access routes.
- Keep the outermost backface steeper and less traversable so players do not naturally reach the hard map edge.
- Push the quarry into a deeper basin with terraced descent and enclosing wall masses.
- Densify the forest belt heavily with pines, spruce, bushes, and rock clusters.
- Replace bare raised road ribbons with cohesive surfaced roads using EasyRoads3D where reliable, otherwise textured road prefabs already present in the project.

## Terrain Language

The town should sit inside a broad basin with recognizable elevation tiers.

- `Town Core`: lowest readable civic/commercial floor with direct road access.
- `Intermediate Hills`: church hill, water-tower hill, player-overlook hill, and smaller connecting knolls should all have sloped walk-up approaches instead of pedestal geometry.
- `Outer Ridge Ring`: a higher perimeter shelf that players can climb onto from selected approaches, then use for movement and long-range overwatch.
- `Valley Cuts`: at least a few obvious low routes that break the ridge ring and carry roads or forested approaches into and out of the basin.
- `Quarry Basin`: the deepest landform in the scene, below town grade, with a readable descent path and strong enclosing walls.

## Traversal Contract

- Hills inside the playable basin should be fully climbable.
- Ridge access should feel intentional rather than free-form from every point.
- The ridge top should plateau before the map edge.
- The edge-facing backside should become steeper and visually harsher so it reads as world boundary, not a playable overlook lip.
- Quarry traversal should include a staged descent through ramps, terraces, or broad sloped cuts rather than a sheer drop.

## Landscape Density

Forest density should increase substantially around the map edge, on ridge approaches, and in low-visibility travel lanes.

- Use taller conifers as the dominant silhouette.
- Break up sightlines with clusters, not evenly spaced single trees.
- Mix in rocks, bushes, and fallen-tree-style clutter where available to avoid empty lawns between trunks.
- Preserve some deliberate long lanes for sniper play rather than turning the entire map into solid tree cover.

## Road Surface Direction

Roads should read as real roads, not just raised strips.

- Prefer EasyRoads3D if it can reliably build linked surfaced roads in the live editor.
- If EasyRoads3D is unstable through current tooling, replace or skin the existing named road ribbons with textured road prefabs from project assets.
- Main roads should read paved and cohesive through town.
- Spurs to quarry / ridge / forest edges can read rougher, narrower, or more rural.

## Scene Safety Constraints

- Preserve current runtime-critical roots, scene entry points, and population/contract anchors.
- Do not break the `MainTownWorldShell` district/landmark naming contract.
- Keep the dedicated layout EditMode tests relevant; adjust only if the second pass introduces a clearly better and still intentional contract.
- Continue updating the existing progress log during execution.

## Success Criteria

- `MainTown` reads as a large rural sniper sandbox rather than a flat town dropped onto placeholder cubes.
- Players can climb the major hills and selected ridge routes naturally.
- The quarry clearly reads as a deep valley/basin.
- Forest cover is substantially denser and supports concealment routes.
- Roads have visible surfaced geometry/material identity.
- Existing scene/runtime contracts remain intact.

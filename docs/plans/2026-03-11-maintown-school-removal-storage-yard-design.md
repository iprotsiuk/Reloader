# MainTown School Removal Storage Yard Design

## Summary

Replace the `School Campus` graybox district in `MainTown` with a more on-theme `Storage Yard` district. The district should stay in roughly the same northwestern shell position so the broader `2km x 2km` planning-map layout remains stable.

## Goals

- Remove the civilian-school read from the town plan.
- Keep the area visually legible from a zoomed-out editor view.
- Preserve approximate footprint and district spacing.
- Keep the replacement useful for future assassin / sniper sandbox gameplay.

## Replacement District

- Old district: `District_SchoolCampus`
- New district: `District_StorageYard`

## New Graybox Composition

- `MarkerPad_StorageYard`
- `StorageOffice`
- `LockerRow_A`
- `LockerRow_B`
- overhead label: `Label_StorageYard`

## Scale Direction

- Keep the existing school pad footprint roughly intact.
- Use one small office mass and two longer storage-row masses.
- Maintain the same human-scale blockout logic already used elsewhere in `MainTown`.

## Contract Impact

- Remove `SchoolCampus` assertions from the dedicated `MainTown` layout EditMode test.
- Require `StorageYard` objects instead.
- Keep all other district and water-spine expectations intact.

# Persistent Civilian Population Progress

## Status

- [x] Design direction approved
- [x] Implementation plan written
- [x] Runtime implementation started

## 2026-03-07 Checkpoint

- Added the first green runtime checkpoint for the data foundation:
  - registered `CivilianPopulation` as a required save module
  - bumped the runtime save schema from `v6` to `v7`
  - added persistent civilian + queued-replacement payload DTOs
  - added a minimal civilian appearance generator contract in `Reloader.NPCs`
- Verified the red-first tests passed after implementation:
  - `Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests`
  - `Reloader.NPCs.Tests.EditMode.CivilianAppearanceGeneratorTests`
- Follow-up still pending in this slice:
  - save-creation roster generation
  - MainTown spawn-from-records bridge
  - civilian retirement + replacement queue runtime wiring

## Scope Notes

- New saves should generate a persistent `MainTown` civilian roster.
- Save/load must preserve the same generated civilians within one save.
- Vendors and other protected authored roles stay outside the generated civilian pool.
- Civilian death retires the civilian from the save instead of rerolling the entire town.
- Each retired civilian creates one owed replacement entry.
- Replacements should later arrive during the Monday `08:00` population refresh at the bus stop.

## Explicit Non-Goals For This Slice

- contract target selection from the population
- generated contract briefings or portrait capture
- professions, dialogue, voices, schedules, and wandering zones
- executing the Monday refresh beyond recording the replacement debt

## Next Slice After This One

Once the persistent population foundation is stable, the next slice should wire contracts to it:

- pick random living civilians as contract targets
- derive contract descriptions from the selected civilian appearance snapshot
- preserve vendor/protected-role exclusions in target selection
- execute the Monday `08:00` replacement queue so vacancies eventually refill in-world

# Persistent Civilian Population Progress

## Status

- [x] Design direction approved
- [x] Implementation plan written
- [ ] Runtime implementation started

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

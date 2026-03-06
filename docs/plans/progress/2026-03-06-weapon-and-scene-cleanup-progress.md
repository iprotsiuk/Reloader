# Weapon And Scene Cleanup Progress

## Scope

- Supported authored weapons: `Kar98k (.308)` and `Canik TP9 (9mm)`
- Strict registry/runtime behavior: no hidden fallback lookup
- Scene parity: `MainTown` and `IndoorRange`
- Dropped-item visuals: no grey cube fallback in live runtime
- Active PR review feedback: `PoliceHeatController.ReportLineOfSightLost()` idempotence

## Status

- [x] Design approved
- [x] Implementation plan written
- [ ] Docs committed before code changes
- [ ] PR review thread rechecked
- [ ] Police heat LOS-loss review comment fixed
- [ ] WeaponRegistry strict-resolution cleanup complete
- [ ] Tab inventory strict-resolution cleanup complete
- [ ] Canik TP9 naming/content cleanup complete
- [ ] Unsupported authored weapon content pruned
- [ ] MainTown / IndoorRange parity cleanup complete
- [ ] Dropped-item visual cleanup complete
- [ ] Targeted verification complete
- [ ] PR updated with progress/evidence

## Active PR Feedback

- `PR #24` has one active inline review thread to address before completion:
  - `Reloader/Assets/_Project/LawEnforcement/Scripts/Runtime/PoliceHeatController.cs`
  - repeated `ReportLineOfSightLost()` should not refresh the search countdown while already in `Search`

## Notes

- Do not invent fake pistol attachment content in this pass.
- Do not broaden this into the later contract/law/NPC/world architecture rewrite.
- Keep this progress doc updated with commits, verification, and deferred follow-ups.

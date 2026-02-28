# Unity VCS Merge Guard

> Status Pointer (2026-02-28): This is a planning/execution artifact. For live implemented-vs-planned status, use `docs/design/v0.1-demo-status-and-milestones.md`.


## Problem
Unity conflict resolution can silently drop package dependencies or write stale `ProjectSettings` content (for example switching build scenes or clearing project metadata), causing compile/runtime drift.

## Guardrails
1. Configure Unity SmartMerge once per clone:
```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity" ./scripts/setup-unity-yaml-merge.sh
```

2. After every conflict resolution, run:
```bash
./scripts/check-unity-vcs-health.py
```

3. If the check fails:
- Restore unexpected `Reloader/ProjectSettings/**` drift.
- Reopen Unity and allow package refresh.
- Re-run the health check.

## What is checked
- Required packages exist in `Reloader/Packages/manifest.json`:
  - `com.unity.cinemachine`
  - `com.unity.nuget.newtonsoft-json`
  - `com.unity.postprocessing`
- Suspicious conflict artifacts are flagged in:
  - `Reloader/ProjectSettings`
  - `Reloader/Assets/_Project`

## Notes
- `*.unity`, `*.prefab`, and `*.asset` merges are routed through `unityyamlmerge` via `.gitattributes`.
- This reduces bad auto-merges but does not replace review.

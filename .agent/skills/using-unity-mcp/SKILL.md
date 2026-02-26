---
name: using-unity-mcp
description: Routes Unity work to the right execution path with a default preference for Unity MCP editor APIs. Use when tasks involve scenes, GameObjects, components, prefabs, materials, animation, editor state, Unity tests, or when deciding between MCP calls and direct file edits.
---

# Using Unity MCP

## When to Use

- Working on Unity editor state, scene hierarchy, GameObjects, components, prefabs, materials, VFX, animation, or tags/layers
- Running Unity tests or checking Unity console/editor status from automation
- Choosing whether to use Unity MCP tools vs shell commands and direct C# file edits
- NOT appropriate when: task is purely docs/plans/git text work with no Unity editor interaction

## Workflow

```text
MCP Routing Progress:
- [ ] Classify the task (Editor state vs file content)
- [ ] Choose path (MCP-first or file-first)
- [ ] Execute with minimal tool surface
- [ ] Verify in Unity state/tests/console
- [ ] Report result with concrete evidence
```

## Instructions

### Step 0: Bind the target Unity instance

Before any MCP mutation:

1. Read `mcpforunity://instances`
2. If more than one editor is running, set target explicitly with `set_active_instance`
3. Read `mcpforunity://project/info` and confirm project root/name match the intended target

Never run scene/prefab/component mutation commands without explicit instance targeting when multiple instances exist.

### Step 1: Classify the task

Use `MCP-first` if the source of truth is Unity editor/runtime state:

- Scene graph changes: create/move/delete/duplicate objects
- Component operations: add/remove/set serialized properties
- Prefab content operations
- Material, VFX, animation setup
- Editor control (play/pause/stop, tags/layers)
- Unity tests and console diagnostics

Use `File-first` if the source of truth is code/docs text:

- C# architecture/refactors/business logic in scripts
- Design docs, plans, rules, markdown edits
- Git history, diffs, and repository maintenance

### Step 2: Choose tool path

For `MCP-first` tasks:

1. Check readiness and scope
   - Read `mcpforunity://editor/state`
   - Read `mcpforunity://project/info` if version/path context matters
   - If editor is in play mode and task mutates scene/prefab/component state, stop play mode first (`manage_editor` action `stop`) unless task explicitly requires runtime mutation
2. Prefer high-level MCP tools over raw text edits
   - GameObjects: `find_gameobjects`, `manage_gameobject`, `manage_components`
   - Prefabs: `manage_prefabs`
   - Assets/materials/animation/VFX: `manage_asset`, `manage_material`, `manage_animation`, `manage_vfx`
   - Editor/tests/console: `manage_editor`, `run_tests` + `get_test_job`, `read_console`
3. For many independent MCP operations, use `batch_execute`
4. If scripts must change, use `script_apply_edits`/`apply_text_edits` and then validate (`validate_script`)

For `File-first` tasks:

1. Use shell discovery (`rg`, focused file reads) and normal edits
2. Use MCP only when Unity state must be queried/applied
3. Keep text-only changes outside Unity MCP unless Unity APIs are explicitly required

### Step 3: Verify before claiming success

- State changes: read back relevant MCP resources or queried objects (mandatory)
  - Examples: `mcpforunity://editor/state`, `manage_scene` action `get_hierarchy`, `mcpforunity://scene/gameobject/{id}`, `mcpforunity://scene/gameobject/{id}/components`
- Script edits via MCP: run `validate_script`
- Gameplay/editor behavior: run targeted Unity tests (`run_tests`/`get_test_job`)
- Failures: check `read_console` and iterate from the first concrete error

Do not claim completion if write operations were not followed by a read-back verification step.

## Quick Reference

| Task Type | Preferred Path | Primary Tools |
|-----------|----------------|---------------|
| Scene hierarchy/object placement | MCP-first | `manage_gameobject`, `find_gameobjects` |
| Component wiring/tuning | MCP-first | `manage_components` |
| Prefab authoring | MCP-first | `manage_prefabs` |
| Materials/animation/VFX | MCP-first | `manage_material`, `manage_animation`, `manage_vfx` |
| Editor mode/tags/layers | MCP-first | `manage_editor` |
| Unity tests/console | MCP-first | `run_tests`, `get_test_job`, `read_console` |
| C# logic refactor | File-first | shell edits (+ MCP script tools only if needed) |
| Docs/plans/rules | File-first | shell edits |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Editing scene behavior only in code when serialized scene data is the issue | Use MCP scene/component operations first |
| Using many sequential MCP calls for independent object operations | Use `batch_execute` |
| Claiming success without checking Unity-side result | Read state back and run targeted tests |
| Using MCP for pure markdown/git tasks | Stay file-first |

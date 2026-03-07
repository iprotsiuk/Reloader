# TAB Overlay Contracts Redesign Progress

## Status

- [x] Design direction approved
- [x] Implementation plan written
- [x] Non-draft PR opened to `main`
- [x] `@codex` tagged for review

## Execution Checklist

- [ ] Baseline screenshots captured
- [x] Icon source selected
- [ ] Three-region shell landed
- [ ] Icon-first left rail landed
- [ ] Posted contract feed landed
- [ ] Active contract workspace landed
- [ ] Cancel contract action landed
- [ ] Ready-to-claim / claim reward flow landed
- [ ] Right-side terms pane landed
- [ ] Density tightening pass landed
- [ ] Final screenshot set captured

## Notes

- The overlay remains an immersive in-world panel, not a separate full-screen scene.
- The approved shell is `left rail + center workspace + right terms/detail pane`.
- The center pane must serve two distinct modes:
  - posted-contract feed when no contract is active
  - focused mission workspace when one contract is active
- Posted contracts use a dense forum/slack-like row layout with fixed row height and explicit scrolling.
- Active contracts must show mission status above the target name.
- Contract completion must become an explicit `Ready to Claim` -> `Claim Reward` flow.
- The right pane should prioritize payout logic, restrictions, bonus conditions, and failure rules instead of weak metadata like distance labels.
- Validate layout decisions with Unity MCP screenshots at each major step, not just code review.
- Baseline shell audit:
  - current `TabInventory.uxml` still uses a horizontal text tab bar plus stacked section blocks
  - `inventory__panel` already fills most of the screen (`92%` width, `88%` height), so the problem is density and zoning, not overall panel size
  - the contracts section is still a synthesized flat column (`status`, `title`, `target`, `distance`, `payout`, `briefing`, `accept`) created through `EnsureContractsSectionBindings()`
  - the binder is the lowest-risk shell seam because it already owns section wiring and responsive sizing
- Lowest-risk first shell test:
  - extend `UiRuntimeCutoverPlayModeTests.ExecuteCutover_CreatesToolkitDocumentsAndRuntimeBridge`
  - reason: it already boots the real `UiToolkitRuntimeRoot` and `UIDocument` stack, so it can assert named shell containers on the live TAB document before contract-specific rendering changes
- Selected icon source:
  - `/Users/ivanprotsiuk/Documents/assets/LOWPOLY/Post-apocalyptic Survival UI`
  - strongest candidates from asset path metadata:
    - `c66e974f4efd2f545bc747499036a9c2` -> `Assets/Post-apocalyptic Survival UI/Sprites/Icons/Inventory_Icon.png`
    - `2f6df2fefd6bb96488bb0da5dad5cbd5` -> `Assets/Post-apocalyptic Survival UI/Sprites/Icons/Daily_Icon.png`
    - `a2a0ff7c11096f4439110bae98df44f6` -> `Assets/Post-apocalyptic Survival UI/Sprites/Icons/Message_Icon.png`
    - `895ec163d0ac7e549b06bf2a2d45a50f` -> `Assets/Post-apocalyptic Survival UI/Sprites/Icons/Settings_Icon.png`
    - fallback inventory silhouette: `d677d3d72ce8b7d4d912727b5de70547` -> `Assets/Post-apocalyptic Survival UI/Sprites/Backpack_Icon.png`
  - visual direction fits the grounded survival UI better than the fantasy card pack also present under `LOWPOLY`
- Baseline density problems to eliminate:
  - top-level text tabs are too wide for the amount of state they carry
  - inventory and contracts compete inside one loose vertical flow instead of fixed work regions
  - the contracts view spends space on labels without creating hierarchy
  - the current contract panel cannot scale to portrait, terms, and claim-state content without becoming taller and more cramped
- Unity MCP baseline capture:
  - scene discovery is working (`MainTown` active scene)
  - baseline screenshot capture hit transient Unity MCP transport failures (`Unity is reloading`, then `Could not connect to Unity` / `Connection closed before reading expected bytes`)
  - next attempt will use a deterministic UI shell checkpoint after the first test-driven layout change instead of treating the current unstable screenshot pass as authoritative
- Shell checkpoint status:
  - added a new PlayMode shell test: `UiRuntimeCutoverPlayModeTests.ExecuteCutover_TabInventoryUsesThreeRegionShell`
  - verified the test fails red against the old `TabInventory` document because the shell regions were absent
  - implemented the minimal green phase in `TabInventory.uxml`, `TabInventory.uss`, and `TabInventoryViewBinder.cs`
  - local non-Unity verification passed: `xmllint --noout` on `TabInventory.uxml`, `git diff --check`
  - Unity test rerun is currently blocked by a Unity test-framework/editor-state failure after the first PlayMode run:
    - `InvalidOperationException: This cannot be used during play mode, please use SceneManager.CreateScene() instead.`
    - follow-on symptom: the editor was left on an `InitTestScene...` bootstrap scene and the Unity MCP bridge intermittently lost the active editor instance
  - the shell code is checkpointed for review, but the `Three-region shell landed` checklist item stays open until a clean rerun is recorded

## Verification

- `bash scripts/verify-docs-and-context.sh`: passed
- `git diff --check` on redesign docs: passed
- PR / review status: PR `#26` opened and `@codex` requested
- Unity MCP read-back:
  - active instance: `Reloader@1f51a703`
  - active scene: `MainTown`
  - editor state: idle / ready for tools

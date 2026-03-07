# TAB Overlay Contracts Redesign Progress

## Status

- [x] Design direction approved
- [x] Implementation plan written
- [x] Non-draft PR opened to `main`
- [x] `@codex` tagged for review

## Execution Checklist

- [ ] Baseline screenshots captured
- [x] Icon source selected
- [x] Three-region shell landed
- [x] Icon-first left rail landed
- [x] Posted contract feed landed
- [x] Active contract workspace landed
- [ ] Cancel contract action landed
- [ ] Ready-to-claim / claim reward flow landed
- [ ] Right-side terms pane landed
- [x] Density tightening pass landed
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
  - external source pack used during authoring: `Post-apocalyptic Survival UI`
  - selected icons must be imported into a repo-tracked UI images folder before the implementation depends on them
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
  - after restarting Unity, the targeted PlayMode rerun passed cleanly
  - the shell scaffold is now verified and closed for this checkpoint
- Icon rail checkpoint status:
  - replaced the wide text tab rail with icon-first navigation using repo-tracked `TabRail/tab_*.png` assets
  - added a structural runtime check: `UiRuntimeCutoverPlayModeTests.ExecuteCutover_TabInventoryUsesIconRailNavigation`
  - tightened the rail width and per-tab sizing so the left rail now returns more usable width to the workspace
- Minimum-width fallback checkpoint status:
  - verified the new PR review about minimum-width collapse was still valid against the shell scaffold
  - added `TabInventoryResponsiveLayoutEditModeTests.ApplyResponsiveLayout_HidesDetailPane_WhenPanelWidthDropsBelowMinimum`
  - updated `TabInventoryViewBinder.ApplyResponsiveDetailPane()` to collapse the outer detail pane when the panel cannot sustain both the rail and a usable workspace width
  - the fallback now zeroes the workspace right margin when collapsed so the center pane reclaims the lost space
  - Unity CLI verification is currently blocked on inconsistent test-result emission and a flaky post-restart MCP bridge, so the checkpoint remains pending screenshot validation once the editor session is healthy again
- Contracts feed/workspace checkpoint status:
  - replaced the old synthesized flat contracts column with authored `TabInventory.uxml` shells for a posted-offer feed row and an active-contract workspace
  - extended `TabInventoryUiState.ContractPanelState` with explicit `Mode` and `SummaryText` so the controller can switch the center pane between posted-offer and active-contract layouts without inventing new runtime controller dependencies
  - updated `TabInventoryController.BuildContractPanelFields()` so posted offers use the target description as the dense row summary while active contracts surface the target name and briefing in the dedicated workspace
  - added coverage for the authored contracts shell in `TabInventoryUxmlCopyEditModeTests.ContractsSection_AuthorsPostedFeedAndActiveWorkspaceShell`
  - updated `TabInventoryContractsSectionPlayModeTests` to lock the expected center-pane mode switch (`feed` when available, `active workspace` after accept)
  - runtime screenshot and Unity test-runner verification are still pending until the editor session reliably reconnects to Unity MCP
- Density tightening checkpoint status:
  - applied the first screenshot-driven compression pass after user feedback that the left rail, payout segment, and accept button were still consuming too much width
  - narrowed the shell proportions again: icon rail `60px` authored width, tighter workspace gap, and a smaller placeholder detail pane so the center pane gets more real contract width
  - tightened the posted-contract row itself by shrinking the preview tile, portrait, payout segment, and accept button while forcing the summary column to own the remaining width (`flex-basis: 0`)
  - updated `TabInventoryViewBinder.ApplyResponsiveTabs()` to fall back to assigned tab-bar width during deterministic tests instead of depending only on live `contentRect`
  - added `TabInventoryResponsiveLayoutEditModeTests.ApplyResponsiveLayout_ClampsIconRailTabsToCompactWidth_WhenTabBarStaysNarrow` to lock the compact rail sizing contract

## Verification

- `bash scripts/verify-docs-and-context.sh`: passed
- `git diff --check` on redesign docs: passed
- `xmllint --noout Reloader/Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml`: passed
- repo-wide `git diff --check`: passed
- PR / review status: PR `#26` opened and `@codex` requested
- Unity MCP read-back:
  - initial shell red-phase test failed for the expected missing-shell reason
  - `Reloader.UI.Tests.PlayMode.UiRuntimeCutoverPlayModeTests.ExecuteCutover_TabInventoryUsesThreeRegionShell`: passed (`1/1`)
  - `Reloader.UI.Tests.PlayMode.UiRuntimeCutoverPlayModeTests.ExecuteCutover_TabInventoryUsesIconRailNavigation`: passed (`1/1`)
  - `Reloader.UI.Tests.EditMode.TabInventoryResponsiveLayoutEditModeTests`: passed (`2/2`)
  - `Reloader.UI.Tests.EditMode.TabInventoryUxmlCopyEditModeTests`: passed (`3/3`)
  - `Reloader.UI.Tests.PlayMode.UiRuntimeCutoverPlayModeTests`: passed (`5/5`)
  - `Reloader.UI.Tests.PlayMode.TabInventoryContractsSectionPlayModeTests`: passed (`3/3`)
  - `Reloader.UI.Tests.PlayMode.TabInventoryAttachmentsPlayModeTests`: passed (`4/4`)

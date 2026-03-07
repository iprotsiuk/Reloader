# TAB Overlay Contracts Redesign Progress

## Status

- [x] Design direction approved
- [x] Implementation plan written
- [ ] Non-draft PR opened to `main`
- [ ] `@codex` tagged for review

## Execution Checklist

- [ ] Baseline screenshots captured
- [ ] Icon source selected
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

## Verification

- `bash scripts/verify-docs-and-context.sh`: pending
- PR / review status: pending

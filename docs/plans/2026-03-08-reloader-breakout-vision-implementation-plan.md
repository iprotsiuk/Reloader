# Reloader Breakout Vision Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Turn the approved breakout-vision discussion into concrete, testable product slices that sharpen `Reloader` into an assassination sandbox with hardcore prep instead of a generic realism sim.

**Architecture:** Keep the current contract-prep-escape spine intact, then validate high-impact additions through narrow slices rather than broad speculative expansion. Prioritize player-facing fantasy surfaces that increase mission expression, tone, and watchability before adding deeper simulation complexity.

**Tech Stack:** Unity 6.3, C#, ScriptableObject-driven content, existing contract/UI/world runtime seams, design docs under `docs/plans/` and `docs/design/`.

---

### Task 1: Lock the product thesis in canonical docs

**Files:**
- Modify: `docs/design/assassination-contracts.md`
- Modify: `docs/design/v0.1-demo-status-and-milestones.md`
- Reference: `docs/plans/2026-03-08-reloader-breakout-vision-design.md`

**Step 1: Write the failing doc review checklist**

Create a short checklist for:
- assassination sandbox first
- hardcore prep as amplifier, not headline
- specialty job expression called out explicitly

**Step 2: Run docs audit to confirm the current baseline**

Run:

```bash
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

Expected:
- pass, with no structural doc errors before edits

**Step 3: Write minimal doc updates**

Update only the smallest sections needed so canonical docs reflect:
- criminal sniper sandbox framing
- specialty ammo / job-expression priority
- anti-goal against sterile realism-first messaging

**Step 4: Re-run doc audit**

Run the same audit command and confirm green.

**Step 5: Commit**

```bash
git add docs/design/assassination-contracts.md \
        docs/design/v0.1-demo-status-and-milestones.md
git commit -m "docs: sharpen assassination sandbox thesis"
```

### Task 2: Define signature contract modifiers

**Files:**
- Create: `docs/plans/2026-03-08-signature-contract-modifiers-design.md`
- Reference: `docs/plans/2026-03-08-reloader-breakout-vision-design.md`
- Reference: `docs/design/assassination-contracts.md`

**Step 1: Write a shortlist**

Draft 6-8 candidate modifiers, then rank them by:
- readability
- trailer value
- implementation leverage
- reliance on existing systems

**Step 2: Narrow to the top 3**

Pick the 3 modifiers that best prove the fantasy in a demo:
- likely one should be AP-through-glass or similar

**Step 3: Document player-facing payoff**

For each modifier, specify:
- what the player sees
- what prep choice changes
- what failure looks like

**Step 4: Commit**

```bash
git add docs/plans/2026-03-08-signature-contract-modifiers-design.md
git commit -m "docs: define signature contract modifiers"
```

### Task 3: Define specialty ammo fantasy before adding more calibers

**Files:**
- Create: `docs/plans/2026-03-08-specialty-ammo-fantasy-design.md`
- Reference: `docs/design/assassination-contracts.md`
- Reference: `docs/design/prototype-scope.md`

**Step 1: List ammo fantasies**

Examples:
- AP against glass/light cover
- low-penetration rounds for crowded jobs
- subsonic/suppressed later
- premium match loads for narrow windows

**Step 2: Mark what is v0.1 vs later**

Keep scope strict.
Do not let the list become a simulation backlog.

**Step 3: Define one first ammo slice**

Specify the single highest-value first implementation:
- rule
- use case
- user-visible payoff
- test seam

**Step 4: Commit**

```bash
git add docs/plans/2026-03-08-specialty-ammo-fantasy-design.md
git commit -m "docs: define specialty ammo fantasy"
```

### Task 4: Design underworld progression surfaces

**Files:**
- Create: `docs/plans/2026-03-08-underworld-progression-design.md`
- Reference: `docs/plans/2026-03-08-reloader-breakout-vision-design.md`

**Step 1: Define progression buckets**

At minimum:
- workshop upgrades
- intel access
- ammo/component access
- higher-tier contracts

**Step 2: Remove vague progression**

Avoid generic XP/progression language unless tied to the actual fantasy.

**Step 3: Specify 3 concrete progression rewards**

Each should unlock:
- a new type of job
- a new prep option
- or a new way to solve contracts

**Step 4: Commit**

```bash
git add docs/plans/2026-03-08-underworld-progression-design.md
git commit -m "docs: define underworld progression surfaces"
```

### Task 5: Choose the next prototype slice from the vision work

**Files:**
- Create: `docs/plans/2026-03-08-breakout-slice-priority.md`
- Reference: previous docs created in Tasks 2-4

**Step 1: Score candidate slices**

Score by:
- fantasy impact
- engineering cost
- demo/trailer value
- dependence on missing police/social systems

**Step 2: Select one next slice**

Pick exactly one:
- specialty ammo
- one signature contract modifier
- underworld progression surface
- target routine/intel upgrade

**Step 3: Write an implementation recommendation**

Specify:
- why this slice is first
- what to delay
- what existing seams it should reuse

**Step 4: Commit**

```bash
git add docs/plans/2026-03-08-breakout-slice-priority.md
git commit -m "docs: prioritize next breakout slice"
```

### Task 6: Verify doc coherence and hand off

**Files:**
- Verify: all docs added or modified in Tasks 1-5

**Step 1: Run required doc validation**

Run:

```bash
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

Expected:
- all pass

**Step 2: Review for wording drift**

Confirm all new docs consistently describe:
- assassination sandbox first
- hardcore prep second
- specialty job expression as a priority

**Step 3: Commit any final cleanup**

```bash
git add docs
git commit -m "docs: finalize breakout vision pack"
```

### Task 7: Decide execution path

**Files:**
- None required

**Step 1: Pick the next real implementation**

Choose the highest-value slice from Task 5.

**Step 2: Open the next session or stay here**

Use either:
- subagent-driven implementation in this session
- or a fresh session with `executing-plans`

**Step 3: Do not branch into broad feature work**

Implement the chosen slice only.


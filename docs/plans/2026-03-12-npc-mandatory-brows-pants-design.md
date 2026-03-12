# NPC Mandatory Brows Pants Design

**Goal:** Make STYLE/MainTown NPC appearance treat eyebrows and pants as required, with automatic healing for legacy or invalid records.

**Problem**

Recent eyebrow-field work split `EyebrowId` away from `OutfitBottomId`, but some legacy/demo records still carry invalid or blank eyebrow ids, and some records still carry invalid or missing bottom ids. The current applicator only guarantees eyebrow fallback in a narrow way, while clone/save paths keep propagating bad values. That leaves some NPCs in the demo scene and MainTown without eyebrows or pants.

**Approach**

Use one authoritative normalization contract for STYLE/MainTown appearance ids:

- `EyebrowId` must always resolve to an approved `brous*` id.
- `OutfitBottomId` must always resolve to `pants1` for the current curated STYLE pool.
- Legacy misuse of `OutfitBottomId` as eyebrow data remains accepted only as migration input, not as persisted output.

Apply that normalization at every shared seam that can rehydrate or copy appearance:

- appearance rules helpers
- runtime applicator
- save-module clone path
- runtime-bridge clone path

Generated curated records should already produce valid values, but the shared normalizers will still be used so authored/demo and legacy data heal the same way.

**Testing**

Add failing editmode regressions first for:

- blank/invalid eyebrow id healing to an approved eyebrow mesh
- blank/invalid bottom id healing to `pants1`
- clone/save paths normalizing bad eyebrow/bottom values instead of preserving them

Verification stays focused on NPC editmode suites plus guardrails.

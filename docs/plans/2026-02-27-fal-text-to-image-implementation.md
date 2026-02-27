# fal.ai Text-to-Image CLI Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a minimal Node CLI command `npm run gen -- "prompt"` that generates images via fal.ai and saves them into `generatedImages/<summary>/` with a manifest.

**Architecture:** Keep all logic in a small Node module under `scripts/fal/`, with a thin CLI entrypoint. Core functions (summary derivation, output pathing, downloads, manifest writing) are testable in isolation using Node's built-in test runner.

**Tech Stack:** Node.js (ESM), `@fal-ai/client`, Node built-in `node:test`, `fs/promises`, global `fetch`.

---

### Task 1: Project scaffolding for Node CLI

**Files:**
- Create: `package.json`
- Modify: `.gitignore`

**Step 1: Add minimal npm package and scripts**
- Add `package.json` with:
  - `type: "module"`
  - script `gen` -> `node scripts/fal/generate-image.mjs`
  - script `test:fal` -> `node --test test/fal-generator.test.mjs`
  - dependency `@fal-ai/client`

**Step 2: Ignore generated artifacts**
- Add `generatedImages/` to `.gitignore`.

**Step 3: Verify scaffold**
Run: `npm install`
Expected: installs `@fal-ai/client` with lockfile.

### Task 2: Write failing tests first (TDD)

**Files:**
- Create: `test/fal-generator.test.mjs`
- Test target: `scripts/fal/generator.mjs`

**Step 1: Write tests for summary and output behavior**
- `derivePromptSummary` returns at most 2 words, sanitized.
- Empty/invalid prompt falls back to `prompt`.
- `saveImagesFromUrls` writes image files with timestamped names.
- `writeManifest` persists required fields.

**Step 2: Run tests to verify RED**
Run: `npm run test:fal`
Expected: FAIL because `scripts/fal/generator.mjs` does not exist yet.

### Task 3: Implement minimal generator module

**Files:**
- Create: `scripts/fal/generator.mjs`

**Step 1: Implement functions to satisfy tests**
- `derivePromptSummary(prompt)`
- `ensureOutputDir(baseDir, summary)`
- `saveImagesFromUrls({ urls, outputDir, timestamp, fetchFn })`
- `writeManifest({ outputDir, manifest })`

**Step 2: Run tests to verify GREEN**
Run: `npm run test:fal`
Expected: PASS.

### Task 4: Add CLI entrypoint with fal.ai call

**Files:**
- Create: `scripts/fal/generate-image.mjs`

**Step 1: Parse CLI args and validate env**
- Require prompt arg.
- Require `FAL_KEY`.
- Print clear usage and error messages.

**Step 2: Integrate `@fal-ai/client`**
- Configure client with `FAL_KEY`.
- Call default model via `fal.subscribe(...)`.
- Extract image URLs.

**Step 3: Persist files and manifest**
- Build output folder using summary.
- Download image files.
- Save `manifest.json`.
- Print saved file paths.

### Task 5: Documentation and verification

**Files:**
- Create: `docs/fal-image-generator.md`

**Step 1: Document exact usage**
- `npm install`
- `export FAL_KEY=...`
- `npm run gen -- "..."`
- Output path format details.

**Step 2: Run verification commands**
Run:
- `npm run test:fal`
- Optional live check (if key available): `npm run gen -- "minimal test prompt"`

Expected:
- Tests pass.
- Live run produces image file(s) + `manifest.json` under `generatedImages/<summary>/`.

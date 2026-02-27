# fal.ai Text-to-Image CLI Design

## Context
We need a minimal Phase 1 integration for fal.ai text-to-image generation from this repo. The goal is fast asset generation for downstream Meshy.ai model creation, without Unity Editor integration yet.

## Scope
- Single command interface from repo root.
- Output images under:
  - `/Users/ivanprotsiuk/Documents/unity/Reloader/generatedImages/<prompt_summary_2wordsmax>/<files>`
- Store generation metadata in a manifest file for traceability.
- Keep API key out of source control; use environment variable.

## Decisions
- Runtime: Node.js.
- Client: `@fal-ai/client`.
- CLI shape: `npm run gen -- "<prompt>"`.
- Output folder naming: deterministic 2-word sanitized summary derived from prompt.
- Collision handling: append timestamp to generated files to avoid overwrite.

## Behavior
- Read `FAL_KEY` from environment.
- Generate image(s) from one default model ID.
- Download all returned image URLs.
- Save files to the prompt summary folder.
- Save `manifest.json` with prompt, summary, model ID, timestamp, remote URLs, and local paths.
- Fail clearly when key is missing, prompt missing, API fails, or download fails.

## Non-Goals (Phase 1)
- Unity Editor UI.
- Meshy.ai API integration.
- Batch JSON prompt runs.
- Prompt presets, style libraries, or advanced parameter tuning.

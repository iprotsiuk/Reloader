# fal.ai Image Generator (Phase 1)

This project includes a minimal CLI to generate images from text prompts using fal.ai.

## Setup

```bash
npm install
export FAL_KEY="your_fal_api_key"
```

## Generate an image

```bash
npm run gen -- "vintage revolver blueprint, technical drawing"
```

## Output location

Files are saved to:

`/Users/ivanprotsiuk/Documents/unity/Reloader/generatedImages/<prompt_summary_2wordsmax>/`

Examples:
- `generatedImages/vintage-revolver/image-1-20260227-183000.png`
- `generatedImages/vintage-revolver/manifest.json`

## Notes

- Prompt summary folder name uses the first 2 sanitized words from your prompt.
- If no valid words are found, fallback folder is `prompt`.
- The CLI currently uses default model `fal-ai/flux/dev`.

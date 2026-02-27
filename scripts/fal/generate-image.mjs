import path from 'node:path';
import { fal } from '@fal-ai/client';
import {
  derivePromptSummary,
  ensureOutputDir,
  extractImageUrls,
  formatTimestamp,
  saveImagesFromUrls,
  writeManifest
} from './generator.mjs';

const DEFAULT_MODEL_ID = 'fal-ai/flux/dev';
const GENERATED_IMAGES_ROOT = path.resolve(process.cwd(), 'generatedImages');

function usage() {
  console.error('Usage: npm run gen -- "your prompt"');
}

async function main() {
  const prompt = process.argv.slice(2).join(' ').trim();

  if (!prompt) {
    usage();
    throw new Error('Prompt is required.');
  }

  const apiKey = process.env.FAL_KEY;
  if (!apiKey) {
    throw new Error('FAL_KEY is not set. Export your fal.ai API key and retry.');
  }

  fal.config({ credentials: apiKey });

  const result = await fal.subscribe(DEFAULT_MODEL_ID, {
    input: {
      prompt
    },
    logs: true,
    onQueueUpdate(update) {
      if (update.status === 'IN_PROGRESS' && Array.isArray(update.logs)) {
        for (const log of update.logs) {
          if (log?.message) {
            console.log(`[fal] ${log.message}`);
          }
        }
      }
    }
  });

  const imageUrls = extractImageUrls(result?.data);
  if (imageUrls.length === 0) {
    throw new Error('fal.ai response did not contain image URLs.');
  }

  const promptSummary = derivePromptSummary(prompt);
  const outputDir = await ensureOutputDir(GENERATED_IMAGES_ROOT, promptSummary);
  const timestamp = formatTimestamp();
  const localFiles = await saveImagesFromUrls({
    urls: imageUrls,
    outputDir,
    timestamp
  });

  const manifest = {
    prompt,
    promptSummary,
    modelId: DEFAULT_MODEL_ID,
    requestId: result?.requestId ?? null,
    timestamp,
    imageUrls,
    localFiles
  };

  const manifestPath = await writeManifest({ outputDir, manifest });

  console.log('Generation complete.');
  console.log(`Output directory: ${outputDir}`);
  for (const file of localFiles) {
    console.log(`Saved: ${file}`);
  }
  console.log(`Manifest: ${manifestPath}`);
}

main().catch((error) => {
  console.error(`fal generation failed: ${error.message}`);
  process.exit(1);
});

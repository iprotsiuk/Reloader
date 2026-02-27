import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';

import {
  derivePromptSummary,
  saveImagesFromUrls,
  writeManifest
} from '../scripts/fal/generator.mjs';

test('derivePromptSummary keeps max two sanitized words', () => {
  const summary = derivePromptSummary('Wild West revolver concept art, detailed!');
  assert.equal(summary, 'wild-west');
});

test('derivePromptSummary falls back when prompt has no valid tokens', () => {
  const summary = derivePromptSummary('@@@ ###');
  assert.equal(summary, 'prompt');
});

test('saveImagesFromUrls writes timestamped files', async () => {
  const dir = await fs.mkdtemp(path.join(os.tmpdir(), 'fal-gen-'));
  const urls = ['https://example.test/img1.png', 'https://example.test/img2.jpg'];

  const files = await saveImagesFromUrls({
    urls,
    outputDir: dir,
    timestamp: '20260227-120000',
    fetchFn: async () => new Response(new Uint8Array([1, 2, 3]), { status: 200 })
  });

  assert.equal(files.length, 2);
  assert.match(path.basename(files[0]), /^image-1-20260227-120000\.png$/);
  assert.match(path.basename(files[1]), /^image-2-20260227-120000\.jpg$/);

  const bytes = await fs.readFile(files[0]);
  assert.equal(bytes.length, 3);
});

test('writeManifest persists manifest.json', async () => {
  const dir = await fs.mkdtemp(path.join(os.tmpdir(), 'fal-manifest-'));
  const manifest = {
    prompt: 'test prompt',
    promptSummary: 'test-prompt',
    modelId: 'fal-ai/test-model',
    timestamp: '20260227-120000',
    imageUrls: ['https://example.test/img1.png'],
    localFiles: [path.join(dir, 'image-1.png')]
  };

  const manifestPath = await writeManifest({ outputDir: dir, manifest });
  const parsed = JSON.parse(await fs.readFile(manifestPath, 'utf8'));

  assert.equal(parsed.prompt, 'test prompt');
  assert.equal(parsed.promptSummary, 'test-prompt');
  assert.equal(parsed.localFiles.length, 1);
});

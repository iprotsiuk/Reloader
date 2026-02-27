import fs from 'node:fs/promises';
import path from 'node:path';

const VALID_EXTENSIONS = new Set(['.png', '.jpg', '.jpeg', '.webp']);

function normalizePrompt(prompt) {
  return String(prompt ?? '')
    .normalize('NFKD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase();
}

export function derivePromptSummary(prompt) {
  const normalized = normalizePrompt(prompt);
  const words = normalized.match(/[a-z0-9]+/g) ?? [];
  const summaryWords = words.slice(0, 2);

  if (summaryWords.length === 0) {
    return 'prompt';
  }

  return summaryWords.join('-');
}

export async function ensureOutputDir(baseDir, promptSummary) {
  const outputDir = path.join(baseDir, promptSummary);
  await fs.mkdir(outputDir, { recursive: true });
  return outputDir;
}

function extensionFromUrl(url) {
  try {
    const parsed = new URL(url);
    const ext = path.extname(parsed.pathname).toLowerCase();
    if (VALID_EXTENSIONS.has(ext)) {
      return ext;
    }
  } catch {
    // Fall through to default extension.
  }

  return '.png';
}

export async function saveImagesFromUrls({ urls, outputDir, timestamp, fetchFn = fetch }) {
  if (!Array.isArray(urls) || urls.length === 0) {
    throw new Error('No image URLs provided.');
  }

  const localFiles = [];

  for (let index = 0; index < urls.length; index += 1) {
    const url = urls[index];
    const response = await fetchFn(url);

    if (!response.ok) {
      throw new Error(`Failed to download image ${index + 1}: ${response.status} ${response.statusText}`);
    }

    const bytes = Buffer.from(await response.arrayBuffer());
    const ext = extensionFromUrl(url);
    const fileName = `image-${index + 1}-${timestamp}${ext}`;
    const filePath = path.join(outputDir, fileName);

    await fs.writeFile(filePath, bytes);
    localFiles.push(filePath);
  }

  return localFiles;
}

export async function writeManifest({ outputDir, manifest }) {
  const manifestPath = path.join(outputDir, 'manifest.json');
  await fs.writeFile(manifestPath, `${JSON.stringify(manifest, null, 2)}\n`, 'utf8');
  return manifestPath;
}

export function formatTimestamp(date = new Date()) {
  const year = String(date.getFullYear());
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  const seconds = String(date.getSeconds()).padStart(2, '0');

  return `${year}${month}${day}-${hours}${minutes}${seconds}`;
}

export function extractImageUrls(resultData) {
  const urls = [];

  const candidates = [
    ...(Array.isArray(resultData?.images) ? resultData.images : []),
    ...(Array.isArray(resultData?.data?.images) ? resultData.data.images : []),
    ...(Array.isArray(resultData?.output) ? resultData.output : [])
  ];

  for (const item of candidates) {
    if (typeof item === 'string' && item.length > 0) {
      urls.push(item);
      continue;
    }

    if (item && typeof item.url === 'string' && item.url.length > 0) {
      urls.push(item.url);
      continue;
    }

    if (item && typeof item.image === 'string' && item.image.length > 0) {
      urls.push(item.image);
    }
  }

  return urls;
}

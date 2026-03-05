#!/usr/bin/env python3
"""Generate images from fal.ai text-to-image endpoints and save local artifacts.

Usage:
  python3 scripts/ai/generate_fal_images.py \
    --prompt "..." \
    --models "fal-ai/nano-banana-2,fal-ai/nano-banana-pro,fal-ai/flux/dev"
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import urllib.error
import urllib.request
from dataclasses import dataclass, asdict
from datetime import datetime
from pathlib import Path
from typing import Any, Iterable

DEFAULT_MODELS = [
    "fal-ai/nano-banana-2",
    "fal-ai/nano-banana-pro",
    "fal-ai/flux/dev",
]


def _slug(text: str) -> str:
    text = text.strip().lower()
    text = re.sub(r"[^a-z0-9]+", "-", text)
    return text.strip("-")[:72] or "prompt"


def _request_json(url: str, payload: dict[str, Any], fal_key: str) -> dict[str, Any]:
    req = urllib.request.Request(
        url,
        data=json.dumps(payload).encode("utf-8"),
        headers={
            "Authorization": f"Key {fal_key}",
            "Content-Type": "application/json",
        },
        method="POST",
    )
    with urllib.request.urlopen(req, timeout=300) as resp:
        return json.loads(resp.read().decode("utf-8"))


def _walk_urls(value: Any) -> Iterable[str]:
    if isinstance(value, dict):
        for v in value.values():
            yield from _walk_urls(v)
        return
    if isinstance(value, list):
        for v in value:
            yield from _walk_urls(v)
        return
    if not isinstance(value, str):
        return
    if value.startswith("http://") or value.startswith("https://"):
        lowered = value.lower()
        if any(ext in lowered for ext in [".png", ".jpg", ".jpeg", ".webp", "fal.media", "fal.run"]):
            yield value


def _download(url: str, out_path: Path) -> None:
    req = urllib.request.Request(url, method="GET")
    with urllib.request.urlopen(req, timeout=300) as resp:
        out_path.write_bytes(resp.read())


@dataclass
class ModelResult:
    endpoint: str
    ok: bool
    error: str | None
    raw_response_file: str
    downloaded_files: list[str]


def run(prompt: str, models: list[str], out_root: Path, fal_key: str) -> int:
    out_root.mkdir(parents=True, exist_ok=True)

    summary: list[ModelResult] = []
    any_success = False

    for endpoint in models:
        safe_name = endpoint.replace("/", "__")
        model_dir = out_root / safe_name
        model_dir.mkdir(parents=True, exist_ok=True)

        response_path = model_dir / "response.json"
        downloaded: list[str] = []

        try:
            response = _request_json(
                url=f"https://fal.run/{endpoint}",
                payload={"prompt": prompt},
                fal_key=fal_key,
            )
            response_path.write_text(json.dumps(response, indent=2), encoding="utf-8")

            if isinstance(response, dict) and "detail" in response and isinstance(response["detail"], str):
                summary.append(
                    ModelResult(
                        endpoint=endpoint,
                        ok=False,
                        error=response["detail"],
                        raw_response_file=str(response_path),
                        downloaded_files=[],
                    )
                )
                continue

            urls = []
            seen = set()
            for url in _walk_urls(response):
                if url not in seen:
                    urls.append(url)
                    seen.add(url)

            for i, url in enumerate(urls, start=1):
                guessed_ext = ".png"
                lower = url.lower()
                if ".jpg" in lower or ".jpeg" in lower:
                    guessed_ext = ".jpg"
                elif ".webp" in lower:
                    guessed_ext = ".webp"

                out_file = model_dir / f"image-{i}{guessed_ext}"
                try:
                    _download(url, out_file)
                    downloaded.append(str(out_file))
                except Exception as ex:  # keep best-effort downloads
                    (model_dir / f"download-{i}.error.txt").write_text(str(ex), encoding="utf-8")

            ok = len(downloaded) > 0
            any_success = any_success or ok
            summary.append(
                ModelResult(
                    endpoint=endpoint,
                    ok=ok,
                    error=None if ok else "No downloadable image URLs found in response",
                    raw_response_file=str(response_path),
                    downloaded_files=downloaded,
                )
            )

        except urllib.error.HTTPError as ex:
            body = ex.read().decode("utf-8", errors="replace")
            response_path.write_text(body, encoding="utf-8")
            summary.append(
                ModelResult(
                    endpoint=endpoint,
                    ok=False,
                    error=f"HTTP {ex.code}: {body[:500]}",
                    raw_response_file=str(response_path),
                    downloaded_files=[],
                )
            )
        except Exception as ex:
            response_path.write_text(str(ex), encoding="utf-8")
            summary.append(
                ModelResult(
                    endpoint=endpoint,
                    ok=False,
                    error=str(ex),
                    raw_response_file=str(response_path),
                    downloaded_files=[],
                )
            )

    manifest = {
        "created_at": datetime.now().isoformat(timespec="seconds"),
        "prompt": prompt,
        "models": models,
        "results": [asdict(item) for item in summary],
    }
    manifest_path = out_root / "manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    print(f"Saved artifacts to: {out_root}")
    print(f"Manifest: {manifest_path}")
    for item in summary:
        status = "OK" if item.ok else "FAILED"
        print(f"- {item.endpoint}: {status}")
        if item.error:
            print(f"  error: {item.error}")
        if item.downloaded_files:
            for f in item.downloaded_files:
                print(f"  file: {f}")

    return 0 if any_success else 2


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--prompt", required=True, help="Text prompt")
    parser.add_argument(
        "--models",
        default=",".join(DEFAULT_MODELS),
        help="Comma-separated fal endpoint IDs",
    )
    parser.add_argument(
        "--out-dir",
        default=None,
        help="Output directory. Default: tmp/ai-images/maintown-<timestamp>-<prompt-slug>",
    )
    args = parser.parse_args()

    fal_key = os.environ.get("FAL_KEY", "").strip()
    if not fal_key:
        print("FAL_KEY is missing", file=sys.stderr)
        return 1

    models = [m.strip() for m in args.models.split(",") if m.strip()]
    if not models:
        print("No models selected", file=sys.stderr)
        return 1

    if args.out_dir:
        out_root = Path(args.out_dir)
    else:
        ts = datetime.now().strftime("%Y%m%d-%H%M%S")
        out_root = Path("tmp") / "ai-images" / f"maintown-{ts}-{_slug(args.prompt)}"

    return run(prompt=args.prompt, models=models, out_root=out_root, fal_key=fal_key)


if __name__ == "__main__":
    raise SystemExit(main())

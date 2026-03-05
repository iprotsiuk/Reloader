#!/usr/bin/env python3
"""Clean up duplicate-style entries like 'Name 2.ext' when 'Name.ext' exists.

Default mode is dry-run. Use --apply to perform deletions.
"""

from __future__ import annotations

import argparse
import csv
import re
import shutil
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


SUFFIX_PATTERN = re.compile(r"^(?P<stem>.+) (?P<num>\d+)(?P<ext>\.[^/]+)?$")


@dataclass(frozen=True)
class Candidate:
    path: Path
    canonical_path: Path
    number: int
    kind: str


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Delete duplicate-style files/dirs with numeric suffixes "
            "(for example: 'file 2.txt') when canonical sibling exists "
            "('file.txt')."
        )
    )
    parser.add_argument(
        "--root",
        default=".",
        help="Root directory to scan. Default: current directory.",
    )
    parser.add_argument(
        "--min-number",
        type=int,
        default=2,
        help="Minimum numeric suffix to consider. Default: 2.",
    )
    parser.add_argument(
        "--apply",
        action="store_true",
        help="Perform deletions. Default is dry-run.",
    )
    parser.add_argument(
        "--manifest",
        default="",
        help="Optional CSV output path for matched candidates.",
    )
    parser.add_argument(
        "--max-print",
        type=int,
        default=200,
        help="Maximum number of matched paths to print. Default: 200.",
    )
    return parser.parse_args()


def should_skip_dir(name: str) -> bool:
    return name == ".git"


def collect_paths(root: Path) -> set[Path]:
    all_paths: set[Path] = set()
    for dirpath, dirnames, filenames in os_walk(root):
        dirnames[:] = [d for d in dirnames if not should_skip_dir(d)]
        base = Path(dirpath)
        for dirname in dirnames:
            all_paths.add(base / dirname)
        for filename in filenames:
            all_paths.add(base / filename)
    return all_paths


def os_walk(root: Path) -> Iterable[tuple[str, list[str], list[str]]]:
    # Local import keeps module startup small.
    import os

    return os.walk(root)


def find_candidates(all_paths: set[Path], min_number: int) -> list[Candidate]:
    candidates: list[Candidate] = []
    for path in sorted(all_paths, key=lambda p: (len(p.parts), str(p)), reverse=True):
        match = SUFFIX_PATTERN.match(path.name)
        if not match:
            continue

        number = int(match.group("num"))
        if number < min_number:
            continue

        canonical_name = f"{match.group('stem')}{match.group('ext') or ''}"
        canonical_path = path.parent / canonical_name
        if canonical_path not in all_paths:
            continue

        path_is_dir = path.is_dir()
        canonical_is_dir = canonical_path.is_dir()
        if path_is_dir != canonical_is_dir:
            continue

        kind = "dir" if path_is_dir else "file"
        candidates.append(
            Candidate(
                path=path,
                canonical_path=canonical_path,
                number=number,
                kind=kind,
            )
        )
    return candidates


def write_manifest(manifest_path: Path, candidates: list[Candidate], mode: str) -> None:
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    with manifest_path.open("w", newline="", encoding="utf-8") as file_obj:
        writer = csv.writer(file_obj)
        writer.writerow(["mode", "path", "canonical_path", "number", "kind"])
        for candidate in candidates:
            writer.writerow(
                [
                    mode,
                    candidate.path.as_posix(),
                    candidate.canonical_path.as_posix(),
                    candidate.number,
                    candidate.kind,
                ]
            )


def apply_deletions(candidates: list[Candidate]) -> tuple[int, int, list[tuple[Path, str]]]:
    deleted_files = 0
    deleted_dirs = 0
    failures: list[tuple[Path, str]] = []

    for candidate in candidates:
        try:
            if candidate.kind == "file":
                candidate.path.unlink()
                deleted_files += 1
            else:
                shutil.rmtree(candidate.path)
                deleted_dirs += 1
        except Exception as exc:  # noqa: BLE001
            failures.append((candidate.path, str(exc)))

    return deleted_files, deleted_dirs, failures


def main() -> int:
    args = parse_args()
    root = Path(args.root).resolve()
    if not root.exists() or not root.is_dir():
        print(f"Root directory does not exist: {root}", file=sys.stderr)
        return 2

    all_paths = collect_paths(root)
    candidates = find_candidates(all_paths, min_number=args.min_number)

    mode = "apply" if args.apply else "dry-run"
    if args.manifest:
        write_manifest(Path(args.manifest), candidates, mode)

    printed = 0
    for candidate in candidates:
        if printed >= args.max_print:
            break
        print(f"[{mode}] {candidate.kind}: {candidate.path} (canonical: {candidate.canonical_path})")
        printed += 1

    if len(candidates) > args.max_print:
        print(f"... {len(candidates) - args.max_print} additional matches omitted")

    deleted_files = 0
    deleted_dirs = 0
    failures: list[tuple[Path, str]] = []
    if args.apply:
        deleted_files, deleted_dirs, failures = apply_deletions(candidates)

    print(f"mode={mode}")
    print(f"root={root}")
    print(f"min_number={args.min_number}")
    print(f"matched={len(candidates)}")
    if args.apply:
        print(f"deleted_files={deleted_files}")
        print(f"deleted_dirs={deleted_dirs}")
        print(f"failures={len(failures)}")
        for path, reason in failures[:20]:
            print(f"failure: {path} :: {reason}")

    if args.manifest:
        print(f"manifest={Path(args.manifest).resolve()}")

    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())

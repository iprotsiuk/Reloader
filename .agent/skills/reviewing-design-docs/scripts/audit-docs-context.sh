#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
audit-docs-context.sh — Audit design-doc/agent-context consistency.

Usage:
  bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
  bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh --help

Checks:
  1. Existing repo guardrail script
  2. Local markdown link integrity (docs/, .agent/skills/, .cursor/rules/)
  3. Protected router-overlap pairs
  4. Unity context ignore hygiene in .codexignore/.cursorignore/.ignore

Exit code:
  0 on success
  1 if any check fails
USAGE
}

if [[ "${1:-}" == "--help" || "${1:-}" == "-h" ]]; then
  usage
  exit 0
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
cd "$ROOT_DIR"

FAILURES=0

pass() {
  echo "OK: $1"
}

fail() {
  echo "FAIL: $1"
  FAILURES=$((FAILURES + 1))
}

echo "Running docs/context audit in: $ROOT_DIR"

if [[ -f scripts/verify-docs-and-context.sh ]]; then
  if bash scripts/verify-docs-and-context.sh; then
    pass "Repository guardrails"
  else
    fail "Repository guardrails"
  fi
else
  fail "Missing scripts/verify-docs-and-context.sh"
fi

if [[ -f scripts/verify-extensible-development-contracts.sh ]]; then
  if bash scripts/verify-extensible-development-contracts.sh; then
    pass "Extensible development guardrails"
  else
    fail "Extensible development guardrails"
  fi
else
  fail "Missing scripts/verify-extensible-development-contracts.sh"
fi

if python3 - <<'PY'
import os
import re
import sys

root = os.getcwd()
scan_dirs = ["docs", ".agent/skills", ".cursor/rules"]
pattern = re.compile(r"\[[^\]]*\]\(([^)]+)\)")

missing = []
for d in scan_dirs:
    base = os.path.join(root, d)
    if not os.path.isdir(base):
        continue
    for dirpath, _, filenames in os.walk(base):
        for fn in filenames:
            if not (fn.endswith(".md") or fn.endswith(".mdc")):
                continue
            path = os.path.join(dirpath, fn)
            rel_src = os.path.relpath(path, root)
            with open(path, "r", encoding="utf-8", errors="ignore") as f:
                text = f.read()
            for m in pattern.finditer(text):
                target = m.group(1).strip()
                if not target:
                    continue
                if target.startswith("#") or target.startswith("http") or target.startswith("mailto:"):
                    continue
                target_no_frag = target.split("#", 1)[0].strip()
                if not target_no_frag:
                    continue
                if re.match(r"^[A-Za-z]+:", target_no_frag):
                    continue
                abs_target = os.path.normpath(os.path.join(os.path.dirname(path), target_no_frag))
                if not os.path.exists(abs_target):
                    missing.append((rel_src, target_no_frag, os.path.relpath(abs_target, root)))

if missing:
    print("Broken local markdown links:")
    for src, target, resolved in missing:
        print(f"- {src} -> {target} (resolved: {resolved})")
    sys.exit(1)

print("No broken local markdown links.")
PY
then
  pass "Local markdown links"
else
  fail "Local markdown links"
fi

if python3 - <<'PY'
import glob
import os
import re
import sys

root = os.getcwd()
rules_dir = os.path.join(root, ".cursor", "rules")
pairs = [
    ("inventory-save-context.mdc", "save-schema-context.mdc"),
    ("scene-persistence-context.mdc", "world-vehicles-context.mdc"),
]

def parse_globs(rule_name):
    rule_path = os.path.join(rules_dir, rule_name)
    if not os.path.isfile(rule_path):
        return []
    with open(rule_path, "r", encoding="utf-8", errors="ignore") as f:
        text = f.read()
    m = re.search(r"^globs:\s*(.+)$", text, flags=re.M)
    if not m:
        return []
    return [g.strip() for g in m.group(1).split(",") if g.strip()]

def matched_files(globs_list):
    out = set()
    for g in globs_list:
        for p in glob.glob(os.path.join(root, g), recursive=True):
            if os.path.isfile(p):
                out.add(os.path.relpath(p, root))
    return out

violations = []
for left, right in pairs:
    left_files = matched_files(parse_globs(left))
    right_files = matched_files(parse_globs(right))
    overlap = sorted(left_files & right_files)
    if overlap:
        violations.append((left, right, overlap))

if violations:
    print("Protected router overlaps detected:")
    for left, right, overlap in violations:
        print(f"- {left} <-> {right}: {len(overlap)} overlapping files")
        for p in overlap[:10]:
            print(f"  - {p}")
        if len(overlap) > 10:
            print("  - ...")
    sys.exit(1)

print("No protected router overlaps detected.")
PY
then
  pass "Protected router overlap"
else
  fail "Protected router overlap"
fi

IGNORE_FILES=(.codexignore .cursorignore .ignore)
REQUIRED_PATTERNS=(
  "Reloader/Library/**"
  "Reloader/Temp/**"
  "Reloader/Logs/**"
  "Reloader/Assets/ThirdParty/**"
)

IGNORE_FAILURES=0
for f in "${IGNORE_FILES[@]}"; do
  if [[ ! -f "$f" ]]; then
    echo "FAIL: Missing ignore file $f"
    IGNORE_FAILURES=$((IGNORE_FAILURES + 1))
    continue
  fi
  for p in "${REQUIRED_PATTERNS[@]}"; do
    if ! rg -n --fixed-strings "$p" "$f" >/dev/null 2>&1; then
      echo "FAIL: $f missing pattern: $p"
      IGNORE_FAILURES=$((IGNORE_FAILURES + 1))
    fi
  done
done

if [[ "$IGNORE_FAILURES" -eq 0 ]]; then
  pass "Ignore hygiene"
else
  fail "Ignore hygiene"
fi

if [[ "$FAILURES" -eq 0 ]]; then
  echo "All docs/context audit checks passed."
  exit 0
fi

echo "Audit completed with $FAILURES failing check(s)."
exit 1

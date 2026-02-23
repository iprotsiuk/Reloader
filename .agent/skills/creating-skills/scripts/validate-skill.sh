#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'HELP'
validate-skill.sh — Validate an agent skill folder structure and frontmatter.

Usage:
  validate-skill.sh <path-to-skill-folder>
  validate-skill.sh --help

Checks:
  1. SKILL.md exists
  2. YAML frontmatter contains 'name' and 'description'
  3. 'name' is <= 64 chars, lowercase/digits/hyphens only, no "claude" or "anthropic"
  4. 'description' is <= 1024 chars and non-empty
  5. SKILL.md body is <= 500 lines (warning 500-700 for reference skills, hard fail >700)
  6. No backslash path separators in SKILL.md
  7. No deeply nested references (links inside linked files)

Exit codes:
  0  All checks passed
  1  One or more checks failed
HELP
  exit 0
}

[[ "${1:-}" == "--help" ]] && usage
[[ $# -lt 1 ]] && { echo "Error: Provide path to skill folder. Run --help for usage."; exit 1; }

SKILL_DIR="$1"
SKILL_MD="$SKILL_DIR/SKILL.md"
ERRORS=0

fail() {
  echo "FAIL: $1"
  ERRORS=$((ERRORS + 1))
}

pass() {
  echo "  OK: $1"
}

echo "Validating skill at: $SKILL_DIR"
echo "---"

# 1. SKILL.md exists
if [[ ! -f "$SKILL_MD" ]]; then
  fail "SKILL.md not found at $SKILL_MD"
  echo "---"
  echo "Validation aborted: $ERRORS error(s)"
  exit 1
fi
pass "SKILL.md exists"

# Extract frontmatter (between first pair of ---)
FRONTMATTER=$(awk '/^---$/{if(n++)exit;next}n' "$SKILL_MD")

# 2. name field exists
NAME=$(echo "$FRONTMATTER" | grep -E '^name:' | head -1 | sed 's/^name:[[:space:]]*//' | tr -d '"' | tr -d "'")
if [[ -z "$NAME" ]]; then
  fail "'name' field missing from frontmatter"
else
  pass "'name' field present: $NAME"

  # 3a. name length
  if [[ ${#NAME} -gt 64 ]]; then
    fail "'name' exceeds 64 characters (${#NAME} chars)"
  else
    pass "'name' length OK (${#NAME} chars)"
  fi

  # 3b. name format
  if ! echo "$NAME" | grep -qE '^[a-z0-9-]+$'; then
    fail "'name' contains invalid characters (allowed: lowercase, digits, hyphens)"
  else
    pass "'name' format OK"
  fi

  # 3c. forbidden words
  NAME_LOWER=$(echo "$NAME" | tr '[:upper:]' '[:lower:]')
  if echo "$NAME_LOWER" | grep -qE '(claude|anthropic)'; then
    fail "'name' contains forbidden word (claude/anthropic)"
  else
    pass "'name' no forbidden words"
  fi
fi

# 4. description field
DESC=$(echo "$FRONTMATTER" | grep -E '^description:' | head -1 | sed 's/^description:[[:space:]]*//' | tr -d '"' | tr -d "'")
if [[ -z "$DESC" ]]; then
  fail "'description' field missing from frontmatter"
else
  if [[ ${#DESC} -gt 1024 ]]; then
    fail "'description' exceeds 1024 characters (${#DESC} chars)"
  else
    pass "'description' length OK (${#DESC} chars)"
  fi
fi

# 5. Body line count (lines after second ---)
BODY_LINES=$(awk 'BEGIN{n=0} /^---$/{n++;next} n>=2{print}' "$SKILL_MD" | wc -l | tr -d ' ')
if [[ "$BODY_LINES" -gt 700 ]]; then
  fail "SKILL.md body exceeds 700 lines ($BODY_LINES lines) — hard limit"
elif [[ "$BODY_LINES" -gt 500 ]]; then
  echo " WARN: SKILL.md body is $BODY_LINES lines (soft limit: 500 for action skills, 700 for reference skills)"
else
  pass "SKILL.md body line count OK ($BODY_LINES lines)"
fi

# 6. No backslash paths
if grep -nE '\\\\' "$SKILL_MD" | grep -vE '^\s*#' | grep -q '\\'; then
  fail "Backslash path separators found in SKILL.md (use / instead)"
else
  pass "No backslash path separators"
fi

# 7. No deeply nested references (links in linked files)
LINKED_FILES=$(grep -oE '\[[^]]*\]\([^)]+\)' "$SKILL_MD" 2>/dev/null | grep -oE '\([^)]+\)' | tr -d '()' | grep -v '^http' | grep -v '^#' || true)
NESTED=0
for linked in $LINKED_FILES; do
  LINKED_PATH="$SKILL_DIR/$linked"
  if [[ -f "$LINKED_PATH" ]] && grep -qE '\[[^]]*\]\([^)]+\)' "$LINKED_PATH" 2>/dev/null; then
    NESTED_LINKS=$(grep -oE '\[[^]]*\]\([^)]+\)' "$LINKED_PATH" | grep -oE '\([^)]+\)' | tr -d '()' | grep -v '^http' | grep -v '^#' | head -5 || true)
    if [[ -n "$NESTED_LINKS" ]]; then
      NESTED=$((NESTED + 1))
    fi
  fi
done
if [[ $NESTED -gt 0 ]]; then
  fail "Found $NESTED linked file(s) that contain further file links (max depth = 1)"
else
  pass "No deeply nested references"
fi

echo "---"
if [[ $ERRORS -eq 0 ]]; then
  echo "All checks passed."
  exit 0
else
  echo "Validation failed: $ERRORS error(s)"
  exit 1
fi

---
name: guarding-deployments
description: Validates deployment prerequisites before pushing to staging or production. Use when the user mentions deploy, release, push to production, ship, go live, or CI/CD pipeline checks.
---

# Deployment Guard

Pre-flight validation for deployments. Prevents shipping broken builds, missing migrations, or untagged releases.

## When to Use

- Before any `git push` to a deployment branch
- When running CI/CD deploy commands
- When user says "deploy", "release", "ship it", "push to prod"
- NOT for local development builds

## Workflow

```
Deployment Checklist:
- [ ] All tests pass locally
- [ ] No uncommitted changes
- [ ] Branch is up to date with remote
- [ ] Database migrations are in sync
- [ ] Environment variables validated
- [ ] Version tag applied
- [ ] Changelog updated
```

## Instructions

### 1. Run Pre-Flight Checks

```bash
git status --porcelain
git diff origin/main --stat
npm test 2>&1 | tail -5
```

If any check fails, STOP and report the issue. Do not proceed.

### 2. Validate Environment

```bash
scripts/check-env.sh --target staging
```

Run `scripts/check-env.sh --help` for flag details.

### 3. Tag and Deploy

Only after all checks pass:

```bash
git tag -a "v$(date +%Y%m%d-%H%M)" -m "Release"
git push origin --tags
```

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Deploying with uncommitted changes | Run `git stash` or commit first |
| Skipping migration check | Always run `db:migrate:status` before deploy |
| Forgetting version tag | Tag is required; script enforces this |

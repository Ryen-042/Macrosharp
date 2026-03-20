# Git Workflow Conventions For Roadmap Execution

Created: 2026-03-20
Primary branch for this initiative: roadmap/implementation-phased-mar2026

## Branching Strategy

Default strategy for this initiative:
1. Keep roadmap branch as integration branch for approved incremental work.
2. Use short-lived task branches for implementation slices when risk is medium or high.
3. Merge task branches back into the roadmap branch after manual verification notes are recorded.

Branch naming convention:
1. roadmap/implementation-phased-mar2026 (already created)
2. feat/phaseX-taskY-short-title
3. fix/phaseX-short-title
4. docs/phaseX-short-title

Examples:
1. feat/phase1-path-validation
2. fix/phase1-config-watcher-debounce
3. feat/phase3-hotkey-dispatch-policy
4. docs/phase5-hotkey-reference-guide

## Commit Message Convention

Format:
Type: short summary

Recommended types:
1. roadmap
2. feat
3. fix
4. refactor
5. perf
6. docs
7. chore

Examples:
1. roadmap: add phase 0 risk matrix and governance docs
2. fix: replace blocking config watcher wait with async debounce
3. perf: index text expansion triggers by mode and suffix
4. refactor: extract hotkey registry from Program host bootstrap
5. docs: add runtime hotkey reference usage guide

## Commit Granularity Rule

1. One coherent concern per commit.
2. Avoid mixing roadmap/tracker documentation edits with runtime code changes in the same commit.
3. Include tracker update in the same commit only when status changes are directly caused by that commit.

## Merge And Rollback Policy

1. Do not merge work that has unresolved blocker entries without an explicit decision log note.
2. For rollback, prefer targeted revert commits instead of history rewrite.
3. If a high-risk change is reverted, add a blocker log entry and a revised implementation path.

## Clarification Requirement

Ask for clarification before branching or committing when:
1. A task can be interpreted as either refactor-only or behavior-changing.
2. The change may affect key bindings or reminder trigger timing.
3. The implementation could be done in either one large commit or several smaller commits and trade-offs are unclear.

Suggested clarification prompt:
1. Should this be delivered as strict refactor parity, or is behavior adjustment acceptable?
2. Do you prefer one commit for the full task, or multiple commits by sub-step?
3. Should this work land directly in roadmap branch, or via feature branch first?

## Pull Request Checklist (If Used)

1. Roadmap task ID referenced in title or description.
2. Clarification decisions captured for ambiguous points.
3. Manual verification notes attached.
4. Execution tracker updated.
5. Timeline impact noted (none, minor, major).

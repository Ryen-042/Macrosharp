# Manual Verification Checklist

Created: 2026-03-20
Scope: Regression control for roadmap execution without automated tests

## Usage Rules

1. Run relevant sections before merging each task.
2. Record results in EXECUTION_TRACKER_2026-03.md phase closeout and weekly review sections.
3. If uncertain about expected behavior, ask for clarification before marking pass or fail.

## Result Marking

Use one of:
1. Pass
2. Fail
3. Blocked
4. Not Applicable

## A) Build And Startup

1. Build solution in current branch.
2. Start host and confirm no startup crash.
3. Confirm tray icon appears and remains stable.
4. Confirm basic shutdown path works.

Record:
- Result:
- Notes:

## B) Core Hotkey Functionality

1. Verify application control hotkeys execute expected action.
2. Verify repeatable hotkeys do not create obvious lag/stall.
3. Verify conditional hotkeys pass through when condition is false.
4. Verify pause/resume behavior for keyboard and mouse paths.

Record:
- Result:
- Notes:

## C) Text Expansion

1. Immediate trigger mode expansion works.
2. On-delimiter trigger mode expansion works.
3. Placeholder expansion works for supported tokens.
4. Rapid typing does not produce duplicate or overlapping expansions.
5. Toggling text expansion on/off works reliably.

Record:
- Result:
- Notes:

## D) Reminder Scheduling And Delivery

1. Interval reminders fire as expected.
2. Daily/weekly/monthly recurrence paths still function.
3. Snooze updates next trigger behavior correctly.
4. Notification and sound mute toggles behave correctly.
5. No obvious scheduler lag or over-triggering under normal load.

Record:
- Result:
- Notes:

## E) Configuration Lifecycle

For each config file category (main, hotkey, text expansion, reminders):
1. Missing file is recreated with valid defaults or known fallback behavior.
2. Valid file edits are detected and reloaded.
3. Corrupted file recovery path executes expected fallback.
4. Recovery output is clear enough for troubleshooting.

Record:
- Result:
- Notes:

## E.1) Phase 2 Config Lifecycle Matrix (P2-5)

Execute this matrix for [main, hotkeys, text-expansions, reminders]:
1. Start with valid file and confirm startup load succeeds.
2. Edit valid JSON while app is running and confirm expected reload behavior.
3. Introduce invalid JSON and confirm backup creation with incremental .bak naming.
4. Confirm recovery behavior keeps last-known-good state where implemented.
5. Restore valid JSON and confirm system returns to normal behavior.

Expected feedback policy (approved):
1. Mixed approach is acceptable.
2. User-facing dialogs are required only where currently implemented.
3. Console diagnostics are acceptable for other recovery paths.

Record:
- Result:
- Notes:

## F) Path Safety And Input Validation

1. Normal config and asset paths still resolve correctly.
2. Invalid path input patterns are rejected safely.
3. Error output is clear and non-destructive.

Record:
- Result:
- Notes:

## G) UI And Tray Workflows

1. Tray menu actions execute correct handlers.
2. Toast actions (quit, open folder, snooze) still work.
3. Icon switching remains stable.
4. Config opening actions still function.

Record:
- Result:
- Notes:

## H) Heavy Usage Smoke

1. Hold repeatable hotkeys for several seconds and observe responsiveness.
2. Rapidly type text expansion triggers while toggling pause and expansion states.
3. Trigger reminders while interacting with tray actions.
4. Confirm no persistent freeze, crash, or runaway behavior.

Record:
- Result:
- Notes:

## I) Clarification Gate (Mandatory)

Before sign-off, confirm:
1. Any ambiguous behavior was resolved by clarification request.
2. Any task with multiple valid interpretations has a recorded decision.
3. No assumption was made silently for behavior-changing paths.

Record:
- Result:
- Notes:

## Verification Summary Template

- Task or Phase:
- Date:
- Verifier:
- Sections executed:
- Pass count:
- Fail count:
- Blocked count:
- Key findings:
- Required follow-up:
- Clarification requests raised:
- Final recommendation: Go or No-Go

# Phase 2 Verification Log

Date: 2026-03-21
Scope: P2-5 manual regression verification for configuration lifecycle
Status: Completed

## Completed Checks

1. Build validation
- Command: dotnet build src/Macrosharp.sln /property:UseAppHost=false
- Result: Pass

2. Main config defaults
- File: src/macrosharp.config.json
- Verified: FileWatching.MainConfig=false, HotkeysConfig=false, TextExpansionsConfig=false, RemindersConfig=false
- Result: Pass

3. Source wiring audit
- Verified constructor defaults watchForChanges=false across Main, Hotkey, TextExpansion, Reminder managers
- Verified Program reads FileWatching toggles and enables main config watching conditionally
- Result: Pass

4. Host startup smoke
- Command: dotnet run --project src/Macrosharp.Hosts/Macrosharp.Hosts.Console/Macrosharp.Hosts.Console.csproj
- Verified startup banner and successful initialization
- Verified runtime logs show config watchers disabled for reminders and text expansions by default
- Result: Pass

5. Live reload with watchers enabled (main, text-expansions, reminders)
- Setup: Temporarily enabled MainConfig, TextExpansionsConfig, and RemindersConfig watcher toggles.
- Verified: Runtime watcher startup logs, file-change detection logs, and reload messages.
- Result: Pass

6. Corrupted config recovery (text-expansions)
- Setup: Wrote invalid JSON while app was running.
- Verified: Parse failure logged, incremental backup created (text-expansions.bak0.json), and revert-to-last-known-good message logged.
- Result: Pass

7. Corrupted config recovery (reminders)
- Setup: Wrote invalid JSON while app was running.
- Verified: Parse failure logged, incremental backup created (reminders.bak0.json), and revert-to-last-known-good message logged.
- Result: Pass

8. Stderr/runtime error scan
- Verified: No stderr output during scripted live verification run.
- Result: Pass

## Scope Notes

1. Host runtime currently exercises main, text-expansion, and reminder config flows directly.
2. Hotkey configuration manager lifecycle verification was completed at manager-implementation level, but not via host runtime flow because host currently does not wire hotkeys.json loading in Program.
3. Mixed feedback policy remains in effect: targeted UI prompts in selected managers and console diagnostics in others.

## Notes

- Current process lock behavior for apphost binaries requires using UseAppHost=false for unattended build verification while host is running.
- No runtime exceptions observed during startup smoke run.
- Temporary verification artifacts and backup files created during runtime testing were cleaned up after evidence capture.

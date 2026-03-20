# Macrosharp Implementation Roadmap (Initial Version)

Created: 2026-03-20
Planning Horizon: 2026-03-23 to 2026-05-22
Owner: Macrosharp maintainers
Status: Approved for execution

## 1) Operating Rule For All Phases

Before starting or finishing any task, the agent must explicitly ask for clarification when either of these is true:
1. The task has more than one valid interpretation.
2. The expected behavior is not fully specified.
3. A change may impact user workflows, key bindings, reminder behavior, or automation safety.
4. A design trade-off has meaningful downside (performance vs complexity, flexibility vs strictness, etc.).

Standard clarification prompt template for implementers:
- What is the intended behavior in this exact scenario?
- Which option should be preferred if both are technically valid?
- Are there constraints on backward compatibility for this area?

## 2) Scope Included In This Plan

This roadmap includes all approved items:
1. Program decomposition and host architecture cleanup.
2. Text expansion optimization and concurrency hardening.
3. Duplicate configuration manager unification.
4. Bounded hotkey action dispatch strategy.
5. Reminder scheduler cadence optimization.
6. Naming and consistency improvements (Keyboard hook file naming).
7. Exception handling and logging reliability improvements.
8. Path validation and safety hardening.
9. Documentation and readability improvements.
10. Runtime hotkey reference window feature.
11. Manual verification workflow (instead of automated tests).

Excluded by decision:
1. Automated test project creation.

## 3) Phase Plan

## Phase 0 - Planning Baseline And Governance
Timeline: 2026-03-23 to 2026-03-25
Goal: Lock execution model, sequencing, and decision process.

Tasks:
1. Define implementation order and risk matrix for all approved improvements.
   - Description: Create priority matrix (impact, risk, effort, dependency).
   - Clarification checkpoint: Confirm whether maintainability or runtime performance is the primary tie-breaker when priorities conflict.
   - Target completion: 2026-03-23.

2. Publish branch strategy and commit convention for roadmap execution.
   - Description: Define branch naming, commit prefixes, and rollback strategy.
   - Clarification checkpoint: Confirm whether feature branches should be per phase or per task group.
   - Target completion: 2026-03-24.

3. Establish manual verification checklist template for all future phases.
   - Description: Create a repeatable checklist for hotkeys, reminders, text expansion, tray actions, and safety regressions.
   - Clarification checkpoint: Confirm minimum acceptable manual validation depth for each merged change.
   - Target completion: 2026-03-25.

## Phase 1 - Safety, Correctness, And Immediate Reliability
Timeline: 2026-03-26 to 2026-04-04
Goal: Remove high-risk reliability issues before structural refactors.

Tasks:
1. Rename KayboardHook artifacts to KeyboardHook consistently.
   - Description: Rename file/class references and verify all project references compile cleanly.
   - Clarification checkpoint: Confirm whether namespace changes are allowed or filename-only correction is preferred.
   - Target completion: 2026-03-27.

2. Replace empty catch blocks with structured, explicit error handling.
   - Description: Audit AudioPlayer and Program hotkey actions; replace silent failures with typed catches and clear diagnostics.
   - Clarification checkpoint: Confirm desired logging verbosity for expected runtime failures (warning vs info).
   - Target completion: 2026-03-29.

3. Add strict path validation in PathLocator methods.
   - Description: Guard against rooted paths, traversal segments, and unsafe path resolution for config and SFX paths.
   - Clarification checkpoint: Confirm whether to throw exceptions or return safe fallback paths for invalid input.
   - Target completion: 2026-03-31.

4. Remove blocking watcher waits in hotkey config reload path.
   - Description: Replace synchronous delay waits with non-blocking debounce and safe reload scheduling.
   - Clarification checkpoint: Confirm acceptable debounce window and behavior when multiple rapid file changes occur.
   - Target completion: 2026-04-02.

5. Standardize error message format across reliability-sensitive components.
   - Description: Unify message structure (component, operation, result, reason, next action).
   - Clarification checkpoint: Confirm whether console-only output is sufficient or file logging should be included now.
   - Target completion: 2026-04-04.

## Phase 2 - Configuration Architecture Unification
Timeline: 2026-04-05 to 2026-04-16
Goal: Remove duplicated config manager logic and centralize configuration lifecycle behavior.

Tasks:
1. Design shared configuration lifecycle abstraction.
   - Description: Create a reusable base/service for load, normalize, save, watch, debounce, backup, and recover.
   - Clarification checkpoint: Confirm preferred style (base class template vs composition service strategy).
   - Target completion: 2026-04-07.

2. Integrate Main configuration manager into shared lifecycle.
   - Description: Migrate MainConfigurationManager to the new abstraction while preserving behavior.
   - Clarification checkpoint: Confirm backward compatibility requirements for existing config files.
   - Target completion: 2026-04-09.

3. Integrate Hotkey configuration manager into shared lifecycle.
   - Description: Migrate load/recover/watch logic and preserve current event semantics.
   - Clarification checkpoint: Confirm desired behavior on invalid config (revert previous vs default fallback).
   - Target completion: 2026-04-11.

4. Integrate Text Expansion and Reminder configuration managers.
   - Description: Migrate remaining managers and align debounce/recovery behavior.
   - Clarification checkpoint: Confirm whether all managers should share the same backup naming and retention rules.
   - Target completion: 2026-04-14.

5. Complete manual regression verification for all config flows.
   - Description: Validate create, reload, corrupt file recovery, and watcher behavior for each config type.
   - Clarification checkpoint: Confirm if corruption recovery should show user UI feedback in every manager.
   - Target completion: 2026-04-16.

## Phase 3 - Input Pipeline Performance And Concurrency
Timeline: 2026-04-17 to 2026-05-01
Goal: Improve keyboard-driven responsiveness and reduce unbounded async fan-out.

Tasks:
1. Optimize text expansion rule lookup path.
   - Description: Replace full linear per-key scans with indexed lookup by mode/trigger; keep behavior parity.
   - Clarification checkpoint: Confirm expected max rule count and whether trie complexity is justified now.
   - Target completion: 2026-04-21.

2. Harden expansion concurrency gate.
   - Description: Replace non-atomic expansion state transitions with safe synchronization to prevent overlap races.
   - Clarification checkpoint: Confirm if concurrent expansions should be dropped, queued, or merged.
   - Target completion: 2026-04-22.

3. Introduce bounded hotkey action dispatch model.
   - Description: Add dispatch strategies for immediate, serialized, throttled, and coalesced execution.
   - Clarification checkpoint: Confirm default dispatch policy for repeatable hotkeys and destructive actions.
   - Target completion: 2026-04-26.

4. Apply dispatch policies to existing high-frequency actions.
   - Description: Map scroll/move/repeat actions to throttled or coalesced dispatch where appropriate.
   - Clarification checkpoint: Confirm acceptable responsiveness target for repeated movement actions.
   - Target completion: 2026-04-28.

5. Validate runtime responsiveness under heavy usage.
   - Description: Run sustained manual scenarios (holding repeat keys, rapid typing, burst click, reminders).
   - Clarification checkpoint: Confirm pass criteria for responsiveness and no-stall guarantees.
   - Target completion: 2026-05-01.

## Phase 4 - Scheduler Cadence And Resource Efficiency
Timeline: 2026-05-02 to 2026-05-08
Goal: Improve reminder scheduler efficiency by reducing unnecessary wakeups.

Tasks:
1. Refactor reminder loop to next-due scheduling model.
   - Description: Compute nearest due reminder and sleep until next required wake-up, with cancellation support.
   - Clarification checkpoint: Confirm max fallback poll interval to handle clock changes and edge cases.
   - Target completion: 2026-05-04.

2. Update snooze and config-change triggers to force schedule recomputation.
   - Description: Ensure schedule is refreshed immediately when data changes.
   - Clarification checkpoint: Confirm expected behavior when reminders are edited while one is currently due.
   - Target completion: 2026-05-06.

3. Verify reminder behavior parity with existing functionality.
   - Description: Validate interval, daily, weekly, monthly, and snooze flows with manual checks.
   - Clarification checkpoint: Confirm acceptable tolerance for trigger timing drift.
   - Target completion: 2026-05-08.

## Phase 5 - Host Architecture, Readability, And Feature Delivery
Timeline: 2026-05-09 to 2026-05-22
Goal: Decompose host complexity, improve readability/docs, and add runtime hotkey reference.

Tasks:
1. Extract hotkey registrations from Program into grouped registry modules.
   - Description: Split by feature contexts (application control, window management, misc, file management).
   - Clarification checkpoint: Confirm whether static registry files or class-based registration services are preferred.
   - Target completion: 2026-05-12.

2. Reduce Program to bootstrap and lifecycle orchestration only.
   - Description: Move wiring and orchestration into dedicated coordinator classes where helpful.
   - Clarification checkpoint: Confirm if introducing lightweight dependency injection is allowed now or deferred.
   - Target completion: 2026-05-14.

3. Implement runtime hotkey reference window.
   - Description: Build a live reference view from registered hotkeys snapshot, grouped and searchable.
   - Clarification checkpoint: Confirm desired trigger entry points (tray, hotkey, or both) and UI density.
   - Target completion: 2026-05-17.

4. Document architecture and operation guides.
   - Description: Update README and HOTKEYS documentation with architecture, runtime behavior, and troubleshooting guidance.
   - Clarification checkpoint: Confirm required depth for internal design docs vs end-user docs.
   - Target completion: 2026-05-20.

5. Final manual validation and release readiness review.
   - Description: Execute full regression checklist, record unresolved risks, and plan follow-up backlog.
   - Clarification checkpoint: Confirm release go/no-go thresholds for known non-blocking issues.
   - Target completion: 2026-05-22.

## 4) Cross-Phase Review And Update Cadence

1. Daily: Update task status and risks at end of development day.
2. Twice weekly: Review dependencies and timeline drift; re-prioritize if blocked.
3. End of each phase: Publish phase summary, unresolved questions, and next phase readiness.
4. Any major requirement change: Re-baseline this roadmap within 24 hours.

## 5) Progress Tracking Format

Use this format in progress updates:
1. Completed today.
2. In progress now.
3. Blockers and clarification requests.
4. Timeline impact (none, minor, major).
5. Next 1 to 3 tasks.

## 6) Clarification Escalation Rule

If uncertainty remains after one clarification question:
1. Propose 2 to 3 implementation options.
2. State trade-offs for each option.
3. Ask for explicit decision before proceeding.

## 7) Initial Risk Register

1. Regression risk from config manager unification.
   - Mitigation: Migrate one manager at a time with manual checklist after each migration.

2. Behavior drift risk in text expansion after lookup optimization.
   - Mitigation: Keep parity checks for delimiter and case-sensitive behavior during rollout.

3. User experience drift from hotkey dispatch throttling.
   - Mitigation: Apply policy per hotkey category and tune with manual feedback sessions.

4. Schedule drift from host decomposition complexity.
   - Mitigation: Keep decomposition incremental and avoid large-bang redesign.

## 8) Milestone Summary

1. Milestone A (Safety Baseline): 2026-04-04.
2. Milestone B (Config Unification): 2026-04-16.
3. Milestone C (Input Performance): 2026-05-01.
4. Milestone D (Scheduler Optimization): 2026-05-08.
5. Milestone E (Host + Hotkey Reference + Docs): 2026-05-22.

---
This is the initial approved roadmap version and should be updated whenever scope, dependencies, or priorities change.

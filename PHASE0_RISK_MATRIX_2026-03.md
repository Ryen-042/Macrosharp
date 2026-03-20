# Phase 0 Risk Matrix

Created: 2026-03-20
Related roadmap: IMPLEMENTATION_ROADMAP_2026-03.md

## Scoring Model

Impact:
1. Low: localized inconvenience, no workflow breakage.
2. Medium: partial workflow disruption or visible reliability issue.
3. High: frequent failure, major workflow interruption, or widespread regression risk.

Likelihood:
1. Low: unlikely under normal usage.
2. Medium: plausible under common scenarios.
3. High: likely during standard execution paths.

Complexity:
1. Low: small scope, straightforward implementation.
2. Medium: moderate design and integration effort.
3. High: broad changes or multi-component coordination.

Priority score formula:
Priority = (Impact x Likelihood) + Complexity
Higher score indicates earlier sequencing.

Tie-breaker policy:
1. Prefer maintainability when two items have equal score and similar user impact.
2. Prefer performance first only when user-visible latency/stalls are currently reproducible.

## Risk And Priority Table

| ID | Work Item | Impact | Likelihood | Complexity | Priority Score | Priority Tier | Primary Risk | Mitigation |
|----|-----------|--------|------------|------------|----------------|---------------|--------------|------------|
| R1 | Replace silent catch blocks and standardize error handling | 3 | 3 | 1 | 10 | Critical | Hidden failures block troubleshooting | Use typed catches, consistent diagnostics, and clear fallback behavior |
| R2 | Remove blocking config watcher waits | 3 | 3 | 1 | 10 | Critical | Potential responsiveness stalls | Replace with non-blocking debounce and serialized reload gate |
| R3 | Path validation hardening in PathLocator | 3 | 2 | 1 | 7 | High | Path traversal misuse risk | Validate filename inputs and reject unsafe segments |
| R4 | Rename KayboardHook artifacts consistently | 2 | 3 | 1 | 7 | High | Confusing naming and maintenance mistakes | Perform coordinated rename and compile verification |
| R5 | Unify duplicated configuration manager lifecycle logic | 3 | 2 | 3 | 9 | High | Drift and inconsistent error/recovery behavior | Introduce shared lifecycle abstraction; migrate one manager at a time |
| R6 | Optimize text expansion lookup path | 3 | 2 | 2 | 8 | High | Typing latency growth with larger rule sets | Introduce indexed lookup and preserve behavior parity checks |
| R7 | Harden text expansion concurrency gate | 3 | 2 | 2 | 8 | High | Expansion overlap race conditions | Use atomic or lock-based single-expansion gate semantics |
| R8 | Introduce bounded hotkey action dispatch strategy | 3 | 2 | 3 | 9 | High | Task fan-out and unpredictable action ordering | Add dispatch policies: immediate, serialized, throttled, coalesced |
| R9 | Refactor reminder scheduler cadence to next-due model | 2 | 2 | 2 | 6 | Medium | Inefficient polling and scale overhead | Compute nearest due time and recompute on data changes |
| R10 | Decompose Program host orchestration and hotkey registration | 3 | 2 | 3 | 9 | High | High change risk in monolithic startup code | Extract registries and coordinator in incremental slices |
| R11 | Implement runtime hotkey reference window | 2 | 2 | 2 | 6 | Medium | Feature complexity and UX drift | Build from existing hotkey snapshot metadata and tray/hotkey entry |
| R12 | Documentation and readability updates | 2 | 2 | 1 | 5 | Medium | Knowledge silo and onboarding cost | Update docs in lockstep with delivered technical changes |

## Recommended Execution Sequence

1. R1, R2, R3, R4
2. R5
3. R6 and R7
4. R8
5. R9
6. R10
7. R11
8. R12

## Clarification Triggers For Risk Decisions

Always ask for clarification before implementation when any of the following is true:
1. Multiple acceptable fallback behaviors exist for error handling.
2. Dispatch policy selection can alter user-perceived key repeat responsiveness.
3. Config recovery behavior can either revert-to-last or reset-to-default and both are possible.
4. Scheduler timing precision tolerance is not explicitly defined.

## Review Cadence

1. Re-evaluate risk scores at phase boundary.
2. Re-score immediately if scope changes or if major regressions are observed.
3. Log all score changes in the execution tracker decision log.

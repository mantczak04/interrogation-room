---
name: unity-tdd
description: Develop Unity behavior test-first when requested or selected for a behavior change. Covers RoundEngine, Unity lifecycle, and Mirror integration.
---

# Unity TDD

Read [AGENTS.md](../../../AGENTS.md), `CONTEXT.md`, and the relevant architecture or ADR documents.

## Choose the seam

Use these approved seams without asking again:

- `RoundEngine.Handle` for state-changing intentions.
- `RoundEngine.ViewFor` for role- and phase-filtered information.
- `CaseAsset -> CaseDefinition` for authored Unity data entering the domain.
- `NetworkRoundCoordinator -> PlayerRoundView` for private delivery.
- UI rendering a received view and sending intentions.

Prefer existing seams. Choose routine test seams within scope; ask only when a new boundary changes approved architecture or behavior. Test public behavior, not private fields, serialization details, or internal collaborators.

## Choose the test level

- Pure Runda rules: Edit Mode test against `RoundEngine` with plain `CaseDefinition` data.
- `CaseAsset` conversion: Edit Mode unless Unity lifecycle behavior is essential.
- Scene, frame, physics, or lifecycle behavior: Play Mode only when Edit Mode cannot observe the behavior.
- Mirror behavior: first prove domain behavior in Edit Mode, then verify host/client integration on KCP.
- Steam changes: verify after KCP succeeds. Voice changes: test affected behavior with multiple clients, independently of domain rules.

## Run the loop

For one behavior at a time:

1. Write a test whose expected result comes from `CONTEXT.md`, the relevant ADR/spec, or a worked literal.
2. Run it and capture the intended failure.
3. Implement only enough to pass.
4. Re-run the narrow test.
5. Refactor only while all relevant tests stay green.

Avoid tautological assertions, tests coupled to internals, and bulk horizontal test writing. Complete when each new behavior was observed red then green and the appropriate broader verification from `$unity-change-verification` passes.

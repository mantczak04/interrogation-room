---
name: unity-diagnosing-bugs
description: Diagnose hard Unity bugs and performance regressions in this project. Use when gameplay, compilation, Play Mode, Mirror, ParrelSync, Steam, or runtime behavior is broken, throwing, flaky, or slow.
---

# Unity bug diagnosis

Read [AGENTS.md](../../../AGENTS.md) and the documents governing the affected behavior. Diagnosis does not authorize a fix unless the request includes one.

## Build the tightest practical loop

Start with available errors and evidence. Choose the smallest faithful reproduction; this list is not a mandatory sequence:

1. Pure Edit Mode test.
2. Unity compilation or script validation.
3. Bounded Console `Error` query with a specific filter.
4. Minimal Editor scenario through Unity MCP.
5. Play Mode test or scenario.
6. KCP host/client reproduction.
7. ParrelSync multi-client reproduction.
8. FizzySteamworks on two machines/accounts.

Escalate only when the current evidence cannot distinguish causes. Follow `AGENTS.md` for Editor access and performance measurements.

## Diagnose

1. Confirm the exact symptom from evidence; reproduce it when needed to distinguish causes.
2. Test plausible explanations and their predictions. Do not invent alternatives when direct evidence already establishes the cause.
3. Test one variable at a time. Prefer debugger/profiler evidence; tag temporary logs uniquely.
4. For performance, establish a measured baseline before proposing changes.
5. If authorized to fix, add red-green regression coverage for testable behavior; use direct validation for configuration or compilation fixes.
6. After a fix, verify the original symptom with `$unity-change-verification`. Remove temporary instrumentation.

Complete diagnosis when evidence supports the cause; otherwise report what remains unresolved. A fix requires confirmation that the original symptom is resolved.

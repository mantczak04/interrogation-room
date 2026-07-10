---
name: unity-diagnosing-bugs
description: Diagnose hard Unity bugs and performance regressions in this project. Use when gameplay, compilation, Play Mode, Mirror, ParrelSync, Steam, or runtime behavior is broken, throwing, flaky, or slow.
---

# Unity bug diagnosis

Read [AGENTS.md](../../../AGENTS.md) and the documents governing the affected behavior. Diagnosis does not authorize a fix unless the request includes one.

## Build the tightest practical loop

Try the first level that can reproduce the exact symptom:

1. Pure Edit Mode test.
2. Unity compilation or script validation.
3. Bounded Console `Error` query with a specific filter.
4. Minimal Editor scenario through Unity MCP.
5. Play Mode test or scenario.
6. KCP host/client reproduction.
7. ParrelSync multi-client reproduction.
8. FizzySteamworks on two machines/accounts.

Do not escalate while a lower level gives a faithful signal. Unity reloads may take minutes; optimize for the tightest practical deterministic loop, not an arbitrary duration. If required Editor interaction is unavailable through MCP, follow the mandatory stop rule in `AGENTS.md`.

## Diagnose

1. Reproduce the user's exact symptom and minimize the scenario until every remaining element is load-bearing.
2. Rank 3-5 falsifiable hypotheses. State the prediction for each; share the list, then continue unless user input is required.
3. Test one variable at a time. Prefer debugger/profiler evidence; tag temporary logs uniquely.
4. For performance, establish a measured baseline before proposing changes.
5. Identify the cause with evidence. If authorized to fix, turn the minimal repro into a regression test at the correct seam, observe red, apply the fix, and observe green.
6. Re-run the original scenario, remove temporary instrumentation, and use `$unity-change-verification`.

Complete diagnosis when the cause and disproved alternatives are evidence-backed. Complete a fix only when both minimal and original scenarios pass.

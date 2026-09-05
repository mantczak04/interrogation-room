---
name: unity-implement
description: Implement requested Unity code, tests, scenes, prefabs, or configuration from a user request, spec, or ticket.
---

# Unity implementation

1. Follow [AGENTS.md](../../../AGENTS.md) for context, dirty-worktree protection, and Editor operations.
2. Identify the requested behavior, affected boundary, and acceptance evidence. Flag product-rule conflicts before editing; a separate spec or ticket is not required for a clear request.
3. Implement the smallest complete change. Use `$unity-tdd` when requested or when a behavior change benefits from red-green evidence; do not impose it on mechanical or presentation-only edits.
4. Run `$unity-change-verification` for this task's changes and report remaining gaps.
5. Commit or stage only when the user explicitly requests it.

Complete when the accepted outcome is implemented, unrelated changes remain untouched, and verification evidence is reported.

---
name: unity-implement
description: Implement approved work in this Unity project from a spec or ticket. Use for code, tests, scenes, prefabs, or configuration changes that must preserve a dirty worktree and receive change-specific Unity verification.
---

# Unity implementation

1. Read [AGENTS.md](../../../AGENTS.md). Read `CONTEXT.md` for gameplay and `docs/architecture/MVP-ARCHITECTURE.md` for architecture, networking, or Runda work. Read only relevant ADRs. Treat `docs/design/OPEN-QUESTIONS.md` as unapproved.
2. Run `git status --short`. Record pre-existing changes and keep them outside the implementation scope.
3. State the requested outcome, affected seam, and acceptance evidence. Flag any conflict with approved product rules before editing.
4. Use `$unity-tdd` at approved or user-confirmed seams where behavior can be driven test-first. Implement the smallest complete increment; keep vendor and Unity-managed files under the protections in `AGENTS.md`.
5. For required Editor interaction, follow the Unity MCP preflight and bounded-query rules in `AGENTS.md`. If MCP lacks the operation, apply the mandatory stop rule and leave that part incomplete.
6. Run `$unity-change-verification` for every changed category. Review the final diff and run `git diff --check`.
7. Report changed files, behavior delivered, evidence run, and anything not verified. Commit or stage only when the user explicitly requests it.

Complete when the accepted outcome is implemented, unrelated changes remain untouched, and verification evidence is reported.

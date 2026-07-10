---
name: ask-interrogation-room
description: Route work in this Unity project to the smallest applicable repo skill. Use when the user asks which project workflow fits, starts mixed Unity work, or explicitly asks for the Interrogation Room skill router.
---

# Interrogation Room router

Read [AGENTS.md](../../../AGENTS.md), then select only the skills needed for the task:

- Implement approved work: `$unity-implement`; add `$unity-tdd` for test-first work.
- Diagnose a bug or regression: `$unity-diagnosing-bugs`.
- Review a diff against repo rules and a spec: `$unity-code-review`.
- Answer a design question with throwaway code: `$unity-prototype`.
- Synthesize an agreed change: `$unity-to-spec`; split it into ordered increments with `$unity-to-tickets`.
- Change Mirror Runda networking, authority, secrets, or private views: `$mirror-round-networking`.
- Select evidence before declaring a change complete: `$unity-change-verification`.
- Choose class roles or test seams: `$unity-script-roles` or `$unity-testability`.
- Plan assembly boundaries: `$unity-asmdef`.
- Define scene bootstrap and Inspector wiring: `$unity-scene-contracts`.
- Investigate a measured hotspot: `$unity-performance`.

Combine skills only when their procedures cover distinct parts of the request. State the selected skills and order. Complete routing when every requested outcome has one owning workflow and no unrelated skill is loaded.

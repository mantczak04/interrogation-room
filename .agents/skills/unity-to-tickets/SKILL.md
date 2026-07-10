---
name: unity-to-tickets
description: Break an approved Unity plan, spec, or conversation into dependency-ordered tickets. Use when the team needs small independently verifiable increments for Domain, CaseAsset, Mirror, UI, ParrelSync, or voice work.
---

# Unity tickets

1. Read the source plan/spec in full plus [AGENTS.md](../../../AGENTS.md). For gameplay, read `CONTEXT.md`; for slice ordering, read `docs/architecture/MVP-ARCHITECTURE.md`; preserve unresolved decisions from `docs/design/OPEN-QUESTIONS.md` as blocked or out of scope.
2. Draft the smallest independently verifiable increments. Respect the dependency path where applicable:

   `Domain -> CaseAsset -> Mirror/KCP -> UI -> ParrelSync -> voice -> Steam`

   A finished pure `RoundEngine` ticket with Edit Mode evidence is a valid increment. Do not force UI, networking, or every layer into it.
3. Give each ticket an outcome, acceptance evidence, blockers, relevant approved seam, and explicit exclusions. Keep it small enough for one fresh agent context.
4. Use tracer bullets within a layer or integration boundary when they produce real end-to-end evidence. Use expand-contract for wide mechanical refactors that cannot remain green as independent vertical slices.
5. Present the dependency graph for user approval. Publish only to a tracker or file destination the user selected; do not infer tracker semantics.
6. Name project workflows in execution notes: `$unity-implement`, `$unity-tdd`, `$mirror-round-networking`, and `$unity-change-verification` as applicable.

Complete when every ticket can be verified independently, every blocking edge is necessary, and no ticket silently approves an open product decision.

---
name: unity-to-spec
description: Synthesize the current conversation into a complete but minimal specification for this Unity project. Use after decisions are already discussed and the user wants a spec without another interview.
---

# Unity spec synthesis

Do not interview. Synthesize only established conversation and repository facts.

1. Read [AGENTS.md](../../../AGENTS.md), `CONTEXT.md`, and documents governing the feature. Read `docs/design/OPEN-QUESTIONS.md` and relevant ADRs when product scope is involved.
2. Separate evidence into `Approved`, `New decision in this conversation`, and `Unresolved`. Never promote a hypothesis or unresolved option into a requirement.
3. Prefer approved seams from `docs/architecture/MVP-ARCHITECTURE.md`. Record a new seam as a proposal requiring confirmation rather than silently establishing it.
4. Write a compact spec with:
   - Problem and desired outcome
   - Approved behavior and acceptance criteria
   - Implementation decisions and module boundaries
   - Testing decisions and verification level
   - Out of scope
   - Open decisions
5. Add user stories only when they clarify distinct player value; do not manufacture an exhaustive list.
6. Save or publish only to the destination and tracker the user selected. If none is configured, return the draft and ask where it should live rather than creating tracker configuration.

Complete when every requirement has a source, unresolved items are visibly separated, and the spec is sufficient to produce tickets without restating the repository.

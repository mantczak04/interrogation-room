---
name: unity-code-review
description: "Review Unity changes in this project against two separate axes: repository Standards and the originating Spec. Use for branches, pull requests, work-in-progress diffs, or reviews since a commit, tag, branch, or merge-base."
---

# Two-axis Unity review

1. Resolve the fixed point supplied by the user and review `git diff <fixed-point>...HEAD`. If absent and no safe convention is evident, ask for it. Include uncommitted changes only when requested.
2. Identify the spec from the user, referenced issue, or matching repo document. If none exists, report the Spec axis as unavailable.
3. Read [AGENTS.md](../../../AGENTS.md), `CONTEXT.md` for gameplay, relevant architecture/ADR documents, and `docs/design/OPEN-QUESTIONS.md` when scope may be unresolved.
4. Run both axes independently in parallel subagents when available. Otherwise run them sequentially with separate notes; never let one axis excuse the other.

## Standards axis

Check every changed hunk for documented repo violations and evidence gaps. In particular verify:

- `RoundEngine` remains free of Unity, Mirror, Steamworks, UI, and effects.
- `NetworkRoundCoordinator` remains the sole Mirror adapter and connection-to-`PlayerId` mapper for a Runda.
- Secrets stay host-owned and never enter global `SyncVar` state or another player's targeted payload.
- `PlayerRoundView` is filtered for the recipient and phase.
- `CaseAsset`, UI, voice, and dependency directions retain their approved roles.
- Editor mutations obey MCP rules; verification matches each change category.

Classify documented violations separately from maintainability judgments. Skip issues already guaranteed by tooling.

## Spec axis

Report missing or partial requirements, incorrect behavior, and unrequested scope. Quote the governing source. Treat hypotheses in `docs/design/OPEN-QUESTIONS.md` as out of scope unless the user made a new decision.

Report actionable findings first with file and tight line range, then `Standards` and `Spec` summaries. If no findings exist, say so and list residual verification risks. Complete when every changed hunk is accounted for on both axes.

---
name: unity-change-verification
description: Select and run deterministic evidence for changes in this Unity project. Use after C# code, RoundEngine, Mirror, scene, prefab, UI, voice, package, or configuration work and before reporting completion.
---

# Unity change verification

Read [AGENTS.md](../../../AGENTS.md). Classify every changed file, then run the union of applicable checks:

| Change category | Required evidence |
| --- | --- |
| Pure `RoundEngine` or domain | Relevant Edit Mode tests through public interfaces |
| Any C# | Unity compilation/script validation, then bounded Console `Error` query |
| `CaseAsset` conversion | Edit Mode conversion test; inspect authored asset only if changed |
| Unity lifecycle, scene interaction, physics | Narrow Play Mode evidence only when Edit Mode is insufficient |
| Mirror messages/coordinator | Domain tests, then local KCP host/client |
| Multi-client state | KCP first, then ParrelSync |
| Steam transport | KCP success first, then two machines/accounts |
| Voice/acoustics | Independent spike with at least two clients; keep separate from `RoundEngine` |
| Scene/prefab/Inspector | MCP hierarchy/component verification and save the correct scene/asset |
| Documentation/skills only | Targeted validator or link check; no Unity run by default |

Before any Editor operation, perform the MCP preflight from `AGENTS.md`. If MCP lacks a required operation, apply its mandatory stop rule; do not substitute raw YAML or OS automation.

Always:

1. Run the narrowest evidence first and escalate only when the change category requires it.
2. Inspect the final diff for unrelated or Unity-generated edits.
3. Run `git diff --check`.
4. Report each check as `passed`, `failed`, or `not run`, with the exact reason for every `not run`.

Complete when every changed category maps to evidence and failures or omissions are visible rather than implied away.

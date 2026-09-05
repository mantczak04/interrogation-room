---
name: unity-change-verification
description: Select and run deterministic evidence for changes in this Unity project. Use after C# code, RoundEngine, Mirror, scene, prefab, UI, voice, package, or configuration work and before reporting completion.
---

# Unity change verification

Follow [AGENTS.md](../../../AGENTS.md) for Editor operations and final checks. Classify this task's changes, excluding untouched local/upstream work. Reuse passing evidence unless changes or failures invalidate it.

| Change category | Required evidence |
| --- | --- |
| Pure `RoundEngine` or domain | Relevant Edit Mode tests through public interfaces |
| Any C# | Unity compilation/script validation, then bounded Console `Error` query |
| `CaseAsset` conversion | Edit Mode conversion test; inspect authored asset only if changed |
| Unity lifecycle, scene interaction, physics | Narrow Play Mode evidence only when Edit Mode is insufficient |
| Mirror messages/coordinator | Relevant rule/view-filter tests, then local KCP host/client |
| Multi-client state | KCP first, then ParrelSync |
| Steam transport | KCP success first, then two machines/accounts |
| Pure voice logic | Relevant Edit Mode tests |
| Voice runtime/acoustics | Affected behavior with at least two clients; no new spike required |
| Scene/prefab/Inspector | MCP hierarchy/component verification and save the correct scene/asset |
| Documentation/skills only | Targeted validator or link check; no Unity run by default |

Run the applicable rows, starting with the narrowest check. Complete when every affected category has evidence or an explicit failed/not-run result with its reason. Unavailable checks remain verification gaps.

# Claude Code Instructions

@AGENTS.md

Read and follow [AGENTS.md](./AGENTS.md) before starting any work. `AGENTS.md` is the repository-wide source of truth.

## CRITICAL: Stop Instead of Working Around Unity MCP

For any operation that requires interaction with Unity Editor, first use Unity MCP.

If Unity MCP does not expose the capability required to complete that Editor operation:

1. **STOP THE UNITY-EDITOR PART OF THE TASK IMMEDIATELY.**
2. **DO NOT improvise, experiment with, or attempt any fallback yourself.**
3. **DO NOT edit raw Unity YAML, drive Unity windows from shell scripts, simulate mouse or keyboard input, modify Unity caches, or use any OS/system-level workaround.**
4. Tell the user exactly what Unity MCP cannot do and what remains unfinished.
5. Give the user a complete, copy-ready prompt for a new Codex task that uses the `computer-use:computer-use` skill.
6. Do not continue making dependent Unity changes after this stop condition is reached.

This stop rule is mandatory. Finishing less work and handing off a precise Computer Use prompt is the correct outcome. A clever workaround is not an acceptable outcome.

Normal source-code and documentation edits through `apply_patch` are still allowed; this rule applies specifically to work that requires the Unity Editor UI.

Use this handoff template:

```text
Use the `computer-use:computer-use` skill to complete this task through the visible Unity Editor interface.

Project: C:\Users\Piotr\Documents\Unity projects\interrogation-room
Scene/object/asset: <exact target>
Goal: <precise expected final state>

Reason for handoff: Unity MCP cannot perform <specific missing capability>.

Do not edit raw Unity YAML. Do not modify unrelated files or revert existing user changes. Use only the visible Unity Editor interface for the blocked operation.

After completing the operation:
1. save the correct scene or asset,
2. inspect Unity Console for Errors,
3. visually verify <specific expected result>,
4. report exactly what was changed and any remaining issue.
```

---
name: unity-prototype
description: Build a throwaway prototype to answer a Unity design question in this project. Use to test a Runda state model, gameplay interaction, or tentative Unity UI without treating the prototype as production scope.
---

# Unity prototype

1. Write one decision question and the observable result that would answer it. Check `docs/design/OPEN-QUESTIONS.md` when the experiment touches unresolved product behavior.
2. Choose the smallest branch:
   - Pure logic: use a harness outside `Assets/` or a clearly temporary Edit Mode test against plain C#.
   - Unity UI or scene interaction: use a clearly temporary scene or objects created through Unity MCP.
3. Keep state in memory, expose relevant state after each action, and add only enough handling to run the experiment.
4. Follow [AGENTS.md](../../../AGENTS.md) for Editor preflight and mutation. If MCP cannot perform the required operation, stop that branch; do not edit raw Unity YAML or use OS automation.
5. Run the experiment and record what was learned, including inconclusive results.
6. Ask whether to absorb the validated decision into production work or remove the prototype. Capture a durable decision in an existing appropriate spec/ADR only when authorized, then delete or absorb the prototype.

Complete when the question has evidence and no ambiguous prototype remains presented as production code.

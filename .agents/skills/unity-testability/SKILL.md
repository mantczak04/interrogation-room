---
name: unity-testability
description: Improve testability and choose test levels for Unity code in this project. Use when extracting logic from MonoBehaviour, selecting Edit Mode versus Play Mode coverage, or evaluating whether a new seam is justified.
---

# Unity testability

Read [AGENTS.md](../../../AGENTS.md) and the governing architecture. Classify each behavior:

1. Can it run without `GameObject`, `Transform`, Unity time, scene state, or Mirror? Move the rule to plain C# and cover it in Edit Mode.
2. Does it convert authored Unity data into immutable domain data? Keep the adapter thin and test the conversion in Edit Mode.
3. Does it depend on callbacks, frames, physics, or scene composition? Keep only that bridge Unity-facing; use Play Mode when the lifecycle itself is the behavior.
4. Does it cross Mirror? Prove rules and view filtering first, then add the narrow KCP integration evidence.

Prefer existing approved seams. Introduce an interface only when it isolates a real effect or unstable dependency; confirm a new seam with the user. Inject configuration and clocks/randomness when determinism is required, but avoid abstractions that only mirror one concrete class.

Output logic to extract, logic to retain in Unity, chosen seam, Edit Mode cases, Play Mode/integration cases, and unnecessary test levels. Complete when each behavior has the lowest faithful test level and no domain test requires a scene.

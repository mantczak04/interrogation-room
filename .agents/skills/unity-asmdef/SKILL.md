---
name: unity-asmdef
description: Plan or review Unity assembly definitions in this project. Use when separating Domain, runtime, editor, or test code; untangling dependencies; preventing cycles; or improving compile boundaries.
---

# Unity assembly definitions

Read [AGENTS.md](../../../AGENTS.md) and `docs/architecture/MVP-ARCHITECTURE.md`. Start from the approved direction:

```text
Domain <- Runtime adapters/UI/Voice <- Play Mode integration tests
  ^
  └─ Edit Mode domain tests
```

Keep `Domain` independent of Unity, Mirror, Steamworks, UI, and voice SDKs such as Vivox. Runtime may reference Domain plus the packages it adapts. Editor code stays outside player assemblies; tests reference only the assemblies and framework they exercise.

Before proposing changes, inspect existing `*.asmdef` files and package references with targeted search. Recommend a split only when it enforces a meaningful boundary, test isolation, or compile benefit. Prefer a few directional assemblies over one assembly per folder; reject cycles and shared dumping grounds.

Report current graph, proposed graph, references added/removed, migration order, and risks. If implementation requires creating or changing Unity-managed assets, use Unity MCP and its stop rule. Complete when the graph is acyclic and every reference has an architectural reason.

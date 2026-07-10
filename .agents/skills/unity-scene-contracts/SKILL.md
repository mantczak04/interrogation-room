---
name: unity-scene-contracts
description: Define or review explicit Unity scene composition contracts in this project. Use when a scene needs required roots, components, bootstrap order, Inspector references, runtime-spawned objects, or early validation.
---

# Unity scene contracts

Read [AGENTS.md](../../../AGENTS.md) and relevant architecture. Produce one contract table:

| Object/root | Required component | Created by | References | Lifetime | Validation |
| --- | --- | --- | --- | --- | --- |

Then state:

1. Bootstrap sequence and authority owner.
2. References assigned in Inspector versus resolved at runtime.
3. Objects spawned by Mirror versus present in the scene.
4. Earliest deterministic validation for each assumption.

Prefer explicit wiring and small bootstrap components over chains of `Find`, hidden singleton discovery, or order-dependent `Start` calls. Keep Runda rules out of scene objects and preserve the `NetworkRoundCoordinator` boundary.

For inspection or mutation, perform Unity MCP preflight and use bounded hierarchy/component queries. If MCP cannot perform the operation, apply the mandatory stop rule; never edit scene or prefab YAML. Complete when every required dependency has an owner, assignment time, and failure signal.

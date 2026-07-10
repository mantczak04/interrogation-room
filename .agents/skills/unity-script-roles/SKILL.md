---
name: unity-script-roles
description: Assign explicit roles to Unity C# types in this project. Use before creating or splitting gameplay scripts, or when choosing among MonoBehaviour, ScriptableObject, plain C#, presenter, state object, and bootstrap code.
---

# Unity script roles

Read [AGENTS.md](../../../AGENTS.md) and `docs/architecture/MVP-ARCHITECTURE.md` when the type touches a named module.

For each proposed type, report:

| Type | Role | Responsibility | Dependencies | Lifecycle/owner | Why |
| --- | --- | --- | --- | --- | --- |

Choose from these roles:

- Plain C# domain/service/state for rules that do not need Unity lifecycle or scene state.
- `MonoBehaviour` bridge for Unity callbacks, scene references, and effects.
- `ScriptableObject` for authored configuration such as `CaseAsset`, not mutable runtime Runda state.
- Presenter/controller for translating views and intentions without resolving rules.
- Bootstrap/installer for explicit composition only.

Keep responsibilities and ownership singular. Prefer dependency injection or explicit Inspector references over static globals and runtime searches. For this project, preserve `RoundEngine`, `CaseAsset`, `NetworkRoundCoordinator`, `RoundPresenter`, and `VoiceRuntime` roles already approved in architecture.

Complete when every type has one owner, one lifecycle, an allowed dependency direction, and a reason it needs Unity or does not.

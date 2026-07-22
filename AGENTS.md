# Agent Instructions

These instructions apply to the entire repository. This is a Unity 6 game project. Protect gameplay secrets, the user's existing changes, and the context/token budget.

Project design documents are written in Polish. Preserve the canonical Polish domain terms defined in `CONTEXT.md`, such as `Runda`, `Detektyw`, `Winny`, `Niewinny`, `Alibi`, and `Egzekucja`.

## Agent skills

Start project workflows with [ask-interrogation-room](./.agents/skills/ask-interrogation-room/SKILL.md). Repository skills take precedence over similarly named global skills for work in this project; `AGENTS.md` remains the policy source of truth.

### OpenCode compatibility

- At the start of every OpenCode session, load the global `i-have-adhd` skill and use its action-first, numbered, low-noise response format throughout the session.
- OpenCode discovers the project workflows under `.agents/skills/`; do not duplicate them under `.opencode/skills/`.
- The project-level `opencode.json` owns the OpenCode connection to `unityMCP` at `http://127.0.0.1:8080/mcp`.
- If the ADHD skill cannot be loaded, continue with the same response constraints and report the missing skill briefly.

## Start Here

Before changing code, read only the documents required for the current task:

1. [CONTEXT.md](./CONTEXT.md) — canonical language and approved domain rules. Read it for every gameplay change.
2. [MVP-ARCHITECTURE.md](./docs/architecture/MVP-ARCHITECTURE.md) — first vertical-slice scope, modules, seams, and implementation order. Read it for architecture, networking, or Round changes.
3. [OPEN-QUESTIONS.md](./docs/design/OPEN-QUESTIONS.md) — deferred decisions. Do not implement them as approved features without a new user decision.
4. [proximity-voice-tools.md](./docs/research/proximity-voice-tools.md) — Dissonance/Vivox/Photon/Steam Voice research. Read it only for voice-chat or acoustics work.
5. [STEAM-NETWORKING.md](./docs/architecture/STEAM-NETWORKING.md) — implemented Steam multiplayer stack (Steamworks.NET, FizzySteamworks, `SteamLobby`, KCP fallback). Read it only for Steam, lobby, or transport work.
6. [docs/adr](./docs/adr/) — durable decision rationale. Read only ADRs relevant to the current task; do not load the entire directory by default.

## Approved Product Rules

- Round composition: 3–8 players, primarily balanced for 5; exactly 1 `Detektyw`, 1 `Winny`, and 1–6 `Niewinny` players.
- The `Winny` committed a public absurd `Przestępstwo` and receives the true `Alibi` with selected facts hidden.
- `Niewinny` players see the complete `Alibi`; the `Detektyw` never sees it.
- Suspects see the `Alibi` only during `Przygotowanie`. They cannot reopen it afterward.
- A `Runda` is continuous and free-roaming, with no formal interrogation turns.
- The `Detektyw` has one shared `Limit Rundy` and exactly one `Egzekucja`.
- Executing the `Winny` gives the `Detektyw` a win. Executing a `Niewinny`, or failing to execute before time expires, is a loss.
- The `Detektyw` starts the Round with a pistol that suspects cannot take or use. Misses do not consume the `Egzekucja`; the first hit on a living suspect does.
- `Niewinny` players have individual outcomes and win only by completing exactly one `Prywatny Cel` plus achieving `Przetrwanie`.
- An `Osobista Sprawa` is the default `Prywatny Cel`. A `Sekretny Cel` replaces it and requires `Wrobienie`, the designated `Niewinny`'s elimination, and the owner's survival.
- `Sekretny Cel` is disabled for three or four players. For five to eight players, one is enabled by default and the host may disable it in the lobby.
- The `Winny` may combine case-authored `Trop do Alibi` clues with preparation of a visible final `Ucieczka`; clues support testimony without restoring every hidden fact. The `Winny` wins by avoiding a correct Execution or completing the Escape.
- Suspicious actions are readable but motives remain ambiguous. Loud Incidents report immediately; quiet Incidents enter the Detective's private registry only after personal discovery.
- `Bunt` is an emergent alignment of individual interests after private goals are completed. It has no phrase, signal, button, dedicated action, or separate Round phase.
- Crimes and alibis use hand-authored modules; do not generate runtime case content with AI.
- Voice is always spatial. Privacy comes from distance, rooms, and doors, not private voice channels.
- The final Detective Notes UI and Alibi presentation remain unresolved. Exact objective timings, content volume, and map expansion are playtest tuning, not approved fixed values.

If code or a request contradicts these rules, identify the conflict before changing the domain model.

## ADR Index by Topic

### Roles, objectives, and Round resolution

- [ADR-0001](./docs/adr/0001-one-hidden-guilty-suspect.md) — one hidden Guilty suspect.
- [ADR-0002](./docs/adr/0002-innocents-play-for-their-own-survival.md) — individual Innocent outcomes.
- [ADR-0003](./docs/adr/0003-one-execution-ends-the-round.md) — one Execution ends the Round.
- [ADR-0004](./docs/adr/0004-one-time-limit-for-the-whole-round.md) — one time limit for the whole Round.
- [ADR-0013](./docs/adr/0013-private-goals-and-emergent-rebellion.md) — mandatory private goals and emergent Rebellion.

### Flow and information

- [ADR-0005](./docs/adr/0005-continuous-free-roaming-rounds.md) — continuous free-roaming Rounds.
- [ADR-0006](./docs/adr/0006-guilty-receives-redacted-alibi.md) — redacted Alibi for the Guilty player.
- [ADR-0007](./docs/adr/0007-alibi-is-hidden-after-preparation.md) — Alibi disappears after Preparation.
- [ADR-0008](./docs/adr/0008-detective-reconstructs-alibi-from-testimony.md) — Detective reconstructs the Alibi from testimony.

### Voice, content, and architecture

- [ADR-0009](./docs/adr/0009-voice-privacy-comes-from-space.md) — voice privacy comes from space.
- [ADR-0010](./docs/adr/0010-authored-modular-case-content.md) — hand-authored modular cases.
- [ADR-0011](./docs/adr/0011-server-owns-secrets-and-exposes-private-views.md) — server-owned secrets and private player views.
- [ADR-0012](./docs/adr/0012-steam-lobby-with-runtime-transport-fallback.md) — Steam lobbies with runtime transport fallback to KCP.
- [ADR-0014](./docs/adr/0014-readable-actions-ambiguous-motives.md) — readable actions with ambiguous motives, incidents, and Escape.

## Files and Directories: Do Not Read or Edit

Never scan, recursively open, or edit:

```text
Library/
Temp/
Obj/
Build/
Builds/
Logs/
UserSettings/
MemoryCaptures/
.git/
.idea/
.vscode/
```

These contain generated Unity data, builds, logs, caches, or tool state. If diagnostics absolutely require a log, use a precise filter and a small output limit. Never load an entire `Editor.log`, `Player.log`, or file under `Logs/`.

Do not read binary or media files as text (`*.png`, `*.jpg`, `*.fbx`, `*.wav`, `*.mp3`, `*.webm`, `*.dll`). Inspect a specific asset only when the task requires it.

## Third-Party Code and Unity-Managed Files

Do not edit vendor code unless the user explicitly requests a fork or vendor patch:

```text
Assets/Mirror/
Assets/Plugins/
Assets/TextMesh Pro/
Assets/Tutorials/
Packages/com.mirror.steamworks.net/
```

A targeted read of one vendor file is allowed when verifying an integration. Do not index or summarize an entire vendor tree.

- Do not edit `Packages/packages-lock.json` manually; Unity Package Manager owns it.
- Do not edit `*.meta` files manually. Let Unity create them and move them with their assets.
- Do not modify raw YAML in `*.unity`, `*.prefab`, or `*.asset` when Unity MCP can perform the operation safely.
- Read or change `ProjectSettings/` only when required by the task; prefer the corresponding Unity MCP operation.
- Never overwrite, revert, or clean unrelated user changes. Check `git status --short` before editing and ignore unrelated files.

## Efficient Repository Search

- Use `rg` and `rg --files` with generated directories excluded. Do not recursively enumerate the repository root to search for code.
- Start with `Assets/Scripts/`, `CONTEXT.md`, and the relevant document under `docs/`.
- Read `Packages/manifest.json` or individual `ProjectSettings/` files only for dependency or Unity-configuration work.
- Inspect large scenes, prefabs, and ScriptableObjects through Unity MCP or a targeted `rg -n` query using a type, object name, or GUID. Never dump an entire file.
- Do not generate asset previews or screenshots unless visual inspection is required.

Safe search examples:

```powershell
rg -n "RoundEngine|NetworkRoundCoordinator" Assets/Scripts docs CONTEXT.md
rg --files Assets/Scripts docs -g '*.cs' -g '*.md'
```

## Unity MCP Rules

Before the first Unity MCP operation:

1. Read `mcpforunity://custom-tools`.
2. Read `mcpforunity://instances`; if more than one instance is running, select the correct active instance.
3. Check editor state and the active scene before any mutation.

Bound every query:

- `manage_scene(get_hierarchy)`: use pagination, initially no more than 50 objects.
- `manage_gameobject(get_components)`: start with `include_properties=false` and 10–25 entries; request properties only for the specific object, 3–10 entries at a time.
- `manage_asset(search)`: 25–50 results per page and `generate_preview=false`.
- Console: query `Error` first, then `Warning` only if needed; use a small limit and a message filter. Never fetch full console history.
- Use `batch_execute` for repetitive mutations.
- After script changes, wait for compilation and inspect compiler errors only before continuing.
- Do not enter Play Mode or run a build when Edit Mode tests or script validation are sufficient.

### Mandatory Stop Rule When Unity MCP Cannot Perform an Editor Operation

This rule applies to tasks that require Unity Editor interaction, including hierarchy, Inspector, components, scenes, prefabs, asset import, package windows, or Play Mode. It does not prohibit normal source-code or documentation edits through `apply_patch`.

If the required Editor operation cannot be performed with the available Unity MCP tools:

1. **Stop that part of the task immediately.**
2. **Do not attempt a workaround** through raw scene/prefab/asset YAML, shell-driven window automation, scripted keyboard or mouse input, Unity cache modification, or any other OS/system-level hack.
3. State exactly which Unity MCP capability is missing and what remains incomplete.
4. Return a copy-ready prompt for a separate Codex task that explicitly uses the `computer-use:computer-use` skill.
5. The prompt must include the absolute project path, exact scene/object/asset, desired final state, prohibition on unrelated changes, and visual plus Console verification steps.
6. Do not invoke Computer Use yourself unless the user explicitly asks for it in the current task.

Fallback prompt template:

```text
Use the `computer-use:computer-use` skill to perform the following operation in Unity Editor.

Project: C:\Users\Piotr\Documents\Unity projects\interrogation-room
Scene/object/asset: <exact target>
Goal: <expected final state>

Unity MCP cannot perform: <specific missing capability>.
Make the change only through the visible Unity Editor interface. Do not edit raw YAML, do not modify unrelated files, and do not revert existing user changes.

After the change:
1. save the correct scene or asset,
2. check Console for Errors,
3. visually verify <specific expected result>,
4. report the actions taken and any remaining problems.
```

## MVP Architecture Constraints

- Round rules belong to the pure `RoundEngine` module; it must not depend on Unity, Mirror, Steamworks, or UI.
- `NetworkRoundCoordinator` is the single Mirror adapter for a Round and the only place that maps connections to `PlayerId`.
- The host owns roles, complete Alibi data, hidden facts, all `Prywatny Cel` assignments and progress, Alibi clues, Incident authors, and Escape progress.
- Never synchronize secrets through global `SyncVar` fields. A client receives only its own `PlayerRoundView` through a targeted message.
- `CaseAsset` is for authoring; immutable `CaseDefinition` data enters the domain.
- UI renders a received view and sends intentions; it does not resolve rules.
- Voice is independent of `RoundEngine`. The recommended first spike is Dissonance over Mirror/FizzySteamworks with custom door attenuation.
- Use the current KCP transport and ParrelSync for local development. Test FizzySteamworks with two machines/accounts.

## Verification

- Cover `RoundEngine` logic with Edit Mode tests through its public interface.
- After C# changes, verify Unity compilation and Console errors.
- After networking changes, run a local host + client test; test Steam only after KCP succeeds.
- After scene changes, verify the hierarchy and save the active scene through Unity MCP.
- Before finishing, run `git diff --check` and report any tests that could not be run.

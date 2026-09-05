# Agent instructions

This Unity 6 repository uses Polish design documents and canonical terms from `CONTEXT.md`.

## Scope

- Complete the smallest change requested. Diagnosis, review, or explanation alone does not authorize implementation or unrelated refactoring.
- Before editing, run `git status --short`. Preserve existing staged, unstaged, and untracked work; never overwrite, revert, or clean unrelated changes.
- Resolve routine choices within the authorized scope. Ask only for missing decisions that affect behavior or scope; continue independent work when one operation is blocked.

## Agent skills

Start with [ask-interrogation-room](./.agents/skills/ask-interrogation-room/SKILL.md). Load only applicable skills; reuse unchanged documents already read. User instructions take precedence over skill guidelines. This file owns repository policy; prefer repository skills over global equivalents. If a skill blocks authorized work, cite its exact instruction and explain the remaining blocker.

### OpenCode compatibility

- In OpenCode sessions, load global `i-have-adhd` and use action-first, numbered, low-noise responses. If unavailable, retain that format and report the missing skill.
- OpenCode uses `.agents/skills/` and the MCP connection in `opencode.json`; do not duplicate skills under `.opencode/skills/`.

## Task-specific context

Read only the references triggered by the task:

1. Gameplay: [CONTEXT.md](./CONTEXT.md), canonical terms and domain rules.
2. Architecture, networking, or `Runda`: [MVP-ARCHITECTURE.md](./docs/architecture/MVP-ARCHITECTURE.md), module boundaries and slice scope.
3. Unresolved product decisions: [OPEN-QUESTIONS.md](./docs/design/OPEN-QUESTIONS.md). Deferred features require a user decision.
4. Voice or acoustics: [glos-przestrzenny.md](./docs/design/mechanics/glos-przestrzenny.md), approved Vivox behavior. Use [proximity-voice-tools.md](./docs/research/proximity-voice-tools.md) only for historical research; Dissonance is superseded.
5. Steam, lobby, or transport: [STEAM-NETWORKING.md](./docs/architecture/STEAM-NETWORKING.md).
6. Decision rationale: select relevant files from [docs/adr](./docs/adr/); do not load the whole directory.

## Approved product rules

Preserve these constraints. Flag conflicts before changing the domain model; an explicit user decision may supersede an existing rule. Research and proposals are not approved features.

- Round composition: 3–8 players, primarily balanced for 5; exactly 1 `Detektyw`, 1 `Winny`, and 1–6 `Niewinny` players.
- The public absurd `Przestępstwo` was committed by the `Winny`. During `Przygotowanie`, `Niewinny` players see the complete `Alibi`, the `Winny` receives it with selected facts hidden, and the `Detektyw` never sees it. Suspects cannot reopen it afterward.
- A `Runda` is continuous and free-roaming, with no formal interrogation turns.
- The `Detektyw` has one shared `Limit Rundy` and one `Egzekucja`. The starting pistol cannot be taken or used by suspects. Misses do not consume the `Egzekucja`; the first hit on a living suspect ends the `Runda`. Hitting the `Winny` wins; hitting a `Niewinny` or timing out loses.
- `Niewinny` players have individual outcomes and win only by completing exactly one `Prywatny Cel` plus achieving `Przetrwanie`.
- An `Osobista Sprawa` is the default `Prywatny Cel`. A `Sekretny Cel` replaces it and requires `Wrobienie`, the designated `Niewinny`'s elimination, and the owner's survival.
- `Sekretny Cel` is disabled for three or four players. For five to eight players, one is enabled by default and the host may disable it in the lobby.
- The `Winny` may combine case-authored `Trop do Alibi` clues with preparation of a visible final `Ucieczka`; clues support testimony without restoring every hidden fact. The `Winny` wins by avoiding a correct Execution or completing the Escape.
- Suspicious actions are readable but motives remain ambiguous. Loud Incidents report immediately; quiet Incidents enter the Detective's private registry only after personal discovery.
- `Bunt` is an emergent alignment of individual interests after private goals are completed. It has no phrase, signal, button, dedicated action, or separate Round phase.
- Crimes and alibis use hand-authored modules; do not generate runtime case content with AI.
- The final Detective Notes UI and Alibi presentation remain unresolved. Exact objective timings, content volume, and map expansion are playtest tuning, not approved fixed values.

## Excluded files and directories

Never scan, recursively open, or edit:

```text
Library/
Temp/
Obj/
Build/
Builds/
Releases/
Logs/
UserSettings/
MemoryCaptures/
.git/
.idea/
.vscode/
models/
Assets/SourceFiles/
docs/map-polish/screenshots/
```

For essential log diagnostics only, use a precise filter and small output limit; never load a full `Editor.log`, `Player.log`, or file under `Logs/`.

Use Git commands to inspect repository status and history; the `.git/` exclusion prohibits direct file inspection and editing there.

Do not read binary or media files as text (`*.png`, `*.jpg`, `*.fbx`, `*.wav`, `*.mp3`, `*.webm`, `*.dll`). Inspect a specific asset only when the task requires it.

## Third-party code and Unity-managed files

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
- Modify scenes, prefabs, and serialized assets through Unity MCP. If MCP cannot perform the operation, follow the stop rule below; raw YAML editing is not a fallback.
- Read or change `ProjectSettings/` only when required by the task; prefer the corresponding Unity MCP operation.

## Efficient repository search

- Use `rg` and `rg --files` with generated directories excluded. Do not recursively enumerate the repository root to search for code.
- Start with `Assets/Scripts/`, `CONTEXT.md`, and the relevant document under `docs/`.
- Read `Packages/manifest.json` or individual `ProjectSettings/` files only for dependency or Unity-configuration work.
- Inspect large scenes, prefabs, and ScriptableObjects through Unity MCP or a targeted `rg -n` query using a type, object name, or GUID. Never dump an entire file.
- Do not generate asset previews or screenshots unless visual inspection is required.

## Unity MCP rules

Apply this section to Editor operations, including required verification. Source and documentation edits may use `apply_patch`.

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
- After script changes, wait for compilation to finish and check compiler errors before dependent Editor operations. Fix task-introduced errors before continuing; report pre-existing errors separately.

### Stop rule when Unity MCP cannot perform an Editor operation

If MCP cannot perform a required hierarchy, Inspector, component, scene, prefab, import, package-window, or Play Mode operation:

1. Stop that operation. Do not substitute raw YAML, shell/window automation, scripted input, cache modification, or other OS workarounds.
2. Report the missing capability and incomplete work. Supply a copy-ready prompt for a separate task using `computer-use:computer-use` through the visible Editor.
3. Include the absolute project path, exact target, desired state, no unrelated changes or reverts, no raw YAML, and steps to save, check Console Errors, visually verify the result, and report remaining problems.
4. Invoke Computer Use in this task only if the user explicitly requests it here.

## Code-authored Editor tooling

Scene and asset construction is scripted through `[MenuItem]` editor tools. When one of these covers the change, re-run it through Unity MCP `execute_menu_item` instead of editing the scene by hand:

- `Assets/Editor/MainMenuSetup.cs` — the obsolete uGUI builder is intentionally disabled. The production MainMenu uses UI Toolkit; do not rebuild or overwrite it until a replacement builder implements the current scene contract.
- `Assets/Editor/PtakuCharacterSetup.cs` — character prefab/rig setup menu items.
- `Assets/Scripts/Editor/ChairSeatBaker.cs` — bakes chair seat alignment data.
- `Assets/Scripts/Editor/Content/CaseAssetSync.cs`, `PersonalMatterAssetSync.cs` — sync authored case content into assets.

After running a builder, check the Console for errors and save the affected scene or asset.

## Architecture constraints

- Round rules belong to the pure `RoundEngine` module; it must not depend on Unity, Mirror, Steamworks, or UI.
- `NetworkRoundCoordinator` is the single Mirror adapter for a Round and the only place that maps connections to `PlayerId`.
- The host owns roles, complete Alibi data, hidden facts, all `Prywatny Cel` assignments and progress, Alibi clues, Incident authors, and Escape progress.
- Never synchronize secrets through global `SyncVar` fields. A client receives only its own `PlayerRoundView` through a targeted message.
- `CaseAsset` is for authoring; immutable `CaseDefinition` data enters the domain.
- UI renders a received view and sends intentions; it does not resolve rules.
- Vivox is independent of `RoundEngine`: runtime and occlusion in `Assets/Scripts/Voice/`, pure logic in `Assets/Scripts/Game/Voice/`. Voice is global in Lobby and spatial from `Przygotowanie` through `Finished`. Privacy comes from distance, rooms, and doors; there are no private voice channels.

## Verification

Use [unity-change-verification](./.agents/skills/unity-change-verification/SKILL.md) for implementation. Check this task's changes, not untouched local or upstream work. Documentation-only changes need diff, link, and instruction-consistency checks, without Unity. Run the narrowest sufficient checks; broaden or repeat only for new changes, failures, or unresolved concerns. Skip Play Mode and builds when Edit Mode or script validation suffices.

- Cover `RoundEngine` logic with Edit Mode tests through its public interface.
- Run tests through Unity MCP: `run_tests` with `EditMode`, then poll `get_test_job` to completion. Filter by the affected test assembly when only one area changed. Use Play Mode tests only when Edit Mode coverage is insufficient.
- After C# changes, verify Unity compilation and Console errors.
- After networking changes, test local KCP host + client with ParrelSync; test FizzySteamworks only after KCP succeeds, using two machines/accounts.
- After scene changes, verify the hierarchy and save the affected scene through Unity MCP.
- Before finishing, inspect the final diff and run `git diff --check` plus `git diff --cached --check` when staged changes exist. Report pre-existing failures separately.
- Report the result and checks as passed, failed, or not run, with reasons for omissions and remaining work. Distinguish pre-existing errors. A submitted test job is not a passing result.

### Performance and visual-regression evidence

- For each A/B sample, record scene, Play Mode, transport, connection counts, `Runda` phase, local-player presence, camera, and UI state before and after. Discard runs with changed preconditions.
- Warm variants equally. Frame-time claims require at least 60 unique frames per variant; report count, median, p95, and noise/outliers. Prefer attributed Profiler markers; never compare isolated frames or different gameplay states.
- Re-establish the scenario after compilation/domain reloads. MCP/Editor activity contaminates global GC/frame counters; claim no improvement from inconsistent or below-noise measurements.
- Preserve UI binding, initialization, and first render. Verify cold Play Mode startup and every affected transition, including hidden, visible, open, and close.
- Await completed screenshot rendering, confirm a non-empty file, and inspect it. Discard black, transitional, stale, or wrong-state captures. Pin random/animated content or report pixel comparison as invalid.
- Compare baseline and final Console errors. Minimum-spec performance claims require a standalone Player on representative hardware with matching scenario and quality settings; Editor profiling alone is insufficient.

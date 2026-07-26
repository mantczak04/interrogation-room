# Refactor Plan — Architecture and Cleanup

Plan prepared 2026-07-25 for execution by a coding agent. Read `AGENTS.md` first and obey all of its rules (Unity MCP stop rule, no manual `*.meta` edits, no raw YAML edits, bounded searches). Work in the order below; each stage must compile and pass EditMode tests before the next stage starts. Run tests through Unity MCP `run_tests` (EditMode) as described in `AGENTS.md` → Verification.

Already done in a previous session (do not redo): `serverSpeakingStates` pruning in `VivoxVoiceRuntime`, `SettingsMenu` discovery throttling, `RaycastNonAlloc` conversions in `PlayerInteractor`/`PlayerController`/`QuietIncidentDiscoveryProbe`, `RoundPresenter` timer-string gating, `/Releases/` gitignore, Dissonance→Vivox doc fixes.

## Stage 1 — `InterrogationRoom.Gameplay` assembly definition (low risk, high value)

Goal: move `Assets/Scripts/Gameplay/**` out of `Assembly-CSharp` into its own assembly.

1. Create `Assets/Scripts/Gameplay/InterrogationRoom.Gameplay.asmdef` referencing: `InterrogationRoom.Domain`, `InterrogationRoom.Game`, `Mirror`, and `Unity.InputSystem` if any script uses the new Input System. Files here already use `InterrogationRoom.Gameplay.*` namespaces, so this is mostly mechanical.
2. Expect missing-reference compile errors from scripts that reach into `Assembly-CSharp` types (`PlayerController`, `VivoxVoiceRuntime`, `SteamLobby`, UI scripts). Assembly-CSharp is compiled last and cannot be referenced, so any such dependency must be broken — either move the depended-on type into an assembly (see Stage 2) or invert the dependency with an interface/event owned by the Gameplay assembly. Do Stage 2 first for any type that blocks this, then return.
3. Update the test asmdef `InterrogationRoom.Game.Gameplay.EditModeTests` to reference the new assembly.
4. Verify: full compile clean, all EditMode suites green, then a Play Mode smoke test of one Round in the sandbox.

## Stage 2 — Root scripts into assemblies (`Voice`, `Steam`, `UI`)

Goal: empty the loose `Assets/Scripts/` root and give the remaining code compiler-enforced boundaries. These files mostly have **no namespace** — add namespaces as part of the move (class names stay the same, so scene references survive; moving files must be done together with their `.meta` files, or better, move them via Unity MCP `manage_asset` so Unity moves the meta itself).

Suggested grouping:

- `InterrogationRoom.Voice.Runtime` (new folder `Assets/Scripts/Voice/`): `VivoxVoiceRuntime.cs`, `VivoxVoiceOcclusion.cs`. References: `InterrogationRoom.Game` (for `GameSettings`, `VoiceSpeakingState`), `Mirror`, Vivox/Unity Services assemblies (`Unity.Services.Vivox`, `Unity.Services.Authentication`, `Unity.Services.Core`), `Unity.InputSystem`.
- `InterrogationRoom.Steam` (new folder `Assets/Scripts/Steam/`): `SteamLobby.cs`, `SteamManager.cs`. References: Mirror, FizzySteamworks/Steamworks.NET assemblies.
- `InterrogationRoom.UI.Runtime` (Assets/Scripts/UI/): `SettingsMenu.cs`, `MainMenuPresenter.cs`, `MainMenuManager.cs`, `MenuButtonHover.cs`, plus root presenters `LobbyCharacterPresenter.cs`, `LobbyDisplayNameProvider.cs`, `PlayerWorldNameplate.cs`, `CenteredNetworkManagerHUD.cs`.
- `PlayerController.cs` joins `InterrogationRoom.Gameplay` (or a `Player` assembly) — coordinate with Stage 3.

Rules: no assembly may reference `Assembly-CSharp`; cycles between the new assemblies must be broken with events/interfaces. If a scene or prefab loses a script reference after a move, STOP per `AGENTS.md` and report — do not edit scene YAML.

Verify after each assembly: compile, EditMode tests, then host+client KCP smoke test (voice connect, lobby join).

## Stage 3 — Split `PlayerController` (1,294 lines)

Extract cohesive components, keeping the public surface other scripts use (`PlayerInteractor`, `PlayerWeaponController`, `RoundPresenter` read it):

1. `PlayerCameraRig` — mouse-look, pitch, third-person orbit + obstacle distance, seated camera yaw.
2. `PlayerSeating` — chair enter/exit, seated pose alignment, seat state.
3. `PlayerAnimationDriver` — animator params, `OnAnimatorIK`, look-target smoothing.
4. `PlayerController` keeps: movement/gravity/jump, network identity plumbing, death.
5. Replace the global static `PlayerController.CursorReleased` / `SetCursorReleased` with a small injected service (e.g. `CursorState` owned by `PlayerInputGate`), because `PlayerInteractor` and `RoundPresenter` read the static today.

Add EditMode tests for any pure logic extracted along the way (e.g. camera obstacle-distance math, seat alignment math). Verify with a host+client run: movement, sitting, third-person toggle, death, cursor behavior in menus.

## Stage 4 — Slim `NetworkRoundCoordinator` (1,569 lines)

Keep it the single Mirror adapter (AGENTS.md constraint) but extract collaborators it owns:

1. `LobbyRosterTracker` — lobby profile/ready/character state (plain class, host-side).
2. `DeveloperScenarioPlanner` — developer-panel scenario planning.
3. Expose a read-only voice-roster view (list of `(netId, displayName)` for connected, non-simulated players) so `SettingsMenu` stops querying `NetworkClient.spawned` and diffing `PublicLobbyPlayers` itself (see Stage 5).

No behavior change; EditMode networking tests must stay green.

## Stage 5 — Extract `VoiceSettingsPresenter` from `SettingsMenu`

`SettingsMenu` currently owns mic-test lifecycle, per-participant volume/mute dispatch, roster signature diffing, and UI construction. Extract:

1. A pure, testable roster-diff helper (input: coordinator's voice-roster view; output: add/remove/update operations) — replaces `BuildParticipantSignature` string building.
2. A `VoiceSettingsPresenter` that binds the roster view + `VivoxVoiceRuntime` to the UI Toolkit elements; `SettingsMenu` keeps only sheet lifecycle (open/close/Esc/cursor callbacks).
3. EditMode tests for the diff helper.

## Stage 6 — Decision needed: dead lobby-channel machinery in `VivoxVoiceRuntime`

`ResolveWantsSpatialVoice()` hard-returns `true`, so `SwitchVoiceChannelAsync`, the `JoinGroupChannelAsync` lobby branch, and the `-lobby`/`-round` channel-name split are unreachable. Ask the owner which way to go, then implement:

- **Option A (wire it):** return `false` while the round phase is `Lobby` (coordinator exposes the phase), so lobby chat is global and Runda chat is spatial. Note ADR-0009 says voice privacy comes from space — a global lobby channel is compatible only outside the Runda.
- **Option B (delete it):** remove the switching machinery and `BuildModeChannelName`'s lobby branch until a real driver exists; keep the tests for what remains.

## Stage 7 — Test coverage additions

1. `MicrophoneTestPlayback` — extract the state transitions (Idle/Starting/Monitoring/NoInputDevice/Failed, startup timeout, resync) behind a seam that does not require `Microphone`, and cover them in EditMode tests.
2. `MicrophoneMonitorBuffer` — degenerate cases: `CalculateReadPosition` with `capacity <= 0`, `RequiresResync` with `capacity <= 1`.
3. `VivoxVoiceRuntime` — once moved into an assembly (Stage 2), unit-test `Get/SetParticipantVolumePercent`, `SetParticipantLocallyMuted`, and speaking-state publish logic (extract the pure parts if needed).
4. `MinigameRules.cs` and `EscapePlanDefinition.cs` — confirm existing suites exercise them; add dedicated EditMode tests if not.

## Stage 8 — Repo hygiene (needs owner confirmation before deleting)

- Delete or relocate: `models/` (20 MB FBX at repo root, outside Assets — unused by Unity), `malpa.jpg`, empty `Nowy folder`. Confirm with the owner first; these are tracked files.
- `Assets/SourceFiles/` (82 MB source art) — consider moving out of the repo or to Git LFS; owner decision.
- Add `docs/README.md` indexing `docs/design/mechanics/` and the other doc folders.
- Optional: `[Conditional("UNITY_EDITOR")]`-style logging wrapper (or a `VerboseLogging` flag) for the `Debug.Log` calls in `NetworkRoundCoordinator`, `VivoxVoiceRuntime`, and Steam scripts.

## Pre-commit checklist for the current branch (separate from the stages above)

- `Assets/Fonts/RoomLabels SDF.asset` — the working-tree diff wipes the TMP glyph table (likely dynamic-atlas repopulation). Verify room labels render in Play Mode before committing, or revert the file.
- `Assets/Scenes/MainMenu/*.png.meta` and `Assets/UI/Sprites/MainMenuBackground.jpg.meta` — import-settings drift (maxTextureSize 4096 + Android/iOS blocks). Keep only if intended.
- `Assets/Editor/MainMenuSetup.cs` background-texture GUID change — confirm intended.

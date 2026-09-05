# Station rebuild — 5 September 2026

This records the initial map rebuild. See [Realism follow-up](REALISM-PASS.md) for the later scanned assets, revised lighting, two-sided door fix, graphics settings and current verification results. Counts and screenshots below describe the earlier pass.

## Scope

`Assets/Scenes/Room.unity` now uses a Blender-authored station with ten rooms and a circulation loop around the central interrogation room. The room-volume floor area is approximately 634 m², compared with approximately 207 m² in the original four-room layout. These are summed interior room/corridor areas, not exterior bounding-box measurements.

The original round rules and authored tasks remain. Existing objectives were moved to readable props in the archive, evidence store, kitchen, workshop, common room and exits. This change adds space and furnishings, not new objective types.

The second interior pass replaces visible white placeholders, gives the interrogation room suitable seating, rearranges the kitchen against a wall, separates the office sofa and table, furnishes reception and briefing, and adds signs, blinds, radiators and noticeboards. Doorways, spawn positions, interaction sightlines and seat stand-up positions are validated using the player's collision envelope. The pistol now rests above its table surface.

Lighting uses baked ceiling fixtures and window light, a mixed interrogation light, room reflection probes and an expanded adaptive probe volume. The palette is neutral plaster, sage paint, timber and muted metal. The revised bake uses 20 texels/metre. The geometry/material work aims to keep the station inexpensive to render, but no frame-time improvement or minimum-spec performance is claimed.

## Authored assets and rebuilding

- `ArtSource/StationRebuild/StationRebuild.blend`: editable Blender source, with separate station, door and detail scenes.
- `tools/station-rebuild/build_station.py`: architecture, shelves and layout export.
- `tools/station-rebuild/build_door.py`: panelled interactive door export.
- `tools/station-rebuild/build_details.py`: fifteen reusable interior models.
- `Assets/Art/Environment/StationRebuild/`: Unity FBX imports, materials, copied sign font, lighting settings, volume profile and layout.

Run Blender scripts through Blender MCP with `__file__` set to the script path. The architecture generator replaces the active Blender scene's objects; run it only in the station source document or an empty document. Run architecture, door and details in that order when regenerating everything.

Unity authoring menus are under **Tools → Interrogation Room → Station Rebuild**. **Compose Room** is a one-time migration of the original scene and rejects an already composed scene. For the current scene:

1. After changing architecture FBX, run **Import architecture**, then **Refresh architecture**.
2. After changing DoorLeaf FBX, run **Update door visuals**.
3. After importing detail FBX files with generated lightmap UVs, run **Polish interiors**.
4. Run **Tools → Interrogation Room → Bake Chair Seats**, then **Finalize player access**.
5. Generate lighting, await completion, bake occlusion culling, save the scene/assets, then run **Validate traversal**.

**Align waiting benches**, **Space lounge furniture** and **Update wayfinding** are targeted authoring commands also included in **Polish interiors**. After moving seating, repeat the chair bake and access finalization before baking lighting.

Polish is repeatable but deliberately reapplies the authored furniture positions. Revisit this authoring script before running it over manual interior changes. All scene/asset construction is performed inside Unity; there is no serialized YAML editing.

## Scene contract

| Object/root | Required component | Created by | References | Lifetime | Validation |
| --- | --- | --- | --- | --- | --- |
| Map_Station/BlenderArchitecture | MeshRenderer, MeshFilter, static MeshCollider | StationRebuildSetup | Imported FBX meshes and station materials | Scene | Import checks, collision grid, visual inspection |
| Map_Station/Meble | Existing furniture; NetworkChairSeat where applicable | Original scene, repositioned by editor tools | Seat/stand anchors, colliders, original network identities | Scene | Chair bake, safe stand anchors, interaction checks |
| Map_Station/Drzwi | NetworkDoor, NetworkIdentity | Original doors plus authored copies | DoorLeaf, blocking collider, handle anchor, room IDs | Scene; spawned by Mirror | Portal/ID validation and live open/closed checks |
| Map_Station/RoomVolumes | RoomVolume, trigger BoxCollider | StationRebuildSetup | Ten room IDs and shared corridor ID | Scene | Room/portal graph and reachable occupancy cells |
| Map_Station/InteriorDetails | Renderers/colliders; two extra NetworkChairSeat objects | StationInteriorPolish | Detail FBX, materials, copied font | Scene | No overlaps at entries/spawns; seat and sightline checks |
| Map_Station/Lighting | Lights and ReflectionProbes | StationRebuildSetup, adjusted by polish | Baked lightmaps, per-room probes | Scene | Completed bake, Console and player-height captures |
| RoundPhysicalIntegration | RoundPhysicalActionBinder and existing interaction components | Existing scene | Original authored action IDs; new visual children and adjusted colliders/interaction points | Scene; identities spawned by Mirror | Bound-action count and interaction range/visibility |
| Existing NetworkManager / coordinator / player prefab | Existing Mirror and round components | Existing bootstrap | Original player prefab, coordinator and view wiring | Host/client session | Cold singleplayer startup and round transitions |

The existing network manager starts the session and spawns players and scene identities. `NetworkRoundCoordinator` remains the host authority for the round; no rules move into environment code. Scene authoring assigns door, seat, collider, font and model references before Play Mode. Existing runtime discovery and round binding remain unchanged. The old building shell is inactive under `Map_PreRebuild_Disabled`; the original complete scene remains recoverable from Git.

## Script roles

| Type | Role | Responsibility | Dependencies | Lifecycle/owner | Why |
| --- | --- | --- | --- | --- | --- |
| StationRebuildSetup | Editor bootstrap | Import and compose the station from authored layout | UnityEditor, Unity rendering, existing scene components | Explicit menu invocation | Construction must run before gameplay, never per frame |
| StationInteriorPolish | Editor bootstrap | Furnish rooms, fit presentation/colliders and assign safe stand anchors | UnityEditor, imported FBX, existing interaction components | Explicit menu invocation | Keeps spatial authoring out of runtime behavior |
| StationRebuildValidation | Editor verification service | Check floor routes, entries, interactions, seats and IDs | Unity physics and existing component public interfaces | Explicit validation invocation | Provides repeatable evidence for the authored scene |
| Nested Layout/Room/Door/Fixture/Window DTOs | Plain authoring data | Read the Blender-exported layout | Unity JSON conversion only | Owned by each editor invocation | No Unity lifecycle or round state |

All new C# lives under `Assets/Editor`. No runtime networking, voice, UI or domain source was changed. The player prefab's serialized `PlayerInteractor.serverViewHeight` changes from 0.7 m to 1.55 m to match the standing camera. The old ray origin could hit a neighbouring sofa cushion even when the player could see the target; all seats were checked through the real interaction command after this correction.

## Verification record

Singleplayer is the target of this pass. Live checks use the Room scene in Unity Editor Play Mode, a local KCP host with one player, an active developer Round and a local Detective. This is the project's solo development path, not a multiplayer test.

| Check | Result | Evidence / limit |
| --- | --- | --- |
| Cold startup and round transition | Passed | Local player spawned; Lobby → Preparation → Round; physical action binder reports 14 bound actions |
| Layout and interaction access | Passed | 8 clear spawns, 14 clear doorway approaches, 14 reachable room volumes, 58 interactions with reachable range and line of sight; 5,485 connected quarter-metre grid cells |
| Live seating | Passed | All 37 distinct seats accepted the player's interaction command; standing left the player unseated with a clear collision capsule |
| Live door traversal | Passed | All 14 doors opened through the player's interaction command, allowed a 2.60 m crossing, then blocked the return crossing when closed (movement stopped after 0.91 m) |
| Door / room regression fixture | Passed | `NetworkRoomsAndDoorsTests`: 8/8 Play Mode tests, no failures or skips; job `454a779b97044ff1802f2005a5bba237` |
| Compilation | Passed | No C# compiler errors after final source changes |
| Final Editor state | Passed | Room saved, not dirty, Play Mode stopped, no active bake; final Console error query returned zero entries |
| Final lighting / occlusion bake | Passed | Final furniture arrangement baked; 3 lightmaps; occlusion generation completed (28,912 bytes); assets and scene saved |
| Records-cabinet minigame lifecycle | Passed | Fresh solo Round: interaction command opened the panel and locked movement; cancellation closed it, released the reservation and restored movement |
| Final diff checks | Passed | `git diff --check` and `git diff --cached --check`; existing staged work preserved; original sign font has no new unstaged change |
| Multiplayer | Not run | Explicitly excluded by the user |
| Every objective completed end to end | Not run | This pass checks binding, spatial access, doors and seats; it does not establish every authored objective's completion behavior |
| Standalone / minimum-spec performance | Not run | No frame-time or hardware performance claim |

Detailed live seat and door results are in [singleplayer-checks.json](singleplayer-checks.json). The grid test treats doors as open and checks geometry conservatively with a 0.44 m radius; live seat checks use the actual 0.30 m player radius. The complete door pass preceded the camera-height correction; the final static line-of-sight checks and all-seat live pass include it. The archive door was also rechecked after that correction and the final bake: it opened via the player command, allowed a 2.60 m crossing and blocked the return when closed.

The solo session logged Vivox HTTP Timeout (10028) and a faulted voice state. Movement and local interactions remained functional. Voice connectivity was not repaired in this environment pass, and the observation is not classified as a baseline error. A transient session teardown also required a fresh Play Mode start; subsequent complete seating checks used a confirmed active host and unpaused Editor.

Intermediate images prefixed `audit-`, `polish-`, `first-bake-` and `second-bake-` record rejected or unfinished states. They are not final visual evidence. Captures are qualitative reviews; they are not a controlled pixel or frame-time A/B benchmark.

Final captures: [common room](completed-common.png), [central interrogation room](completed-interrogation.png), and [live records task](completed-singleplayer-task.png). The first two are positioned player-height cameras in Edit Mode; the task capture is the actual Main Camera and UI in a solo Round. Screenshot capture pauses Play Mode, so live checks resumed the Editor before collecting results. Play Mode was stopped at handoff; Computer Use is released.

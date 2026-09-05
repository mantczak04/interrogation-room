# Realism follow-up

The visual reference is Bodycam. Assets must be free. The target is detailed, believable surfaces and lighting while preserving readable gameplay. This pass does not claim parity with a shipped AAA game or a verified hardware target.

## Changes

- Correct the door interaction target so visibility rays hit the leaf from either face. The earlier one-sided pass missed this regression. The traversal validator now explicitly tests both approaches on every door.
- Replace flat station surface materials with six scanned texture sets. Convert Poly Haven ARM maps to Unity's metallic, occlusion and smoothness channel layout, use normal-map import settings and retain mipmaps.
- Prepare eight textured prop models through Blender. Bake static poses and remove decorative rigs; preserve source UVs. Fit replacement models into existing gameplay objects and retain their identities.
- Replace the two colored carryable cubes with recognizable textured objects, retaining the original item IDs and behavior.
- Turn the scanned cabinets' drawers toward the room. Seat the document on the actual upper shelf and stop the cabinet collider at that shelf, so the open frame cannot block interaction rays to the document.
- Add saved Low, Medium, High and Ultra graphics presets to the existing settings menu in Polish and English. Presets control render scale, antialiasing, shadow resolution/distance/cascades and texture mip limits. Rendering changes use a runtime copy of the pipeline, so playing does not rewrite the project pipeline asset.
- Rebalance practical and window lighting for the scanned materials, with restrained grain and lens distortion. Bake before judging the result.

## Sources

Powered by [Poly Haven](https://polyhaven.com). Assets are [CC0](https://polyhaven.com/license). Exact asset pages, download URLs and verified hashes are recorded in `Assets/Art/Environment/StationRebuild/Scanned/sources.json`. Downloading these files does not require a paid asset pack or a model-generation subscription.

## Ownership and scene contract

| Type | Role | Responsibility | Dependencies | Lifecycle / owner | Reason |
| --- | --- | --- | --- | --- | --- |
| StationRealismSetup | Editor authoring service | Import scanned material channels, fit static props, configure lighting | UnityEditor, URP, existing scene components | Explicit menu invocation in Room Edit Mode | Keep asset processing outside gameplay |
| GraphicsQualityController | MonoBehaviour bridge and bootstrap | Apply saved quality to a runtime pipeline copy; configure Game cameras | GameSettingsService, Unity rendering, URP | BeforeSceneLoad creates one persistent owner; destroys its copy and restores settings at teardown | Requires renderer and camera lifecycle callbacks |
| GameSettings | Existing plain settings state | Normalize and persist the quality selection | ISettingsStore | Existing GameSettingsService owner | Storage remains testable without rendering |
| SettingsMenu | Existing presenter | Render localized graphics choices and send selection | Existing settings state and UI Toolkit | Existing menu lifecycle | No rendering decisions in UI |

The existing Map_Station, RoundPhysicalIntegration and network identity contracts remain. New ScannedDetails contains static decoration only. Carryable renderers use probes and remain non-static. The player's inventory components retain their IDs and get an interaction anchor inside the fitted collider.

## Rebuild order

1. Run `tools/station-rebuild/download_scanned_assets.py` using Python.
2. Run `tools/station-rebuild/prepare_scanned_models.py` and `build_status_indicator.py` through Blender MCP.
3. Refresh Unity, await compilation, run **12 Prepare scanned materials** once after source changes. Texture imports can take longer than one MCP request; check the completion log before repeating.
4. Run **13 Apply scanned environment**, then **14 Light realistic surfaces**.
5. Run **11 Fix two-sided door interaction** after rebuilding old door visuals.
6. Validate traversal, bake lighting and occlusion, await both completions, save and visually inspect.

**15 Correct scanned cabinet facing** applies the facing, shelf-height and collider corrections to an existing scanned scene. They are also included when **13 Apply scanned environment** rebuilds the presentation.

These commands deliberately reapply authored choices. Run them after the earlier architecture/interior builders; running the earlier polish pass last would overwrite some of these materials and lighting choices.

## Verification

| Check | Result | Evidence |
| --- | --- | --- |
| Compilation | Passed | No C# compiler errors after the final source changes |
| Settings tests | Passed | 42/42 `GameSettingsTests`, no failures or skips; job `14c0d71ba2cd4b0c8e11f24d660d360d` |
| Door regression | Passed | All 14 doors opened through the player's interaction command from both sides: 28/28. See [record](two-sided-door-checks.json). The interrupted second-half session was discarded and rerun. |
| Spatial access | Passed | 8 clear spawns, 14 doors checked from both sides, 14 reachable room volumes, 58 accessible interactions, 5,438 connected grid cells |
| Carryable replacements | Passed | Both document and toolbox picked up and dropped through player commands; original item IDs retained. Document pickup/drop repeated successfully after the final shelf/collider correction. |
| Records cabinet | Passed | Actual interaction opened the minigame and locked movement; cancellation closed it and restored movement |
| Conditional state visuals | Passed | All 16 state roots activated in a presentation check: zero enabled primitive markers, 24 visible replacement mesh renderers. This is a presentation check, not completion of all objectives. |
| Graphics menu | Passed | Dropdown changes applied Low 0.75 / 2× MSAA, Medium 0.9 / 2×, High 1.0 / 4×, Ultra 1.0 / 8×, with corresponding shadow settings. Selection persisted after close/reopen and a fresh Play Mode startup loaded Ultra. Actual UI screenshot inspected. These are pipeline settings, not measured antialiasing performance. |
| Pipeline asset safety | Passed | Leaving Play Mode restored `PC_RPAsset`; runtime quality changes used a separate pipeline instance |
| Lighting / occlusion | Passed | CPU bake completed successfully at 23:04 local, 5 September; fresh files confirmed. Four directional lightmaps, one 2048² and three 512², plus 11,931 baked probe positions. Occlusion completed with 29,120 bytes. Scene/assets saved and four finished-room captures inspected. |
| Final cold singleplayer startup | Passed | Local player present, Lobby → Preparation → Round; saved Ultra applied. Records minigame open/cancel and document pickup/drop passed after the final bake. |
| Final Editor state | Passed | Room saved and not dirty; Play Mode, compilation, lighting bake and occlusion bake stopped; project pipeline restored to PC_RPAsset. Final traversal validation passed. Computer Use remains released. |
| Diff whitespace | Passed | Working and staged diff checks pass |

The local MCP server stopped during testing. The user restarted it; the interrupted samples were discarded. Computer Use was released immediately after recovery. Vivox HTTP Timeout (10028) was observed again in the solo session; voice was not changed by this pass.

The first resumed bake was cancelled and its unchanged files were rejected. The final bake completed successfully. Unity reported a transient metadata write error for `Lightmap-0_comp_dir.png`; an explicit importer save/reimport then passed with no Console errors and the 2048² directional map remained assigned. The later Play Mode error query contained Vivox timeouts. Eight Steam-manager roots already existed in the original Git scene and were left unchanged.

Multiplayer remains excluded by the user's instruction. No minimum-spec or frame-time claim is made without a matching standalone benchmark. The original 37-seat live pass remains applicable to unchanged seating; the final traversal pass also checks every standing anchor.

The scanned source textures are 2K with mipmaps and anisotropic filtering, chosen for metre-scale tiling rather than one texture stretched across a whole room. The scene still contains reused furniture and deliberately simple background geometry; this pass should not be described as completed AAA art production.

[Graphics menu](realism-settings-ui.png) is an actual ScreenCapture image with UI. `realism-graphics-settings.png` is a rejected camera-only capture that omitted the UI, and `realism-prebake-archive.png` predates the new light bake.

Finished lighting captures: [archive and corrected shelf](realism-final-archive_1.png), [common room](realism-final-common.png), [reception](realism-final-reception.png), and [interrogation room](realism-final-interrogation.png). These are positioned player-height Edit Mode views for qualitative inspection, not a pixel or frame-time benchmark. `realism-final-archive.png` without the `_1` suffix was captured after the cancelled bake and is not final evidence.

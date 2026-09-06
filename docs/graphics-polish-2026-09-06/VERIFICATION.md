# Graphics polish verification

This pass reduces excessive corner ambient occlusion, replaces the most visibly crude decorative plants and common-room television, adds six Blender-authored detail models, introduces scanned carpet surfaces, and adjusts the lounge/corridor lighting and HUD presentation.

[Open screenshots and camera sweep](gallery.html). [Research and controlled AO comparison](RESEARCH.md).

## Completed changes

- Isolated the broad corner darkening to SSAO. Reduced radius from 0.7 to 0.18, intensity from 1.1 to 0.55, and direct-lighting strength from 0.5 to 0.15. Retained full resolution and the existing high sample/blur settings.
- Replaced five decorative geometric plants with scanned foliage and removed their remaining legacy leaf meshes.
- Added a television with shallow curved rectangular glass, speaker grille and controls; an inset-door credenza; a detailed monitor, keyboard and wired mouse; a socket tray and cable; and record bundles. Added organized desk, workshop and shelf details.
- Applied a 2K scanned carpet with albedo, normal, occlusion and roughness-derived smoothness to both lounge rugs and the entry mat. The texture is [Dirty Carpet by Rohit Seervi, CC0](https://polyhaven.com/a/dirty_carpet). Download checksums were verified.
- Increased only the two lounge practical lights from 32 to 38. Reduced corridor practical intensity from 25 to 21 and broadened their falloff. Other rooms retain their light intensities.
- Rebuilt lighting, all ten reflection probes and occlusion-culling data.
- Matched HUD backgrounds to the existing ink palette, gave the objective toggle a readable Lato font and type-scale size, and reduced the microphone image to 55% of its previous dimensions with more restrained status colors.
- Preserved existing interaction roots, collider envelopes, spawn points and gameplay rules. Graphics presets remain available, and the saved Ultra preference was not changed.

## Evidence

| Check | Result |
|---|---|
| Unity compilation after the final source change | Passed, no compiler errors |
| Console after authoring and baking | Passed, no errors returned |
| Final lighting bake | Completed callback at 2026-09-06 00:21:06 UTC; three lightmaps and ten populated reflection probes |
| Occlusion culling | Background generation completed |
| Station traversal validator | Passed: eight spawns, fourteen doorways, fourteen volumes, 58 interactions, 5,758 reachable grid cells |
| Singleplayer cold startup | Passed: one local connection, Innocent role, Round phase, Ultra preset |
| Runtime renderer selection | StationPolishedRenderer, AO radius 0.18, camera near plane 0.08, occlusion culling enabled |
| HUD transitions | Expanded, collapsed, settings open and settings close inspected |
| Camera sweep | 24 screenshots recorded; samples 00, 05, 11 and 18 inspected, including matching outward/return angles |
| Git whitespace checks | Passed for working and staged diffs; no changes staged |

The sampled camera sweep retained the corner and nearby objects without the broad AO band abruptly disappearing. This is a sampled visual check, not a proof covering every corner, every frame or every quality preset. SSAO still has screen-space limits. The controlled pre-bake comparison isolates the AO settings; the final sweep checks the completed environment in gameplay.

The final gameplay captures use the actual Main Camera and ScreenCapture with the HUD visible, one local player, a developer round with unlimited time, and Ultra quality. The sweep starts at player position (-3.6, 0.04, 11.3), pitch 27 degrees, and varies yaw from -41 toward -7 and back. Captures are approximately 0.18 seconds apart. They are not a frame-time benchmark. The callback removes itself after the 24 captures.

Vivox service errors occurred during the solo session, including buddy/block-list registration and async-result errors. Voice behavior was not changed or diagnosed in this graphics pass, so the overall runtime Console is not reported as clean.

No multiplayer testing, standalone build or hardware performance benchmark was run. No runtime gameplay C# changed, so additional domain or networking tests were not needed. This is a substantial finishing pass, not a claim of AAA parity or replacement of every original asset. The simple exterior views and some existing furniture still limit realism.

After the test, the Stop command reported success, but Unity stopped answering MCP pings and Windows reported the project Editor as not responding. With explicit user approval, only that Editor process was force-closed and the project reopened. Unity preserved scene backups in Assets/_Recovery. The saved Room scene reopened in Edit Mode without unsaved changes, with three lightmaps, ten baked reflection probes and all 26 FinishPolish children. No missing materials were found under FinishPolish.

Restart verification exposed a null renderer reference in StationPolishedPipeline. The reference was repaired through Unity MCP, saved and verified after forced asset reimport. It now persistently points to StationPolishedRenderer; AO radius 0.18 and intensity 0.55 are intact. The lounge was visually checked in recovery-lounge-verified.png. The two earlier recovery captures were black and are excluded from visual evidence. No compiler errors were returned. The Console error filter also returned three startup entries marked Exception: two ScriptTemplates migration messages and a Mirror banner; an entirely clean Console is therefore not claimed. No computer-control session was used. Unity is responsive in Edit Mode; the cause of the original exit hang remains undiagnosed.

## Screenshots

- [Lounge](final-lounge.png)
- [Office close-up](final-office.png)
- [Workshop close-up](final-workshop.png)
- [Storage](final-storage.png)
- [Gameplay with expanded objective](final-gameplay-expanded.png)
- [Gameplay with collapsed objective](final-gameplay-collapsed.png)
- [Settings](final-settings.png)
- [Camera sweep, first frame](sweep-00.png)

Only the explicitly labelled final images are final presentation evidence. Earlier preview files include construction states and unbaked lighting and are retained as diagnostic history.

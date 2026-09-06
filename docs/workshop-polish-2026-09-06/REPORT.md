# Workshop finishing pass

Rebuilt the workbench and both supply racks in Blender. The bench has a welded frame, adjustable feet, an inset drawer cabinet, bent handles, edge strips, a vice and varied hand tools. The existing toolbox and socket tray remain on the worktop with separate working areas.

The two racks now contain maintenance supplies rather than duplicate archive folders: open fastener bins, labelled cartons, cans and cable spools. Their contents differ while retaining the original rack footprints and collision envelopes. Carton material and worktop grain direction were corrected after preview inspection.

## Authoring

- Run `tools/station-rebuild/build_workshop_details.py` with background Blender.
- Open Room in Edit Mode and run `Tools/Interrogation Room/Station Rebuild/23 Polish workshop` through Unity MCP.
- Bake lighting, regenerate occlusion, save and visually verify the scene.

The builder replaces presentation under Map_Station/WorkshopPolish and moves only the three tabletop props. It preserves existing task roots and collision geometry. Earlier graphics and social-room work, including the pre-existing modified terrazzo source material, is preserved.

## Verification

- Blender exports passed: bench 20,060 triangles; rack A 18,528; rack B 16,448. These counts are not performance measurements.
- Compilation passed. Console error query returned no errors after applying the builder.
- Three replacement roots, 20 renderers, zero missing materials.
- Traversal validation passed: 8 spawns, 14 doorways, 14 room volumes, 58 interactions and 5,747 reachable grid cells. Includes approaches from both door sides, interaction visibility and stand-up clearance.
- Entrance inspected from both oblique sides in Edit Mode. No see-through gap was reproduced in these views. No claim is made about every animated door angle or every moving-camera frame.
- Bake completed at 2026-09-06T02:26:42Z. Three lightmaps and ten baked reflection probes are present. Occlusion generation completed and Room was saved clean in Edit Mode.
- Final bench, entrance/rack and close-up screenshots were inspected after occlusion finished. All gallery image files are non-empty.
- Unity reported metadata-write errors during baking. All nine referenced lightmap textures were force-reimported through Unity MCP and assets saved. Console counts stayed at 18 errors, 25 warnings and 2 logs across that reimport, with no added errors. All three color and shadowmask references resolve. The original write-error cause is unconfirmed; the Console is not reported as clean. No metadata was edited manually.
- Working and staged Git whitespace checks passed. No staging or commit performed.
- No multiplayer, standalone build or performance benchmark. No runtime gameplay code changed; this pass uses Edit Mode inspection and geometry validation.

## Screenshot conditions

Room in Edit Mode, MapOverviewCamera positioned through Unity MCP, no local player, transport or gameplay HUD. Bench comparison: position (10.8, 1.65, 5.6), target (13.2, 1.1, 7.5). Entrance/rack comparison: position (9, 1.65, 4.5), target (6.1, 1.3, 3.3). Final captures use rebaked lighting; previews retain old baked shadows and are construction evidence only.

[Before and after](gallery.html)

## Toolbox orientation correction

Both scanned toolboxes had their carrying handles facing forward. Their roots are now rotated 90 degrees in pitch so the handles face upward. The main box faces the player with its clasps; the spare box is turned across the bench. Bounds-based placement puts both bases at worktop height, y = 0.958 m, without changing their scale. The dedicated menu `24 Align workshop toolboxes` applies this correction without rebuilding the furniture, and the main workshop builder preserves it on reruns.

The corrected geometry was inspected close up, compilation passed, and the scene was saved. The initial correction bake was cancelled to turn the main box's clasp side toward the player. The replacement bake completed at 2026-09-06T02:47:01Z, followed by occlusion generation and saving. Both bases are at y = 0.958 m. Updated close-up and overview captures are toolboxes-corrected.png and bench-toolboxes-corrected.png.

The metadata-write errors recurred during the bake. All nine lightmap textures were reimported through Unity MCP, with Console counts unchanged across reimport: 32 errors, 25 warnings and 1 log. Three lightmaps and ten baked probes remain present. Scene is clean in Edit Mode. No Play Mode or multiplayer test was needed for this decorative rotation change; compiler, placement, visual and Git whitespace checks passed. The metadata-write issue remains separate from the completed placement correction.

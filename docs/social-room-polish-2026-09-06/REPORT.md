# Staff room finishing pass

The kitchen and refrigerator now use two new Blender models. Cabinet fronts have separate drawers, recessed seams, bent handles and a toe-kick. The counter surrounds an open, tapered sink basin with a drain, tap, drainer and soap dispenser. The refrigerator has separate doors, gaskets, hinges, feet, rear coils and a clipped note.

The noticeboard moved from behind the lockers to the wall beside the dining area. The table has painted metal legs, and the existing scanned terrazzo has a room-specific material with roughly half-metre tiles. Existing task roots, mugs, seating and furniture colliders remain in place.

## Reproduce

1. Run `tools/station-rebuild/build_social_details.py` with background Blender.
2. Open `Assets/Scenes/Room.unity` in Edit Mode.
3. Run `Tools/Interrogation Room/Station Rebuild/22 Polish staff room` through Unity MCP.
4. Bake lighting and reflection probes, regenerate occlusion, save and inspect the room.

The builder affects only staff-room presentation. It does not run the older whole-map builders. The previous graphics pass and recovery backups are preserved.

## Verification

- Blender exports passed: kitchen 14,998 triangles; refrigerator 8,620 triangles. This is a mesh count, not a performance claim.
- Unity compilation passed; the bounded Console error query returned no errors after applying the builder.
- Traversal validator passed: 8 spawns, 14 doorways, 14 room volumes, 58 interactions and 5,747 reachable grid cells. Includes door approaches from both sides, interaction visibility and stand-up clearance.
- Before captures and construction previews were inspected. A reversed sink surface found in the close-up was corrected before baking.
- Final bake completed at 2026-09-06T01:52:43Z. Three lightmaps and ten baked reflection probes are present. Occlusion generation completed, then Room and assets were saved.
- Final kitchen, locker and sink screenshots were inspected. Use final-kitchen_1.png; the earlier final-kitchen.png was captured during occlusion generation and contained a temporarily culled noticeboard, so it is excluded from the gallery.
- Both models have their materials assigned: two roots, eight renderers, zero missing materials. Scene saved clean in Edit Mode. Git whitespace checks passed.
- No multiplayer, build or frame-time benchmark. No gameplay code changed; Edit Mode geometry and traversal checks cover this presentation change.

All captures use Room in Edit Mode, the MapOverviewCamera positioned by MCP, no local player, no transport and no gameplay HUD. The kitchen comparison uses position (8.8, 1.65, -0.7), target (14.2, 1, -2.1). The locker comparison uses position (13.1, 1.65, -1), target (8.3, 1.05, -4.3). Lighting is rebaked for the changed geometry, so these are complete scene comparisons rather than an isolated material A/B test.

## Scope remaining

This pass finishes the staff-room kitchen and corrects its composition. Workshop, office and broader door-seam visual checks remain future passes. The user's request to leave the occasional Editor freeze investigation aside was followed.

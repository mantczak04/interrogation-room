# Final room cleanup and unlimited sprint

## Changes

- Removed PlayerController's stamina duration, recovery delay and recharge threshold. Sprint retains its existing forward-only input and 1.55 speed multiplier. The compatibility HUD charge property stays at 1.
- Replaced the deposit room's freestanding combined shelves with three detailed wall racks and retained the interactive evidence shelf. Kept an open central circulation area.
- Swapped the archive desk and side-table locations. Turned the PC desk toward the room and moved its equipment, chair, trash bin and separate carryable box with the furniture. Furniture feet remain at floor level.
- Added a Blender-authored archive alarm controller with a grille, individual controls, fasteners, label strip and conduit fittings (3068 triangles). Preserved the alarm interaction and state indicators.
- Corrected the two backwards corridor alarm units and the workshop's open PowerHaven cabinet, including its collider.
- Moved the kitchen countertop props toward the wall and kept the mug interaction reachable.
- Separated room 08's round tables and aligned their chairs. Moved the board left in the room-facing view to clear the workshop sign.
- Moved the office coffee table toward the sofa, leaving a 12 cm gap between their visible bounds.

## Verification before baking

- Compilation and Console: passed, zero errors before the bake.
- Singleplayer sprint: 9.00 seconds of held W + Left Shift; 3556 unique sampled frames after the first second, zero non-sprinting frames, charge remained 1. Sprint became false after release. Test used one local connection and local player in a solo round; position was reset along the clear central lane to permit sustained movement. Temporary keyboard and callback removed afterward.
- Layout validator: passed, 8 spawns, 14 doorways, 14 room volumes, 58 interactions and 5994 reachable grid cells. Includes interaction line of sight and stand-up clearance.
- Door sweep: 518 collider poses, no prop penetration above 5 mm. Door descendants and architectural shell/lining excluded.
- New model materials: no missing references.
- Inspected archive, deposit, kitchen, corridor alarms, workshop cabinet and briefing previews. Preview shadows are from the previous bake.

## Build order

Run **Tools / Interrogation Room / Station Rebuild / 33 Final room cleanup** after earlier map builders. It preserves gameplay roots and identities. Visibility data was rebuilt for the final layout; do not restore data from an earlier layout.

## Final bake and inspection

- Lighting completion callback: 2026-09-06 05:55:38 UTC, after approximately 10 minutes 26 seconds. Two complete directional lightmap sets (2048 and 1024 pixels); all ten reflection probe files updated by this bake.
- Unity reported four metadata-write messages for `Lightmap-0_comp_dir.png.meta`. The metadata was subsequently written. A targeted force-reimport through Unity succeeded, preserved the GUID and loaded the texture; no new Console errors appeared afterward. Historical bake messages remain in the Console.
- Final archive, deposit, kitchen, briefing, office and alarm screenshots inspected. Old furniture shadows are gone.
- Occlusion generation completed. At twelve camera positions around the original coat-rack failure, culling-on versus culling-off captures had zero pixels differing above the test threshold (summed RGB difference greater than 30, 320×180 captures). A full-size final screenshot also retained the rack. This is a bounded regression check, not a guarantee for every map viewpoint.
- Final scene saved in Edit Mode. No multiplayer or standalone performance benchmark was run.
- [Final baked screenshot gallery](gallery.html).
- Commit checks: authored code/doc whitespace checks passed. Full staged whitespace checking reports Unity-generated trailing spaces in material serialization; those serialized assets were left under Unity's ownership rather than edited as raw YAML.

# Map batch review

## Doorway movement

Reproduced a physical camera-height change using the real PlayerController in Room Play Mode. The first input attempts did not move and were discarded. Focusing the Unity Game View allowed a temporary Input System keyboard to drive the existing movement action. Each temporary device was removed after the run; the input-focus setting was restored. No OS input automation was used.

Scenario: solo developer Round, local host, one connection, local player present, workshop doorway open, first-person camera with downward pitch set to 80 degrees, no menu input block. Forward direction from x=4.7 toward x=7.5 at z=4.5, speed 2.5 m/s. The initial y=.04 spawn settles to y=.05; comparison uses x=5.8 through x=6.5.

| Run | Total unique frames | Door-zone frames | Player Y in door zone |
| --- | ---: | ---: | --- |
| Before | 480 | 121 | .050 to .054 m |
| After, reverse | 489 | 125 | .050 m |
| After, matching forward | 480 | 119 | .050 m |

The raised visual sill also raised the collider. All 14 thresholds now retain the 4 mm visual separation from the floor while a child MeshCollider remains flush at floor height. This removes the reproduced physical bump without restoring coplanar visible floor surfaces. These are movement samples, not performance measurements. They do not prove that every possible rendering shimmer is eliminated.

Data: DoorWalkBefore.txt, DoorWalkAfter.txt, DoorWalkAfterForward.txt. The correction is persisted by StationLayoutClearance menu 29 and included in menu 26.

## Room sweep and changes

Inspected archive, storage, evidence, briefing, social, reception, common room, interrogation and corridor views, alongside the current office/workshop views from the preceding passes. Traversal and door checks cover the whole map. Images of evidence racks are close aisle views and do not show every wall.

- Archive: typewriter and receipt tray floated above the visible scanned desktop; both task groups now rest on it. Computer height aligned to the desktop. Phone moved off the top of the computer and replaced with the detailed phone on a free part of the desk.
- Archive shelves: three detailed units with bolted posts, cross braces, varied binders, spine rings, labels, document bundles and lidded record boxes.
- Storage shelves: six replacements using two alternating supply layouts, with cartons, bins and handled cans. Existing collision envelopes and aisle footprints retained.
- Other inspected rooms: no additional furniture movement was justified by this pass. Earlier office, corridor and workshop corrections retained.

Run tools/station-rebuild/build_archive_storage.py in background Blender, then StationArchiveStoragePolish menu 30 through Unity MCP. Do not rerun earlier broad room builders to apply this pass.

Geometry: archive unit 21,088 triangles; supply variants 13,744 and 14,068. Small hardware tessellation was reduced before final export. No hardware performance claim is made.

## Verification

- Passed: Unity compilation and final Console error check.
- Passed: 8 spawns, 14 doors, 14 room volumes, 58 interactions, 5,808 reachable grid cells. Occupancy, approach clearance, visibility, interaction range and stand-up clearance.
- Passed: 518 door poses across both directions, 5-degree steps. No prop overlap above 5 mm. Existing shell/lining hinge contact excluded from this furniture check.
- Passed: 10 new presentation roots, zero missing materials; all 14 flush threshold colliders present. Room saved in Edit Mode.
- Passed: inspected archive/storage after images and archive desk image; whitespace diff checks.
- Not run: multiplayer, standalone build, frame-time benchmark. Single local host was used only for movement reproduction.
- Deferred for batch review: lighting bake and occlusion rebuild. Screenshots show current layout with existing lighting and may retain stale baked shadows.

[Review gallery](gallery.html)
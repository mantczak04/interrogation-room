# Furniture and doorway clearance

Implemented the requested office and corridor arrangement in `StationLayoutClearance.Apply`, menu 26. Run this after older furniture builders, which retain their previous layouts.

- Desk and all current desktop props moved 0.45 m left and 0.52 m toward the back wall. Chair moves with them.
- Office sofa moved 0.75 m toward the corner, away from the door swing. Coffee table aligned to its centre and moved 0.40 m closer. Board moved 0.50 m along the wall so its edge no longer extends through the corner.
- Waiting benches have mirrored centres at x = -1.70 and +1.70, at the same distance from the wall. Coat rack moved away from the workshop door sweep. Plant moved into the corridor corner.
- Closed scanned toolbox replaced visually by a purpose-built open steel box, upright with the tray facing the player and the lid behind it. Original object retained. Blender model has 4,996 triangles.
- All 14 threshold tops raised 4 mm from the floor plane. The previous y = 0 tops overlapped adjacent floor edges at the same height. Replaced stretched threshold texture with a local metal material. This addresses the observed geometry condition that can cause flickering; an actual player walking reproduction remains unverified.
- Sofa stand-up anchors moved to clear floor beside the seating group.

## Checks

- Unity compilation and Console errors passed before bake.
- Door furniture clearance: 14 doors, angles -90 through +90 degrees in 5-degree steps, 518 poses. Physics.ComputePenetration found no prop overlaps above 5 mm. Door's own colliders excluded. Structural shell and lining contact at hinges excluded and not claimed fixed by this furniture pass.
- Traversal: 8 spawns, 14 doorways, 14 room volumes, 58 interactions, 5,808 reachable grid cells. Occupancy, door approaches, interaction visibility/range and stand-up clearance passed.
- Visual previews inspected for open toolbox, office lounge and downward doorway view. Preview shadows predate the new bake.
- Bake ceased while Unity entered Play Mode, without the session completion marker. Completion is unconfirmed; no restart or occlusion rebuild was performed. Final-position captures were inspected in the existing Play session, through MapOverviewCamera without changing the player. No Console errors were reported at the last check.
- Floor-height sampling passed: 574 downward samples at 2 cm spacing across all 14 doorways, heights 0 to 4 mm.
- Future workflow agreed with user: batch layout and asset adjustments, inspect with temporary lighting, then bake once after the layout settles.
- No multiplayer, build or frame-time benchmark. No runtime door logic changes.

## Capture conditions

Room in Edit Mode, MapOverviewCamera, no local player or HUD. Before/after lounge camera (-11,1.7,10) looking at (-6.5,1.3,12). Toolbox (12.7,1.65,6.7) looking at (12.72,1.12,7.5). Threshold preview temporarily opened the workshop leaf, then restored it closed before saving and baking. Final captures use the currently available lighting; the bake completion and refreshed occlusion are not certified. Actual player walking reproduction is still not verified.
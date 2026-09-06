# Repeated fixtures and materials — 6 September 2026

Implemented in `Assets/Scenes/Room.unity`, using four new Blender-authored meshes and the repeatable menu **Tools / Interrogation Room / Station Rebuild / 31 Refine repeated fixtures**.

- 14 door leaves: panel beading, handles on both faces, keyholes, hinges, screws and metal kickplates. Existing door pivots, state and colliders retained.
- 14 radiators: cast sections, connecting pipes, feet and thermostat details.
- 29 ceiling fixtures: enamel trim, clips, vents and diffuser ribs added around existing fixtures. Light intensity and emission unchanged.
- Two briefing tables replaced with adjacent rectangular tables, metal frames, rubber feet, folders and pens.
- Dedicated wood, enamel, steel and brass materials; reduced plastic and cardboard gloss and normal strength.

## Verification

- PASS: Unity compilation and Console query, zero Console errors.
- PASS: saved Room scene, Edit Mode, no unsaved scene changes.
- PASS: 45 fixture-root children (14 radiators, 29 trims, two tables), no missing materials there; all 14 doors have the new attached visual.
- PASS: layout validation — 8 spawns, 14 doorways, 14 room volumes, 58 interactions, 5808 reachable grid cells. Occupancy, doorway clearance, room reachability, interaction range/line of sight and stand-up clearance passed.
- PASS: 518 door collider poses sampled from -90 to +90 degrees in 5-degree increments; no prop penetration exceeding 5 mm. Door descendants and architectural shell/lining were excluded from this prop check.
- PASS: inspected saved door, radiator, briefing and ceiling screenshots.
- Not run: a new lighting bake, per user instruction. Existing baked lighting remains in these previews; final contact shadows need review after the eventual bake.
- Not run: multiplayer, standalone performance benchmark or a new Play Mode session. This pass changes presentation; no frame-time improvement is claimed.

## Screenshots

Before/after cameras match for each pair; images are Edit Mode captures from MapOverviewCamera with no local player or round UI. No pixel-difference claim: output sizes differ between baseline and final captures.

- [Door before](door-before.png) / [after](door-after.png)
- [Radiator before](radiator-before.png) / [after](radiator-after.png)
- [Briefing before](briefing-before.png) / [after](briefing-after.png)
- [Ceiling detail](ceiling-detail.png)

Blender source: `tools/station-rebuild/build_repeated_fixtures.py`. Per-model triangles: door 6808, radiator 5100, fixture trim 2556, table 2160. These are geometry counts, not a performance measurement.

# Office finishing pass

Rebuilt the desk, telephone and filing storage in Blender. The desk has inset drawers, bent handles, feet, a modesty panel, a cable rail and a writing mat. Two document trays, a pen cup and a compact computer give the desktop a working purpose. The existing notebook and records form one supported stack, with the keyboard and phone remaining accessible.

The phone has individual keys, a rounded receiver, cradle, coiled handset lead and rear cable. The monitor, keyboard and mouse leads join the computer instead of ending freely on the desktop. A mesh-position check confirmed the retained monitor lead and its extension meet at the same endpoint, within their 3 mm tube radius.

The former staff lockers are replaced by aligned filing drawers with handles, label holders and locks. Their original footprints and collision envelopes are retained. Earlier room work is preserved.

## Authoring

Run `tools/station-rebuild/build_office_details.py` with background Blender. In Room Edit Mode, execute `Tools/Interrogation Room/Station Rebuild/25 Polish office` through Unity MCP. Bake lighting, regenerate occlusion, save and inspect the result.

The builder replaces office presentation under Map_Station/OfficePolish and positions only the existing notebook and records. It retains gameplay roots, desk and cabinet collision, chair, monitor and keyboard.

## Verification

- Blender exports passed: desk 10,100 triangles; telephone 15,204; filing unit 6,916, instantiated twice. Curved-cable subdivision was reduced before import. These are mesh counts, not frame-time measurements.
- Compilation passed; no Console errors after applying the builder.
- Four replacement roots, 20 renderers, zero missing materials in the initial validation.
- Traversal validation passed: 8 spawns, 14 doorways, 14 room volumes, 58 interactions and 5,747 reachable grid cells. Includes interaction visibility and stand-up clearance.
- Desk, storage and phone previews were inspected. Unconnected existing computer leads identified in the close-up were joined before baking.
- Lighting bake completed at 2026-09-06 03:08:36 UTC. Occlusion regenerated and Room saved in Edit Mode with three lightmaps. Final desk, storage and phone captures were inspected.
- Bake reported four metadata-write errors for a lightmap. Reimporting all nine lightmap textures through Unity completed without additional errors (Console counts remained 4 errors, 25 warnings, 4 logs). The underlying metadata-write issue remains unresolved; this was not an error-free bake.
- No Play Mode, multiplayer, build or performance benchmark. No runtime gameplay code changed; Edit Mode geometry and visual checks cover this presentation change.

## Capture conditions

Room in Edit Mode, MapOverviewCamera positioned through Unity MCP, no player, transport or gameplay HUD. Desk comparison: position (-10.8, 1.65, 10.7), target (-12.1, 1.05, 12.8). Storage comparison: position (-10.5, 1.65, 11), target (-13.7, 1.1, 8.6). Phone close-up: position (-11.55, 1.35, 12.05), target (-11.52, .88, 12.79). Final captures use rebaked lighting; construction previews retain older shadows.

[Before and after](gallery.html)

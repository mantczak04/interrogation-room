# Furniture placement and disappearing props

## Changes

- Restored the two round tables in room 08, aligned four chairs around them and applied a warmer chair material. Kept clearance to the workshop doorway and provided clear stand-up anchors.
- Moved both interrogation chairs 18 cm closer to the table.
- Placed the corridor mat immediately outside the interrogation door.
- Moved the archive desk, its equipment, chair, side table and separate carryable box toward the south wall. The box remains on its table in singleplayer startup.
- Turned both starting-room PowerHaven cabinets around, aligned their backs against the walls and adjusted their collision bounds.
- Cleared outdated baked occlusion data. No lighting was baked or cleared.

## Disappearing coat rack

Reproduced at camera position (3.6, 1.6, 3.8), looking at (2.6, 0.9, 5.55). Matching direct camera renders with only `useOcclusionCulling` changed showed the rack absent with culling enabled and present with it disabled. See `direct-occlusion-True.png` and `direct-occlusion-False.png`.

The same view now displays the rack with camera occlusion culling enabled after clearing the scene's stale visibility data. Also checked the deposit room in singleplayer Play Mode. Visibility culling needs rebuilding once furniture placement is settled; this is separate from lighting. No performance improvement is claimed.

## Verification

- PASS: Room scene saved and returned to Edit Mode. Compilation and the completed builder had zero Console errors before Play Mode.
- Runtime check caveat: after leaving Play Mode, Console logged `Tried to Initialize the SteamAPI twice in one session!` and an object-cleanup error. These were not investigated or fixed in this furniture pass; their pre-existing status is unconfirmed.
- PASS: validation of 8 spawns, 14 doorways, 14 room volumes, 58 interactions and 5869 reachable grid cells. Includes doorway approaches, interaction reach/line of sight and chair stand-up clearance.
- PASS: 518 door collider poses from -90 to +90 degrees in 5-degree steps, no prop penetration above 5 mm. Door descendants and architectural shell/lining excluded from the prop check.
- PASS: singleplayer Play Mode startup, one local connection and local player present; relocated token and both power panels retained their positions. Main Camera captures inspected for corridor and deposit visibility, without overlay UI. This was a startup visibility check, not a full round playthrough.
- PASS: final screenshots inspected for round tables, interrogation seating, mat, archive group and cabinet orientation.
- PASS: working and staged diff whitespace checks. Existing unrelated work preserved; nothing committed or pushed.
- Not run: lighting bake, occlusion bake, multiplayer, standalone build or performance benchmark.

Existing baked shadows still mark some previous furniture positions. Those shadows are intentionally left for the later approved lighting bake. Edit Mode and Play Mode images are not used as a lighting A/B comparison.

Repeatable builder: `Assets/Editor/StationFurnitureFollowup.cs`, menu **Tools / Interrogation Room / Station Rebuild / 32 Correct furniture placement**. Run after the earlier fixture builder if rebuilding the scene.

# Graphics inspection, 6 September 2026

Yes, there is substantial room for improvement. The strongest existing assets already establish a worn institutional setting, but the asset quality is inconsistent. The next pass should concentrate on the weakest visible objects, room identity and lighting composition.

This is an inspection of the current station, not a graphics implementation. All ten rooms and four corridor sections were captured, followed by close-ups and a solo Innocent round with expanded/collapsed objective HUD and settings. The original scene and graphics settings were preserved.

## What to improve first

1. Replace the remaining visibly crude props. The cyan geometric plants in the common and briefing rooms are the clearest example. The common-room TV, console, simple monitors, phones and repeated noticeboard sheets also need a consistent level of detail. Start with objects players approach or face for long periods.
2. Give the furniture believable construction. Add panel thickness, joints, seams, screws, sensible wood-grain direction, material-specific roughness and localized wear. The kitchen, refrigerator, doors and shelving should hold up next to the worn chairs. Higher texture resolution alone will not fix basic silhouettes or cloned contents.
3. Refine lighting in the actual runtime camera. The common lounge is dark in the verified gameplay captures. Corridor walls show large bright spots, while ceiling diffusers become featureless white rectangles. Preserve the successful atmosphere and tune fixture shape, bounce and light placement locally. Do not increase global brightness indiscriminately.
4. Give rooms individual purpose through organized detail. Workshop tools and consumables, office keyboard and cabling, archive record formats, and catalogued evidence packaging would help. Keep walkways and interactions clear. The user liked reception, so retain its composition as a reference.
5. Improve windows and surface transitions. Nearly identical blinds and flat exterior views recur throughout. Add plausible depth outside, window construction, appropriate trim, floor transitions and restrained wear where people actually walk or touch things.
6. Unify the HUD and settings. Preserve readability while bringing font choices, spacing, colors and icon treatment together. Reduce the microphone's visual dominance. Check the missing-looking glyphs in the objective toggle and replace tiny debug-style hints with readable prompts.

## Evidence and limits

- Baseline commit: ec6dfe66131969b69e0f526da0ef3abb5c1bf4c0, main.
- Scene: Assets/Scenes/Room.unity, Unity 6000.5.3f1.
- Images 01–18 are 1280×720 positioned Editor camera renders, approximately eye height, without gameplay HUD. These are useful for geometry and composition, not a validated comparison of graphics presets.
- Images 21–23 are actual singleplayer ScreenCapture images. One local player, Innocent role, active developer round with unlimited timer. No multiplayer testing was performed.
- The runtime pipeline reported Station graphics (runtime); Edit Mode reported PC_RPAsset and the PC quality level. The saved in-game preset was not changed or independently identified.
- The early automatic gameplay capture 19 looked brighter. Capture 20 omitted UI and is invalid evidence for a collapsed HUD. Both are excluded from the gallery. Use the settled captures 22–23 for runtime observations. Different capture paths make a numerical brightness comparison invalid.
- Material inventory among active scene renderers: 71 distinct materials, 69 URP/Lit, one text shader and one unlit particle shader. 39 materials had a main texture. Unique main-texture sizes included 22 at 2048×2048 and 10 at 1024×1024. This counts main textures only, not all normal/roughness maps. A material without a main texture is not automatically defective.
- The black shape on the south-corridor floor is an observed anomaly, not a diagnosed shadow bug. The doorway still does not establish that every moving-camera gap is fixed.
- Main menu, other role-specific UI, character appearance and animation, all interaction transitions, collision coverage and standalone performance were not verified. The local character renderers were hidden in first person. This is broad station coverage, not an exhaustive audit of every asset or game state.
- Console baseline contained Vivox HTTP Timeout 10028 errors. At the end Unity also reported recursive PlayerLoop errors during the observation/capture session. Their cause is unconfirmed; this is not a clean Console pass.
- Final Editor state: Edit Mode, Room scene clean, original PC_RPAsset restored. No game source, scenes, assets or settings were changed. Only this inspection folder was added. No tests or builds were needed for the read-only inspection; no FPS or AAA-parity claim is made.

## Screenshot gallery

[Open the visual gallery](gallery.html). Each image links to its original file.

### Common room / lounge

The sofa has convincing wear. The pale TV, console and plain brown rug are much simpler and clash with it.

[![Common room — lounge](01-common-lounge.png)](01-common-lounge.png)

### Common room / tables

The paired dining groups are very symmetrical. Angular cyan plants, empty tabletops and flat exterior views weaken the room.

[![Common room — tables](02-common-tables.png)](02-common-tables.png)

### Reception

Reception remains one of the strongest compositions. Preserve the layout; improve counter construction details and repeated wood grain.

[![Reception](03-reception.png)](03-reception.png)

### Storage

Storage has a clear aisle, but identical binders and shelf arrangements look cloned. Vary contents, labels and occupancy deliberately.

[![Storage](04-storage.png)](04-storage.png)

### Archive

The archive reads as a working room. Add varied record formats and believable desk details; the repeated binders and bright lamp still stand out.

[![Archive](05-archive.png)](05-archive.png)

### Evidence room

This close-up is partly obstructed by shelving, so it is not a room overview. It exposes identical smooth wooden boxes with little packaging detail.

[![Evidence room](06-evidence.png)](06-evidence.png)

### Office

The office workstation lacks everyday equipment and cables. The monitor, phone and clock look much simpler than the chair.

[![Office](07-office.png)](07-office.png)

### Staff room

The staff-room arrangement is coherent. The kitchen and fridge need better seams, hardware, surface response and wear. The carpet-like floor treatment also needs an intentional material choice.

[![Staff room](08-social.png)](08-social.png)

### Workshop

The workshop needs more convincing equipment and a useful work surface. The large bright floor patch and repetitive room shell dominate.

[![Workshop](09-workshop.png)](09-workshop.png)

### Briefing room

The briefing room still contains a cyan geometric plant. The table grouping needs a stronger meeting-room identity, with organized paperwork and presentation equipment.

[![Briefing room](10-briefing.png)](10-briefing.png)

### Interrogation room

Interrogation has a useful focus on the table. The table and repeated noticeboard papers need more detail and authored content.

[![Interrogation room](11-interrogation.png)](11-interrogation.png)

### North corridor

North corridor benches have good wear. Broad bright wall patches and repeated fixtures make the lighting feel staged.

[![North corridor](12-north-corridor.png)](12-north-corridor.png)

### West corridor

West corridor has a stark black wall device and very little visual variety. The exposed vertical wall seams need inspection at normal movement speed.

[![West corridor](13-west-corridor.png)](13-west-corridor.png)

### East corridor

East corridor repeats the same wall hotspot pattern. The wall device needs more convincing detail and material variation.

[![East corridor](14-east-corridor.png)](14-east-corridor.png)

### South corridor

South corridor contains a conspicuous solid-black floor shape. Its cause is unconfirmed; inspect the object and shadow contribution before changing lighting.

[![South corridor](15-south-corridor.png)](15-south-corridor.png)

### Evidence overview

Evidence storage has readable shelving, but uniform wooden boxes do not communicate sealed, catalogued evidence. Add varied packaging and legible tags.

[![Evidence overview](16-evidence-overview.png)](16-evidence-overview.png)

### Workshop corridor entrance

The workshop doorway is closed in this image. No outside world is visible here, but this still image does not verify gaps while moving or opening the door.

[![Workshop corridor entrance](17-workshop-doorway.png)](17-workshop-doorway.png)

### Common room plant close-up

The common-room plant is plainly angular and untextured-looking. Replace its silhouette and leaf materials; a larger texture alone would not solve this.

[![Common room plant close-up](18-common-plant.png)](18-common-plant.png)

### Singleplayer settings

Verified ScreenCapture. The beige settings panel uses a different visual language from the dark HUD. Small condensed labels and tiny bottom hints deserve a readability pass.

[![Singleplayer settings](21-settings.png)](21-settings.png)

### Singleplayer HUD, collapsed objective

Verified ScreenCapture. The lounge is dark in the settled runtime view. The large red microphone draws attention away from the scene. The objective toggle appears to contain missing glyph boxes.

[![Singleplayer HUD, collapsed objective](22-hud-collapsed-verified.png)](22-hud-collapsed-verified.png)

### Singleplayer HUD, expanded objective

Verified ScreenCapture. Expanded goal and role text are readable, but the two large cards compete for screen area. Inspect font coverage in the toggle and the tiny debug-style hints.

[![Singleplayer HUD, expanded objective](23-hud-expanded-verified.png)](23-hud-expanded-verified.png)

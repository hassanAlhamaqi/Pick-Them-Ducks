# Exploration and authored prefabs validation

The final Unity 6000.0.60f1 Windows player passed the gameplay checks in `ExplorationValidation/PASS.txt` with no recorded runtime errors. Source and the packaged gameplay assembly match the isolated tested project.

Checks cover timed pickup; individual deposit credit; grouped and accelerating deposits; throwing acceleration; capacity/save conservation; jumping; four-sided station intake; both rollers crediting the bag while retaining front visuals and clearing those visuals on release; bush reveal and persistent broken state; tree shake activation; floating lake duck positions; and bridge traversal. The scene's authored UI prefab was verified in use, and its serialized purchase button successfully bought an upgrade after instantiation.

Visual review includes dense grass at ground level, crossing bridges and floating ducks, taller piles, and the cream/green/pink journal. Screenshots are in `ExplorationValidation/`. The grass mesh is normalized from the supplied pack and rendered through terrain instancing; non-requested imported scenery is absent from the active nature group.

Reusable assets include the Duck HUD, nested Journal Buy Button, eight habitat variants, Meadow Grass and both lake bridge prefabs. See `PREFAB_EDITING.md` for inspector editing instructions. Existing nature-source assets remain in the project; only requested categories are placed in the current scene.

No new FPS benchmark was run for this iteration. Older benchmark reports apply to their respective earlier builds.

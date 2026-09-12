# Meadow and gameplay polish validation

Validated in Unity 6000.0.60f1 and the Windows development player on 2026-09-12.

The player run passed jumping and airborne jump rejection; shared capacity when switching back to Hands; physical intake from four cardinal directions on both the main box and an installed casket; nearby roller attraction; faster sustained depositing and throwing; and final duck/save conservation. Existing hand hold, per-duck arrival credit/bounce, tool upgrades, repeatable casket installation, and automatic driving checks also passed. No runtime errors were recorded.

`PolishValidation/PASS.txt` records the checks. Screenshots in the same directory show the authored meadow, grass, clustered ducks, stations and tools. The packaged player is `Builds/DuckPrototype/PickThemDucks.exe`.

The authored scene update preserves every existing scene object. It adds the Meadow details hierarchy and updates the main box intake bounds. New grass meshes and Park Fence prefab assets are included; the Duck Casket prefab intake is updated. Reload the saved scene in Unity to display these changes if an earlier version is open.

No fresh FPS benchmark was taken for this polish pass; prior benchmark figures apply to their documented earlier builds.

# Rocky mountain map area

The authored DuckPrototype scene now includes a rocky mountain north of the lake. The entrance is approximately **(-63, 0, 155)**. Follow the broad winding trail around the formation; it rises about 18 metres over one and a quarter turns without requiring jumps.

- Summit: **600 ducks**, on an overlook around **(-63, 18, 183)**.
- Halfway cave: **220 Golden Ducks**, worth **5 coins each**, around **(-72, 9, 192)**.
- Trail: **53 duck placements** lead between the entrance and summit.
- Lake: radii reduced from 38 × 52 to **28 × 35** (roughly half the surface area). Terrain shores, bridges, dock and stepping-platform positions were adjusted. **76** staggered stepping platforms remain active.

## Edit the area

All new map assets are in **Assets/Sandouq/Park/Mountain**. The scene contains a linked **Rocky switchback mountain.prefab** instance, with nested prefabs for **Golden grotto**, **Rare cave duck pile**, **Summit duck pile**, and the trail signs. The imported Ghibli rocks remain editable prefab instances. No runtime script builds the mountain or the cave.

Expand a pile to move individual Duck Placement instances or change their Variant. The existing finite total stays 50,000; these placements relocate existing IDs, with no duplicate duck spawning. A fresh run shows the redesigned initial positions; existing collected/moved ducks retain saved progress. No save was reset during this change.

The mountain root has **DuckMountainArea**:

- **Walking Surfaces**: trail, cave floor and summit colliders. Keep these references when replacing geometry; they support pile collapse and rolling ducks on raised surfaces.
- **Trail Waypoints**: editor-visible guide/validation points. Moving them alone does not deform the authored trail mesh.
- **Cave Entrance / Cave Pile / Summit Pile**: editable references for inspecting the area.
- **Footprint Radius**: excludes procedural meadow ducks from the solid rock formation; explicit placement prefabs supply its ducks.

The area is assigned to **DuckPark > Mountain**. Lake Center, Lake Radius and Water Height remain on DuckPark; visual shore geometry must stay consistent when changing them.

Trail signs use **DuckWorldLabel** and the **World sign text** material, which obey scene depth so labels do not show through the mountain. Change their TextMesh text and color in the sign prefab.

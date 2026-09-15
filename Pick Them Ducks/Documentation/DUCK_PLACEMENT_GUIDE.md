# Editing duck placements

Open the authored DuckPrototype scene, then **Sandouq > Ducks > Edit duck placements**. Keep **Show ducks in Scene view** enabled.

## Move an existing duck

1. Choose **Pick duck**, then click a visible duck in Scene view.
2. It becomes a linked **Duck Placement.prefab** instance at its existing position, with the same stable duck ID.
3. Switch the window to **Off** (or press Escape), then use normal move/rotate tools and Transform values.
4. Set **Variant** on DuckPlacement in the Inspector. The preview updates when the placement moves.
5. Save the scene.

## Place at a specific location

Select the desired variant in the window, choose **Place duck**, and click a solid surface. The editor assigns an unused meadow ID and moves that duck to your chosen point. It skips IDs already used by placements, habitat ducks, and the floating lake group. There is still one finite population; placing does not silently add more ducks.

Adjust Transform Y to put a duck on water or a prop. Scale does not change duck size. Parent placements under your area objects to move a whole group together.

The prefab lives at `Assets/Sandouq/Park/Prefabs/Duck Placement.prefab`. You can also drag it into the scene or duplicate a placement. After duplication, click **Assign free IDs to selected placements** in the Inspector. Invalid/duplicate IDs are flagged and rejected at runtime.

Deleting a placement restores that duck's default field location on a fresh run; disabling a placement also removes its override. It does not delete a duck from the finite population.

## Duck types

Assign an existing Rubber Duck or Golden Duck Variant, or create one using **Assets > Create > Sandouq > Duck Variant**. The exact-ID placement overrides configured ID ranges, and the type stays attached to the duck through pickup, throwing, and deposit. A variant currently controls display name, coin value and deposit-effect prefab; both existing variants share the rubber-duck model. An empty Variant uses the configured ID range/default.

## Saves and trees

Scene view shows your starting layout. Existing saves preserve already moved or collected ducks, so use a fresh run to assess a redesigned starting layout. Editing does not erase player saves. Initial placement positions are not written as player movement until that duck actually moves.

For tree/bush-owned ducks, continue editing the habitat's spawn-point children when you want shaking/breaking to control them. Picking one through this editor instead explicitly takes ownership of that ID: it becomes an independent placement and is excluded from its old habitat.

## Sweeper behavior

Hold LMB and move to gather ducks into the same stable front arrangement used by the Collector. Releasing LMB or changing tools releases them with normal rolling physics. The Sweeper remains manually moved and push-only: ducks stay in the world and can be pushed into stations. Its width upgrade still widens the intake/front arrangement.

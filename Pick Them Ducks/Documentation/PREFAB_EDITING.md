# Editing the prototype in Unity

## Interface

Open `Assets/Sandouq/UI/Duck HUD.prefab` in Prefab Mode. The Canvas contains the money/bag cards, tool slots and Journal panel as real serialized UI objects. Enable Journal panel temporarily while editing its rows; runtime visibility follows the menu state.

- Change a card or button's **Image > Color** for its normal background.
- Change **Button > Colors** for hover, pressed and disabled tint.
- Change a child **Text** component for font, size, color and alignment.
- Adjust the **RectTransform** for size, anchors and position.
- Change **DuckHUD > Selection Color** for the active tool and journal tab accent.

The five journal purchase buttons are nested instances of `Journal Buy Button.prefab`. Edit that prefab to update their common styling. Individual instance overrides remain possible. Runtime binds actions to the saved references; it does not rebuild the UI hierarchy. The scene contains a Duck HUD prefab instance, and DuckGame references it via Authored HUD.

## World objects

Reusable prefabs are under `Assets/Sandouq/Park/Prefabs/`:

- `Habitat Tree_01` through `Habitat Tree_05`: tree model and DuckHabitat component.
- `Habitat Shrubs_01` through `Habitat Shrubs_03`: breakable bush model and DuckHabitat component.
- `Meadow Grass`: normalized imported grass mesh/material used by Terrain details.
- `North south bridge` and `East west bridge`: collidable wooden lake crossings.
- Existing Player, shop, collection box, tool and Duck Casket prefabs remain in use.

DuckHabitat exposes Duck Count (1-10), Interaction Range, Spawn Points and Shake Strength. Select a habitat with Scene Gizmos enabled to see gold duck previews and numbered position handles. Expand `Duck spawn points` and move its children in Prefab Mode to change all trees of that type, or override a scene instance. Crown Height is only a fallback when a spawn marker is missing. Saved moved ducks retain their saved positions; test new spawn layouts with a fresh field. Preserve each scene habitat's unique Index: it allocates stable duck IDs and identifies broken bushes in saves. Moving a habitat is safe; changing an index in an existing saved field changes its allocation.

Terrain detail density, distance, prototype scale and painting remain editable on the Terrain. Grass material and mesh assets are `Materials/Terrain Grass.mat` and `GrassMesh.asset`.

The old scene, nature, tool and UI generators have been removed. Edit the saved scene and prefabs directly. `Sandouq > Ducks > Build authored Windows player` builds that scene without regenerating content.

`Lake stepping stone` and `Lake floating timber` are replaceable platform prefabs. Replace their visual child, and keep the root collider plus DuckLakePlatform component. Adjust Footprint and Top Height to match the collider. Scene instances are registered in DuckPark > Platforms; add new platform instances to that list.

UI border colors live on each `Outline > Effect Color`, now #F9C001. Purchase buttons retain their current interactable state between refreshes, so affordable buttons no longer restart their color transition.


## Click pickup and interaction bounds

Hands collect on an LMB click. Pickup Speed controls the cooldown between successful clicks; holding the button does not repeat pickup. The circular Image assigned to DuckHUD > Hold displays the remaining cooldown, preserving its authored color. The small central crosshair is a separate hollow-circle Image. UI palette changes preserve existing GameObject active states and component enabled states.

Open a Habitat tree or shrub prefab, select `Interaction collider (edit bounds)`, and use BoxCollider > Edit Collider to adjust its Center and Size. DuckHabitat > Interaction Collider explicitly selects the shape used for both hover and LMB/E interactions; it no longer uses renderer bounds. Interaction Range is the maximum ray reach. Keep the collider a trigger so it does not block movement. Existing physical colliders remain independently editable.

Picking ducks from an elevated ground pile wakes nearby elevated ducks through the bounded physics pool. They tumble down and return to instanced rendering when still.


Tool upgrades appear in the purchased tool row on the Tools tab. Sweeper, Collector and Roller Car rows show their own level, cost and UPGRADE/MAXED state. Upgrading a row does not switch the equipped tool. The Upgrades tab contains only the four player upgrades; the shared fifth row is shown for Tools and Field Guide. Existing prefab styling and disabled HUD objects are preserved.


## Skybox and reusable components

The Player camera now uses Skybox background instead of Solid Color. Open Window > Rendering > Lighting > Environment and assign Skybox Material to try the imported materials. The default demonstration uses Fantasy Skybox FREE / Panoramics / FS017 / FS017_Day. The camera reads the scene setting; runtime code no longer creates or restyles a fallback camera.

`Duck Game Systems.prefab` contains Population, Physics, Feedback and Deposits components, plus audio sources with replaceable audio clips. The scene DuckGame references those components. `Rolling Duck.prefab` contains its Rigidbody, sphere collider and normalized visual. `Collection Box.prefab` now owns its deposit-station component and landing target.

UI buttons have DuckUIAnimation: edit Hover Scale, Selected Scale, Pressed Scale and Duration in the prefab inspector. Journal Buy Button is shared by the purchase/upgrade rows. Tweens use unscaled time and only animate selection on a state change. Existing Button colors remain editable.

The remaining duck template normalization and instance batching are rendering preparation for the supplied duck prefab. Fixed pools instantiate reusable visuals; they do not regenerate authored scenery or UI. Benchmark and editor-preview objects remain isolated testing/editor infrastructure.

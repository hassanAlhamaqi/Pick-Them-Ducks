# Pick Them Ducks — editable park prototype

Open `Assets/Sandouq/Scenes/DuckPrototype.unity` and press Play. The terrain, landmarks, player, tools and scenery are saved scene objects. The settings asset controls population and economy; it does not construct the environment on Play.

## Controls

| Input | Action |
| --- | --- |
| WASD / mouse / Shift | Walk / look / run |
| Hold LMB | Pick up, scoop, vacuum, or push with the fork |
| Hold RMB | Throw inventory ducks forward, one every 0.18 seconds |
| E at collection box | Deposit inventory plus the casket you are carrying |
| E at shop | Buy tools, upgrades and a casket |
| E near casket | Transfer inventory into its 1,000 spaces |
| F near casket | Carry it; press F again to set it down |
| 1 / 2 / 3 / 4 / 5 | Hands / basket / vacuum / fork / industrial vacuum |
| Escape / F3 | Menu / render diagnostics |

The casket costs $450. Fill it in the field, physically carry it back, and press E at the collection box. Ducks stored in a distant casket cannot be deposited remotely. Carrying it slows movement and occupies your hands. Its contents, location and carried state are saved, along with the player's return position.

## Progression

| Tool | Cost | Base inventory | Function |
| --- | ---: | ---: | --- |
| Hands | Free | 10 | Single pickup |
| Basket | $25 | 45 | Scoop up to 4 ducks |
| Vacuum | $220 | 120 | Up to 6 ducks per pulse, 5.5 m range |
| Big fork | $80 | 120 | Push up to 12 nearby ducks into clusters |
| Industrial vacuum | $1,500 | 250 | Up to 12 ducks per pulse, 9 m range |

Capacity upgrades add 25 spaces, starting at $35. Pickup speed starts at $40; vacuum range and speed at $80 and $100. Upgrade prices multiply by 1.5 per level, with eight levels. Only depositing earns money. Buying or equipping a smaller tool never discards inventory.

## Editing the park

- `Assets/Sandouq/Park/Prefabs/` contains the tool shop, collection box, portable casket, player, five tools, willow tree, boulder, bench and dock.
- `Assets/Sandouq/Park/ParkTerrain.asset` is editable Unity TerrainData, 300 × 310 metres, with hills and a lake basin.
- Terrain, water, a ground-following walking trail, lakeside trees, boulders, benches and a fishing dock are already placed in the scene.
- `DuckGame` has explicit references to the scene's player, landmarks and park. Move prefab instances in the editor normally. The depot/shop interaction points follow those instances.
- `DuckPark` specifies the terrain and lake exclusion area. Keep its lake centre/radii aligned if reshaping or moving the lake.
- `Sandouq > Ducks > Create or refresh prototype scene` deliberately rebuilds the authored park. **Do not use this to launch the game:** it replaces scene layout and resets the starter environment assets/economy. Ordinary Play and player builds preserve scene edits.

Ducks remain GPU instances rather than 50,000 hierarchy objects. `Sandouq > Ducks > Show ducks in Scene view` enables their editor preview; `Frame duck field` finds them. Before Play the full population is previewed; during Play the live remaining population is shown.

## Physics and performance

`DuckPopulationManager.Query` detects ducks in nearby world-space cells. `DuckGame.CollectId` performs the collection transaction. Cached matrices render sleeping ducks in batches of at most 256. A moved duck rejoins the spatial cell containing its new location; dense piles can use multiple batches in a cell.

`DuckPhysics` owns a prewarmed pool of 96 Rigidbody proxies using the supplied duck visual. Throws, player proximity and fork pushes activate proxies. When a body sleeps or remains below the motion thresholds for 0.65 seconds, its pose returns to the instanced population and the proxy becomes inactive/kinematic. Collecting a moving duck also releases its proxy. Saturation refuses extra throws without consuming inventory. Sleeping world ducks have no individual GameObjects or Update methods.

The existing duck model and materials are retained. Hover outlines use a URP shader; DOTween animates pickups, deposits and fork strokes. The lake uses a lightweight animated ripple shader. Terrain/scenery collisions remain normal authored colliders.

## Saving and validation

The existing `duck-stage-v1.json` path is retained for compatibility, with a version-2 payload. Old saves migrate their collected IDs, inventory, money, tools and upgrades. Saves include explicit inventory/casket IDs and moved world poses. Count conservation, duplicate IDs, invalid numbers and capacity violations are checked; atomic writes and backup recovery remain supported.

`DuckParkChecks.Run` validates authoring references, progression, save recovery, 1k/10k/50k/100k populations, casket capacity/overflow and relocated spatial batches. `DuckParkPlayChecks` is an opt-in standalone check (`--duck-park-check <output-directory>`), isolated from the player's save. `DuckBenchmark` remains available with `--duck-benchmark <output-directory>` for real GPU measurements.

See `PARK_VALIDATION.md` for the current validation results. Earlier flat-field benchmark results describe the previous scene and should not be used as performance claims for this park.

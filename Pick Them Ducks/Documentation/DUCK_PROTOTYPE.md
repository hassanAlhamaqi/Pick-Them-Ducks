# Pick Them Ducks

Open `Assets/Sandouq/Scenes/DuckPrototype.unity` and press Play. Reload the saved scene if an older version is open. The Windows player is `Builds/DuckPrototype/PickThemDucks.exe`.

## Controls

- WASD / mouse: move and look. Shift sprints; Space jumps.
- 1 Hands, 2 Duck Sweeper, 3 Duck Collector, 4 Duck Vacuum, 5 Duck Roller Car.
- Hold LMB to use a tool. Hands require a timed hold. Rollers drive forward; S brakes.
- Release a roller to pull its visible front pile into the player. Ducks are already reserved in the shared bag when contacted, preventing capacity overflow or duplicate deposits.
- E: deposit at a station, break a nearby bush, or shake a nearby tree while looking toward it.
- Hold RMB: throw from the bag, accelerating while held.
- Tab: open the journal, with Upgrades, Tools and Field Guide tabs. Escape closes/pauses.
- F: install a purchased Duck Casket kit; R rotates; LMB installs; RMB cancels.

## Exploration

The active imported scenery uses only grass, trees, grass ground texture, rocks and bushes. Dense grass uses terrain-instanced meshes with a 65 m detail distance. Other imported scenery has been removed from the authored nature group; unused package source assets remain available in the project.

Duck piles now have smaller footprints and stacked tiers. Bushes reveal ten existing ducks when broken and save their broken state. Trees contain ten perched ducks: reachable ducks can be picked directly; E shakes the tree and drops remaining perched ducks through the bounded physics pool. These ducks belong to the original finite population, so interactions never create bonus IDs or respawn collected ducks.

Five hundred ducks float alongside two crossing wooden lake bridges. Walk onto a bridge to grab them. Bridges and the grass prototype are reusable prefabs. Existing saved moved-duck poses take precedence over new spawn positions.

## Collection and economy

All tools share the highest purchased capacity plus Bag Capacity upgrades. Global upgrades are Pickup Amount, Pickup Speed, Bag Capacity, and Walk / Sprint Speed. Both rollers collect on contact while showing up to 96 pooled duck visuals ahead of the roller. Releasing, switching tools, or opening the menu draws those visuals into the player. Additional collected ducks still count when the visual pool is full. The car starts at 20 m/s and the small roller at 8 m/s, before upgrades.

Each deposit launch releases a group matching the active Pickup Amount. Every duck animates and credits on landing. Launch intervals accelerate from 0.09 to 0.015 seconds over three seconds; flight time shortens from 0.32 to 0.16 seconds. Cancelling or stopping resets the ramp. RMB throwing accelerates from 0.18 to 0.035 seconds between throws.

Duck Caskets cost $450 each, can be purchased repeatedly, and become permanent deposit stations after installation. Both caskets and the main box accept physical ducks from every direction.

## Editing and validation

Terrain, bridges, habitats, scenery and stations are authored scene objects. `DuckNatureBuilder.Build` rebuilds the current exploration layout in the chosen project and makes a validation player; use an isolated copy if your main scene has unsaved work. Habitat indices provide stable save keys, so preserve them when moving habitat objects manually.

Validation evidence for each iteration is under `Documentation/`; older reports describe their respective builds. New spawn layouts apply to uncollected IDs without a saved moved pose. Player progress is saved atomically with a backup.

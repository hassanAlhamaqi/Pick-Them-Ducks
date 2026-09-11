# Pick Them Ducks — tools and deposit stations

Open `Assets/Sandouq/Scenes/DuckPrototype.unity` and press Play. If Unity already has the previous scene open, reload it to see the updated serialized tool references. A tested Windows build is at `Builds/DuckPrototype/PickThemDucks.exe`.

The terrain, lake, scenery, shop and depot remain authored scene objects. Tool and player instances reference reusable prefabs in `Assets/Sandouq/Park/Prefabs/`.

## Controls

| Input | Action |
| --- | --- |
| WASD / mouse / Shift | Walk / look / sprint |
| Space | Jump when grounded |
| 1 | Hands: hold LMB on a duck until the progress bar finishes |
| 2 | Duck Sweeper: hold LMB and move the brush into ducks to push them |
| 3 | Duck Collector: hold LMB to drive forward and collect floor contacts |
| 4 | Duck Vacuum: hold LMB to collect within the forward cone |
| 5 | Duck Roller Car: hold LMB to drive and collect with the wide front roller |
| Release LMB / hold S | Stop automatic movement / brake |
| Hold RMB | Throw ducks; ramps from 0.18 to 0.035 seconds between throws over 3 seconds |
| E near any deposit station | Start transferring inventory; press E again to stop new launches |
| F | Preview installation of a purchased Duck Casket kit |
| LMB / R / RMB while placing | Install / rotate 45 degrees / cancel |
| Escape / F3 | Menu / diagnostics |

Moving away from a station stops new inventory launches. Ducks already in flight still arrive. Tools and throwing are temporarily locked while inventory transfers are in flight, so those IDs cannot be spent twice.

## Tools and upgrades

| Tool | Price | Base bag | Behavior |
| --- | ---: | ---: | --- |
| Hands | Free | 10 | Timed hold; starts at 0.65 seconds per grab |
| Duck Sweeper | $50 | 120 | Physical floor brush; does not collect into the bag |
| Duck Collector | $120 | 45 | Smaller roller collector, automatic forward speed 8 m/s |
| Duck Vacuum | $350 | 120 | Forward-cone pickup, 5.5 m range |
| Duck Roller Car | $2,000 | 600 | Ride-on roller, wide intake, automatic speed 11 m/s |
| Duck Casket | $450 each | No storage limit | Permanent installed deposit station; repeat purchases allowed |

Global upgrades are exactly **Pickup Amount**, **Pickup Speed**, **Bag Capacity**, and **Walk / Sprint Speed**. Their starting prices are $30, $40, $35 and $60. Effects are +1 duck per pickup, +20% pickup rate (shorter hand hold), +25 bag spaces, and +10% walking/sprinting speed. They have eight levels and a 1.5 price multiplier.

The selected sweeper, collector or car also has a dedicated tool upgrade in the shop. Each of six levels increases working width by 18% of base width; collector and car upgrades also increase automatic speed by 12% of base speed. Sweeper/collector upgrades start at $75, car upgrades at $250, with a 1.6 price multiplier. All tools share the highest base bag unlocked by any purchased tool, plus Bag Capacity upgrades. Equipping Hands never reduces capacity. The table shows each purchase's capacity floor, not a separate container. Both rollers pull nearby ducks toward the intake while active; collection still requires contact.

## Duck Caskets and deposit feel

Buy as many kits as you can afford. Install each on clear, reasonably level ground using the green/red preview. Water, other stations and solid scenery block installation. Cancelling or rejecting a placement retains the kit. Installed stations are permanent; they are not carried back to the hub.

Both the main depot and installed caskets accept ducks pushed or thrown from any direction, including behind the walls. They also accept inventory via E. Each duck takes a visible arcing flight into the receiver. Every landing bounces the receiving object and increments money/deposited counts by one. Launch spacing ramps from 0.09 to 0.015 seconds over 3 seconds of sustained depositing; flight duration shortens from 0.32 to 0.16 seconds. Cancelling or stopping resets the ramp. Several ducks can be in flight, but a bag is never credited as one bulk transaction.

## Rendering, physics and saving

Sleeping ducks stay in world-space GPU instance batches. Nearby queries, rectangular tool contact checks and deposit intake scans visit only nearby spatial cells. A pool of 96 Rigidbody proxies handles rolling; a separate pool of 16 reusable DOTween deposit flights ensures every queued duck can be shown without creating an object per duck. World intake queues wait for visual capacity instead of silently discarding transactions.

Save version 3 preserves individual duck IDs, multiple installed stations, unused kits and tool upgrades. Inventory remains owned until its animation lands; queued world ducks remain part of the world count until landing. Saving or quitting during a transfer therefore cannot lose or duplicate ducks. The existing save filename is retained.

Old basket, fork and industrial-vacuum ownership maps to Collector, Sweeper and Roller Car. Bag and pickup-speed upgrades survive. Old vacuum range/speed upgrades are refunded. An old portable casket becomes an installed Duck Casket; its stored ducks visibly drain into that station on load and pay once per landing.

## Validation and editing

See `TOOLS_VALIDATION.md` for the current checks. Earlier park performance reports describe the earlier tool set.

`DuckToolsBuilder.UpgradeScene` upgrades tools/settings in the existing saved scene without regenerating its terrain. `DuckPrototypeBuilder.Build` deliberately rebuilds the full starter park and then installs the current tools; use ordinary Play/build commands to preserve authored layout changes.

## Meadow polish

One third of the ducks form 90 seeded clusters across dry meadow, with low mounds. The rest remain scattered. Moved-duck save poses override seeded positions. Grass is baked into 144 cullable mesh patches without per-blade objects or colliders. The authored Meadow details scene group contains these patches and reusable Park Fence prefab instances around the park, with a front entrance gap. The terrain, lake and existing landmarks remain editable.

`DuckPolishBuilder.Build` refreshes the authored detail group and station intake bounds, then validates and builds in the selected project. Use an isolated copy when the main scene has unsaved editor work.

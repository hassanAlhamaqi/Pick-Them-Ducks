# Particle systems to make and where to assign them

The hooks below are implemented. Empty slots are optional and stay quiet. Existing deposit bursts are already assigned; the new slots are ready for your particle prefabs/child systems.

## Shared one-shot effects

Open `Assets/Sandouq/Park/Prefabs/Duck Game Systems.prefab`. Assign **ParticleSystem prefab assets** to the matching fields on **DuckParticleFeedback**. The component places and pools them automatically; do not manually scatter them through the scene.

| Effect to make | Inspector field | Spawn location and trigger | Suggested look |
|---|---|---|---|
| Duck pickup | Pickup | Picked duck's world position, hands/vacuum | Tiny yellow sparkle/rubber pop, 0.2–0.35 s |
| Roller collection complete | Roller Complete | Player carry target after the last front visual reaches the bag | Short yellow ring/confetti puff, 0.35 s |
| Pile collapse | Pile Collapse | Removed duck's location when nearby elevated ducks start falling | Low dust puff with a few feathers, 0.5 s |
| Duck throw | Duck Throw | In front of the camera when an inventory duck is launched | Small air puff, 0.2 s |
| Rolling duck settles | Duck Settle | Duck position when a rolling Rigidbody becomes still | Very small dust tick, 0.2 s |
| Lake splash | Water Splash | Water entry point of a rolling/thrown duck | Blue-white splash and ripple, 0.6–0.9 s |
| Jump takeoff | Jump | Player feet on a successful jump | Short ground dust ring, 0.3 s |
| Landing | Land | Player feet after an airborne landing | Wider dust ring, 0.4 s |
| Bag becomes full | Bag Full | Player carry target on the transition to full | Small gold burst, 0.4 s |
| Casket installation | Casket Placed | Newly installed casket position | Ground dust plus gold sparkle, 0.7 s |
| Placement rejection | Placement Blocked | Rejected installation position | Small red/orange crossed sparkle, 0.3 s |

For these one-shots: **Looping off**, **Play On Awake off**, **Stop Action None**. Use World simulation space so particles do not follow a recycled emitter. The shared component enforces non-looping/world-space playback and pools up to four copies per assigned prefab. Minimum Interval controls burst spam; default 0.04 s. Use URP-compatible particle materials. Prefer short lifetimes and modest particle counts (roughly 6–30 per burst).

## Habitat effects

These fields accept ParticleSystem prefab assets and use the same shared pool.

| Effect to make | Prefab/component field | Placement |
|---|---|---|
| Bush break | Habitat Shrubs_01–03 / DuckHabitat > Break Particles | Set Particle Origin to a child near the bush center. Leaves/feathers burst once before the bush disappears. |
| Tree shake | Habitat Tree_01–05 / DuckHabitat > Shake Particles | Set Particle Origin near the canopy or trunk. Leaves drift down when LMB/E shakes the tree. |

Particle Origin is optional; the fallback is one metre above the habitat root. Pooled habitat particles live outside the shrinking bush hierarchy, so the break effect survives the bush hiding itself.

## Continuous tool and vehicle effects

These fields accept **child ParticleSystem instances**, not assets. Add your system inside the listed prefab and drag that child component into the field. Turn **Looping on**, **Play On Awake off**, and **Stop Action None**. The component starts/stops emission; existing particles may finish naturally.

| Effect to make | Where to put it | Inspector reference |
|---|---|---|
| Sweeper floor dust | Duck Sweeper.prefab, child at the floor-contact edge | DuckToolParticles > Active Loop |
| Collector roller dust | Duck Collector.prefab, child along the drum at ground level | DuckToolParticles > Active Loop |
| Vacuum suction | Duck Vacuum.prefab, child at the nozzle | DuckToolParticles > Active Loop |
| Car exhaust | Duck Roller Vehicle.prefab, child behind the engine/rear | DuckRollerVehicle > Exhaust Loop |
| Car wheel/drum dust | Duck Roller Vehicle.prefab, child root containing emitters at wheel/drum contacts | DuckRollerVehicle > Wheel Dust Loop |

Car exhaust runs while occupied; wheel dust runs while moving, including reverse. Tool effects run while that tool is active. World-space dust/exhaust generally looks best. Set the tool enum on DuckToolParticles correctly if you make a new tool prefab.

## Deposit and valuable-duck effects

| Effect to make | Assign here | Trigger/location |
|---|---|---|
| Standard deposit burst | Collection Box.prefab and Duck Casket.prefab / DuckDepositStation > Deposit Effect Prefab | After a successful duck landing/credit, at Effect Origin (or Landing target). Existing Duck Deposit Burst is the default. |
| Valuable duck burst | A Duck Variant asset > Deposit Effect Prefab | Overrides the station effect for that variant; e.g. a larger golden sparkle for Golden Duck. |

Deposit fields accept a **GameObject prefab**, normally with a ParticleSystem on the root. Set Effect Duration on each station long enough for the effect's longest lifetime (default 1 s). These use a separate bounded pool of 16 effect instances per station. On Duck Deposited exposes `(duck ID, coin value)` for your own additional feedback.

## Sound and tuning

Roller completion audio is on Duck Game Systems.prefab: **DuckFeedback > Roller Collection Audio** references the child **Roller collection complete** AudioSource. Replace its AudioClip, volume or pitch there. It plays once per completed front-to-bag batch, after the final visual lands; empty overflow-only pushing does not play it.

Car acceleration, deceleration, braking, reverse-speed limit and steering are on DuckRollerVehicle. S brakes forward motion to zero before accelerating backwards. Releasing controls coasts to a stop. Pile collapse radius, gravity and spread are on DuckPhysics; collapse animation does not require a free Rigidbody slot.

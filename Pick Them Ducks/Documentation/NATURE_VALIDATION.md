# Group deposits, roller pushers and imported nature

Validated on 2026-09-12 with Unity 6000.0.60f1 and the Windows development player. Both roller variants were checked for unchanged inventory after contact and front retention after player movement. The car speed check requires at least 20 m/s. Pickup Amount 3 was checked for three simultaneous deposit flights. The existing timed pickup, per-duck credit/bounce, jump, station intake, acceleration and save-conservation checks also passed. No runtime errors were recorded.

The supplied nature pack replaces active primitive vegetation with its tree and rock variants and adds shrubs, grasses, flowers, fences, lamps and landmark props. Its ground texture is assigned to the meadow TerrainLayer. Original vegetation and Meadow details remain inactive for editing. Scene instances retain their package prefab references.

Screenshots and the passing runtime report are in `NatureValidation/`. The tested Windows build is `Builds/DuckPrototype/PickThemDucks.exe`. Source and packaged gameplay assembly were verified against the isolated tested copy. No new FPS benchmark is claimed.

Rollers retain world-owned ducks in the existing bounded physics pool, shared with rolling/thrown ducks. Releasing the tool returns held ducks to dynamic physics; settled ducks return to instanced rendering. Deposit animation capacity is now 256 pooled flights to accommodate simultaneous upgraded groups while preserving individual arrival transactions.

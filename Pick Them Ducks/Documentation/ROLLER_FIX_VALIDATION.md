# Roller, vehicle and pile validation

Unity 6000.0.60f1 built the authored scene successfully. The final standalone regression passed; evidence is in `RollerFixValidation/Final/PASS.txt` and its screenshots/player log.

Verified continuous intake collects all 12 controlled dense contacts, including ducks underfoot, beyond a single pickup batch. Existing full-bag overflow remains world-owned and pushed.

Verified the completion callback fires exactly once after the last front-to-bag animation, with an assigned completion AudioSource. Empty flushing does not add a completion.

Verified forward acceleration, reduced speed when coasting, and braking into bounded reverse speed. Steering remains separate from mouse look.

Verified removing a supporting duck starts nearby pile gravity settling even with all 96 Rigidbody slots occupied, and the elevated duck actually falls. Tree-supported ducks are excluded until released.

Shared particle feedback and completion audio references are assigned in the authored systems prefab/scene. New particle slots are intentionally empty for custom effects; see `PARTICLE_SYSTEMS_GUIDE.md` for the complete creation and assignment list. Existing deposit particles remain assigned.

The packaged executable is in `Builds/DuckPrototype`. Script files match the isolated Unity project used for this build. Custom particle appearances require review after their assets are assigned.

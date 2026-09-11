# Tool and deposit overhaul validation

Validated with Unity 6000.0.60f1 in an isolated project copy. The open working Unity editor and the player's real save were not used for the test run.

- Editor and Windows development player compiled successfully.
- Logic checks passed: individual deposit transactions, duplicate landing rejection, all four global upgrades, repeated kit purchases, kit consumption, sweeper width upgrades, collector width/speed upgrades, multiple station save/load, legacy migration and world-flight reservations.
- Runtime checks passed: hand hold duration and reset on release; incremental arrival credit; throwing locked during transfers; valid in-flight snapshots; one bounce per landing; invalid placement preserves kits; multiple independent caskets; inventory delivery to a casket; physical duck intake deposits once; floor sweeper contact; roller pickup; vehicle capacity and actual automatic movement; final conservation.
- Final player log contains no runtime errors or exceptions. The placement preview was explicitly exercised after correcting its initialization lifecycle.
- Screenshots of the installed casket, sweeper, collector, roller car and shop were inspected. They are in `ToolValidation/final/`.
- Packaged `Builds/DuckPrototype` uses the same managed assembly as the tested build (SHA-256 checked).

Raw results are `ToolValidation/logic-PASS.txt` and `ToolValidation/final/PASS.txt`. No new FPS claims are made: the previous park benchmark used the prior tool set and did not stress the new deposit queues.

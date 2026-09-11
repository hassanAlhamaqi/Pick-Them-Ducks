# Park prototype validation

Validated with Unity 6000.0.60f1 on 11 September 2026. The Windows development player is available at `Builds/DuckPrototype/PickThemDucks.exe`; keep its adjacent data directory and DLLs with it.

## Passed checks

- Authored scene references, actual shop/casket prefab instances, and expanded TerrainData.
- Economy, capacity limits, upgrade limits, atomic save replacement and backup recovery.
- 1,000, 10,000, 50,000 and 100,000 instance populations; packed removal and relocated spatial cells, including 1,000 ducks stacked in one location.
- Pickup/throw conservation, empty-inventory throws, collecting rolling ducks and returning sleeping bodies to instanced rendering.
- All 96 physics slots occupied: further throws preserve inventory; collecting active ducks releases the pool for reuse.
- Casket purchase, 1,000-duck capacity, overflow protection, stash transactions, physical carrying and deposit, blocked remote deposit and repeated-deposit protection.
- Fork and industrial vacuum purchases, fork push conservation, version-2 save roundtrip and migration from version 1.
- Standalone screenshots of the park, lake, hub and shop inspected. The missing casket label found in the first pass was corrected and verified in the final pass.

The test players used isolated state and did not load or overwrite the player's save.

## Rendering measurements

NVIDIA GeForce RTX 4070 Laptop GPU, Direct3D 11, 1600 × 900 windowed development build, VSync disabled. Each view was warmed up and measured over 600 frames. These are stationary rendering measurements, not a sustained throwing/physics stress benchmark.

| Ducks | Ground FPS | Overview FPS | Ground p95 frame time |
| ---: | ---: | ---: | ---: |
| 1,000 | 698.7 | 685.7 | 2.04 ms |
| 10,000 | 478.7 | 449.7 | 2.66 ms |
| 50,000 | 187.0 | 168.0 | 6.33 ms |
| 100,000 | 113.2 | 93.6 | 9.53 ms |

The 50,000-duck structural query check improved from 292.18 ms to 28.53 ms for 1,000 queries after moving to destination-based spatial cells. These editor timings describe query throughput, not GPU frame time.

Raw results: `ParkValidation/population.csv`, `ParkValidation/benchmark/render-benchmark.csv`, `ParkValidation/structural-checks.txt`, and the PASS files in the `play` and `benchmark` directories. Final visual captures are in `ParkValidation/play/`.

## Integration notes

The existing duck prefab remains the visual source. Environment assets occupy approximately 2.2 MB before Unity import. Terrain and landmark generation only runs through the explicit editor rebuild command; Play and ordinary builds use the saved scene.

Unity refreshed serialized rendering/build settings during the build. Automatic review rejected an attempted rollback of those settings because it could discard uncommitted edits; they were preserved. No rollback workaround was used.

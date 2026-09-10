# Prototype validation — September 10, 2026

**Result:** Unity compilation, Windows development-player build, progression/save checks, spatial population checks, and rendered runtime checks passed. No player save was read or modified during automated validation.

## Rendered benchmark

Windows 11, Unity 6000.0.60f1, Direct3D 11, NVIDIA GeForce RTX 4070 Laptop GPU (8 GB), 1600 × 900, development build, VSync off. Each viewpoint uses 600 measured frames after warming up (five seconds for initial startup, two seconds thereafter). These are local workstation snapshots, not guarantees for other hardware. Other desktop applications were open.

| Stage population | Ground FPS | Overview FPS | Overview median / p95 frame ms | Overview main-thread / GPU ms | Overview instance batches |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1,000 | 869.4 | 536.3 | 0.80 / 6.96 | 1.86 / 1.19 | 4 |
| 10,000 | 472.0 | 420.9 | 1.80 / 6.08 | 2.35 / 2.17 | 49 |
| 50,000 | 168.1 | 137.5 | 6.45 / 10.23 | 7.24 / 7.14 | 196 |
| 100,000 | 95.5 | 85.6 | 10.79 / 16.28 | 11.64 / 12.40 | 400 |

The runtime gameplay checks collect and deposit 134 ducks before each measurement. Overview rendering therefore includes 866 / 9,866 / 49,866 / 99,866 live instances. Ground views conservatively submit 866 / 9,292 / 43,339 / 84,208 instances after tile frustum culling. Population sizes were not reduced to reach a performance target.

All populations had **178 GameObjects including inactive pooled visuals and test infrastructure, 12 active colliders, and zero rigidbodies**. The world population itself has no duck GameObjects or colliders. Unity allocated memory measured approximately 94–106 MB; managed memory approximately 5–17 MB. Those metrics are different memory views and should not be added as a process memory total. Exact per-view values are in [render-benchmark.csv](Benchmark/render-benchmark.csv).

Steady-state GC allocation averaged **0.0 bytes/frame** except 0.1 bytes/frame in the first 1k sample. These stationary warmed samples do not claim zero allocation while updating pickup notices, interacting with shop UI, saving JSON, or taking screenshots. Main-thread recorder values include frame waits; they are not CPU utilization percentages. GPU values are frame timings, not GPU utilization percentages.

An initial hidden-window run was rejected because it produced black captures and skipped meaningful rendering. Final measurements used a visible player window. Screenshot validation now rejects a black capture. Earlier startup samples showed outliers; the final run includes longer initial warm-up. No hardware-independent performance promise is implied.

## Functional checks

- Hands stop at capacity; pickups cannot award money.
- Deposits clear inventory, increment deposited count, and award the configured money once.
- Repeated deposit / depleted-ID pickup cannot duplicate rewards or ducks.
- Basket purchases equip the tool, increase capacity, and collect a patch of four aimed ducks.
- Vacuum purchases are funded by repeated deposits; the vacuum collects a five-duck cone batch at distance and rejects an out-of-range query.
- Purchased/locked/unaffordable equipment, smaller-tool switching, upgrade limits and range increases are checked.
- Completion occurs after the last deposit, not while the last duck is still carried.
- Save roundtrip, atomic replacement, corrupt-primary backup recovery, and invalid/duplicate depleted IDs are checked using isolated files.
- Spatial queries, swap removal, duplicate removal rejection and deterministic reconstruction are checked at 1k/10k/50k/100k.
- Runtime conservation is checked: remaining + carried + deposited equals original population.
- The player log contains no runtime errors or exceptions from the final run.

The original structural test measured 100k setup at about 60 ms and 1,000 nearby queries at about 69 ms. The supplied duck contains 276 vertices in one render part. See [population-checks.csv](population-checks.csv). Those editor CPU checks are separate from the rendered frame benchmarks.

## Visual verification

Inspected captures of the field, collection box, shop, and close hover target. Corrected world-space sign scale, HUD contrast on the bright ground, and first-person tool scale. Verified the supplied duck appearance, URP hover outline, readable shop state/costs, and tens of thousands of visible instances.

- [Ground view](Benchmark/prototype-ground.png)
- [Full-field overview](Benchmark/prototype-overview.png)
- [Collection area](Benchmark/prototype-hub.png)
- [Hover outline](Benchmark/prototype-hover.png)
- [Shop](Benchmark/prototype-shop.png)

Automated runtime tests call the same collection/economy methods used by gameplay. They are not a full manual mouse/keyboard usability playtest or an assessment of long-session balance. Broader hardware coverage, extended rapid-collection profiling, and player feedback remain useful next steps.

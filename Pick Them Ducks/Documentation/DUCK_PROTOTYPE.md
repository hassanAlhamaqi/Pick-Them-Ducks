# Pick Them Ducks — one-stage prototype

Open `Assets/Sandouq/Scenes/DuckPrototype.unity` and press Play. The menu **Sandouq → Ducks → Open playable prototype** opens it too. The stage is assembled at runtime; an empty-looking editor scene with the Duck Prototype root is expected. The original SampleScene is preserved. The prototype is the only enabled build scene.

A tested Windows development build is also available locally at `Builds/DuckPrototype/PickThemDucks.exe`. Launch it normally to play. Keep the executable alongside its `_Data` directory and DLLs. The build directory is excluded by the project's existing Git ignore rules.

## Play

| Input | Action |
| --- | --- |
| WASD / mouse | Walk / look |
| Shift | Run |
| Hold left mouse | Pick, scoop, or vacuum |
| E near collection box | Deposit carried ducks and earn money |
| E near shop | Open shop |
| 1 / 2 / 3 | Equip purchased hands / basket / vacuum |
| Escape | Pause/menu, release cursor, resume |
| F3 | Lightweight frame and rendering diagnostics |

Look down slightly at nearby ducks. The white outline identifies the target. Start with ten spaces. Two full trips pay for the $20 basket, which scoops up to four nearby ducks per action and carries 35. The $100 vacuum holds 65 and continuously collects up to five ducks per pulse from its forward cone. Upgrades improve capacity, pickup rate, vacuum range and vacuum rate. Deposits alone award money. Switching to equipment too small for the current load is blocked until depositing.

The collection box and shop are on the starting apron. The central ivory lane leads back to them. There is one finite field, no respawning, and no other stages or duck variants. Completion requires every duck to reach the box.

## Tuning

Select `Assets/Sandouq/Settings/DuckPrototypeSettings.asset`:

- Population defaults to 50,000, with deterministic layout and configurable seed, spacing, size and tile width.
- Money per deposited duck, equipment costs, capacities, rates, batch sizes and ranges are editable.
- Upgrade costs, cost multipliers, effects and maximum levels are editable.
- Movement speed, mouse sensitivity, effect pool size and autosave interval are editable.
- Tool and upgrade arrays follow the enum order shown in the inspector. Additional tools require an enum entry and collection behavior; numeric tuning is data-driven.

Existing saves retain their original population total and seed. To test a changed total, choose **Start a fresh field** from the pause menu and confirm with a second click. This resets progression and uses the current settings. Prices/equipment tuning otherwise apply immediately to a new Play session.

## Existing assets and project integration

- Unity 6000.0.60f1, Universal RP 17.0.4, Input System 1.14.2, uGUI 2.0.0; both input backends were enabled in the existing project.
- The single supplied `Sandouq/Prefabs/LowPoly Duck.prefab` supplies all duck meshes, transforms, textures and materials. No replacement duck model/material/variant is created. Its material has GPU instancing enabled. Runtime copies retain its appearance.
- DOTween drives preallocated pickup-flight tweens and a box response on deposit. Generated short audio clips provide squeak/chime feedback with no external assets.
- The switches-game's mouse-look and DOTween interaction patterns informed the implementation. Its scripts depend on unrelated facility gameplay, so they were not copied wholesale.
- QuickOutline is installed, but its bundled built-in-pipeline shader is not used for the population. A small URP outline shader draws only the hovered duck; no Outline component is attached to every duck.

## Performance architecture

`DuckPopulationManager` separates rendering and interactions. It stores positions, yaw, tile IDs and packed slots in arrays. Tile batches contain at most 961 ducks (default 256), below DrawMeshInstanced's 1023 limit. Matrices are generated once. CPU frustum culling submits only visible tiles; removing a duck swaps the last live entry into its slot, with no complete-buffer rebuild. Tile bounds remain conservative after removals.

Pickup queries visit only spatial tiles within tool range. World ducks have **zero GameObjects, colliders, rigidbodies or per-duck Update methods**. A single disabled prefab template plus a fixed pool (32 by default) represent animated ducks. Pool saturation drops surplus animation only, never inventory transactions. Depositing displays at most 12 representative ducks and does not accumulate objects in the box.

No per-frame allocations are intentional in population rendering or spatial queries. Cached DOTween flights are restarted rather than created for every pickup. The HUD refreshes text at 10 Hz; UI strings and notices allocate, and JSON snapshots allocate at save boundaries. The system does not claim zero allocations across the entire application. Population memory and draw submission still grow linearly; GPU vertex cost scales with visible source-mesh complexity. Hundreds of thousands may eventually benefit from indirect rendering or an impostor/LOD path, guided by profiling rather than GameObject expansion.

## Saves

`duck-stage-v1.json` under Unity's `Application.persistentDataPath` stores version, money, carried count, deposited count, owned/current tools, four upgrade levels, original total/seed and depleted duck IDs. Positions reconstruct deterministically. The invariant is **remaining + carried + deposited = stage total**.

Pickup changes autosave every eight seconds; deposits, purchases and equipment changes save immediately. Application pause, focus loss, disable and quit also save. A temporary file is atomically replaced and the preceding valid file is kept as `.bak`. Loading validates quantities, indices and upgrade levels; corrupt primary saves fall back to the backup. A forced process termination between autosaves can lose the most recent unsaved pickups. No network service is used.

## Validation tools

**Sandouq → Ducks → Run logic and population checks** exercises inventory limits, duplicate prevention, deposit economy, purchases, tool switching, upgrade limits, completion, save/recovery and spatial query/removal/reload at 1k/10k/50k/100k. Results go to `Temp/DuckValidation` and do not touch player saves.

For an actual Windows development player, invoke the editor method `Sandouq.Ducks.Editor.DuckPrototypeBuilder.BuildValidationPlayer`. Run the resulting executable in a **visible window** with `--duck-benchmark <absolute output folder>` to measure all four populations from ground and overview viewpoints. It runs isolated gameplay checks, writes CSV frame/CPU/GPU/allocation/memory/object statistics, and captures 50k screenshots. A black capture fails validation; a hidden window can skip graphics work and produce invalid frame rates. GPU timings are explicitly reported as unavailable when the graphics driver does not expose them. Ordinary play never installs this runner.

See `VALIDATION.md` for measured results and limitations from this implementation session.

# Lake and prefab update validation

Validated in Unity 6000.0.60f1 using an isolated project copy, then copied authored assets back to the main project.

- Logic/population checks and Windows development build succeeded.
- Full player interaction regression passed: pickup timing, grouped deposits, accelerated deposits/throws, caskets, roller front visuals and bag collection, jumping, habitat interactions and save conservation.
- New checks passed: editable tree marker references, 150 authored stepping platforms, landing on a platform, lake ducks distributed away from bridges, physical duck settling at water level, and UI outline color #F9C001.
- Player run produced PASS.txt and no errors.txt. Lake and journal screenshots were visually inspected.
- Hover uses the duck outline shader on habitat meshes. LMB and E call the same habitat interaction method. These input paths were code-reviewed; automated tests call the interaction method directly.
- Purchase refresh no longer toggles every button disabled before restoring its affordable state.

Evidence: `LakeValidation/`. Editing instructions: `PREFAB_EDITING.md`.

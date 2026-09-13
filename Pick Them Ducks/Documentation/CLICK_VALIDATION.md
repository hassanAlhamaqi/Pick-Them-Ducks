# Click pickup and HUD validation

Unity 6000.0.60f1 logic checks and Windows development build succeeded. The final player regression run passed and produced no error log.

Verified: immediate click pickup, rapid-click cooldown rejection, pickup after cooldown, no repeat from holding, circular cooldown Image, pile ducks falling after collection, authored tree/bush interaction colliders, and the existing deposits, tools, throws, lake and save-conservation checks. Physics fixtures select available ducks independently of earlier stress-test activity.

Visually inspected `ClickValidation/Complete/pickup-cooldown.png` and `tool-shop.png`: retained yellow cooldown color, circular crosshair, cream/amber/brown palette and hidden user-disabled HUD cards. All 85 existing HUD GameObject active states were compared against the user's edited prefab and preserved. The saved scene was not replaced.

Pile collapse prioritizes recent pickups, wakes a small group immediately, and limits background physics work. Throwing pauses background collapse activation to reserve pool capacity.

Editing instructions: `PREFAB_EDITING.md`. Evidence: `ClickValidation/Complete/`.

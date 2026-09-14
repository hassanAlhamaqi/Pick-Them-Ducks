# Roller vehicle and variant deposits

Unity 6000.0.60f1 build, logic checks and full player regression passed. No runtime errors were logged.

Verified: unlocking delivers a parked vehicle; nearby entry, auto-forward driving, steering independent of mouse look, safe exit and parked collision; both rollers push world-owned overflow while full; front collection still credits only available bag space; parked position/yaw survive save/load; variant currency is independent of deposited count, credits once and works for inventory, legacy casket and world deposits; deposit event receives ID/value and effect prefabs are assigned.

Existing pickup cooldown, pile collapse, deposits, throwing, lake, UI and conservation checks passed. The deposit fixture was restored to Hands so its expected batch size and acceleration baseline remained valid.

Screenshots and PASS evidence: VehicleValidation/Final. The parked car screenshot was inspected. Deposit effects are hooked and pooled; the single captured landing frame does not demonstrate the complete particle animation.

Editing and controls: PREFAB_EDITING.md. Variant ranges configure reward/effect behavior; mesh/appearance variants are not enabled by this change. Golden Duck (5 coins) is an unassigned example asset; existing ducks remain 1 coin.

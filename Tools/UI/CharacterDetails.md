# Basic attack preview

The stat button and popup were removed at the user’s request. The Play attack button remains in the lobby character details. It plays the selected character’s original attack sprites/timing, then restores idle. Changing characters or closing the panel cancels playback. The button supports Korean, English and Japanese.

## Inline three-stat summary

Character details now displays HP, Attack and normal-attack cooldown inline. White values show current enhancement values; green signed values show the next enhancement gain. No stat popup is used. The baseline cooldowns come from SkillData (200 sword, 201 mage). Labels support Korean/English/Japanese.

UpgradeManager caches effective basic-attack cooldown, movement-lock duration and animation speed on initialization, permanent enhancement refresh and cooldown-upgrade changes. Lock duration = original lock × (effective basic cooldown / original cooldown). AnimationController reads the cached values on attack. SkillSpawner uses a per-attack coroutine to release movement from the cached duration, ignores early fixed clip release events, and cancels the timer on dash/disable. These timing calculations do not run in Update or per attack.

Checks: BasicAttackTimingVerification.txt and SummaryStatsRuntimeVerification.txt. Reference: 0.75s cooldown / 0.70s lock, with 20% cooldown reduction -> 0.60s / 0.56s; repeated refresh is idempotent. Permanent speed and runtime cooldown modifiers combine. Zero-duration mage lock remains zero.

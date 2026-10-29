> 2026-10-27 사용자 요청으로 밸런스 패치 전체 취소. 아래 내용은 적용되지 않는 과거 기록입니다.

# Five-sheet balance pass, 2026-10-27

Run `python3 Tools/Balance/full_balance.py` from the repository root. Source data,
assumptions and editable tuning choices are retained in `full_balance_parameters.json`,
`full_balance_baseline_2026-10-27.json` and `full_balance_supporting_data_2026-10-27.json`.
Results are written to `full_balance_results.json`; the exact online edit audit is
`full_balance_write_plan.json`. Backups are linked in `BACKUPS_2026-10-27.md`.

## Diagnosis and accepted changes

| Finding | Evidence | Change |
|---|---|---|
| Mage damage ignored SkillData.Damage; sword ignored attack-power/critical upgrades | SkillSpawner and BasicAttackInstanceState | One UpgradeManager calculation for both: sheet damage × (1 + attack-power bonus / 100) × character-skill modifier; critical uses player critical chance and multiplier; round once |
| Waves could require up to 17,280 enemy HP cleared per second | Exact row counts, float32 growth, complete slime split trees | Keep row order/wave count; reduce batch sizes and spread intervals. Peak workload becomes 108 HP/s |
| A complete slime tree had 648 HP but awarded 75 XP | Root HP 60 plus 14 descendants at floor(60/1.4); 15 × 5 XP | Keep normal monster combat stats; slime uses blue orbs, 15 × 30 = 450 XP |
| Octopus rewarded disproportionate XP | HP 15; 50 XP each | Blue orb 50 → 30 XP, still above green 5 |
| Reward budget changed with reduced spawns | Full stage 787,100 → 47,340 XP | Refit all 61 level costs: level 30 costs 10,400 cumulatively; cap costs 33,905 |
| Boss HP must fit the revised common damage/upgrade pipeline | Reachable build estimates and seeded attack simulations | First boss 1,000 → 1,400 HP; final boss root 3,000 → 2,200 HP. All attack stats remain positive |

Basic attack sheet values: sword 8 damage / 0.9 seconds; mage 4 / 0.45 seconds.
Both have about 9.33 expected single-target DPS at base critical stats before
accuracy and animation timing. Sword retains area coverage and more player HP;
mage retains ranged targeting. Magic flight lifetime is 1.5 seconds at speed 12.
The sheet's MovementLockTime is 0.12 for sword. SkillSpawner now releases
movement after that duration (scaled by attack animation speed), rather than
requiring the end-of-animation event and keeping the player locked while
retriggering attacks.

## Progression checks

| Collection | Before first boss | Before final boss |
|---|---:|---:|
| 50% | 23 | 43 |
| 65% | 26 | 51 |
| 80% | 30 | 58 |
| 85% | 30 | 61 |
| 100% | 33 | 61 |

The run begins at level 0, matching LevelManager. Counts above exclude optional
boss summons. Scheduled primary spawns fall from 17,709 to 1,330; split children
are accounted for in health and XP rather than silently counted as primaries.
Normal-wave wait time changes from 930 to 1,066 seconds, plus boss fight time,
spawn batching and time spent in upgrade menus.

## Combat evidence and limits

Seed 20261027, 500 trials per character/build/boss comparison. Ordinary first-boss
TTK medians are 12.81 seconds (sword) and 13.34 (mage); final split-family medians
are 30.24 and 32.55 seconds. These values omit boss invulnerable/movement phases,
incoming damage, healing, extra boss summons and deaths. They are damage-budget
estimates, not evidence of a completed live run. Optimized first-boss builds can
clear much faster; deliberately low-damage builds can take several minutes.

Builds are checked against available level-up choices (ordinary: 29/30 choices
at first boss, 50/61 at final boss; optimized first boss: exactly 30). Passive
data are read from the frozen existing game CSVs and are not changed in this pass.
Single-target passive estimates reflect distinct-target lightning selection and
the 180-degrees/second shuriken orbit duty cycle; DOT/fusion bonuses are excluded.
Projectile hits and exposure are assumptions exposed as hit probability/uptime.

Enemy Defense is not currently applied by EnemyCombat.TakeDamage, so no fictional
mitigation is added and Defense cells are left unchanged. SkillData.Critical Damage
retains its schema but is not read by the shared basic-attack critical calculation.
MovementLockTime is now read and applied by SkillSpawner for basic attack locks.

## Acceptance, follow-up and rollback

- Numerical guards: row XP matches the live sheet formulas; normal monster HP,
  speed, attack and defense unchanged; no BombSlime added; no zero-damage monsters;
  positive monotonic level costs; 30/61 progression targets; ordinary modeled TTK
  in 10–45 / 20–90-second bands and character median gap below 15%.
- During live play, record boss-spawn level, collected XP, deaths, boss HP at
  death and selected upgrades for both characters. A model/observed discrepancy
  must be resolved before changing the curve again.
- Restore individual original values from the write plan, or the complete native
  backup files. Use the code snapshot if rolling back the shared damage fix.

The prior single-system `level_progression.py` inputs/results are historical;
the five-sheet model is the current accepted configuration.

## Applied and verified in Unity

All 103 changed cells were read back from the original five online workbooks.
Stage1 row XP formulas matched every modeled block; OnlyLevel matched all 61
accepted costs. Existing GetLevelData/GetExpData/GetSkillData/GetStageData/
GetEnemyData components downloaded the published CSVs. All five local CSVs
were compared against accepted values.

Unity compiled the updated scripts. A short live Stage1 check verified 61 level
rows, blue orb XP 30, slime drop ID 301, first/final boss root HP 1400/2200,
and initial wave count 6. At +90% attack power and forced 100% critical chance,
sword damage was 23 and mage damage 11, matching the model's final rounding.
The movement lock was initially active and then released with the configured
0.12-second duration. Temporary test stat overrides were restored and Play mode
was stopped. No live run to either boss was performed in this pass.

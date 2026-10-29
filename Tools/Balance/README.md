> 밸런스 패치 전체 복원 완료. 백업 당시 데이터로 복구했으며 아래 분석은 과거 기록입니다.

# Stage 1 level progression

> Current accepted five-sheet balance: see [FULL_BALANCE.md](FULL_BALANCE.md). The single-level analysis below is historical.

Run `python3 Tools/Balance/level_progression.py` from the project root.

`level_progression.json` records the online Level, Stage Master, Enemy Master and
EXP Orb tables read on 2026-10-27. `level_progression_results.json` records the
accepted curve, before/after scenarios and checks. Refresh these inputs before
reusing the model after spreadsheet edits.

## Model contract

- LevelManager starts at level 0. Each row purchases one level; there are 61 rows.
- Every stage row starts from its own `enemyPerSpawn`, matching the spreadsheet.
  The previous runtime carried counts between rows with the same enemy ID. This
  mismatch was fixed in StageManager, rather than compensated for in XP costs.
- After each growth interval, the next wave's count is multiplied using Unity
  float32 arithmetic and floored. Rows execute sequentially.
- Enemy deaths yield one orb. A normal slime with 3 binary splits yields
  `1 + 2 + 4 + 8 = 15` orbs when the entire tree is killed.
- Stage 1 has 258,550 XP before the first boss, 783,000 before the final boss,
  and 787,100 including both boss trees. Optional boss summons are excluded.
- Collection ratio combines enemies killed, split trees completed and XP picked
  up. Timing/lag, survivability, builds, skill choice and combat are not modeled.
- The curve retains a 50-XP first level and 61-level cap. Positive requirements
  rise smoothly to a plateau; no late-game cost cliff is introduced.

## Accepted targets

- At 80% pre-first-boss XP collection: level 30 (cost 206,060 XP).
- At 85% pre-final-boss XP collection: level 61 (cost 665,255 XP).
- At 65% collection: level 27 before first boss / level 50 before final boss.
- At 100% collection: level 33 before first boss / cap before final boss.

The original cap cost was 5,874,450 XP. All planned per-row XP totals agree with
Stage1 column I. The simulator additionally checks fixed hand calculations,
rounding, monotonicity, zero XP and the cap boundary.

## Follow-up and rollback

Observe actual collected XP at both boss spawns on ordinary runs. If players
collect 80–85% but fall outside levels 29–31 before the first boss or cannot
reach the cap before the final boss, check spawn/kill/pickup fidelity first.
These numerical checks do not establish that either boss is beatable.

For rollback, `baseline_costs` retains all original Level!A2:A62 values.
OnlyLevel mirrors Level column A; keep it as the one-column game export.

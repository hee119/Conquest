"""Deterministic XP-budget model. Run: python3 Tools/Balance/level_progression.py

This models collected XP, not combat, survival, or collection timing within a wave.
Inputs and accepted results are retained for future stage/level tuning.
"""
import bisect
import json
import math
from pathlib import Path
import struct

ROOT = Path(__file__).resolve().parent


def f32(value):
    return struct.unpack('f', struct.pack('f', value))[0]


def waves(config, carry=False):
    orbs = {r[0]: r[2] for r in config['orbs']}
    drops = {r[0]: orbs[r[7]] for r in config['enemies']}
    counts, result = {}, []
    for row_index, row in enumerate(config['stage_rows'], 2):
        stage, enemy, count, interval, initial, multiplier, step, kind, expected = row
        per = counts.setdefault(enemy, initial) if carry else initial
        total = 0
        for wave in range(1, count + 1):
            reward = drops[enemy]
            if enemy == 106:
                reward *= 15  # 1 + 2 + 4 + 8 ordinary slime deaths.
            if enemy == 151:
                reward = drops[151] * 7 + drops[106] * 15 * 8
            amount = reward * (1 if kind == 'Boss' else per)
            result.append(dict(row=row_index, wave=wave, enemy=enemy, kind=kind,
                               count=1 if kind == 'Boss' else per, xp=amount,
                               interval=interval))
            total += amount
            if kind != 'Boss' and step > 0 and wave % step == 0:
                per = math.floor(f32(f32(per) * f32(multiplier)))
                counts[enemy] = per
        if not carry:
            assert total == expected, (row_index, total, expected)
    return result


def thresholds(costs):
    values, total = [], 0
    for cost in costs:
        total += cost
        values.append(total)
    return values


def fit_curve(cap_xp, first_boss_xp, cap_level, early_xp=1050,
              tau_min=13, tau_max=23, power_min=2, power_max=3.5):
    # Smooth monotonic costs approaching a plateau, without a late-game cost cliff.
    best = None
    for p_i in range(round(power_min * 100), round(power_max * 100) + 1):
        power = p_i / 100
        for tau_i in range(round(tau_min * 10), round(tau_max * 10) + 1):
            tau = tau_i / 10
            shape = [1 - math.exp(-((level - 1) / tau) ** power)
                     for level in range(1, cap_level + 1)]
            # Leave one 5-XP rounding unit per level below the collection budget.
            scale = (cap_xp - cap_level * 5 - cap_level * 50) / sum(shape)
            costs = [max(5, round((50 + scale * value) / 5) * 5) for value in shape]
            if sum(costs[:30]) > first_boss_xp:
                continue
            score = abs(sum(costs[:6]) - early_xp) / early_xp + abs(sum(costs[:30]) - first_boss_xp) / first_boss_xp * 5
            if best is None or score < best[0]:
                best = score, costs, dict(power=power, tau=tau, scale=scale)
    return best[1:]


def main():
    config = json.loads((ROOT / 'level_progression.json').read_text())
    planned = waves(config)
    carry_baseline = waves(config, carry=True)
    bosses = [i for i, wave in enumerate(planned) if wave['kind'] == 'Boss']
    pre_first = sum(w['xp'] for w in planned[:bosses[0]])
    pre_final = sum(w['xp'] for w in planned[:bosses[-1]])
    target = config['targets']
    costs, params = fit_curve(round(pre_final * target['pre_final_boss_collection']),
                             pre_first * target['pre_first_boss_collection'], target['cap_level'])
    old = thresholds(config['baseline_costs'])
    new = thresholds(costs)
    assert planned[0]['xp'] == 40 * 5 == 200
    assert sum(w['xp'] for w in planned[:15]) == 3000
    assert sum(w['xp'] for w in planned[15:30]) == 30000
    assert costs[0] == 50 and all(a <= b for a,b in zip(costs,costs[1:]))
    assert len(costs) == target['cap_level'] and all(c > 0 and c % 5 == 0 for c in costs)
    assert bisect.bisect_right(new, 0) == 0
    assert bisect.bisect_right(new, sum(costs) * 2) == len(costs)
    assert bisect.bisect_right(new, pre_first * .8) in range(29,32)
    assert bisect.bisect_right(new, pre_final * .85) == len(costs)
    scenarios = []
    for collection in [.0, .65, .8, .85, 1.0]:
        for label, xp in [('before_first_boss', pre_first), ('before_final_boss', pre_final), ('full_stage', sum(w['xp'] for w in planned))]:
            scenarios.append(dict(collection=collection, milestone=label, xp=round(xp*collection),
                                  old_level=bisect.bisect_right(old, xp*collection),
                                  new_level=bisect.bisect_right(new, xp*collection)))
    budget = dict(pre_first_boss=pre_first, pre_final_boss=pre_final,
                  whole_stage=sum(w['xp'] for w in planned),
                  current_runtime_carry_xp=sum(w['xp'] for w in carry_baseline),
                  baseline_cap=old[-1], proposed_cap=new[-1], proposed_level_30=new[29])
    report = dict(model_version=config['model_version'], budget=budget, curve=params,
                  costs=costs, scenarios=scenarios,
                  checks='passed: row XP matches sheet, float32 spawn growth, split trees, milestones, monotonicity, cap/zero boundaries',
                  assumptions=config['assumptions'],
                  writes=dict(level_range='Level!A2:A62', export_range='OnlyLevel!A2:A62'))
    (ROOT / 'level_progression_results.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()

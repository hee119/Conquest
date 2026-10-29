"""Offline economy forecast. Match settings.json to Resources/CharacterProgressionSettings."""
import json
import random
import statistics
from pathlib import Path

BASE = Path(__file__).parent
cfg = json.loads((BASE / 'settings.json').read_text())

def cost(level):
    return (cfg['firstGoldCost'] + cfg['goldCostPerLevel'] * level,
            cfg['firstFragmentCost'] + cfg['fragmentCostPerLevel'] * level)

def simulate(win_rate, trials=2000):
    first, maximum = [], []
    for seed in range(trials):
        rng = random.Random(seed)
        gold = level = runs = 0
        shards = [0] * cfg['ownedCharacters']
        while level < cfg['maxLevel'] and runs < 10000:
            win = rng.random() < win_rate
            runs += 1
            gold += cfg['victoryGold' if win else 'defeatGold']
            shards[rng.randrange(len(shards))] += cfg['victoryFragments' if win else 'defeatFragments']
            gc, sc = cost(level)
            while level < cfg['maxLevel'] and gold >= gc and shards[0] >= sc:
                gold -= gc
                shards[0] -= sc
                level += 1
                if level == 1:
                    first.append(runs)
                assert gold >= 0 and min(shards) >= 0
                gc, sc = cost(level)
        assert level == cfg['maxLevel'], 'Unreachable maximum'
        maximum.append(runs)
    def stats(values):
        values.sort()
        return dict(mean=round(statistics.mean(values), 2), p50=values[len(values)//2],
                    p95=values[int(.95*(len(values)-1))])
    return dict(first_upgrade=stats(first), max_one_character=stats(maximum))

assert cost(0) == (200, 3)
assert cost(9) == (1100, 21)
assert sum(cost(i)[0] for i in range(10)) == 6500
assert sum(cost(i)[1] for i in range(10)) == 120
result = {name: simulate(rate) for name, rate in [('all_defeats', 0), ('half_wins', .5), ('all_wins', 1)]}
for item in result.values():
    assert 1 <= item['first_upgrade']['mean'] <= 4
    assert item['first_upgrade']['p95'] <= 6
(BASE / 'results.json').write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps(result, indent=2))

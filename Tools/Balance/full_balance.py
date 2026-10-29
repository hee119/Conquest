"""Reproduce the 2026-10-27 five-sheet balance pass without writing to Google.

Run: python3 Tools/Balance/full_balance.py
Outputs baseline/proposal tables, an exact write plan and seeded TTK scenarios.
"""
import bisect
import copy
import csv
import json
import math
from pathlib import Path
import random
import statistics

from level_progression import f32, fit_curve, thresholds

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parent.parent


def read_table(snapshot, key, title):
    sheet = next(s for s in snapshot['source_snapshots'][key]['sheets'] if s['title'] == title)
    rows = []
    for row in sheet['data'][0]['rowData']:
        values = []
        for cell in row['values']:
            effective = cell.get('effectiveValue') or {}
            values.append(effective.get('numberValue', effective.get('stringValue')))
        if any(v is not None for v in values):
            rows.append(values)
    return rows


def records(rows):
    return {int(row[0]): dict(zip(rows[0], row)) for row in rows[1:]}


def schedule(stage, enemies, orbs):
    rows, waves, elapsed = [], [], 0
    for index, row in enumerate(stage[1:], 2):
        _, enemy_id, n, interval, per, multiplier, step, kind = row[:8]
        enemy = enemies[enemy_id]
        reward = orbs[enemy['Drop Exp Orb Id']]['exp']
        health = enemy['Hp']
        if enemy_id == 106:
            health = health + 14 * math.floor(health / 1.4)
            reward *= 15
        elif enemy_id == 151:
            hp = enemy['Hp']
            child_hp = enemies[106]['Hp'] + 14 * math.floor(enemies[106]['Hp'] / 1.4)
            child_reward = orbs[enemies[106]['Drop Exp Orb Id']]['exp'] * 15
            health = hp + 2 * math.floor(hp / 1.4) + 4 * math.floor(hp / 1.4 ** 2) + 8 * child_hp
            reward = 7 * reward + 8 * child_reward
        block_xp, block_health = 0, 0
        for wave in range(1, n + 1):
            count = 1 if kind == 'Boss' else per
            data = dict(row=index, enemy=enemy_id, wave=wave, kind=kind,
                        count=count, hp=count*health, xp=count*reward,
                        time=elapsed, interval=interval)
            data['hp_per_second'] = count * health / interval if interval else 0
            waves.append(data)
            block_xp += data['xp']
            block_health += data['hp']
            elapsed += interval
            if kind != 'Boss' and step > 0 and wave % step == 0:
                per = math.floor(f32(f32(per) * f32(multiplier)))
        rows.append(dict(row=index, enemy=enemy_id, xp=block_xp, hp=block_health))
    boss_indexes = [i for i,w in enumerate(waves) if w['kind'] == 'Boss']
    budget = dict(before_first_boss=sum(w['xp'] for w in waves[:boss_indexes[0]]),
                  before_final_boss=sum(w['xp'] for w in waves[:boss_indexes[-1]]),
                  total=sum(w['xp'] for w in waves),
                  primary_spawns=sum(w['count'] for w in waves),
                  peak_hp_per_second=max(w['hp_per_second'] for w in waves),
                  normal_wave_seconds=elapsed)
    return rows, waves, budget


def basic_damage(skill, player, build, phase, critical, old=False):
    if old and player['Id'] == 1:
        return skill['Damage']  # Old sword ignored player attack/critical stats.
    if old:
        normal = max(1, math.floor(player['Damage'] * (1 + build['power_bonus'][phase]/100)))
    else:
        normal = skill['Damage'] * (1 + build['power_bonus'][phase]/100)
    factor = player['Critical Damage'] * (1 + build['crit_damage_bonus'][phase]/100)
    return max(1, round(normal * (factor if critical else 1)))


def passive_dps(upgrades, player, build, phase):
    tier = build['upgrade_tiers'][phase] - 1
    power = math.floor(player['Damage'] * (1 + build['power_bonus'][phase]/100))
    crit_chance = min(1, player['Critical Chance']/100 * (1+build['crit_chance_bonus'][phase]/100))
    crit_damage = player['Critical Damage'] * (1+build['crit_damage_bonus'][phase]/100)
    total = 0
    for upgrade_id in build['upgrades'][phase]:
        upgrade = upgrades[upgrade_id]
        normal = math.floor(upgrade['Damage'][tier] * (1 + power/100))
        damage = normal * (1-crit_chance) + math.floor(normal * (2+crit_damage/100))*crit_chance
        cooldown = upgrade['CoolTime'][tier] * (1-build['passive_cdr'][phase]/100)
        count = upgrade['Count'][tier]
        if upgrade_id == 400:
            total += damage / cooldown  # DetectUpgrade chooses distinct targets.
        elif upgrade_id == 405:
            duration = upgrade['OrbitTime'][tier]
            total += damage * count * 0.5 * duration / (duration + 1 + cooldown)
        else:
            total += damage * count / cooldown
    return total * build['passive_uptime']


def run_boss(hp, skill, player, build, phase, upgrades, seed, trials, old=False):
    rng = random.Random(seed)
    cooldown = skill['CoolTime'] * (1-build['skill_cdr'][phase]/100)
    chance = min(1, player['Critical Chance']/100 * (1+build['crit_chance_bonus'][phase]/100))
    support = passive_dps(upgrades, player, build, phase)
    times = []
    for _ in range(trials):
        remaining, now = hp, 0.0
        while remaining > 0 and now < 600:
            if rng.random() < build['basic_hit_probability']:
                remaining -= basic_damage(skill, player, build, phase, rng.random()<chance, old)
            if remaining <= 0:
                break
            # Integrate explicitly modeled continuous expected support between casts.
            delta = min(cooldown, remaining / support) if support else cooldown
            remaining -= support * delta
            now += delta
        times.append(now)
    times.sort()
    return dict(median_seconds=round(statistics.median(times),2),
                p10_seconds=round(times[int(.1*(len(times)-1))],2),
                p90_seconds=round(times[int(.9*(len(times)-1))],2),
                support_dps=round(support,2), trials=trials)


def main():
    snapshot = json.loads((ROOT/'full_balance_baseline_2026-10-27.json').read_text())
    params = json.loads((ROOT/'full_balance_parameters.json').read_text())
    supporting = json.loads((ROOT/'full_balance_supporting_data_2026-10-27.json').read_text())
    skill_table = read_table(snapshot, 'skill', 'Skilldata')
    enemy_table = read_table(snapshot, 'enemy', 'Master')
    orb_table = read_table(snapshot, 'orb', 'Exp Ord Info')
    stage = read_table(snapshot, 'stage', 'Stage1')
    old_costs = [r[0] for r in read_table(snapshot, 'level', 'Level')[1:]]
    old_skills, old_enemies, old_orbs = map(records, [skill_table, enemy_table, orb_table])
    skills, enemies, orbs = map(copy.deepcopy, [old_skills, old_enemies, old_orbs])
    stage_new = copy.deepcopy(stage)
    for key, change in params['skills'].items(): skills[int(key)].update(change)
    for key, change in params['enemies'].items(): enemies[int(key)].update(change)
    for key, amount in params['orbs'].items(): orbs[int(key)]['exp'] = amount
    for change in params['stage_rows']:
        row = stage_new[change['row']-1]
        row[3:7] = [change['interval'],change['count'],change['multiplier'],change['step']]
    old_rows, old_waves, old_budget = schedule(stage, old_enemies, old_orbs)
    new_rows, new_waves, new_budget = schedule(stage_new, enemies, orbs)
    growth = params['growth']
    costs, curve = fit_curve(round(new_budget['before_final_boss'] * growth['cap_collection']),
                            new_budget['before_first_boss']*growth['first_boss_collection'],
                            growth['cap_level'], early_xp=growth['early_six_level_xp'],
                            tau_min=10, tau_max=65, power_min=1, power_max=4)
    for row, metrics in zip(stage_new[1:],new_rows): row[8] = metrics['xp']
    players = {}
    for row in supporting['players']:
        parsed={k:(v if k=='Name' else float(v)) for k,v in row.items()}
        players[int(parsed['Id'])]=parsed
    upgrades = {}
    for row in supporting['upgrades']:
        if not row.get('Id'): continue
        upgrades[int(row['Id'])]={key:[float(v.strip()) for v in row[key].split(',')]
                                 for key in ['Damage','Count','CoolTime','OrbitTime']}
    stat_columns = dict(power_bonus='AttackPower',skill_cdr='SkillCooldown',
                        passive_cdr='PassiveCooldown',crit_chance_bonus='CritChance',
                        crit_damage_bonus='CritDamage')
    stat_costs = {}
    for key,column in stat_columns.items():
        source=next(r[column] for r in supporting['stats'] if r.get(column))
        stat_costs[key]={float(v.strip()):i+1 for i,v in enumerate(source.split(','))}
    build_budgets=[]
    for name,build in params['builds'].items():
        for phase,available in enumerate([30,61]):
            picks=len(build['upgrades'][phase])*build['upgrade_tiers'][phase]
            picks+=sum(stat_costs[key].get(build[key][phase],0) for key in stat_columns)
            assert picks<=available,(name,phase,picks,available)
            build_budgets.append(dict(build=name,phase=phase,damage_picks=picks,available=available))
    # Model-fidelity and adjacent-system boundary checks.
    assert old_budget['total'] == 787100
    assert old_rows[0]['xp']==3000 and old_waves[0]['hp']==200
    assert 60+14*math.floor(60/1.4)==648
    assert all(r[1]!=107 for r in stage_new[1:])
    assert all(enemies[i]['Damage']>0 for i in [101,106,108,150,151])
    assert all(old_enemies[i][key]==enemies[i][key] for i in [101,106,108]
               for key in ['Hp','Speed','Damage','Defense'])
    assert all(a<=b for a,b in zip(costs,costs[1:])) and len(costs)==61
    assert bisect.bisect_right(thresholds(costs), new_budget['before_first_boss']*.8)==30
    assert bisect.bisect_right(thresholds(costs), new_budget['before_final_boss']*.85)==61
    assert new_budget['peak_hp_per_second'] <= 110
    empty_build=dict(power_bonus=[0,0],crit_damage_bonus=[0,0])
    assert basic_damage(skills[200], players[1], empty_build, 0, False)==8
    assert basic_damage(skills[201], players[2], empty_build, 0, False)==4
    # Seeded ordinary/novice/optimized boss scenarios, not a survival verdict.
    boss_results=[]
    for phase, boss_id in enumerate([150,151]):
        old_hp=next(w['hp'] for w in old_waves if w['enemy']==boss_id)
        hp=next(w['hp'] for w in new_waves if w['enemy']==boss_id)
        for character in [1,2]:
            skill_id=200 if character==1 else 201
            for name, build in params['builds'].items():
                seed=params['seed']+phase*100+character
                old=run_boss(old_hp,old_skills[skill_id],players[character],build,phase,upgrades,seed,params['trials_per_boss_scenario'],old=True)
                new=run_boss(hp,skills[skill_id],players[character],build,phase,upgrades,seed,params['trials_per_boss_scenario'])
                boss_results.append(dict(boss=boss_id,character=character,build=name,before=old,after=new))
    for phase,boss_id in enumerate([150,151]):
        ordinary=[r['after']['median_seconds'] for r in boss_results if r['boss']==boss_id and r['build']=='ordinary']
        assert max(ordinary)/min(ordinary) < 1.15, ('character gap',boss_id,ordinary)
        low,high=(10,45) if phase==0 else (20,90)
        assert all(low<=value<=high for value in ordinary),('TTK target',boss_id,ordinary)
    scenarios=[]
    for collection in [0,.5,.65,.8,.85,1]:
        scenarios.append(dict(collection=collection,
            first_boss_before=bisect.bisect_right(thresholds(old_costs),old_budget['before_first_boss']*collection),
            first_boss_after=bisect.bisect_right(thresholds(costs),new_budget['before_first_boss']*collection),
            final_boss_before=bisect.bisect_right(thresholds(old_costs),old_budget['before_final_boss']*collection),
            final_boss_after=bisect.bisect_right(thresholds(costs),new_budget['before_final_boss']*collection)))
    report=dict(model_version=params['model_version'],seed=params['seed'],
        checks='PASS: baseline/hand cases, nonzero damage, normal monster combat stats preserved, wave load, progression, rounding, cap.',
        before_budget=old_budget,after_budget=new_budget,curve=curve,costs=costs,
        level_30_xp=sum(costs[:30]),cap_xp=sum(costs),scenarios=scenarios,
        boss_ttk=boss_results,build_budgets=build_budgets,
        row_comparison=[dict(before=a,after=b) for a,b in zip(old_rows,new_rows)],
        proposed=dict(skills=skills,enemies=enemies,orbs=orbs,stage=stage_new),
        assumptions=params['assumptions'],backups=snapshot['backups'])
    (ROOT/'full_balance_results.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps({k:report[k] for k in ['checks','before_budget','after_budget','level_30_xp','cap_xp','scenarios','boss_ttk']},ensure_ascii=False,indent=2))


if __name__=='__main__': main()

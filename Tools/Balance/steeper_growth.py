"""Run: python3 Tools/Balance/steeper_growth.py. Frozen live inputs + quadratic costs."""
import json,bisect,itertools,copy
from pathlib import Path
from full_balance import schedule, records, read_table
ROOT=Path(__file__).resolve().parent
p=json.loads((ROOT/'steeper_growth_parameters.json').read_text())
b=json.loads((ROOT/'full_balance_baseline_2026-10-27.json').read_text())
f=json.loads((ROOT/'full_balance_results.json').read_text())
stage=copy.deepcopy(p['stage_before'])
e=records(read_table(b,'enemy','Master'));o=records(read_table(b,'orb','Exp Ord Info'))
for k,v in f['proposed']['enemies'].items(): e[int(k)].update(v)
for k,v in f['proposed']['orbs'].items(): o[int(k)]['exp']=v if isinstance(v,(int,float)) else v['exp']
# Runtime SplitEnemy: two children, three generations, reward on every death.
assert sum(2**i for i in range(4))==15
assert 15*o[301]['exp']==450
_,_,before=schedule(stage,e,o)
for change in p['changes']:
 stage[change['row']-1][3]=change['interval'];stage[change['row']-1][4]=change['count']
rows,waves,after=schedule(stage,e,o)
costs=[round((p['base']+p['linear']*n+p['quadratic']*n*n)/5)*5 for n in range(p['cap'])]
t=list(itertools.accumulate(costs))
assert len(costs)==61 and all(a<b for a,b in zip(costs,costs[1:]))
assert bisect.bisect_right(t,after['before_first_boss']*.8)==30
assert bisect.bisect_right(t,after['before_final_boss']*.85)==61
assert after['peak_hp_per_second']<=108
scenarios=[{'collection':v,'first_boss_level':bisect.bisect_right(t,after['before_first_boss']*v),'final_boss_level':bisect.bisect_right(t,after['before_final_boss']*v)} for v in [.5,.65,.8,.85,1]]
sweep=[{'quadratic':a,'total_xp':sum(round((p['base']+p['linear']*n+a*n*n)/5)*5 for n in range(61))} for a in [.65,.7,.75,.8,.85]]
r={'costs':costs,'before':before,'after':after,'level30_xp':t[29],'cap_xp':t[-1],'scenarios':scenarios,'rows':rows,'stage':stage,'sweep':sweep,'checks':'PASS: split rewards, monotonicity, first boss 30, final boss cap, peak workload <=108; assumes all scheduled split families killed; no survival verdict'}
(ROOT/'steeper_growth_results.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({k:v for k,v in r.items() if k not in ['stage','rows','costs']},ensure_ascii=False,indent=2))

#!/usr/bin/env python3
"""Validate 78 unique priorities and their reproducible scoring rubric."""
import csv
from pathlib import Path
root=Path(__file__).resolve().parent.parent
def read(path):
    with (root/path).open(encoding='utf-8-sig',newline='') as f:
        return list(csv.DictReader(f))
source=read(Path('Data/Migration_78.csv'))
ranking=read(Path('Data/Reconstruction_Priority_78.csv'))
assert len(source)==len(ranking)==78
assert {r['module_id'] for r in source}=={r['module_id'] for r in ranking}
assert len({r['module_id'] for r in ranking})==78
assert [int(r['rank']) for r in ranking]==list(range(1,79))
for r in ranking:
    value=int(r['gameplay_value'])
    effort=int(r['engineering_effort'])
    risk=int(r['compatibility_risk'])
    reuse=int(r['existing_source_reuse'])
    assert all(1<=n<=5 for n in (value,effort,risk))
    assert 0<=reuse<=3
    assert 5*value+3*reuse-2*effort-2*risk==int(r['priority_score'])
    assert r['phase'] in {'EARLY_AFTER_M0','MID','LATER','DEFER'}
    assert r['verification']=='HISTORICAL_UNVERIFIED'
    assert r['reason']
keys=[(-int(r['priority_score']),-int(r['gameplay_value']),r['module_id'].lower()) for r in ranking]
assert keys==sorted(keys), 'Ranking is out of score order'
print('PASS: 78 historical reconstruction priorities, score formula and sorted ranks')

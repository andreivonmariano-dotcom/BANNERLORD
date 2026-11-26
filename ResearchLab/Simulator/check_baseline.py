import json,os,pathlib
r=pathlib.Path(os.environ['LOCALAPPDATA'])/'BannerlordResearchLab/Simulator/ValidationRuns'
a=json.loads((r/'D002-observer/baseline.json').read_text(encoding='utf8'));b=json.loads((r/'733f0ebf3705f264a53174c7907484310c331f0a/fixture.json').read_text(encoding='utf8'))
for key in ['heroes','clans','kingdoms','forts','parties','relations','wars','source_sha','ticks_per_hour','days_per_year','days_per_season']:
 assert a[key]==b[key],key
assert a['clocks'][0]==b['clocks'][0]
print('D001 BASELINE EXACT CONTENT EQUALITY: 12 comparisons')
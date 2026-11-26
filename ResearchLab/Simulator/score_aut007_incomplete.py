import hashlib,json,os
from pathlib import Path
r=Path(os.environ["LOCALAPPDATA"])/"BannerlordResearchLab"
o=r/"Simulator/ValidationRuns/AUT007"
def sha(b):return hashlib.sha256(b).hexdigest()
p=o/"food_forecast.json"
assert sha(p.read_bytes())=="e2b962f25fcbc54e15e76223e0426260f39378033ec1f622ce32b64fcf6e0bf4"
m=json.loads(p.read_text())
matches=[]
for p in (r/"D002-observations-v032").glob("session-*.log"):
    raw=p.read_bytes()
    lines=raw.splitlines(keepends=True)
    if sha(b"".join(lines[:4]))==m["prior_log_sha"]:matches.append(raw)
assert len(matches)==1
raw=matches[0];rows=[]
for line in raw.decode().splitlines():
    pairs=[t.split("=",1) for t in line.split()]
    assert all(len(t)==2 for t in pairs) and len({t[0] for t in pairs})==len(pairs)
    rows.append(dict(pairs))
assert [int(x["seq"]) for x in rows]==list(range(1,len(rows)+1))
assert all(int(a["tick"])<=int(b["tick"]) for a,b in zip(rows,rows[1:]))
future=rows[4:]
assert not any(x.get("reason")=="food_event" for x in future)
assert not any(x.get("kind") in {"error","recruit_cart","unit_recruited_unattributed","recruit_event","companion_added"} for x in future)
party=[x for x in future if x.get("kind")=="party"]
assert all((x["healthy"],x["wounded"],x["food"],x["sea"],x["hero_gold"])==("3","0","0.95","False","960") for x in party)
durations=[10145,10133,10016,10033,10069,10046,10029,10033]
assert len(durations)==m["maximum_supervised_pulses"]
res={"experiment":m["experiment"],"forecast_commit":"d280a3994b1b6cf60a7dfac143c24e78b830fb55","forecast_sha":"e2b962f25fcbc54e15e76223e0426260f39378033ec1f622ce32b64fcf6e0bf4","prior_log_sha":m["prior_log_sha"],"log_snapshot_sha":sha(raw),"status":"INCOMPLETE_NO_ENDPOINT_WITHIN_PULSE_LIMIT","predicted_food":m["predicted_food"],"observed_endpoint_food":None,"absolute_error":None,"last_observed_food":float(party[-1]["food"]),"last_observed_tick":int(party[-1]["tick"]),"baseline_tick":m["baseline_tick"],"observed_elapsed_ticks":int(party[-1]["tick"])-m["baseline_tick"],"post_baseline_party_samples":len(party),"hourly_events":sum(x.get("reason")=="hourly_event" for x in future),"food_events":0,"completed_pulses":len(durations),"local_control_sequence_ms":durations,"rejected_before_input":1,"notes":["local sequence durations include control overhead, not measured unpaused time; 10-second requested cap was not proven exact","focus rejection before input; resumed after fresh paused frame","no new forecast accuracy claim; endpoint not reached","initial prisoners checked via UI; complete perk/provider context unverified"]}
with (o/"food_result.json").open("x",encoding="utf8") as f:json.dump(res,f,indent=2,sort_keys=True)
with (o/"food-private.log").open("xb") as f:f.write(raw)
print(json.dumps(res));print("RESULT_SHA",sha((o/"food_result.json").read_bytes()))
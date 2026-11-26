import hashlib,json,os,struct
from pathlib import Path
r=Path(os.environ["LOCALAPPDATA"])/"BannerlordResearchLab"
o=r/"Simulator/ValidationRuns/AUT003"
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def f32(x):return struct.unpack("<f",struct.pack("<f",float(x)))[0]
p=o/"food_forecast.json"
assert sha(p)=="071d855b4506d30212dd08b1a98188984b22f8237b7501728d66fdc57c8c1f8c"
m=json.loads(p.read_text())
log=[]
for l in (r/"D002-observations").glob("session-*.log"):
    raw=l.read_bytes()
    lines=raw.splitlines(keepends=True)
    if hashlib.sha256(b"".join(lines[:17])).hexdigest()==m["prior_log_sha"]:
        log.append((l,raw,lines))
assert len(log)==1
l,raw,lines=log[0]
rows=[dict(t.split("=",1) for t in line.decode().split()) for line in lines]
assert [int(x["seq"]) for x in rows]==list(range(1,len(rows)+1))
assert all(int(a["tick"])<=int(b["tick"]) for a,b in zip(rows,rows[1:]))
future=rows[17:]
target=next(x for x in future if x.get("reason")=="food_event")
idx=rows.index(target)
previous=[x for x in rows[17:idx] if x.get("kind")=="party"]
assert previous
assert all(x["healthy"]=="2" and x["wounded"]=="0" and x["sea"]=="False" and f32(x["food"])==f32(.95) for x in previous)
assert target["healthy"]=="2" and target["wounded"]=="0" and target["sea"]=="False"
actual=f32(target["food"])
assert abs(actual-m["predicted_food"])<=m["tolerance"]
result={"experiment":m["experiment"],"forecast_sha":sha(p),"preexisting_log_prefix_sha":m["prior_log_sha"],"observed_log_snapshot_sha":hashlib.sha256(raw).hexdigest(),"observed_seq":int(target["seq"]),"observed_tick":int(target["tick"]),"previous_party_samples":len(previous),"observed_food":actual,"predicted_food":m["predicted_food"],"absolute_error":abs(actual-m["predicted_food"]),"status":"CONDITIONAL_NUMERIC_MATCH","limitations":["prisoner and complete perk context not independently observed","first event rather than fixed time horizon","full active model chain not verified","subsequent wait overshoot; no extra events scored"]}
with (o/"food_result.json").open("x",encoding="utf8") as f:json.dump(result,f,sort_keys=True,indent=2)
with (o/"food-observation-private.log").open("xb") as f:f.write(raw)
print(json.dumps(result));print("RESULT_SHA",sha(o/"food_result.json"));print("FINAL_ROWS",len(rows),"LAST_TICK",rows[-1]["tick"])
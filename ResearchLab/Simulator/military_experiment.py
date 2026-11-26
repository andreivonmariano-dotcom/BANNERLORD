"""Counterfactual regular-troop experiment, NOT native battle prediction.
Actual roster and XML levels; supplied survival, HP, morale, advantage; Python RNG.
"""
import argparse,collections,copy,json,random,statistics
from pathlib import Path
from military_economy_rules import base_power,base_hit,morale_factor,xp_reward
def contingent(w,id):
    p=[r for r in w["nodes"]["parties"].values() if r["id"]==id]
    if len(p)!=1 or p[0]["roster"] is None:raise ValueError("party identity/roster")
    rows=[];excluded=0
    for r in p[0]["roster"]:
        healthy=r["count"]-r["wounded"]
        if r["hero"] is not None:excluded+=healthy;continue
        if not healthy:continue
        src=w["character_sources"].get(r["character"],[]);levels={v["level"] for v in src}
        if len(levels)!=1:raise ValueError("unknown/conflicting source level "+r["character"])
        level=levels.pop()
        rows.append({"id":r["character"],"level":level,"count":healthy,"power":base_power(level),"sources":src})
    if not rows:raise ValueError("empty regular contingent")
    return {"party":id,"rows":rows,"healthy_regulars":sum(r["count"] for r in rows),"excluded_healthy_heroes":excluded,"base_power":sum(r["count"]*r["power"] for r in rows)}
def duel(a,b,seed,survival,hp,morale=(50,50),cap=20000):
    if not 0<=survival<=1 or type(hp) is not int or hp<=0 or cap<1:raise ValueError("assumptions")
    rng=random.Random(seed)
    units=[[r for r in c["rows"] for _ in range(r["count"])] for c in (a,b)]
    orig=list(map(len,units));wounds=[0,0];deaths=[0,0];xp=[0,0];hits=0
    # Deliberate experimental policy: alternate equal hit opportunities.
    while all(units) and hits<cap:
        side=hits%2;other=1-side
        s=rng.choice(units[side]);j=rng.randrange(len(units[other]));v=units[other][j]
        damage=base_hit(s["power"],v["power"],1,rng.random())*(1+morale_factor(morale[side],morale[other]))
        if rng.randrange(hp)<damage:
            units[other].pop(j)
            if rng.random()<survival:wounds[other]+=1
            else:deaths[other]+=1
            xp[side]+=xp_reward(v["level"]) # proxy only, not actual distributed XP
        hits+=1
    fit=list(map(len,units))
    if any(fit[i]+wounds[i]+deaths[i]!=orig[i] for i in (0,1)):raise ValueError("conservation")
    return {"winner":0 if fit[0] and not fit[1] else 1 if fit[1] and not fit[0] else None,"fit":fit,"wounded":wounds,"dead":deaths,"xp_proxy":xp,"hits":hits}
def batch(w,a,b,runs,seed,survival,hp):
    if not 1<=runs<=1000 or a==b:raise ValueError("scenario")
    x,y=contingent(w,a),contingent(w,b)
    results=[duel(x,y,seed+i,survival,hp) for i in range(runs)]
    return {"scope":"synthetic alternating-hit regular-only experiment; no native outcome validation","source_sha":w["source_sha"],"contingents":[x,y],"assumptions":{"survival":survival,"hp":hp,"morale":[50,50],"advantage":1,"seed":seed,"runs":runs},"wins":[sum(r["winner"]==i for r in results) for i in (0,1)],"unresolved":sum(r["winner"] is None for r in results),"mean_fit":[statistics.mean(r["fit"][i] for r in results) for i in (0,1)],"mean_wounds":[statistics.mean(r["wounded"][i] for r in results) for i in (0,1)],"mean_deaths":[statistics.mean(r["dead"][i] for r in results) for i in (0,1)],"results":results}
def main():
    p=argparse.ArgumentParser();p.add_argument("world",type=Path);p.add_argument("out",type=Path);p.add_argument("a");p.add_argument("b");p.add_argument("--runs",type=int,default=100);p.add_argument("--seed",type=int,default=123);p.add_argument("--survival",type=float,required=True);p.add_argument("--hp",type=int,required=True);a=p.parse_args()
    w=json.loads(a.world.read_text(encoding="utf8"));r=batch(w,a.a,a.b,a.runs,a.seed,a.survival,a.hp)
    with a.out.open("x",encoding="utf8") as f:json.dump(r,f,allow_nan=False)
    print(json.dumps({k:v for k,v in r.items() if k not in ("results","contingents")}),"PARTIES",[(c["party"],c["healthy_regulars"],c["excluded_healthy_heroes"],c["base_power"]) for c in r["contingents"]])
if __name__=="__main__":main()

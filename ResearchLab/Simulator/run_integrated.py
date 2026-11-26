"""Run paired seeded research scenarios and verify deterministic checkpoints."""
import argparse,json,statistics,time
from pathlib import Path
from integrated_lab import Integrated,inputs
def batch(config,n):
    rows=[]
    for seed in range(n):
        for skill in (56,86):
            s=Integrated(config,seed,skill);s.advance(42)
            resumed=Integrated.restore(s.snapshot());resumed.advance(42)
            s.advance(42)
            if resumed.snapshot()!=s.snapshot():raise AssertionError("reload divergence")
            rows.append(resumed.report())
    summary={}
    for skill in (56,86):
        group=[r for r in rows if r["skill"]==skill]
        total=sum(r["successes"]+r["failures"] for r in group)
        summary[str(skill)]={"campaigns":len(group),"completed_tasks":total,"successes":sum(r["successes"] for r in group),
            "failures":sum(r["failures"] for r in group),"mean_losses":statistics.mean(r["lost"] for r in group),
            "mean_rewards":statistics.mean(r["reward"] for r in group),"births":sum(r["births"] for r in group)}
    return {"scope":"synthetic policy; no native autonomous campaign equivalence","config":config,"campaigns":len(rows),"days_per_campaign":84,
            "checkpoint_matches":len(rows),"summary":summary,"runs":rows}
def main():
    p=argparse.ArgumentParser();p.add_argument("phase",choices=("start","resume","verify","batch"));p.add_argument("directory",type=Path);p.add_argument("--db",type=Path);p.add_argument("--seeds",type=int,default=128);a=p.parse_args()
    d=a.directory
    if a.phase=="start":
        d.mkdir(parents=True,exist_ok=False);s=Integrated(inputs(a.db),20261126);s.advance(35);s.save(d/"active.json");print("ACTIVE",s.report())
    elif a.phase=="resume":
        s=Integrated.load(d/"active.json");s.advance(49);s.save(d/"complete.json");print("COMPLETE",s.report())
    elif a.phase=="verify":
        s=Integrated.load(d/"complete.json");original=Integrated(s.config,s.seed,s.skill);original.advance(84)
        assert original.snapshot()==s.snapshot();print("DISK_REPLAY_MATCH")
    else:
        if not 1<=a.seeds<=1000:raise ValueError("seed count")
        r=batch(inputs(a.db),a.seeds)
        with (d/"batch.json").open("x",encoding="utf-8") as f:json.dump(r,f,sort_keys=True,indent=2)
        print("BATCH",json.dumps({k:v for k,v in r.items() if k not in ("runs","config")},sort_keys=True))
if __name__=="__main__":main()

"""D001 imported-state delegation experiment. Native-derived range/timing and
specified consequence paths; outcome inputs and local application policy are
explicit. No live game state, no native RNG/XP/wage/food scheduler.
"""
import copy,json,random,math,hashlib
from pathlib import Path
from delegation_rules import casualties
from native_rules import f32
from campaign_import import totals
from compare_native import SAVE_HASHES

class Experiment:
    def __init__(self,projection):
        self.origin=copy.deepcopy(projection);self.state=copy.deepcopy(projection)
        self.task=None;self.day=0;self.events=[];self.initial=totals(self.state["roster"])
        if self.state["issue"] is not None:raise ValueError("start from idle checkpoint")
        self.validate()
    def validate(self):
        s=self.state;rows=s["roster"]
        if s["source_sha"]!=SAVE_HASHES[0] or s["party"]!="player_party":raise ValueError("unsupported source")
        ids=[r["character"] for r in rows]
        if len(ids)!=len(set(ids)):raise ValueError("duplicate roster")
        for r in rows:
            if type(r["count"]) is not int or type(r["wounded"]) is not int or not 0<=r["wounded"]<=r["count"] or r["count"]<1:raise ValueError("invalid roster")
            if r["hero"] and r["count"]!=1:raise ValueError("invalid hero count")
        if type(s["gold"]) is not int or s["gold"]<0 or not -100<=s["relation"]<=100:raise ValueError("bad resources")
        t=self.task
        pending=0;lost=0
        if t:
            if t["phase"] in ("working","returning"):pending=sum(r["count"] for r in t["sent"])
            elif t["phase"]=="awaiting_return":pending=sum(r["count"] for r in t["survivors"]);lost=t["cached_loss"]
            elif t["phase"]=="returned":lost=t["cached_loss"]
            else:raise ValueError("phase")
        if sum(r["count"] for r in rows)+pending+lost!=sum(self.initial.values()):raise ValueError("personnel conservation")
        for h in s["people"].values():
            present=next((r for r in rows if r["hero"]==h["id"]),None)
            if bool(present)!=(h["party"]=="player_party"):raise ValueError("hero membership")
        return self
    def dispatch(self,actor,allocation,difficulty,seed=None,cached_loss=None):
        self.validate()
        if self.task is not None:raise ValueError("single task experiment")
        if actor=="main_hero" or actor not in self.state["people"]:raise ValueError("invalid actor")
        h=self.state["people"][actor]
        if h["state"]!="Active" or h["party"]!="player_party":raise ValueError("actor unavailable")
        if not 0<=difficulty<=1 or not math.isfinite(difficulty):raise ValueError("difficulty")
        n=8+math.ceil(f32(f32(11)*f32(difficulty)));days=5+math.ceil(f32(f32(7)*f32(difficulty)))
        if any(type(v) is not int or v<=0 for v in allocation.values()) or sum(allocation.values())!=n:raise ValueError("allocation")
        if (seed is None)==(cached_loss is None):raise ValueError("select one outcome source")
        rows={r["character"]:r for r in self.state["roster"]}
        commander=next(r for r in rows.values() if r["hero"]==actor)
        if commander["wounded"]:raise ValueError("wounded commander")
        for k,v in allocation.items():
            if k not in rows or rows[k]["hero"] is not None or rows[k]["count"]-rows[k]["wounded"]<v:raise ValueError("ineligible soldiers")
        skill=max(h["skills"].get("Tactics",0),h["skills"].get("Scouting",0))
        lo,hi=casualties(skill,120,n)
        loss=random.Random(seed).randint(lo,hi) if seed is not None else cached_loss
        if type(loss) is not int or not lo<=loss<=hi:raise ValueError("loss out of range")
        sent=[copy.deepcopy(commander)]
        for k,v in allocation.items():
            r=rows[k];sent.append(dict(r,count=v,wounded=0,xp=0));r["count"]-=v
        commander["count"]=0
        self.state["roster"]=[r for r in rows.values() if r["count"]]
        h.update(state="Disabled",party=None)
        self.task={"actor":actor,"sent":sent,"duration":days,"start":self.day,"risk":f32(max(0,min(.9,(120-skill)*.005))),"cached_loss":loss,
          "loss_source":"observed_input" if seed is None else "python_rng_at_dispatch","phase":"working"}
        self.events.append(["dispatch",self.day]);self.validate()
    def advance(self,days):
        if type(days) is not int or days<0 or days>365:raise ValueError("invalid duration")
        self.day+=days
        if self.task and self.task["phase"]=="working" and self.day>=self.task["start"]+self.task["duration"]-1:self.task["phase"]="returning"
        self.validate()
    def resolve(self,success,losses,wounds,reward):
        self.validate();t=self.task
        if not t or t["phase"]!="returning" or self.day<t["start"]+t["duration"]:raise ValueError("not due or already resolved")
        if type(success) is not bool or type(reward) is not int or reward<0 or (not success and reward):raise ValueError("outcome")
        if any(type(v) is not int or v<0 for v in (*losses.values(),*wounds.values())) or sum(losses.values())!=t["cached_loss"]:raise ValueError("cached loss mismatch")
        sent={r["character"]:r for r in t["sent"]}
        if not set(losses).issubset(sent) or not set(wounds).issubset(sent):raise ValueError("unknown troop")
        survivors=[]
        for k,r in sent.items():
            loss=losses.get(k,0);w=wounds.get(k,0);n=r["count"]-loss
            if loss>r["count"] or w>n or (r["hero"] and loss):raise ValueError("invalid casualties")
            if r["hero"] and not success and w!=1:raise ValueError("failure wounds commander")
            if n:survivors.append(dict(r,count=n,wounded=w))
        s=self.state
        # Direct requested relation delta only; no effective provider model.
        s["relation"]=max(-100,min(100,s["relation"]+(3 if success else -5)))
        s["town"]["prosperity"]+=5 if success else -10
        if success:s["town"]["security"]+=5
        s["gold"]+=reward
        t.update(phase="awaiting_return",success=success,survivors=survivors,reward=reward,requested_renown=1 if success else 0)
        self.events.append(["resolve",self.day]);self.validate()
    def return_party(self,prisoner=False,at_sea=False,in_settlement=False,map_event=False):
        self.validate();t=self.task
        if not t or t["phase"]!="awaiting_return":raise ValueError("not awaiting return")
        if prisoner or (at_sea and not in_settlement) or map_event:return False
        rows={r["character"]:r for r in self.state["roster"]}
        for r in t["survivors"]:
            k=r["character"]
            if k in rows:
                if r["hero"]:raise ValueError("duplicate hero")
                rows[k]["count"]+=r["count"];rows[k]["wounded"]+=r["wounded"]
            else:rows[k]=copy.deepcopy(r)
            if r["hero"]:
                # Aggregate wound slot only: exact native HP not inferred.
                self.state["people"][r["hero"]].update(state="Active",party="player_party",health=None if r["wounded"] else self.origin["people"][r["hero"]]["health"])
        self.state["roster"]=list(rows.values());t["phase"]="returned"
        self.events.append(["return",self.day]);self.validate();return True
    def snapshot(self):
        self.validate();return json.dumps(self.__dict__,sort_keys=True,allow_nan=False)
    @classmethod
    def restore(cls,data):
        x=json.loads(data);s=cls(x["origin"])
        s.state=x["state"];s.task=x["task"];s.day=x["day"];s.events=x["events"];s.initial=x["initial"]
        return s.validate()
    def save(self,path):
        path=Path(path)
        if path.suffix.lower()!=".json":raise ValueError("simulator JSON only")
        with path.open("x",encoding="utf-8") as f:f.write(self.snapshot())

def roster_map(rows):
    return {r["character"]:(r["count"],r["wounded"]) for r in rows}

def replay(base,active,returned,out):
    s=Experiment(base);a=active["issue"];actor=next(r["hero"] for r in a["sent"] if r["hero"])
    allocation={r["character"]:r["count"] for r in a["sent"] if not r["hero"]}
    s.dispatch(actor,allocation,a["difficulty"],cached_loss=a["cached_loss"])
    assert roster_map(s.state["roster"])==roster_map(active["roster"])
    assert roster_map(s.task["sent"])==roster_map(a["sent"])
    assert s.task["risk"]==a["risk"] and s.task["duration"]==6
    s.save(out/"dispatch.json")
    s=Experiment.restore((out/"dispatch.json").read_text())
    s.advance(6);s.resolve(True,{"nord_spear_warrior":2},{"nord_drengr":5},550)
    before=s.snapshot();assert not s.return_party(map_event=True) and s.snapshot()==before
    s.save(out/"awaiting.json");s=Experiment.restore((out/"awaiting.json").read_text())
    assert s.return_party() and roster_map(s.state["roster"])==roster_map(returned["roster"])
    assert s.state["relation"]==returned["relation"]
    s.save(out/"returned.json")
    fail=Experiment(base);fail.dispatch(actor,allocation,a["difficulty"],seed=17);fail.advance(6)
    fail.resolve(False,{"nord_spear_warrior":fail.task["cached_loss"]},{actor:1},0)
    assert fail.return_party();fail.save(out/"failure.json")
    return {"success":totals(s.state["roster"]),"failure":totals(fail.state["roster"]),
       "relation":s.state["relation"],"failure_relation":fail.state["relation"],"failure_commander_wounded":next(r["wounded"] for r in fail.state["roster"] if r["hero"]==actor),
       "gold_residual":returned["gold"]-s.state["gold"],"prosperity_residual":returned["town"]["prosperity"]-s.state["town"]["prosperity"],
       "security_residual":returned["town"]["security"]-s.state["town"]["security"],"scope":"observed success input replay; failure synthetic branch; other daily changes unmodeled"}
if __name__=="__main__":
    import argparse
    p=argparse.ArgumentParser();p.add_argument("projection",type=Path);p.add_argument("out",type=Path);a=p.parse_args()
    a.out.mkdir(exist_ok=False);base,active,returned=[json.loads((a.projection/(n+".json")).read_text()) for n in ("LABD001BASE","LABD001ACTIVE","LABD001RETURN")]
    print(json.dumps(replay(base,active,returned,a.out),sort_keys=True))

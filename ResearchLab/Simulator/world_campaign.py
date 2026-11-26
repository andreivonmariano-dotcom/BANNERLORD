"""Persistent independent world model. Synthetic entities, no game save writes.
Only delegation risk/lifecycle uses prior SIM-001; no native AI or economy claimed.
"""
from dataclasses import dataclass,asdict
from pathlib import Path
import json
import math
from delegation import Campaign

@dataclass
class Character:
    id:str
    clan:str
    skill:int
    party:str
    available:bool=True
@dataclass
class Clan:
    id:str
    gold:int
@dataclass
class Settlement:
    id:str
    owner:str
    food:float
    market_gold:int
    construction_reserve:int
@dataclass
class Party:
    id:str
    clan:str
    settlement:str
    healthy:int
    wounded:int

class World:
    VERSION=1
    def __init__(self):
        self.day=0;self.characters={};self.clans={};self.settlements={};self.parties={};self.tasks={}
    def validate(self):
        if type(self.day) is not int or self.day<0:raise ValueError("bad time")
        def count(v):return type(v) is int and v>=0
        for k,c in self.clans.items():
            if k!=c.id or not count(c.gold):raise ValueError("invalid clan")
        for k,s in self.settlements.items():
            if k!=s.id or s.owner not in self.clans or not all(count(v) for v in (s.market_gold,s.construction_reserve)) or type(s.food) not in (int,float) or not math.isfinite(s.food) or s.food<0:raise ValueError("invalid settlement")
        for k,p in self.parties.items():
            if k!=p.id or p.clan not in self.clans or p.settlement not in self.settlements or not all(count(v) for v in (p.healthy,p.wounded)):raise ValueError("invalid party")
        for k,c in self.characters.items():
            if k!=c.id or c.party not in self.parties or c.clan!=self.parties[c.party].clan or not count(c.skill) or type(c.available) is not bool:raise ValueError("invalid character")
            active=[v for v in self.tasks.values() if v["actor"]==k and v["state"] in ("working","returning")]
            if len(active)>1 or c.available==bool(active):raise ValueError("availability mismatch")
        for k,t in self.tasks.items():
            if t["actor"] not in self.characters or t["party"] not in self.parties or self.characters[t["actor"]].party!=t["party"]:raise ValueError("invalid task")
            if t["state"] not in ("working","returning","completed","failed"):raise ValueError("invalid state")
            if not all(count(t[f]) for f in ("start","duration","sent","lost","returned","reward")) or t["start"]>self.day or t["duration"]<1 or t["sent"]<2:raise ValueError("invalid task accounting")
            if type(t["risk"]) not in (int,float) or not 0<=t["risk"]<=.9:raise ValueError("invalid risk")
            if t["state"] in ("working","returning"):
                if t["lost"] or t["returned"] or t["reward"]:raise ValueError("active task has outcome")
            elif t["lost"]+t["returned"]!=t["sent"] or t["returned"]<1 or (t["state"]=="failed" and t["reward"]):raise ValueError("invalid completed outcome")
        for p in self.parties.values():
            # Aggregate healthy count includes named available characters and one player/leader.
            if p.healthy<1+sum(c.available for c in self.characters.values() if c.party==p.id):raise ValueError("insufficient healthy slots for named characters")
        return self
    def dispatch(self,id,actor,soldiers,duration):
        self.validate()
        if id in self.tasks:raise ValueError("duplicate task")
        c=self.characters[actor];p=self.parties[c.party];clan=self.clans[c.clan]
        if not c.available:raise ValueError("actor busy")
        # Do not accidentally allocate another named available character as an ordinary soldier.
        if type(soldiers) is not int or p.healthy-soldiers-1<1+sum(x.available for x in self.characters.values() if x.party==p.id and x.id!=actor):raise ValueError("insufficient ordinary troops")
        m=Campaign(p.healthy,p.wounded,clan.gold);m.dispatch(c.skill,soldiers,duration)
        p.healthy=m.healthy;c.available=False
        self.tasks[id]={"actor":actor,"party":p.id,"start":self.day,"duration":duration,"sent":m.sent,"risk":m.probability,"state":"working","lost":0,"returned":0,"reward":0}
        self.validate()
    def advance(self,days):
        if type(days) is not int or not 0<=days<=3650:raise ValueError("invalid days")
        self.day+=days
        for t in self.tasks.values():
            if t["state"]=="working" and self.day-t["start"]>=t["duration"]-1:t["state"]="returning"
    def finish(self,id,lost,wounded,reward,success=True,**blockers):
        self.validate();t=self.tasks[id];c=self.characters[t["actor"]];p=self.parties[t["party"]];clan=self.clans[c.clan]
        m=Campaign(p.healthy,p.wounded,clan.gold,day=self.day-t["start"],state=t["state"],duration=t["duration"],sent=t["sent"],commander_available=c.available)
        if not m.finish(lost,wounded,reward,success,**blockers):return False
        p.healthy=m.healthy;p.wounded=m.wounded;clan.gold=m.gold;c.available=True
        t.update(state=m.state,lost=m.lost,returned=m.returned,reward=m.reward_paid)
        self.validate();return True
    def snapshot(self):
        self.validate()
        return json.dumps({"version":self.VERSION,"day":self.day,"characters":{k:asdict(v) for k,v in self.characters.items()},"clans":{k:asdict(v) for k,v in self.clans.items()},"settlements":{k:asdict(v) for k,v in self.settlements.items()},"parties":{k:asdict(v) for k,v in self.parties.items()},"tasks":self.tasks},sort_keys=True)
    @classmethod
    def restore(cls,data):
        x=json.loads(data)
        if x["version"]!=cls.VERSION:raise ValueError("unsupported simulator snapshot")
        w=cls();w.day=x["day"]
        for name,kind in (("characters",Character),("clans",Clan),("settlements",Settlement),("parties",Party)):
            setattr(w,name,{k:kind(**v) for k,v in x[name].items()})
        w.tasks=x["tasks"];return w.validate()

def save_snapshot(world,path):
    """Exclusive-create simulator JSON only, no overwrite. Caller chooses lab directory."""
    path=Path(path)
    if path.suffix.lower()!=".json":raise ValueError("simulator JSON only")
    data=world.snapshot()
    if len(data.encode("utf-8"))>8*1024*1024:raise ValueError("snapshot size limit")
    with path.open("x",encoding="utf-8") as stream:stream.write(data)

def load_snapshot(path):
    path=Path(path)
    if path.suffix.lower()!=".json":raise ValueError("simulator JSON only")
    with path.open("rb") as stream:data=stream.read(8*1024*1024+1)
    if len(data)>8*1024*1024:raise ValueError("snapshot size limit")
    return World.restore(data.decode("utf-8"))

def sample_world():
    w=World();w.clans["house"]=Clan("house",49622)
    w.settlements["holding"]=Settlement("holding","house",100,20000,500)
    w.parties["main"]=Party("main","house","holding",65,0)
    w.characters["retainer_a"]=Character("retainer_a","house",86,"main")
    w.characters["retainer_b"]=Character("retainer_b","house",56,"main")
    return w.validate()
if __name__=="__main__":
    w=sample_world();w.dispatch("first","retainer_a",10,6);w.dispatch("second","retainer_b",10,6)
    w.advance(6);w=World.restore(w.snapshot());w.finish("first",2,5,550);w.finish("second",1,1,0,False)
    print(w.snapshot())

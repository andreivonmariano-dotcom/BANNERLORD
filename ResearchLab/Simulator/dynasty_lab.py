"""Persistent synthetic family experiment. Explicit outcomes; NOT native marriage,
fertility, mortality, succession or provider policy. Uses verified age/calendar
and effective relation bounds. No actual game entities or save writes.
"""
from dataclasses import dataclass,asdict
import json
from pathlib import Path
from native_rules import calendar,age_stage,pregnancy_age_eligible,relation_after_effective_delta

@dataclass
class Person:
    id:str
    clan:str
    birth_day:int
    female:bool
    alive:bool=True
    spouse:str|None=None
    parents:tuple=()

class Dynasty:
    VERSION=1
    def __init__(self,fast=False):
        self.fast=fast;self.day=0;self.people={};self.leaders={};self.relations={};self.pregnancies={};self.events=[]
    def age(self,id):return (self.day-self.people[id].birth_day)/calendar(self.fast)["days_per_year"]
    def ancestors(self,id):
        seen=set();pending=list(self.people[id].parents)
        while pending:
            p=pending.pop()
            if p not in seen:
                seen.add(p);pending.extend(self.people[p].parents)
        return seen
    def validate(self):
        if type(self.day) is not int or self.day<0 or type(self.fast) is not bool:raise ValueError("invalid time")
        for id,p in self.people.items():
            if id!=p.id or p.clan not in self.leaders or type(p.birth_day) is not int or p.birth_day>self.day or type(p.female) is not bool or type(p.alive) is not bool:raise ValueError("invalid person")
            if len(p.parents) not in (0,2) or len(set(p.parents))!=len(p.parents):raise ValueError("invalid parents")
            for parent in p.parents:
                if parent not in self.people or self.people[parent].birth_day>=p.birth_day:raise ValueError("invalid parent reference")
            if p.spouse is not None:
                q=self.people.get(p.spouse)
                if q is None or q.id==id or q.spouse!=id or not p.alive or not q.alive:raise ValueError("invalid spouse link")
        for c,id in self.leaders.items():
            if id is not None and (id not in self.people or self.people[id].clan!=c or not self.people[id].alive or self.age(id)<18):raise ValueError("invalid leader")
        for pair,value in self.relations.items():
            a,b=json.loads(pair)
            if a>=b or a not in self.people or b not in self.people or type(value) is not int or not -100<=value<=100:raise ValueError("invalid relation")
        for id,p in self.pregnancies.items():
            if id not in self.people or p["father"] not in self.people or not self.people[id].alive or not self.people[id].female or type(p["due"]) is not int or p["due"]<0:raise ValueError("invalid pregnancy")
        return self
    def eligible(self,a,b):
        if a==b:return False
        p,q=self.people[a],self.people[b]
        return (p.alive and q.alive and p.spouse is None and q.spouse is None and p.female!=q.female
                and self.age(a)>=18 and self.age(b)>=18 and a not in self.ancestors(b) and b not in self.ancestors(a)
                and not self.ancestors(a)&self.ancestors(b))
    def marry(self,a,b,consent,move=None):
        if consent is not True or not self.eligible(a,b):raise ValueError("ineligible or declined")
        if move is not None:
            if move not in (a,b):raise ValueError("invalid mover")
            mover=self.people[move]
            if self.leaders[mover.clan]==move:raise ValueError("leader transfer requires separate policy")
            mover.clan=self.people[b if move==a else a].clan
        self.people[a].spouse=b;self.people[b].spouse=a
        self.events.append([self.day,"marriage",a,b]);self.validate()
    def change_relation(self,a,b,effective_delta):
        if a==b or a not in self.people or b not in self.people:raise ValueError("invalid pair")
        k=json.dumps(sorted((a,b)))
        self.relations[k]=relation_after_effective_delta(self.relations.get(k,0),effective_delta)
    def conceive(self,mother):
        p=self.people[mother]
        if not p.alive or not p.female or not p.spouse or not self.people[p.spouse].alive or mother in self.pregnancies or not pregnancy_age_eligible(self.age(mother)):raise ValueError("ineligible pregnancy")
        self.pregnancies[mother]={"father":p.spouse,"due":self.day+calendar(self.fast)["pregnancy_days"]}
        self.events.append([self.day,"pregnancy",mother])
    def advance(self,days):
        if type(days) is not int or not 0<=days<=3650:raise ValueError("invalid duration")
        self.day+=days
    def deliver(self,mother,children):
        # Each supplied (ID, female) denotes one surviving child. Empty = supplied loss.
        p=self.pregnancies.get(mother)
        if p is None or self.day<p["due"] or len(children)>2:raise ValueError("not due or invalid outcome")
        ids=[id for id,_ in children]
        if len(ids)!=len(set(ids)) or any(not isinstance(id,str) or not id or id in self.people or type(f) is not bool for id,f in children):raise ValueError("invalid child")
        for id,female in children:
            self.people[id]=Person(id,self.people[mother].clan,self.day,female,parents=(mother,p["father"]))
        del self.pregnancies[mother];self.events.append([self.day,"birth",mother,ids]);self.validate()
    def death(self,id,successor=None):
        p=self.people[id]
        if not p.alive:raise ValueError("already dead")
        if self.leaders[p.clan]==id:
            if successor is not None:
                q=self.people[successor]
                if successor==id or q.clan!=p.clan or not q.alive or self.age(successor)<18:raise ValueError("invalid successor")
            self.leaders[p.clan]=successor
        elif successor is not None:raise ValueError("not a leader")
        if p.spouse:self.people[p.spouse].spouse=None;p.spouse=None
        p.alive=False;self.pregnancies.pop(id,None);self.events.append([self.day,"death",id,successor]);self.validate()
    def snapshot(self):
        self.validate()
        return json.dumps({"version":self.VERSION,"fast":self.fast,"day":self.day,"people":{k:asdict(v) for k,v in self.people.items()},
            "leaders":self.leaders,"relations":self.relations,"pregnancies":self.pregnancies,"events":self.events},sort_keys=True)
    @classmethod
    def restore(cls,data):
        x=json.loads(data)
        if x["version"]!=cls.VERSION:raise ValueError("unsupported snapshot")
        d=cls(x["fast"]);d.day=x["day"]
        d.people={k:Person(**v) for k,v in x["people"].items()}
        for name in ("leaders","relations","pregnancies","events"):setattr(d,name,x[name])
        return d.validate()
    def save(self,path):
        p=Path(path)
        if p.suffix.lower()!=".json":raise ValueError("simulator JSON only")
        with p.open("x",encoding="utf-8") as f:f.write(self.snapshot())
    @classmethod
    def load(cls,path):
        p=Path(path)
        if p.suffix.lower()!=".json":raise ValueError("simulator JSON only")
        with p.open("rb") as f:data=f.read(8*1024*1024+1)
        if len(data)>8*1024*1024:raise ValueError("snapshot too large")
        return cls.restore(data.decode("utf-8"))

def sample():
    d=Dynasty();d.leaders={"a":"father","b":"other"}
    for id,clan,age,female in (("father","a",25,False),("mother","b",22,True),("other","b",40,False)):
        d.people[id]=Person(id,clan,-age*84,female)
    return d.validate()

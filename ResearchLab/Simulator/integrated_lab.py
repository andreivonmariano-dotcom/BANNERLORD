"""SIM-005 integrated research campaign, NOT a native game simulation.
Two explicitly selected source towns, synthetic ownership and resources.
One shared day clock, named retainers, family, settlement market accounts.
All daily orchestration, quotes, casualties, births and logistics are
experimental policy; only identified bounded model arithmetic is native-derived.
"""
import hashlib,json,random
from pathlib import Path
from world_campaign import World,Clan,Settlement,Party,Character
from dynasty_lab import Dynasty,Person
from economy_lab import Account,trade,recipes
from source_world import source_world
from native_rules import market_change

def inputs(database):
    src=source_world(database,"war_sails")
    towns=sorted((k,v["prosperity"]) for k,v in src["settlements"].items() if v["kind"]=="town")[:2]
    r,sha,dbsha=recipes(database)
    recipe=r["brewery:0"]
    if recipe["inputs"]!={"grain":1} or recipe["outputs"]!={"beer":2}:raise ValueError("unexpected recipe")
    return {"towns":towns,"recipe":recipe,"source_sha":src["source_sha256"],"recipe_sha":sha,"database_sha":dbsha}

class Integrated:
    VERSION=1
    def __init__(self,config,seed,skill=86):
        self.config=json.loads(json.dumps(config));self.seed=seed;self.skill=skill
        self.world=World();self.family=Dynasty();self.events=[]
        self.accounts={};self.ledger={"market_adjustment":0,"quest_rewards":0,"grain_created":0,"grain_used":0,"beer_created":0,"beer_consumed":0}
        self.world.clans={"a":Clan("a",49622),"b":Clan("b",10000)}
        self.family.leaders={"a":"father","b":"other"}
        for id,clan,age,female in (("father","a",25,False),("mother","b",22,True),("other","b",40,False),("retainer","a",30,False)):
            self.family.people[id]=Person(id,clan,-age*84,female)
        for i,(id,prosperity) in enumerate(config["towns"]):
            self.world.settlements[id]=Settlement(id,("a","b")[i],100,10000,500)
            self.accounts[id]=Account(10000,{"grain":20} if i==0 else {})
        self.accounts["caravan"]=Account(200,{})
        self.world.parties["main"]=Party("main","a",config["towns"][0][0],65,0)
        self.world.characters["retainer"]=Character("retainer","a",skill,"main")
        self.family.marry("father","mother",True,move="mother");self.family.conceive("mother")
        self.initial_gold=self.total_gold()
        self.events.append([0,"scenario_marriage_and_pregnancy"])
        self.validate()
    def total_gold(self):return sum(c.gold for c in self.world.clans.values())+sum(a.gold for a in self.accounts.values())
    def count(self,good):return sum(a.quantity(good) for a in self.accounts.values())
    def validate(self):
        self.world.validate();self.family.validate()
        if self.world.day!=self.family.day:raise ValueError("clock divergence")
        if set(self.world.clans)!=set(self.family.leaders):raise ValueError("clan mismatch")
        for k,c in self.world.characters.items():
            p=self.family.people[k]
            if p.clan!=c.clan or not p.alive:raise ValueError("character mismatch")
        for k,s in self.world.settlements.items():
            if s.market_gold!=self.accounts[k].gold:raise ValueError("market account mismatch")
        for a in self.accounts.values():a.validate()
        l=self.ledger
        if self.total_gold()!=self.initial_gold+l["market_adjustment"]+l["quest_rewards"]:raise ValueError("gold mismatch")
        if self.count("grain")!=20+l["grain_created"]-l["grain_used"]:raise ValueError("grain mismatch")
        if self.count("beer")!=l["beer_created"]-l["beer_consumed"]:raise ValueError("beer mismatch")
        p=self.world.parties["main"]
        active=sum(t["sent"] for t in self.world.tasks.values() if t["state"] in ("working","returning"))
        if p.healthy+p.wounded+active+sum(t["lost"] for t in self.world.tasks.values())!=65:raise ValueError("personnel mismatch")
        if any(e[0]>self.world.day for e in self.events):raise ValueError("future event")
        return self
    def draw(self,task):
        # Per-task deterministic random stream; Python RNG, not game RNG.
        return random.Random(int.from_bytes(hashlib.sha256(f"{self.seed}:{task}".encode()).digest()[:8],"big"))
    def tick(self):
        self.validate();w=self.world;f=self.family;w.advance(1);f.advance(1);day=w.day;l=self.ledger
        origin,dest=[v[0] for v in self.config["towns"]];a,b,c=(self.accounts[k] for k in (origin,dest,"caravan"))
        # Explicit hypothetical daily flows; no native scheduler claim.
        a.produce({"grain":3});l["grain_created"]+=3
        a.convert(self.config["recipe"],1);l["grain_used"]+=1;l["beer_created"]+=2
        if day%7==0:
            q=min(6,a.quantity("beer"),c.gold//12,b.gold//16)
            if q:
                trade(a,c,"beer",q,12);trade(c,b,"beer",q,16)
                self.events.append([day,"scenario_trade",q])
        if b.quantity("beer"):
            b.consume({"beer":1});l["beer_consumed"]+=1
        for k,prosperity in self.config["towns"]:
            acc=self.accounts[k];delta=market_change(prosperity,acc.gold);acc.gold+=delta;l["market_adjustment"]+=delta
            w.settlements[k].market_gold=acc.gold
        # Synthetic recovery and task outcome policy.
        p=w.parties["main"]
        if p.wounded:p.wounded-=1;p.healthy+=1
        for k,t in sorted(w.tasks.items()):
            if t["state"]=="returning" and day-t["start"]>=t["duration"]:
                rng=self.draw(k);success=rng.random()>=t["risk"];lost=rng.randint(1,2);wounded=rng.randint(0,3);reward=550 if success else 0
                w.finish(k,lost,wounded,reward,success);l["quest_rewards"]+=reward
                self.events.append([day,"scenario_return",k,success,lost,wounded,reward])
        if day%7==1 and w.characters["retainer"].available and p.healthy>=12:
            k=f"task-{day}";w.dispatch(k,"retainer",10,6);self.events.append([day,"dispatch",k])
        if "mother" in f.pregnancies and day>=f.pregnancies["mother"]["due"]:
            f.deliver("mother",[("child",self.draw("birth").random()<.51)])
            self.events.append([day,"scenario_birth","child"])
        self.validate()
    def advance(self,days):
        if type(days) is not int or not 0<=days<=365:raise ValueError("invalid duration")
        for _ in range(days):self.tick()
    def snapshot(self):
        self.validate()
        return json.dumps({"version":self.VERSION,"config":self.config,"seed":self.seed,"skill":self.skill,"world":json.loads(self.world.snapshot()),
          "family":json.loads(self.family.snapshot()),"accounts":{k:a.record() for k,a in self.accounts.items()},"ledger":self.ledger,
          "initial_gold":self.initial_gold,"events":self.events},sort_keys=True,separators=(",",":"))
    @classmethod
    def restore(cls,data):
        x=json.loads(data)
        if x["version"]!=cls.VERSION:raise ValueError("invalid version")
        s=cls(x["config"],x["seed"],x["skill"])
        s.world=World.restore(json.dumps(x["world"]));s.family=Dynasty.restore(json.dumps(x["family"]))
        s.accounts={k:Account(**v) for k,v in x["accounts"].items()}
        s.ledger=x["ledger"];s.initial_gold=x["initial_gold"];s.events=x["events"]
        return s.validate()
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
        return cls.restore(data.decode())
    def report(self):
        self.validate();t=list(self.world.tasks.values())
        return {"seed":self.seed,"skill":self.skill,"day":self.world.day,"successes":sum(x["state"]=="completed" for x in t),
          "failures":sum(x["state"]=="failed" for x in t),"active":sum(x["state"] in ("working","returning") for x in t),
          "lost":sum(x["lost"] for x in t),"healthy":self.world.parties["main"].healthy,"wounded":self.world.parties["main"].wounded,
          "gold":self.total_gold(),"reward":self.ledger["quest_rewards"],"births":int("child" in self.family.people),"ledger":self.ledger.copy(),
          "snapshot_sha":hashlib.sha256(self.snapshot().encode()).hexdigest()}

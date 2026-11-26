"""Source workshop recipes and explicit inventory/transaction experiments.
Conversion cycles, production input, consumption and final trade quotes are
supplied scenario actions, not a reconstructed native economic scheduler.
"""
import collections
import hashlib
import json
import math
import sqlite3
import xml.etree.ElementTree as ET
from pathlib import Path
from native_rules import f32, nonnegative

def base_price_factor(stock,supply,demand,selling=False,transfer_value=0,animal=False,trade_good=True):
    for v in (stock,supply,demand):nonnegative(v)
    if type(transfer_value) is not int or transfer_value<0 or any(type(v) is not bool for v in (selling,animal,trade_good)):
        raise ValueError("invalid price input")
    stock=f32(f32(stock)+f32(transfer_value)) if selling else f32(stock)
    denominator=f32(f32(f32(f32(.1)*f32(supply))+f32(stock*f32(.04)))+2)
    r=f32(math.pow(f32(f32(demand)/denominator),f32(.3 if animal else .6)))
    return max(f32(.1 if trade_good else .8),min(f32(10 if trade_good else 1.3),r))

def recipes(database):
    p=Path(database).resolve()
    with p.open("rb") as f:before=hashlib.file_digest(f,"sha256").hexdigest()
    db=sqlite3.connect(p.as_uri()+"?mode=ro",uri=True)
    result={}
    try:
        source=db.execute("SELECT id,sha256 FROM sources WHERE path=? AND status='parsed'",("Modules/SandBox/ModuleData/spworkshops.xml",)).fetchone()
        if not source:raise ValueError("workshop source missing")
        for oid,xml in db.execute("SELECT object_id,xml FROM definitions WHERE source=? ORDER BY ordinal",(source[0],)):
            for i,e in enumerate(ET.fromstring(xml).findall("Production")):
                def collect(nodes,key,count):
                    out=collections.Counter()
                    for n in nodes:
                        ref=n.get(key)
                        if not ref:raise ValueError("missing recipe reference")
                        # Both source forms denote category references; no runtime registry lookup.
                        ref=ref.removeprefix("ItemCategory.")
                        quantity=int(n.get(count,"1"))
                        if quantity<=0:raise ValueError("invalid quantity")
                        out[ref]+=quantity
                    return dict(out)
                result[f"{oid}:{i}"]={"speed":float(e.get("conversion_speed","0")),
                    "inputs":collect(e.findall("Inputs/Input"),"input_item","input_count"),
                    "outputs":collect(e.findall("Outputs/Output"),"output","output_count")}
    finally:db.close()
    with p.open("rb") as f:
        if hashlib.file_digest(f,"sha256").hexdigest()!=before:raise ValueError("database changed")
    return result,source[1],before

class Account:
    def __init__(self,gold,stock):
        self.gold=gold;self.stock=dict(stock);self.validate()
    def validate(self):
        if type(self.gold) is not int or self.gold<0 or any(type(n) is not int or n<0 for n in self.stock.values()):raise ValueError("invalid account")
    def quantity(self,k):return self.stock.get(k,0)
    def produce(self,goods):
        self.validate()
        if any(type(n) is not int or n<0 for n in goods.values()):raise ValueError("invalid production")
        for k,n in goods.items():self.stock[k]=self.quantity(k)+n
    def consume(self,goods):
        self.validate()
        if any(type(n) is not int or n<0 or self.quantity(k)<n for k,n in goods.items()):raise ValueError("insufficient stock")
        for k,n in goods.items():self.stock[k]-=n
    def convert(self,recipe,cycles):
        if type(cycles) is not int or not 1<=cycles<=10000:raise ValueError("invalid cycles")
        inputs={k:n*cycles for k,n in recipe["inputs"].items()}
        outputs={k:n*cycles for k,n in recipe["outputs"].items()}
        if any(type(n) is not int or n<=0 for n in (*inputs.values(),*outputs.values())):raise ValueError("invalid recipe")
        self.consume(inputs);self.produce(outputs)
    def record(self):return {"gold":self.gold,"stock":dict(self.stock)}

def trade(seller,buyer,good,quantity,unit_price):
    seller.validate();buyer.validate()
    if seller is buyer or type(quantity) is not int or quantity<=0 or type(unit_price) is not int or unit_price<0:raise ValueError("invalid trade")
    cost=quantity*unit_price
    if seller.quantity(good)<quantity or buyer.gold<cost:raise ValueError("insufficient trade resources")
    seller.consume({good:quantity});buyer.produce({good:quantity});buyer.gold-=cost;seller.gold+=cost

def experiment(db):
    r,sha,dbsha=recipes(db)
    a,b,c=Account(1000,{"grain":20}),Account(1000,{}),Account(200,{})
    recipe=r["brewery:0"]
    assert recipe["inputs"]=={"grain":1} and recipe["outputs"]=={"beer":2}
    a.convert(recipe,5)
    trade(a,c,"beer",6,12);trade(c,b,"beer",6,16);b.consume({"beer":4})
    assert sum(x.gold for x in (a,b,c))==2200
    return {"recipe_count":len(r),"workshops":len({k.split(":")[0] for k in r}),"source_sha":sha,"database_sha":dbsha,
        "brewery_recipe":recipe,"origin":a.record(),"destination":b.record(),"caravan":c.record(),
        "price_examples":{"scarce":base_price_factor(10,1,100),"abundant":base_price_factor(1000,100,10)},
        "scope":"explicit cycles/quotes/consumption, not native scheduler"}

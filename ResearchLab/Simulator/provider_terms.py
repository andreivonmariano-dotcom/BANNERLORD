"""Bounded provider-chain arithmetic; not final campaign wages or prediction."""
from dataclasses import dataclass
import struct, math, csv, itertools, unittest, sys
BASE="TaleWorlds.CampaignSystem.GameComponents.DefaultPartyWageModel"
NAVAL="NavalDLC.GameComponents.NavalDLCPartyWageModel"
NAV_HASH="6294d1bd181c4a7366d4bafca4fbf446a1e9523512a33ecb3d818aa19c8ccb45"
BASE_HASH="d7e5c67a04ef83964c882fda2a1f20f0e2afaf2bfe31345ed31d1061c495d462"
def f32(x):
    if type(x) not in (int,float) or not math.isfinite(x): raise ValueError("finite numeric input required")
    try: return struct.unpack("<f",struct.pack("<f",x))[0]
    except OverflowError: raise ValueError("float32 overflow")
@dataclass(frozen=True)
class FactorState:
    base: float
    factors: tuple=()
    def __post_init__(self):
        if not isinstance(self.factors,tuple): raise ValueError("immutable factors")
        object.__setattr__(self,"base",f32(self.base))
        object.__setattr__(self,"factors",tuple(f32(f) for f in self.factors))
    @property
    def sum(self):
        s=0.0
        for f in self.factors:s=f32(s+f)
        return s
    @property
    def result(self):return f32(self.base+f32(self.base*self.sum))
def wage_terms(state,chain,*,sea,caravan):
    """Explicit assumed chain; output terms only; no perk or clamp inference."""
    if type(sea) is not bool or type(caravan) is not bool:raise ValueError("explicit flags")
    if not isinstance(state,FactorState):raise ValueError("factor state")
    if chain!=((BASE,BASE_HASH),(NAVAL,NAV_HASH)):
        return {"status":"UNMODELED_CHAIN","state":None,"final_wage":None}
    added=((-0.2,-0.8) if caravan else (-0.2,)) if sea else ()
    return {"status":"PARTIAL_TERMS_ONLY","state":FactorState(state.base,state.factors+added),"final_wage":None}
CHAIN=((BASE,BASE_HASH),(NAVAL,NAV_HASH))
def compare(path):
    expected=set(itertools.product((0.,1.,100.,-100.,123.5,9876.25),(0.,.125,-.125,.3,-.3),(0,1),(0,1)))
    seen=set(); exact=0; maxerr=0.; failures=[]
    with open(path,newline="") as f:
        reader=csv.DictReader(f)
        if reader.fieldnames!=["base","prior","sea","caravan","sum","result"]:raise ValueError("schema")
        for row in reader:
            b,p=float(row["base"]),float(row["prior"]);s,c=int(row["sea"]),int(row["caravan"])
            k=(b,p,s,c)
            if k not in expected or k in seen:raise ValueError("unexpected/duplicate grid key")
            seen.add(k)
            state=wage_terms(FactorState(b,(p,)),CHAIN,sea=bool(s),caravan=bool(c))["state"]
            native_sum,native_result=f32(float(row["sum"])),f32(float(row["result"]))
            err=abs(native_result-state.result);maxerr=max(err,maxerr)
            if state.sum==native_sum and state.result==native_result:exact+=1
            else:failures.append((k,state.sum,native_sum,state.result,native_result))
    if seen!=expected:raise ValueError("incomplete grid")
    print("GRID",len(seen),"EXACT",exact,"MAX_RESULT_ERROR",maxerr,"MISMATCH",len(failures))
    for v in failures[:5]:print("MISMATCH",v)
    return len(failures)
class Tests(unittest.TestCase):
    def test_land(self):self.assertEqual(wage_terms(FactorState(100),CHAIN,sea=False,caravan=True)["state"].result,100)
    def test_sea(self):self.assertEqual(wage_terms(FactorState(100),CHAIN,sea=True,caravan=False)["state"].result,80)
    def test_caravan(self):self.assertEqual(wage_terms(FactorState(100),CHAIN,sea=True,caravan=True)["state"].result,0)
    def test_prior(self):self.assertEqual(wage_terms(FactorState(100,(.125,)),CHAIN,sea=True,caravan=True)["state"].result,12.5)
    def test_unknown(self):
        for ch in ((),CHAIN[::-1],CHAIN+(("unknown","unknown"),),((BASE,"bad"),(NAVAL,NAV_HASH))):
            self.assertEqual(wage_terms(FactorState(1),ch,sea=True,caravan=False)["status"],"UNMODELED_CHAIN")
    def test_no_final(self):self.assertIsNone(wage_terms(FactorState(1),CHAIN,sea=True,caravan=False)["final_wage"])
    def test_invalid(self):
        with self.assertRaises(ValueError):FactorState(float("nan"))
        with self.assertRaises(ValueError):wage_terms(FactorState(1),CHAIN,sea=1,caravan=False)
    def test_immutable(self):
        state=FactorState(100,(.125,));wage_terms(state,CHAIN,sea=True,caravan=True);self.assertEqual(state.factors,(.125,))
if __name__=="__main__":
    if len(sys.argv)>1:sys.exit(bool(compare(sys.argv[1])))
    unittest.main()

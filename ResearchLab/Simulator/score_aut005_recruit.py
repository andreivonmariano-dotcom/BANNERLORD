"""Score one observed UI recruitment transaction, not arbitrary actorless events."""
from pathlib import Path
import hashlib, json, os
from save_reader import inspect_save
from world_projection import project
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rows(raw):
    result=[]
    for line in raw.decode().splitlines():
        pairs=[x.split("=",1) for x in line.split()]
        assert all(len(p)==2 for p in pairs) and len({p[0] for p in pairs})==len(pairs)
        result.append(dict(pairs))
    assert [int(x["seq"]) for x in result]==list(range(1,len(result)+1))
    assert all(int(a["tick"])<=int(b["tick"]) for a,b in zip(result,result[1:]))
    return result
def score(raw):
    r=rows(raw)
    assert len(r)==17 and r[7]["reason"]=="arrival_event"
    tick="78690884321455"
    assert all(x["tick"]==tick for x in r[7:])
    assert [x["kind"] for x in r[9:]]==["recruit_cart","party","recruit_cart","party","unit_recruited_unattributed","party","unit_recruited_unattributed","party"]
    assert all(r[i]["troop"]=="imperial_recruit" for i in (9,11,13,15))
    assert all(r[i]["count"]=="1" for i in (13,15))
    for i,n in [(10,1),(12,1),(14,2),(16,3)]:
        x=r[i]; assert x["healthy"]==str(n) and x["hero_gold"]=="1000" and x["wounded"]=="0" and x["food"]=="0.95"
        assert x["id"]=="player_party" and x["sea"]=="False" and x["settlement"]=="town_ES1"
    return {"cart_events":2,"unit_events":2,"total_units":2,"event_tick":int(tick),"event_gold":1000,"event_party":3}
def main():
    r=Path(os.environ["LOCALAPPDATA"])/"BannerlordResearchLab"
    o=r/"Simulator/ValidationRuns/AUT005"
    candidates=[p for p in (r/"D002-observations-v032").glob("session-*.log") if sha(p)=="fb09a1f23fb4d158059e7e664b51b14a9ef58c810d5a45766607dceda931bdf9"]
    assert len(candidates)==1
    raw=candidates[0].read_bytes(); result=score(raw)
    p=o/"labd002rthree.sav"; m,cid=inspect_save(p)
    a,aid=inspect_save(r/"ControlCampaigns/D002-R-start/labd002rstart.sav")
    assert cid==aid and m["modules"]==a["modules"]
    assert sha(p)=="e704f14fb4aa0b75e871e3b4bc8f99585405c9cd78196d28a3900326ffbfe652"
    assert (m["healthy"],m["wounded"],m["gold"],m["food"])==(3,0,960,.95)
    assert m["module_versions"]["CUNativeObservation"]=="v0.0.3.2"
    w=project(p,m["sha256"],m)
    party=[x for x in w["nodes"]["parties"].values() if x["id"]=="player_party"]
    assert len(party)==1
    roster=party[0]["roster"];assert {(x["character"],x["count"],x["wounded"]) for x in roster}=={("main_hero",1,0),("imperial_recruit",2,0)} and len(roster)==2
    result.update({"experiment":"AUT005-R01","protocol_commit":"157655299cb8ed753b0765e53f70dc380ed8655c","session_sha":sha(candidates[0]),"checkpoint_sha":sha(p),"post_gold":m["gold"],"payment":result["event_gold"]-m["gold"],"post_roster":roster,"status":"PASS_UI_CORRELATED_RECRUITMENT","limitations":["actorless event is not a general player receipt","no cancellation case","no new blind quote prediction","projected party trade_gold differs from live observer; not used as hero gold"]})
    with (o/"recruit_result.json").open("x",encoding="utf8") as f:json.dump(result,f,sort_keys=True,indent=2)
    with (o/"recruit-private.log").open("xb") as f:f.write(raw)
    print(json.dumps(result));print("RESULT_SHA",sha(o/"recruit_result.json"))
if __name__=="__main__":main()

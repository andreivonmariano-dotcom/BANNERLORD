import argparse,json
from pathlib import Path
from economy_lab import experiment
from dynasty_lab import sample,Dynasty
def main():
    p=argparse.ArgumentParser();p.add_argument("phase",choices=("start","resume","verify"));p.add_argument("directory",type=Path);p.add_argument("--db",type=Path);a=p.parse_args()
    d=a.directory
    if a.phase=="start":
        d.mkdir(parents=True,exist_ok=False)
        print("ECONOMY",json.dumps(experiment(a.db),sort_keys=True))
        x=sample();x.marry("father","mother",True,move="mother");x.change_relation("father","mother",15);x.conceive("mother");x.advance(35);x.save(d/"pregnant.json")
        print("PREGNANT",x.day,x.pregnancies)
    elif a.phase=="resume":
        x=Dynasty.load(d/"pregnant.json");x.advance(1);x.deliver("mother",[("child",False)]);x.advance(18*84);x.death("father","child");x.save(d/"succession.json")
        print("SUCCESSION",x.day,x.leaders,"child_age",x.age("child"),"people",len(x.people))
    else:
        x=Dynasty.load(d/"succession.json");assert x.leaders["a"]=="child" and x.age("child")==18 and not x.people["father"].alive
        before=x.snapshot()
        try:x.death("father","child")
        except ValueError:pass
        else:raise AssertionError("duplicate death")
        assert x.snapshot()==before
        print("DYNASTY_RELOAD_PASS")
if __name__=="__main__":main()

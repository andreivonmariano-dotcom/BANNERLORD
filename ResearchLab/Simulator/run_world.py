"""Separate-process demonstration of independent simulator persistence."""
import argparse
from pathlib import Path
from world_campaign import sample_world, save_snapshot, load_snapshot

def main():
    p=argparse.ArgumentParser()
    p.add_argument("phase",choices=("start","resume","verify"))
    p.add_argument("directory",type=Path)
    args=p.parse_args()
    d=args.directory
    if args.phase=="start":
        d.mkdir(parents=True,exist_ok=False)
        w=sample_world()
        w.dispatch("a","retainer_a",10,6)
        w.dispatch("b","retainer_b",10,6)
        save_snapshot(w,d/"active.json")
        assert w.parties["main"].healthy==43
        print("ACTIVE",w.day,w.parties["main"].healthy)
    elif args.phase=="resume":
        w=load_snapshot(d/"active.json")
        assert w.day==0 and w.parties["main"].healthy==43
        w.advance(6)
        w.finish("a",2,5,550)
        w.finish("b",1,1,0,False)
        save_snapshot(w,d/"completed.json")
        print("COMPLETED",w.snapshot())
    else:
        w=load_snapshot(d/"completed.json")
        assert (w.day,w.parties["main"].healthy,w.parties["main"].wounded,w.clans["house"].gold)==(6,56,6,50172)
        before=w.snapshot()
        try:w.finish("a",2,5,550)
        except ValueError:pass
        else:raise AssertionError("duplicate permitted")
        assert before==w.snapshot()
        print("COMPLETED_RELOAD_AND_DUPLICATE_REJECTION_PASS")

if __name__=="__main__":main()

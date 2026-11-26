import unittest
from world_campaign import World,sample_world,save_snapshot,load_snapshot
from pathlib import Path
import tempfile
import json
class WorldTest(unittest.TestCase):
    def test_parallel(self):
        w=sample_world();w.dispatch("a","retainer_a",10,6);w.dispatch("b","retainer_b",10,6)
        self.assertEqual(w.parties["main"].healthy,43);w.advance(6);w=World.restore(w.snapshot())
        w.finish("a",2,5,550);w.finish("b",1,1,0,False)
        p=w.parties["main"];self.assertEqual((p.healthy,p.wounded),(56,6))
        self.assertEqual(w.clans["house"].gold,50172)
        self.assertEqual(p.healthy+p.wounded+sum(t["lost"] for t in w.tasks.values()),65)
    def test_duplicate_actor(self):
        w=sample_world();w.dispatch("a","retainer_a",10,6);before=w.snapshot()
        with self.assertRaises(ValueError):w.dispatch("b","retainer_a",10,6)
        self.assertEqual(before,w.snapshot())
    def test_duplicate_reward(self):
        w=sample_world();w.dispatch("a","retainer_a",10,6);w.advance(6);w.finish("a",2,5,550)
        w=World.restore(w.snapshot());before=w.snapshot()
        with self.assertRaises(ValueError):w.finish("a",2,5,550)
        self.assertEqual(before,w.snapshot())
    def test_blocked_return(self):
        w=sample_world();w.dispatch("a","retainer_a",10,6);w.advance(6);before=w.snapshot()
        self.assertFalse(w.finish("a",2,5,550,map_event=True));self.assertEqual(before,w.snapshot())
    def test_separate_resources(self):
        w=sample_world();w.dispatch("a","retainer_a",10,6);w.advance(6);w.finish("a",2,5,550)
        s=w.settlements["holding"];self.assertEqual((s.food,s.market_gold,s.construction_reserve),(100,20000,500))
    def test_missing_reference(self):
        w=sample_world();w.parties["main"].settlement="missing"
        with self.assertRaises(ValueError):w.snapshot()
    def test_restore_roundtrip(self):
        w=sample_world();w.dispatch("a","retainer_a",10,6)
        self.assertEqual(World.restore(w.snapshot()).snapshot(),w.snapshot())
    def test_early_return(self):
        w=sample_world();w.dispatch("a","retainer_a",10,6)
        with self.assertRaises(ValueError):w.finish("a",2,5,550)
    def test_protect_named_personnel(self):
        w=sample_world();before=w.snapshot()
        with self.assertRaises(ValueError):w.dispatch("a","retainer_a",63,6)
        self.assertEqual(before,w.snapshot())
    def test_invalid_completed_accounting(self):
        w=sample_world();w.dispatch("a","retainer_a",10,6);w.advance(6);w.finish("a",2,5,550)
        x=json.loads(w.snapshot());x["tasks"]["a"]["returned"]=99
        with self.assertRaises(ValueError):World.restore(json.dumps(x))
    def test_disk_roundtrip_and_no_overwrite(self):
        w=sample_world();w.dispatch("a","retainer_a",10,6)
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/"active.json";save_snapshot(w,p)
            self.assertEqual(load_snapshot(p).snapshot(),w.snapshot())
            with self.assertRaises(FileExistsError):save_snapshot(w,p)
            with self.assertRaises(ValueError):save_snapshot(w,Path(d)/"game.sav")
if __name__=="__main__":unittest.main()

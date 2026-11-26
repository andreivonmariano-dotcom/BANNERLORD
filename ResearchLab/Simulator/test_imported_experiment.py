import unittest,copy
from imported_experiment import Experiment,totals
from compare_native import SAVE_HASHES
def fixture():
    def row(k,n,hero=None):return {"character":k,"hero":hero,"count":n,"wounded":0,"xp":0}
    return {"source_sha":SAVE_HASHES[0],"party":"player_party","issue":None,"gold":1000,"relation":0,"town":{"prosperity":100,"security":30},
      "roster":[row("main_hero",1,"main_hero"),row("a",1,"a"),row("soldiers",12)],
      "people":{"main_hero":{"id":"main_hero","state":"Active","party":"player_party","health":100,"skills":{}},
      "a":{"id":"a","state":"Active","party":"player_party","health":100,"skills":{"Tactics":86}}}}
class TestImported(unittest.TestCase):
    def setUp(self):self.s=Experiment(fixture())
    def dispatch(self):self.s.dispatch("a",{"soldiers":10},.1,cached_loss=2)
    def test_dispatch_cache(self):
        self.dispatch();self.assertEqual(self.s.task["cached_loss"],2);self.assertEqual(totals(self.s.state["roster"])["healthy"],3)
    def test_invalid_dispatch_atomic(self):
        before=self.s.snapshot()
        with self.assertRaises(ValueError):self.s.dispatch("a",{"soldiers":11},.1,cached_loss=2)
        self.assertEqual(before,self.s.snapshot())
    def test_invalid_loss_atomic(self):
        self.dispatch();self.s.advance(6);before=self.s.snapshot()
        with self.assertRaises(ValueError):self.s.resolve(True,{"soldiers":1},{},550)
        self.assertEqual(before,self.s.snapshot())
    def test_failure_wound(self):
        self.dispatch();self.s.advance(6)
        with self.assertRaises(ValueError):self.s.resolve(False,{"soldiers":2},{},0)
        self.s.resolve(False,{"soldiers":2},{"a":1},0);self.s.return_party()
        self.assertEqual(self.s.state["relation"],-5);self.assertEqual(totals(self.s.state["roster"]),{"healthy":11,"wounded":1})
    def test_blockers_and_duplicate(self):
        self.dispatch();self.s.advance(6);self.s.resolve(True,{"soldiers":2},{"soldiers":5},550)
        before=self.s.snapshot()
        for kw in ({"prisoner":True},{"at_sea":True},{"map_event":True}):self.assertFalse(self.s.return_party(**kw));self.assertEqual(before,self.s.snapshot())
        self.s=Experiment.restore(before);self.assertTrue(self.s.return_party(at_sea=True,in_settlement=True))
        with self.assertRaises(ValueError):self.s.return_party()
        with self.assertRaises(ValueError):self.s.resolve(True,{"soldiers":2},{},550)
        self.assertEqual(self.s.state["gold"],1550)
    def test_early_resolution(self):
        self.dispatch();self.s.advance(5)
        with self.assertRaises(ValueError):self.s.resolve(True,{"soldiers":2},{},550)
    def test_seed_persistence(self):
        self.s.dispatch("a",{"soldiers":10},.1,seed=42);t=Experiment.restore(self.s.snapshot())
        self.assertEqual(t.task,self.s.task)
if __name__=="__main__":unittest.main()

import unittest,tempfile,json
from pathlib import Path
from integrated_lab import Integrated
CFG={"towns":[["one",1000],["two",2000]],"recipe":{"inputs":{"grain":1},"outputs":{"beer":2}},"source_sha":"synthetic","recipe_sha":"synthetic","database_sha":"synthetic"}
class IntegratedTests(unittest.TestCase):
    def test_clock_and_birth(self):
        s=Integrated(CFG,1);s.advance(36)
        self.assertEqual(s.world.day,s.family.day);self.assertIn("child",s.family.people)
    def test_complete_year(self):
        s=Integrated(CFG,1);s.advance(84);r=s.report()
        self.assertEqual(r["successes"]+r["failures"],12);self.assertEqual(r["active"],0)
        self.assertEqual(r["healthy"]+r["wounded"]+r["lost"],65)
    def test_reload_replay(self):
        a=Integrated(CFG,2);a.advance(84);b=Integrated(CFG,2);b.advance(42)
        b=Integrated.restore(b.snapshot());b.advance(42);self.assertEqual(a.snapshot(),b.snapshot())
    def test_disk_roundtrip(self):
        s=Integrated(CFG,3);s.advance(35)
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/"state.json";s.save(p);self.assertEqual(Integrated.load(p).snapshot(),s.snapshot())
            with self.assertRaises(FileExistsError):s.save(p)
    def test_detect_market_mismatch(self):
        s=Integrated(CFG,1);s.accounts["one"].gold+=1
        with self.assertRaises(ValueError):s.validate()
    def test_detect_personnel_mismatch(self):
        s=Integrated(CFG,1);s.world.parties["main"].healthy+=1
        with self.assertRaises(ValueError):s.validate()
    def test_detect_clock_mismatch(self):
        s=Integrated(CFG,1);s.family.advance(1)
        with self.assertRaises(ValueError):s.validate()
    def test_detect_goods_mismatch(self):
        s=Integrated(CFG,1);s.accounts["one"].produce({"grain":1})
        with self.assertRaises(ValueError):s.validate()
    def test_detect_unaccounted_gold(self):
        s=Integrated(CFG,1);s.world.clans["a"].gold+=1
        with self.assertRaises(ValueError):s.validate()
    def test_seed_determinism(self):
        a=Integrated(CFG,4);b=Integrated(CFG,4);a.advance(14);b.advance(14)
        self.assertEqual(a.snapshot(),b.snapshot())
if __name__=="__main__":unittest.main()

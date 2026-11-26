import unittest
import tempfile
import struct
import zlib
import json
from pathlib import Path
from delegation import Campaign, risk, allowed_return, replay, sample_risk
from save_reader import inspect_save

class DelegationTests(unittest.TestCase):
    def setUp(self):
        self.c=Campaign(65,0,49622)
    def dispatch(self):
        self.c.dispatch(86,10,6)
    def ready(self):
        self.dispatch();self.c.advance(6)
    def test_observed_risk(self):
        self.assertAlmostEqual(risk(86),.17);self.assertAlmostEqual(risk(56),.32)
    def test_clamps(self):
        self.assertEqual(risk(200),0);self.assertEqual(risk(0,1000),.9)
    def test_invalid_skill(self):
        with self.assertRaises(ValueError):risk(-1)
    def test_dispatch_conservation(self):
        self.dispatch();self.assertEqual((self.c.healthy,self.c.sent,self.c.gold),(54,11,49622))
    def test_reject_duplicate_dispatch(self):
        self.dispatch();before=self.c.snapshot()
        with self.assertRaises(ValueError):self.dispatch()
        self.assertEqual(self.c.snapshot(),before)
    def test_insufficient_troops(self):
        with self.assertRaises(ValueError):self.c.dispatch(86,64,6)
        self.assertEqual(self.c.healthy,65)
    def test_early_finish(self):
        self.dispatch()
        with self.assertRaises(ValueError):self.c.finish(2,5,550)
    def test_intermediate(self):
        self.dispatch();self.c.advance(5)
        self.assertEqual(self.c.state,"returning");self.assertFalse(self.c.commander_available)
        self.assertEqual(self.c.healthy,54)
    def test_blockers(self):
        self.ready()
        for b in ({"prisoner":True},{"at_sea":True},{"map_event":True}):
            before=self.c.snapshot();self.assertFalse(self.c.finish(2,5,550,**b));self.assertEqual(self.c.snapshot(),before)
        self.assertTrue(allowed_return(at_sea=True,in_settlement=True))
    def test_return_conservation(self):
        self.ready();self.c.finish(2,5,550)
        self.assertEqual((self.c.healthy,self.c.wounded,self.c.lost,self.c.returned),(58,5,2,9))
        self.assertTrue(self.c.commander_available);self.assertEqual(self.c.gold,50172)
    def test_duplicate_after_restore(self):
        self.ready();self.c.finish(2,5,550);self.c=Campaign.restore(self.c.snapshot())
        before=self.c.snapshot()
        with self.assertRaises(ValueError):self.c.finish(2,5,550)
        self.assertEqual(self.c.snapshot(),before)
    def test_invalid_outcome(self):
        self.ready();before=self.c.snapshot()
        with self.assertRaises(ValueError):self.c.finish(7,5,550)
        self.assertEqual(self.c.snapshot(),before)
    def test_failure_policy(self):
        self.ready();self.c.finish(2,5,0,success=False)
        self.assertEqual(self.c.state,"failed");self.assertEqual(self.c.gold,49622)
    def test_active_restore(self):
        self.dispatch();self.c=Campaign.restore(self.c.snapshot())
        self.assertFalse(self.c.commander_available);self.assertEqual(self.c.healthy,54)
    def test_risk_sample_repeatable(self):
        self.assertEqual(sample_risk(86,100),sample_risk(86,100))
    def test_replay_residual(self):
        result=replay(dict(healthy=65,wounded=0,gold=49622),dict(healthy=54,wounded=0,gold=49622),dict(healthy=58,wounded=5,gold=50257))
        self.assertEqual(result["unmodeled_net_gold"],85)

class ReaderTests(unittest.TestCase):
    def fixture(self,path,extra=b"",bad_size=False):
        def block(b):return struct.pack("<i",len(b))+b
        payload=block(b"header")+struct.pack("<i",1)+block(b"obj")+struct.pack("<i",1)+block(b"ctr")+block(b"resolved the matter")+extra
        meta={"List":{"ApplicationVersion":"v1.5.7.125802","Version":"1","UniqueGameId":"synthetic","Modules":"Native","Module_Native":"v1.5.7.125802",
           "MainPartyHealthyMemberCount":"3","MainPartyWoundedMemberCount":"1","MainHeroGold":"5","MainPartyFood":"7.2","SavePredictedSizes":"h=6 obj=3 ctr=3 str=19" if not bad_size else "wrong"}}
        data=json.dumps(meta).encode();c=zlib.compressobj(wbits=-15)
        path.write_bytes(block(data)+c.compress(payload)+c.flush())
    def test_reader_valid(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/"synthetic.sav";self.fixture(p);before=p.read_bytes()
            r,i=inspect_save(p);self.assertEqual(r["healthy"],3);self.assertTrue(r["markers"]["resolved the matter"]);self.assertEqual(p.read_bytes(),before)
    def test_reader_invalid_metadata(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/"synthetic.sav";p.write_bytes(b"\xff\xff\xff\xffjunk")
            with self.assertRaises(ValueError):inspect_save(p)
    def test_reader_trailing(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/"synthetic.sav";self.fixture(p,b"x")
            with self.assertRaises(ValueError):inspect_save(p)
    def test_reader_mismatch(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/"synthetic.sav";self.fixture(p,bad_size=True)
            with self.assertRaises(ValueError):inspect_save(p)

if __name__=="__main__":unittest.main(verbosity=2)

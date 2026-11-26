import unittest
import sqlite3
from contextlib import closing
import tempfile
from pathlib import Path
from native_rules import *
from source_world import source_world,run

class RuleTests(unittest.TestCase):
    def test_wages(self):
        self.assertEqual([troop_wage(i) for i in range(8)],[1,2,3,5,8,12,17,23])
        self.assertEqual([troop_wage(i,True) for i in range(8)],[1,3,4,7,12,18,25,34])
    def test_market(self):
        self.assertEqual(market_change(5000,10000),15000)
        self.assertEqual(market_change(5000,70000),0)
        self.assertEqual(market_change(5000,90000),-5000)
    def test_ties(self):
        self.assertEqual(market_change(0,9998),0)
        self.assertEqual(market_change(0,9994),2)
        self.assertEqual(market_change(0,10006),-2)
    def test_learning(self):
        self.assertEqual(learning_limit([3],2),80)
        self.assertEqual(learning_limit([2,4],2),80)
        self.assertEqual(learning_limit([0],0),0)
    def test_calendar(self):
        self.assertEqual(calendar()["days_per_year"],84)
        self.assertEqual(calendar(True)["days_per_year"],24)
        self.assertEqual(calendar()["pregnancy_days"],36)
        self.assertEqual(calendar(True)["pregnancy_days"],18)
    def test_age(self):
        self.assertEqual(age_stage(13.999),"child")
        self.assertEqual(age_stage(14),"teenager")
        self.assertEqual(age_stage(age_after_days(17,84)),"adult")
        self.assertEqual(age_after_days(17,24,True),18)
    def test_pregnancy_age(self):
        self.assertFalse(pregnancy_age_eligible(17.9))
        self.assertTrue(pregnancy_age_eligible(18))
        self.assertTrue(pregnancy_age_eligible(45))
        self.assertFalse(pregnancy_age_eligible(45.1))
    def test_relation(self):
        self.assertEqual(relation_after_effective_delta(98,5),100)
        self.assertEqual(relation_after_effective_delta(-98,-5),-100)
    def test_invalid(self):
        for fn,args in ((market_change,(-1,0)),(troop_wage,(-1,)),(learning_limit,([],2)),(age_stage,(float("nan"),))):
            with self.assertRaises(ValueError):fn(*args)
    def test_source_selection(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/"source.sqlite"
            with closing(sqlite3.connect(p)) as db, db:
                db.executescript("CREATE TABLE sources(id,module,path,kind,sha256,status); CREATE TABLE definitions(source,ordinal,object_id,xml);")
                for i,module in enumerate(("SandBox","NavalDLC"),1):
                    db.execute("INSERT INTO sources VALUES(?,?,?,?,?,?)",(i,module,f"Modules/{module}/ModuleData/settlements.xml","Settlements","test","parsed"))
                    db.execute("INSERT INTO definitions VALUES(?,?,?,?)",(i,0,"town_a",f'<Settlement id="town_a"><Components><Town prosperity="{i*1000}"/></Components></Settlement>'))
            w=source_world(p,"war_sails")
            self.assertEqual(w["settlements"]["town_a"]["prosperity"],2000)
            self.assertEqual(run(w,1)["history"],[10000,16000])
    def test_market_scenario_separate(self):
        w={"source":"test","source_sha256":"test","database_sha256":"test","settlements":{"a":{"kind":"town","prosperity":1000},"b":{"kind":"castle","prosperity":1000}}}
        r=run(w,2)
        self.assertEqual(r["history"],[10000,13000,15250])
        self.assertNotIn("b",r["town_market_scenario"])
        self.assertNotIn("gold",w["settlements"]["a"])
if __name__=="__main__":unittest.main()

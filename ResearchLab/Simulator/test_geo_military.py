import unittest
from geography import base_speed
from military_experiment import contingent,duel,batch
class Tests(unittest.TestCase):
    def test_speed(self):
        for n,x in [(0,4.6),(1,4.59083176),(10,4.51109648),(65,4.11028051),(100,3.91130185),(200,3.48614788),(500,2.78695917),(1000,2.24645281)]:
            self.assertAlmostEqual(base_speed(n),x,5)
    def fixture(self):
        return {"source_sha":"fixture","nodes":{"parties":{"a":{"id":"a","roster":[{"hero":None,"character":"x","count":4,"wounded":1},{"hero":"h","character":"h","count":1,"wounded":0}]},"b":{"id":"b","roster":[{"hero":None,"character":"y","count":3,"wounded":0}]}}},"character_sources":{"x":[{"level":11}],"y":[{"level":16}]}}
    def test_contingent(self):
        c=contingent(self.fixture(),"a");self.assertEqual(c["healthy_regulars"],3);self.assertEqual(c["excluded_healthy_heroes"],1)
    def test_conflict(self):
        w=self.fixture();w["character_sources"]["x"].append({"level":16})
        with self.assertRaises(ValueError):contingent(w,"a")
    def test_conservation(self):
        w=self.fixture();a,b=contingent(w,"a"),contingent(w,"b")
        for seed in range(20):
            x=duel(a,b,seed,.5,100)
            self.assertEqual([sum(x[k][i] for k in ("fit","wounded","dead")) for i in (0,1)],[3,3])
    def test_reproducible(self):
        w=self.fixture();self.assertEqual(batch(w,"a","b",10,10,.5,100),batch(w,"a","b",10,10,.5,100))
    def test_survival(self):
        w=self.fixture();a,b=contingent(w,"a"),contingent(w,"b")
        self.assertEqual(duel(a,b,1,1,100)["dead"],[0,0]);self.assertEqual(duel(a,b,1,0,100)["wounded"],[0,0])
if __name__=="__main__":unittest.main()

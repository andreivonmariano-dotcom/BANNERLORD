import unittest
from compare_native import GRID,FIELDS,compare_grid,compare_d001,metric
from economy_lab import base_price_factor
def fixture():
    rows=[]
    for a,t,s,p,d,b,v in sorted(GRID):
        result=base_price_factor(s,p,d,bool(b),v,bool(a),bool(t))
        rows.append(dict(zip(FIELDS,(a,t,s,p,d,b,v,result))))
    return rows
class ComparisonTests(unittest.TestCase):
    def test_complete(self):
        r=compare_grid(fixture());self.assertEqual(r["matches"],800)
    def test_missing(self):
        with self.assertRaises(ValueError):compare_grid(fixture()[:-1])
    def test_duplicate(self):
        r=fixture()
        with self.assertRaises(ValueError):compare_grid(r+[r[0]])
    def test_nan(self):
        r=fixture();r[0]["native"]="nan"
        with self.assertRaises(ValueError):compare_grid(r)
    def test_out_of_scope(self):
        r=fixture();r[0]["stock"]=999
        with self.assertRaises(ValueError):compare_grid(r)
    def test_mismatch(self):
        r=fixture();r[0]["native"]+=.1
        self.assertEqual(compare_grid(r)["mismatches"],1)
    def test_tolerance(self):
        r=fixture();r[0]["native"]+=.000001
        self.assertEqual(compare_grid(r)["matches"],800)
    def test_input_not_prediction(self):
        self.assertEqual(metric("x",1,1,"supplied_outcome")["status"],"INPUT_REPLAY_MATCH")
    def test_unmodeled_not_match(self):
        self.assertEqual(metric("x",None,None,"unmodeled")["status"],"UNMODELED")
    def test_d001_residual(self):
        r=compare_d001([dict(healthy=65,wounded=0,gold=49622,food=91.75),dict(healthy=54,wounded=0,gold=49622,food=91.75),dict(healthy=58,wounded=5,gold=50257,food=75.55)])
        gold=next(x for x in r["metrics"] if x["metric"]=="return gold")
        self.assertEqual(gold["native_minus_simulator"],85);self.assertEqual(gold["status"],"MISMATCH")
if __name__=="__main__":unittest.main()

import unittest
from economy_lab import Account,trade,base_price_factor
from dynasty_lab import Dynasty,sample
class EconomyTest(unittest.TestCase):
    def test_price(self):
        self.assertAlmostEqual(base_price_factor(0,0,2),1)
        self.assertAlmostEqual(base_price_factor(0,0,0),.1)
        self.assertAlmostEqual(base_price_factor(0,0,100000,trade_good=False),1.3)
        self.assertLess(base_price_factor(100,1,100,True,20),base_price_factor(100,1,100))
        self.assertLess(base_price_factor(0,0,100,animal=True),base_price_factor(0,0,100))
    def test_trade_atomic(self):
        a,b=Account(0,{"x":2}),Account(5,{})
        with self.assertRaises(ValueError):trade(a,b,"x",2,3)
        self.assertEqual((a.gold,b.gold,a.quantity("x"),b.quantity("x")),(0,5,2,0))
        trade(a,b,"x",1,3);self.assertEqual((a.gold,b.gold,a.quantity("x"),b.quantity("x")),(3,2,1,1))
    def test_recipe(self):
        a=Account(0,{"grain":5});r={"inputs":{"grain":1},"outputs":{"beer":2}}
        a.convert(r,5);self.assertEqual(a.quantity("beer"),10)
        with self.assertRaises(ValueError):a.convert(r,1)
        self.assertEqual(a.quantity("beer"),10)
    def test_consume_atomic(self):
        a=Account(0,{"x":2})
        with self.assertRaises(ValueError):a.consume({"x":1,"y":1})
        self.assertEqual(a.quantity("x"),2)
class DynastyTest(unittest.TestCase):
    def setUp(self):self.d=sample()
    def marry(self):self.d.marry("father","mother",True,move="mother")
    def test_consent(self):
        before=self.d.snapshot()
        with self.assertRaises(ValueError):self.d.marry("father","mother",False)
        self.assertEqual(before,self.d.snapshot())
    def test_pregnancy_reload(self):
        self.marry();self.d.conceive("mother");self.d.advance(35)
        self.d=Dynasty.restore(self.d.snapshot())
        with self.assertRaises(ValueError):self.d.deliver("mother",[("child",False)])
        self.d.advance(1);self.d.deliver("mother",[("child",False)])
        self.assertEqual(set(self.d.people["child"].parents),{"father","mother"})
        with self.assertRaises(ValueError):self.d.deliver("mother",[("child2",True)])
    def test_succession(self):
        self.marry();self.d.conceive("mother");self.d.advance(36);self.d.deliver("mother",[("child",False)])
        with self.assertRaises(ValueError):self.d.death("father","child")
        self.d.advance(18*84);self.d.death("father","child")
        self.assertEqual(self.d.leaders["a"],"child");self.assertIsNone(self.d.people["mother"].spouse)
    def test_relation(self):
        self.d.change_relation("mother","father",150)
        self.d.change_relation("father","mother",-20)
        self.assertEqual(list(self.d.relations.values()),[80])
    def test_no_related_marriage(self):
        self.marry();self.d.conceive("mother");self.d.advance(36);self.d.deliver("mother",[("child",False),("sister",True)])
        self.d.advance(18*84);self.assertFalse(self.d.eligible("child","sister"))
    def test_duplicate_pregnancy(self):
        self.marry();self.d.conceive("mother");before=self.d.snapshot()
        with self.assertRaises(ValueError):self.d.conceive("mother")
        self.assertEqual(before,self.d.snapshot())
if __name__=="__main__":unittest.main()

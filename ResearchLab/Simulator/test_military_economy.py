import unittest
from military_economy_rules import *
class RuleTest(unittest.TestCase):
    def test_tier(self):
        self.assertEqual([tier(x) for x in (1,5,6,10,11,20,35,36,100)],[0,0,1,1,2,3,6,6,6])
    def test_power(self):
        self.assertAlmostEqual(base_power(5),.4,6);self.assertAlmostEqual(base_power(35),2.56,6)
        self.assertAlmostEqual(base_power(20,True),3.84,6)
    def test_xp(self):
        self.assertEqual(xp_reward(20),225);self.assertEqual(xp_reward(35),560)
    def test_morale(self):
        self.assertAlmostEqual(morale_factor(30,70),-.2,6);self.assertEqual(morale_factor(90,10),0)
    def test_hit(self):
        self.assertEqual(base_hit(1,1,1,0),20);self.assertEqual(base_hit(1,1,1,1),40)
        self.assertGreater(base_hit(2,1,1,.5),base_hit(1,1,1,.5))
    def test_security(self):
        self.assertAlmostEqual(security_drift(35),1,6);self.assertAlmostEqual(security_drift(65),-1,6)
        self.assertAlmostEqual(prosperity_security(1000),-.5,6);self.assertEqual(prosperity_security(20000),-5)
    def test_housing(self):
        self.assertEqual([housing_prosperity(x,True) for x in (0,250,500,750,1000,1250,1500,6000,6001,9001,21001)],[6,5,4,3,2,1,0,0,-1,-2,-6])
        self.assertEqual(housing_prosperity(890,False),0)
    def test_food(self):
        self.assertAlmostEqual(surplus_food_prosperity(100,15.9,100),1.5,6);self.assertEqual(surplus_food_prosperity(100,-20,100),0)
    def test_invalid(self):
        for fn,args in ((tier,(-1,)),(base_hit,(1,0,1,1)),(morale_factor,(101,50)),(security_drift,(-1,)),(xp_reward,(-1,))):
            with self.assertRaises(ValueError):fn(*args)
if __name__=="__main__":unittest.main()

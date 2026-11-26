"""Selected default-model components from static inspection, not full runtime quotes."""
import struct,unittest
def f32(n):return struct.unpack("<f",struct.pack("<f",n))[0]
def recruitment_base(level):
    if type(level) is not int or level<0:raise ValueError("level")
    for upper,cost in ((1,10),(6,20),(11,50),(16,100),(21,200),(26,400),(31,600),(36,1000)):
        if level<=upper:return cost
    return 1500
def horse_surcharge(level,horse_slot_present,without_item_cost):
    recruitment_base(level)
    if type(horse_slot_present) is not bool or type(without_item_cost) is not bool:raise ValueError("flag")
    return (150 if level<26 else 500) if horse_slot_present and not without_item_cost else 0
def base_food_change(members,prisoners,divisor=20):
    if any(type(x) is not int or x<0 for x in (members,prisoners)) or type(divisor) is not int or divisor<=0:raise ValueError("count")
    return f32(-f32(max(1,members+prisoners//2))/f32(divisor))
class Tests(unittest.TestCase):
    def test_level_boundaries(self):
        for l,c in ((0,10),(1,10),(2,20),(6,20),(7,50),(11,50),(12,100),(16,100),(17,200),(21,200),(22,400),(26,400),(27,600),(31,600),(32,1000),(36,1000),(37,1500)):self.assertEqual(recruitment_base(l),c)
    def test_horse(self):
        self.assertEqual(horse_surcharge(25,True,False),150);self.assertEqual(horse_surcharge(26,True,False),500)
        self.assertEqual(horse_surcharge(26,True,True),0);self.assertEqual(horse_surcharge(26,False,False),0)
    def test_food(self):
        self.assertEqual(base_food_change(100,20),-5.5);self.assertEqual(base_food_change(100,21),-5.5);self.assertEqual(base_food_change(0,0),f32(-.05))
    def test_bad_input(self):
        for x in (-1,True,1.2):
            with self.assertRaises(ValueError):recruitment_base(x)
            with self.assertRaises(ValueError):base_food_change(x,0)
if __name__=="__main__":unittest.main()

import unittest,struct
from typed_save import Reader,archive,save_id,field
from delegation_rules import casualties,duration,required_troops
class TypedRulesTests(unittest.TestCase):
    def test_archive(self):
        def u(x):return (x&0xffffff).to_bytes(3,"little")
        d=struct.pack("<i",1)+u(0)+u(-1)+u(0)+bytes([1])+struct.pack("<i",1)+u(0)+u(-1)+bytes([8])+struct.pack("<h",9)+bytes([0])+struct.pack("<ihh",330048,1,0)
        f,e=archive(d);self.assertEqual(f,{0:(-1,0,1)});r=Reader(e[(0,-1,8)]);self.assertEqual(save_id(r),330048)
        with self.assertRaises(ValueError):archive(d[:-1])
    def test_reference(self):
        self.assertEqual(field(struct.pack("<BBhi",0,1,500,42)),(0,1,500,42))
    def test_basic(self):
        self.assertEqual(field(struct.pack("<BBhBii",6,1,650,0,30001,50)),(6,1,650,50))
    def test_casualties(self):
        self.assertEqual(casualties(86,120,10),(1,2));self.assertEqual(casualties(56,120,10),(1,2));self.assertEqual(casualties(0,120,10),(4,8))
    def test_duration_integer(self):
        self.assertEqual(duration(86,120,6),8);self.assertEqual(duration(56,120,6),10);self.assertEqual(duration(121,120,6),6)
    def test_troop_float(self):
        self.assertEqual(required_troops(86,120,10),12);self.assertEqual(required_troops(100,90,10),9);self.assertEqual(required_troops(1000,120,10),2)
if __name__=="__main__":unittest.main()

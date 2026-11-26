import tempfile,unittest,os
from pathlib import Path
from SaveNamespaceGate import digest,validate,live_gate
class GateTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory()
        self.root=Path(self.tmp.name);self.v=self.root/"vault";self.a=self.root/"active"
        self.v.mkdir();self.a.mkdir()
        (self.v/"saveauto2.sav").write_bytes(b"personal")
        (self.a/"lab.sav").write_bytes(b"disposable")
        self.o={"saveauto2.sav":digest(self.v/"saveauto2.sav")}
        self.l={"lab.sav":digest(self.a/"lab.sav")}
    def tearDown(self):self.tmp.cleanup()
    def run_gate(self,**kw):
        flags=dict(closed=True,autosave_off=True,cloud_verified_off=True,native_path_verified=True)
        flags.update(kw)
        return live_gate(self.v,self.a,self.o,self.l,**flags)
    def test_valid(self):self.assertTrue(self.run_gate())
    def test_no_cloud_proof(self):
        with self.assertRaisesRegex(ValueError,"cloud"):self.run_gate(cloud_verified_off=False)
    def test_no_native_path(self):
        with self.assertRaisesRegex(ValueError,"native"):self.run_gate(native_path_verified=False)
    def test_game_open(self):
        with self.assertRaisesRegex(ValueError,"open"):self.run_gate(closed=False)
    def test_autosave_enabled(self):
        with self.assertRaisesRegex(ValueError,"enabled"):self.run_gate(autosave_off=False)
    def test_shared_root(self):
        with self.assertRaisesRegex(ValueError,"collision"):validate(self.v,self.v,self.o,self.l)
    def test_nested_root(self):
        a=self.v/"child";a.mkdir()
        with self.assertRaisesRegex(ValueError,"collision"):validate(self.v,a,self.o,self.l)
    def test_extra_file(self):
        (self.a/"saveauto2.sav").write_bytes(b"unexpected")
        with self.assertRaisesRegex(ValueError,"active inventory"):self.run_gate()
    def test_modified_vault(self):
        (self.v/"saveauto2.sav").write_bytes(b"changed")
        with self.assertRaisesRegex(ValueError,"fingerprint"):self.run_gate()
    def test_modified_lab(self):
        (self.a/"lab.sav").write_bytes(b"changed")
        with self.assertRaisesRegex(ValueError,"fingerprint"):self.run_gate()
    def test_hardlink(self):
        p=self.root/"alias.sav";os.link(self.v/"saveauto2.sav",p)
        with self.assertRaisesRegex(ValueError,"aliased"):self.run_gate()
    def test_missing_lab(self):
        (self.a/"lab.sav").unlink()
        with self.assertRaisesRegex(ValueError,"active inventory"):self.run_gate()
    def test_overlap(self):
        with self.assertRaisesRegex(ValueError,"scope"):validate(self.v,self.a,self.o,self.o)
if __name__=="__main__":unittest.main()

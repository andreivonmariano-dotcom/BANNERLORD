from pathlib import Path
from copy import deepcopy
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from validate_feature_registry import read_rows, validate, ROOT

history=read_rows(ROOT/'Data/Migration_78.csv')
base=read_rows(ROOT/'Data/Feature_Registry.csv')
assert not validate(base,history)

def expect_failure(change, phrase):
    rows=deepcopy(base)
    change(rows)
    problems=validate(rows,history)
    assert any(phrase in error for error in problems),(phrase,problems)

expect_failure(lambda r:r[1].update(feature_id=r[0]['feature_id']),'Duplicate feature IDs')
expect_failure(lambda r:r[0].update(inspired_by_module_id='not-a-real-id'),'unknown historical mod')
expect_failure(lambda r:r[0].update(status='GAME_TESTED',gameplay_evidence=''),'missing gameplay proof')
expect_failure(lambda r:r[0].update(status='RELEASED',highest_gate='B'),'released without Gate G')
expect_failure(lambda r:r[-1].update(status='COMPILED'),'unimplemented historical coverage')
expect_failure(lambda r:r[0].update(source_path='missing.cs'),'missing source path')
print('PASS: feature registry baseline and six negative schema/gate tests')

"""Build private, offline, read-only dashboard from pinned local projections.
No external dependencies, network calls or native save modifications.
"""
import argparse,json,html,hashlib
from pathlib import Path
from geography import NAMES,pinned_json,digest
HASHES=("fa4da7be84a84e9ade3e57549df01e83e57921eaf7617b323ec4b23fa9ad1f60","638114c882fd1a7a592e1ed95aa2487346e428a8e4efbba5139f6eb08cabfb2e","e755134a0d388e98188bc45a650e0782b5bc1e8981ef700d6b1f35bb3a629d1b")
MIL_HASH="7429531a2eb58128867b088e797d9f4da478364f3432db664eb246f97bc44a88"
def build(source,output):
    worlds=[pinned_json(source/(name+".json"),sha) for name,sha in zip(NAMES,HASHES)]
    mil=pinned_json(source/"military.json",MIL_HASH)
    payload=json.dumps({"worlds":worlds,"experiment":mil},ensure_ascii=True,separators=(",",":"),allow_nan=False).replace("<","\\u003c").replace(">","\\u003e").replace("&","\\u0026")
    template=Path(__file__).with_name("dashboard.html").read_text(encoding="utf8")
    if template.count("/*__PRIVATE_DATA__*/")!=1:raise ValueError("template marker")
    if output.suffix.lower()!=".html":raise ValueError("HTML required")
    with output.open("x",encoding="utf8") as f:f.write(template.replace("/*__PRIVATE_DATA__*/",payload))
    print("DASHBOARD",output,"BYTES",output.stat().st_size,"SHA",digest(output))
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("source",type=Path);p.add_argument("output",type=Path);a=p.parse_args();build(a.source,a.output)

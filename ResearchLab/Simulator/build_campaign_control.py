"""Private offline executable campaign lab; no game state writes."""
import argparse,json
from pathlib import Path
from geography import digest,pinned_json
FIXTURE_SHA="841325b0c1c4441514a91e60134522a3d3a1ee4f5276428991651a9accebebca"
def build(source,out):
    b=pinned_json(source,FIXTURE_SHA)
    p=Path(__file__).parent
    data=json.dumps(b,ensure_ascii=True,allow_nan=False,separators=(",",":")).replace("<","\\u003c").replace(">","\\u003e").replace("&","\\u0026")
    template=(p/"campaign_control.html").read_text(encoding="utf8")
    core=(p/"campaign_lab.js").read_text(encoding="utf8")
    if "</script" in core.lower() or template.count("/*__FIXTURE__*/")!=1 or template.count("/*__CORE__*/")!=1:raise ValueError("template")
    if out.suffix.lower()!=".html":raise ValueError("HTML only")
    with out.open("x",encoding="utf8") as f:f.write(template.replace("/*__FIXTURE__*/",data).replace("/*__CORE__*/",core))
    print("CONTROL",out,"BYTES",out.stat().st_size,"SHA",digest(out))
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("fixture",type=Path);p.add_argument("output",type=Path);a=p.parse_args();build(a.fixture,a.output)

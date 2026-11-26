"""Version-pinned read-only typed save record inspection. No object construction.
Selected type/member IDs from installed registration and Saveable attributes.
Unknown member formats remain opaque; object IDs are local to each snapshot.
"""
import collections,hashlib,json,struct,zlib,argparse
from pathlib import Path
from compare_native import read_saves,SAVE_HASHES

TYPES={330016:"Campaign",330032:"Clan",330048:"Hero",330050:"IssueBase",330053:"Kingdom",330074:"MobileParty",330086:"QuestBase",330095:"Settlement",330103:"Town",330031:"CharacterRelationManager",330052:"IssueManager",590001:"BrigandIssue",20001:"TextObject"}
FIELDS={"Hero":{180:"first_name",181:"name",380:"companion_of",500:"clan",700:"father",710:"mother",730:"spouse",740:"children",650:"gold"},"Clan":{57:"kingdom",83:"leader"},"Town":{1040:"owner_clan",1015:"security",1017:"prosperity",1012:"governor"},"MobileParty":{1120:"clan",1001:"settlement"},"BrigandIssue":{11:"issue_state",13:"owner",30:"sent_roster",32:"effects_resolved",33:"casualty_count",34:"failure_chance"},"CharacterRelationManager":{1:"hero_relations"},"IssueManager":{1:"issues",4:"awaiting_roster"},"TextObject":{1:"value"}}
class Reader:
    def __init__(self,data):self.data=memoryview(data);self.pos=0
    def take(self,n):
        if n<0 or self.pos+n>len(self.data):raise ValueError("truncated record")
        r=self.data[self.pos:self.pos+n];self.pos+=n;return r
    def num(self,fmt):return struct.unpack(fmt,self.take(struct.calcsize(fmt)))[0]
    def i(self):return self.num("<i")
    def s(self):return self.num("<h")
    def b(self):return self.num("<B")
    def u24(self):
        n=int.from_bytes(self.take(3),"little");return -1 if n==0xffffff else n
    def count(self):
        n=self.i()
        if not 0<=n<=2_000_000:raise ValueError("count limit")
        return n
    def block(self):return self.take(self.count_size())
    def count_size(self):
        n=self.i()
        if not 0<=n<=128*1024*1024:raise ValueError("size limit")
        return n
    def end(self):
        if self.pos!=len(self.data):raise ValueError("trailing data")

def save_id(r,depth=0):
    if depth>32:raise ValueError("type nesting limit")
    k=r.b()
    if k==0:return r.i()
    if k==1:
        if r.b()!=0:raise ValueError("generic base kind")
        base=r.i();return ("generic",base,tuple(save_id(r,depth+1) for _ in range(r.b())))
    if k==2:
        container=r.b()
        if container not in range(1,7):raise ValueError("container kind")
        return ("container",container,tuple(save_id(r,depth+1) for _ in range(2 if container==2 else 1)))
    raise ValueError("unknown save ID")

def archive(data):
    r=Reader(data);folders={};entries={}
    for _ in range(r.count()):
        p,g,l,e=r.u24(),r.u24(),r.u24(),r.b()
        if g<0 or g in folders:raise ValueError("invalid folder")
        folders[g]=(p,l,e)
    for _ in range(r.count()):
        folder,id,kind=r.u24(),r.u24(),r.b()
        key=(folder,id,kind)
        if key in entries or (folder!=-1 and folder not in folders):raise ValueError("invalid entry")
        entries[key]=r.take(r.s())
    r.end()
    return folders,entries

def field(data):
    r=Reader(data);kind,level,local=r.b(),r.b(),r.s()
    value=None
    if kind in (0,1,2,4):value=r.i()
    elif kind==5:
        tid=save_id(r);n=r.i();value=bytes(r.take(n)).decode("utf-8")
    elif kind==6:
        tid=save_id(r)
        if tid==30001:value=r.i()
        elif tid==30007:value=r.num("<f")
        elif tid==30020:value=bool(r.b())
        else:return kind,level,local,None
    else:return kind,level,local,None
    r.end()
    return kind,level,local,value

def inspect(path,expected):
    data=path.read_bytes()
    if hashlib.sha256(data).hexdigest()!=expected:raise ValueError("source changed")
    size=struct.unpack_from("<i",data)[0]
    dec=zlib.decompressobj(-15);payload=dec.decompress(data[4+size:],128*1024*1024+1)
    if not dec.eof or dec.unused_data or dec.unconsumed_tail or len(payload)>128*1024*1024:raise ValueError("invalid payload")
    r=Reader(payload);folders,entries=archive(r.block())
    config=Reader(entries[(-1,-1,7)]);oc,sc,cc=config.count(),config.count(),config.count();config.end()
    type_by_id={};container_types=collections.Counter()
    for g,(parent,local,ext) in folders.items():
        if parent!=-1:continue
        if ext==1:
            h=Reader(entries[(g,-1,8)]);tid=save_id(h);properties,children=h.s(),h.s();h.end()
            if local in type_by_id or min(properties,children)<0:raise ValueError("bad object header")
            type_by_id[local]=tid
        elif ext==3:
            h=Reader(entries[(g,-1,9)]);tid=save_id(h);kind=h.b();n=h.count();h.end();container_types[kind]+=1
    if set(type_by_id)!=set(range(oc)) or sum(container_types.values())!=cc:raise ValueError("header count mismatch")
    counts=collections.Counter(TYPES.get(t,"other") for t in type_by_id.values())
    obj={};unknown=0
    if r.count()!=oc:raise ValueError("object count mismatch")
    for i in range(oc):
        block=r.block();name=TYPES.get(type_by_id[i])
        if name not in FIELDS and name not in ("Settlement",):continue
        fs,es=archive(block)
        roots=[g for g,(p,l,k) in fs.items() if p==-1 and k==1]
        if len(roots)!=1:raise ValueError("unexpected object root")
        root=roots[0];record={"type":name,"fields":{}}
        for (folder,entry,extension),raw in es.items():
            if folder!=root or extension not in (2,3):continue
            h=Reader(raw);kind,level,local=h.b(),h.b(),h.s()
            label=FIELDS.get(name,{}).get(local) if extension==2 else None
            if extension==3 and local==1 and kind==2 and name in ("Hero","Clan","Town","MobileParty","Settlement","BrigandIssue"):label="string_id"
            if label:
                kind,level,local,value=field(raw)
                if label in record["fields"]:raise ValueError("ambiguous field")
                record["fields"][label]={"kind":kind,"level":level,"value":value}
                unknown+=value is None
        obj[i]=record
    if r.count()!=cc:raise ValueError("container count mismatch")
    for _ in range(cc):r.block()
    sf,se=archive(r.block());r.end()
    strings={}
    for (folder,id,kind),raw in se.items():
        if kind==10:
            h=Reader(raw);n=h.i();v=bytes(h.take(n)).decode("utf-8") if n>=0 else None;h.end();strings[id]=v
    if len(strings)!=sc:raise ValueError("string count mismatch")
    for record in obj.values():
        for label,v in record["fields"].items():
            if v["kind"]==2:v["resolved_string"]=strings.get(v["value"])
            if v["kind"]==0:v["referenced_type"]=TYPES.get(type_by_id.get(v["value"]),"other_or_null")
    if hashlib.sha256(path.read_bytes()).hexdigest()!=expected:raise ValueError("save changed")
    links=collections.Counter()
    for record in obj.values():
        for label,v in record["fields"].items():
            if v["kind"]==0 and v["referenced_type"] not in ("other_or_null",):links[record["type"]+"."+label+"->"+v["referenced_type"]]+=1
    return {"sha256":expected,"objects":oc,"containers":cc,"strings":sc,"type_counts":dict(counts),"reference_counts":dict(links),"unknown_fields":unknown,"records":obj}

def main():
    p=argparse.ArgumentParser();p.add_argument("save_dir",type=Path);p.add_argument("report",type=Path);a=p.parse_args()
    read_saves(a.save_dir)
    results=[]
    for n,h in zip(("LABD001BASE.sav","LABD001ACTIVE.sav","LABD001RETURN.sav"),SAVE_HASHES):
        result=inspect(a.save_dir/n,h);results.append(result)
        print(n,json.dumps({k:v for k,v in result.items() if k!="records"},sort_keys=True))
        # Only sanitized counts; private per-record details remain in local report.
    if a.report.suffix.lower()!=".json":raise ValueError("JSON only")
    with a.report.open("x",encoding="utf-8") as f:json.dump(results,f,allow_nan=False)
    print("REPORT_SHA",hashlib.sha256(a.report.read_bytes()).hexdigest())
if __name__=="__main__":main()

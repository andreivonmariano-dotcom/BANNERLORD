"""Version-pinned read-only typed save record inspection. No object construction.
Selected type/member IDs from installed registration and Saveable attributes.
Unknown member formats remain opaque; object IDs are local to each snapshot.
"""
import collections,hashlib,json,struct,zlib,argparse
from pathlib import Path
from compare_native import read_saves,SAVE_HASHES

TYPES={330016:"Campaign",330032:"Clan",330048:"Hero",330050:"IssueBase",330053:"Kingdom",330074:"MobileParty",330086:"QuestBase",330095:"Settlement",330103:"Town",330031:"CharacterRelationManager",330052:"IssueManager",330186:"HeroRelations",590001:"BrigandIssue",20001:"TextObject"}
FIELDS={"Hero":{180:"first_name",181:"name",380:"companion_of",500:"clan",700:"father",710:"mother",730:"spouse",740:"children",650:"gold",260:"hero_state",560:"party",580:"staying_settlement"},"Clan":{57:"kingdom",83:"leader"},"Town":{1040:"owner_clan",1015:"security",1017:"prosperity",1012:"governor"},"MobileParty":{1120:"clan",1001:"settlement"},"BrigandIssue":{11:"issue_state",13:"owner",30:"sent_roster",32:"effects_resolved",33:"casualty_count",34:"failure_chance"},"CharacterRelationManager":{1:"hero_relations"},"HeroRelations":{1:"relations"},"IssueManager":{1:"issues",4:"awaiting_roster"},"TextObject":{1:"value"}}
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
        elif tid==30009:value=r.num("<q")
        elif tid==11005:value=r.num("<I")
        else:return kind,level,local,None
    else:return kind,level,local,None
    r.end()
    return kind,level,local,value

def member(record,label,kind):
    m=record["fields"][label]
    if m["kind"]!=kind or m["value"] is None:raise ValueError("unexpected member kind")
    return m["value"]
def dictionary(cid,expected,types,blocks):
    if cid not in types or types[cid]!=expected:raise ValueError("unexpected dictionary type")
    fs,es=archive(blocks[cid])
    roots=[g for g,(p,l,k) in fs.items() if p==-1 and k==3 and l==cid]
    if len(roots)!=1:raise ValueError("dictionary root mismatch")
    root=roots[0];keys={};values={}
    for (folder,id,ext),raw in es.items():
        if folder!=root or ext not in (4,5):continue
        k,level,local,value=field(raw)
        if value is None or id in (keys if ext==4 else values):raise ValueError("invalid dictionary element")
        (keys if ext==4 else values)[id]=(k,value)
    if set(keys)!=set(values):raise ValueError("unpaired dictionary entries")
    result={}
    for i in keys:
        if keys[i][0]!=6 or keys[i][1] in result:raise ValueError("unexpected or duplicate dictionary key")
        result[keys[i][1]]=values[i]
    return result
def signed_hash(guid):
    if type(guid) is not int or not 0<=guid<=0xffffffff:raise ValueError("invalid GUID")
    return guid if guid<0x80000000 else guid-0x100000000
def personal_relation(obj,types,blocks):
    def select(name):
        matches=[r for r in obj.values() if r["type"]=="Hero" and r["fields"].get("string_id",{}).get("resolved_string")==name]
        if len(matches)!=1:raise ValueError("ambiguous hero identity")
        return matches[0]
    a,b=(member(select(name),"guid",6) for name in ("main_hero","CharacterObject_3007"))
    if a==b:raise ValueError("same GUID")
    managers=[r for r in obj.values() if r["type"]=="CharacterRelationManager"]
    if len(managers)!=1:raise ValueError("relation manager count")
    rel=obj[member(managers[0],"hero_relations",0)]
    if rel["type"]!="HeroRelations":raise ValueError("relation object mismatch")
    cid=member(rel,"relations",1)
    inner=("container",2,(30009,30001))
    outer=("container",2,(30009,inner))
    pairs=dictionary(cid,outer,types,blocks)
    hi,lo=map(signed_hash,sorted((a,b),reverse=True))
    result=0
    if hi in pairs:
        kind,cid=pairs[hi]
        if kind!=1:raise ValueError("invalid inner container reference")
        pairs2=dictionary(cid,inner,types,blocks)
        if lo in pairs2:
            kind,result=pairs2[lo]
            if kind!=6 or not -100<=result<=100:raise ValueError("invalid relation")
    return {"player":"main_hero","other":"CharacterObject_3007","raw_personal_relation":result,"scope":"raw stored pair, default zero if absent; not effective diplomacy relation"}

def inspect(path,expected):
    data=path.read_bytes()
    if hashlib.sha256(data).hexdigest()!=expected:raise ValueError("source changed")
    size=struct.unpack_from("<i",data)[0]
    dec=zlib.decompressobj(-15);payload=dec.decompress(data[4+size:],128*1024*1024+1)
    if not dec.eof or dec.unused_data or dec.unconsumed_tail or len(payload)>128*1024*1024:raise ValueError("invalid payload")
    r=Reader(payload);folders,entries=archive(r.block())
    config=Reader(entries[(-1,-1,7)]);oc,sc,cc=config.count(),config.count(),config.count();config.end()
    type_by_id={};container_types=collections.Counter();container_ids={}
    for g,(parent,local,ext) in folders.items():
        if parent!=-1:continue
        if ext==1:
            h=Reader(entries[(g,-1,8)]);tid=save_id(h);properties,children=h.s(),h.s();h.end()
            if local in type_by_id or min(properties,children)<0:raise ValueError("bad object header")
            type_by_id[local]=tid
        elif ext==3:
            h=Reader(entries[(g,-1,9)]);tid=save_id(h);kind=h.b();n=h.count();h.end();container_types[kind]+=1
            if local in container_ids:raise ValueError("duplicate container")
            container_ids[local]=tid
    if set(type_by_id)!=set(range(oc)) or set(container_ids)!=set(range(cc)):raise ValueError("header count mismatch")
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
            if extension==3 and local==2 and level==2 and name=="Hero":label="guid"
            if label:
                expected_level=2 if label in ("string_id","guid") or name in ("CharacterRelationManager","HeroRelations","TextObject") else 5 if name=="Town" else 3
                if level!=expected_level:raise ValueError(f"unexpected inheritance level {name}.{label}: {level}")
                kind,level,local,value=field(raw)
                if label in record["fields"]:raise ValueError("ambiguous field")
                record["fields"][label]={"kind":kind,"level":level,"value":value}
                unknown+=value is None
        obj[i]=record
    if r.count()!=cc:raise ValueError("container count mismatch")
    container_blocks=[r.block() for _ in range(cc)]
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
    relation=personal_relation(obj,container_ids,container_blocks)
    return {"relation":relation,"sha256":expected,"objects":oc,"containers":cc,"strings":sc,"type_counts":dict(counts),"reference_counts":dict(links),"unknown_fields":unknown,"records":obj}

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

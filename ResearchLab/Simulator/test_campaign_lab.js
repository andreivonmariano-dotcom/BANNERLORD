"use strict";
const assert=require("node:assert/strict"),fs=require("node:fs"),L=require("./campaign_lab.js");
const b=JSON.parse(fs.readFileSync(process.argv[2],"utf8"));let s=L.create(b);
assert.equal(L.date(b.clocks[0].ticks).year,1084);assert.equal(L.date(b.clocks[2].ticks).day,8);
const orig=JSON.stringify(s);assert.throws(()=>L.command(b,s,{type:"advance",days:0}));assert.equal(JSON.stringify(s),orig);
const a=L.command(b,s,{type:"advance",days:7});let q=s;for(let i=0;i<7;i++)q=L.command(b,q,{type:"advance",days:1});
const state=x=>{const y=JSON.parse(JSON.stringify(x));delete y.commands;return y};assert.deepEqual(state(a),state(q));
assert.deepEqual(L.replay(b,L.exportHistory(b,a)),a);
const k=Object.keys(b.forts).find(k=>b.forts[k].kind==="town");const shock=L.command(b,s,{type:"shock",target:k,kind:"market"});assert.equal(shock.forts[k].gold,Math.max(0,s.forts[k].gold-1000));
assert.throws(()=>L.command(b,s,{type:"shock",target:"bad",kind:"security"}));assert.equal(JSON.stringify(s),orig);
const c=L.command(b,s,{type:"shock",target:k,kind:"security"});assert.equal(c.forts[k].security,Math.max(0,s.forts[k].security-10));
let r=s;for(let i=0;i<3;i++)r=L.command(b,r,{type:"advance",days:30});assert.equal(r.days,90);assert.throws(()=>L.command(b,r,{type:"advance",days:1}));assert.deepEqual(L.replay(b,L.exportHistory(b,r)),r);
assert.throws(()=>L.replay(b,{version:1,source_sha:"wrong",commands:[]}));
for(const [k,h] of Object.entries(b.heroes))if(h.id==="main_hero")assert.equal(r.gold[k],s.gold[k]);
assert.equal(r.counts.economy,90*Object.keys(b.forts).length);
assert.equal(r.counts.offer,r.counts.accepted+r.counts.declined);
const rr=L.command(b,c,{type:"advance",days:7});
assert.deepEqual(L.replay(b,L.exportHistory(b,rr)),rr);
// Explicit synthetic branch fixtures: do not assert these occurred in D001.
const fixture={version:1,source_sha:"synthetic",projection_sha:"synthetic",ticks_per_hour:L.HOUR,days_per_year:84,clocks:[{mode:"Default",ticks:0}],
heroes:{h:{id:"h",state:"Active",gold:1000,spouse:"k",children:[]},k:{id:"k",state:"Active",gold:0},j:{id:"j",state:"Active",gold:1000},z:{id:"z",state:"Active",gold:1000}},
clans:{a:{id:"a",leader:"h",kingdom:"K"},b:{id:"b",leader:"j",kingdom:"J"},v:{id:"v",leader:"z",kingdom:"K"}},
kingdoms:{K:{ruling_clan:"a"},J:{ruling_clan:"b"}},forts:{f:{kind:"castle",owner:"a",gold:0,security:50,prosperity:100}},parties:{p:{active:true,at_sea:false,clan:"a",settlement:"f",healthy:20},q:{active:true,at_sea:false,clan:"b",healthy:20}},relations:[],wars:[]};
const initial=L.create(fixture);const aid=L.command(fixture,initial,{type:"advance",days:1});
assert.equal(aid.counts.aid,1);assert.equal(aid.gold.h,900);assert.equal(aid.gold.k,100);assert.equal(aid.relation["h|k"],1);assert.equal(L.metrics(aid).total,L.metrics(initial).total);
const war=JSON.parse(JSON.stringify(fixture));war.heroes.k.gold=200;war.forts.f.security=95;war.wars=[{faction1:"K",faction2:"J"}];
const truce=L.command(war,L.create(war),{type:"advance",days:1});assert.equal(truce.counts.accepted,1);assert.equal(truce.gold.z,1000);assert.equal(truce.truce["J|K"],true);
const guard=JSON.parse(JSON.stringify(fixture));guard.heroes.k.gold=200;guard.forts.f.security=10;
const watch=L.command(guard,L.create(guard),{type:"advance",days:1});assert.equal(watch.counts.guard,1);assert.equal(watch.gold.h,950);assert.equal(watch.sinks,50);
const declined=JSON.parse(JSON.stringify(war));declined.relations=[{a:"h",b:"j",value:-1}];
const d=L.command(declined,L.create(declined),{type:"advance",days:1});assert.equal(d.counts.declined,1);assert.equal(d.counts.accepted,0);assert.equal(L.command(declined,d,{type:"advance",days:1}).counts.offer,1);
console.log("SYNTHETIC_POLICY_BRANCHES PASS");
console.log("ACTUAL_FIXTURE_CHECKS PASS",JSON.stringify({baseline:L.metrics(s),day7:L.metrics(a),day90:L.metrics(r),shockDay7:L.metrics(rr)}));
fs.writeFileSync(process.argv[3],JSON.stringify(L.exportHistory(b,r)),{flag:"wx"});

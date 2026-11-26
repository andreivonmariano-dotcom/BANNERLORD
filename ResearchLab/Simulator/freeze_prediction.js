/* Freeze a baseline-only experimental forecast; no game writes or outcome input. */
"use strict";
const fs=require("fs"),crypto=require("crypto"),path=require("path");
const hash=b=>crypto.createHash("sha256").update(b).digest("hex");
function freeze(b,lab,targetTick,ids){
 if(!Number.isSafeInteger(targetTick)||targetTick<=b.clocks[0].ticks||targetTick-b.clocks[0].ticks>30*lab.DAY)throw Error("target");
 if(!Array.isArray(ids)||!ids.length||new Set(ids).size!==ids.length||ids.some(id=>typeof id!=="string"))throw Error("ids");
 const selected=ids.map(id=>{const matches=Object.entries(b.forts).filter(([k,r])=>r.id===id);if(matches.length!==1)throw Error("ambiguous target");return matches[0]});
 const s=lab.command(b,lab.create(b),{type:"advanceTicks",ticks:targetTick-b.clocks[0].ticks});
 return {baseline_tick:b.clocks[0].ticks,target_tick:s.ticks,forecast:selected.map(([k,r])=>({id:r.id,kind:r.kind,persistence:{gold:r.gold,security:r.security,prosperity:r.prosperity},experimental:s.forts[k]})),unmodeled:["route","recruitment","healing","tax","actual transactions"],scope:"experimental predictions only"};
}
if(require.main===module){
 const [fixture,core,target,idFile,out,expectedFixture,expectedCore]=process.argv.slice(2),f=fs.readFileSync(fixture),c=fs.readFileSync(core);
 if(hash(f)!==expectedFixture||hash(c)!==expectedCore)throw Error("source identity");
 const b=JSON.parse(f),lab=require(path.resolve(core)),ids=JSON.parse(fs.readFileSync(idFile,"utf8"));
 const result={version:1,fixture_sha256:hash(f),core_sha256:hash(c),...freeze(b,lab,Number(target),ids)};
 fs.writeFileSync(out,JSON.stringify(result,null,2),{flag:"wx"});console.log("FROZEN",result.forecast.length,"SHA256",hash(fs.readFileSync(out)));
}
module.exports={freeze};

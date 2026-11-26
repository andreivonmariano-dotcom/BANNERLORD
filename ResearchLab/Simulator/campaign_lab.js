/* Original experimental policy, not Bannerlord AI or a native scheduler.
 * One implementation shared by Node and offline browser. No external I/O. */
"use strict";
const Lab=(()=>{
const DAY=864000000, HOUR=36000000, MAX_DAYS=90, MAX_COMMANDS=256;
const clone=x=>JSON.parse(JSON.stringify(x));
const keys=o=>Object.keys(o).sort();
const pair=(a,b)=>[a,b].sort().join("|");
const clamp=(v,a,b)=>Math.min(b,Math.max(a,v));
const finite=(x)=>typeof x==="number"&&Number.isFinite(x);
const int=(x)=>Number.isSafeInteger(x);
function require(ok,msg){if(!ok)throw Error(msg)}
function roundEven(x){const f=Math.floor(x);return x-f===.5?(f%2===0?f:f+1):Math.round(x)}
const f=Math.fround;
function market(p,g){return roundEven(f(f(f(10000+f(f(p)*12))-f(g))*.25))}
function security(s,p){return f(f(-f(s-50))/15)+Math.max(-5,f(f(-.0005)*f(p)))}
function housing(p){for(const [l,d] of [[250,6],[500,5],[750,4],[1000,3],[1250,2],[1500,1]])if(p<l)return d;for(const [l,d] of [[21000,-6],[18000,-5],[15000,-4],[12000,-3],[9000,-2],[6000,-1]])if(p>l)return d;return 0}
function date(t){const d=Math.floor(t/DAY);return {year:Math.floor(d/84),season:["Spring","Summer","Autumn","Winter"][Math.floor(d%84/21)],day:d%21+1,hour:Math.floor(t%DAY/HOUR)}}
function total(s){return Object.values(s.gold).reduce((a,b)=>a+b,0)+Object.values(s.forts).reduce((a,b)=>a+b.gold,0)}
function create(b){
 require(b.version===1&&b.ticks_per_hour===HOUR&&b.days_per_year===84&&b.clocks[0].mode==="Default","fixture");
 const t=b.clocks[0].ticks;require(int(t)&&t>=0,"clock");
 const s={ticks:t,start:t,next:Math.floor(t/DAY)*DAY+DAY,days:0,gold:{},forts:{},relation:{},truce:{},cooldown:{},log:[],counts:{economy:0,guard:0,aid:0,offer:0,accepted:0,declined:0,hold:0,shock:0},external:0,sinks:0,commands:[]};
 for(const k of keys(b.heroes)){const g=b.heroes[k].gold;require(int(g)&&g>=0,"hero gold");s.gold[k]=g}
 for(const k of keys(b.forts)){const r=b.forts[k];require(int(r.gold)&&r.gold>=0&&finite(r.security)&&r.security>=0&&r.security<=100&&finite(r.prosperity)&&r.prosperity>=0,"fort");s.forts[k]={gold:r.gold,security:r.security,prosperity:r.prosperity}}
 s.initial=total(s);return s
}
function validate(b,s){
 require(int(s.ticks)&&s.ticks>=s.start&&s.ticks<=s.start+MAX_DAYS*DAY&&s.next>s.ticks&&s.next%DAY===0,"clock invariant");
 require(s.days===Math.floor(s.ticks/DAY)-Math.floor(s.start/DAY),"day invariant");
 for(const g of Object.values(s.gold))require(int(g)&&g>=0,"gold");
 for(const r of Object.values(s.forts))require(int(r.gold)&&r.gold>=0&&finite(r.security)&&r.security>=0&&r.security<=100&&finite(r.prosperity)&&r.prosperity>=0,"fort state");
 for(const r of Object.values(s.relation))require(int(r)&&r>=-100&&r<=100,"relation");
 require(total(s)===s.initial+s.external-s.sinks,"money conservation");
 require(s.log.length<=300&&s.commands.length<=MAX_COMMANDS,"bounded history");
 return s
}
function log(s,type,actor,target,detail){s.log.push({tick:s.ticks,type,actor,target,detail});if(s.log.length>300)s.log.shift()}
function faction(b,c){return b.clans[c]?.kingdom||c}
function leaderClan(b,k){return b.clans[k]?k:b.kingdoms[k]?.ruling_clan}
function playerClan(b,c){return b.clans[c]?.id==="player_faction"||b.heroes[b.clans[c]?.leader]?.id==="main_hero"}
function opposing(b,s,c){const f=faction(b,c),out=[];for(const w of b.wars){const a=w.faction1,d=w.faction2;if(s.truce[pair(a,d)])continue;const other=a===f?d:d===f?a:a===c?d:d===c?a:null;if(other)out.push({a,d,other})}return out}
function relation(b,s,a,c){const k=pair(a,c);if(Object.hasOwn(s.relation,k))return s.relation[k];return b.relations.find(x=>x.a&&x.b&&pair(x.a,x.b)===k)?.value??0}
function budget(s,h,cost){return s.gold[h]>=500+cost}
function daily(b,s){
 s.days++;
 // Economy: partial default arithmetic, external market correction; no taxes/wages.
 for(const k of keys(s.forts)){const r=s.forts[k],src=b.forts[k];r.security=clamp(r.security+security(r.security,r.prosperity),0,100);if(src.kind==="town"){r.prosperity=Math.max(0,r.prosperity+housing(r.prosperity));const delta=market(r.prosperity,r.gold);r.gold+=delta;s.external+=delta} s.counts.economy++}
 // Snapshot of stationary regular personnel, not pathfinding or troop assignment.
 const guards={};const strengths={};
 for(const p of Object.values(b.parties)){if(!p.active||p.at_sea||!p.clan||!int(p.healthy))continue;strengths[p.clan]=(strengths[p.clan]||0)+p.healthy;if(p.settlement&&b.forts[p.settlement]?.owner===p.clan)guards[p.settlement]=(guards[p.settlement]||0)+p.healthy}
 // Each active nonplayer clan leader gets one deterministic choice per day.
 for(const c of keys(b.clans)){
  const h=b.clans[c].leader,hero=b.heroes[h];if(!hero||hero.state!=="Active"||playerClan(b,c))continue;
  const war=opposing(b,s,c),options=[];
  for(const k of keys(b.forts))if(b.forts[k].owner===c&&guards[k]>=10&&s.forts[k].security<60&&budget(s,h,50))options.push({type:"guard",target:k,score:60-s.forts[k].security+(war.length?20:0)});
  const kin=[...new Set([hero.spouse,hero.father,hero.mother,...(hero.children||[])])].filter(k=>k&&k!==h&&b.heroes[k]?.state==="Active"&&s.gold[k]<200);
  for(const k of kin)if(b.heroes[k].id!=="main_hero"&&budget(s,h,100))options.push({type:"aid",target:k,score:30+(200-s.gold[k])/20});
  for(const w of war){const own=w.a===w.other?w.d:w.a;if(leaderClan(b,own)!==c)continue;const other=leaderClan(b,w.other),oh=b.clans[other]?.leader;if(!other||other===c||!oh||b.heroes[oh]?.state!=="Active"||playerClan(b,other)||!budget(s,h,100)||s.days<(s.cooldown[pair(w.a,w.d)]||0))continue;
   options.push({type:"offer",target:oh,other,war:pair(w.a,w.d),score:20+Math.min(30,(strengths[other]||0)/Math.max(1,strengths[c]||0)*10)})
  }
  options.sort((a,b)=>b.score-a.score||((a.type+"|"+a.target)<(b.type+"|"+b.target)?-1:(a.type+"|"+a.target)>(b.type+"|"+b.target)?1:0));
  const a=options[0];if(!a){s.counts.hold++;continue}
  if(a.type==="guard"){s.gold[h]-=50;s.sinks+=50;s.forts[a.target].security=clamp(s.forts[a.target].security+1,0,100);s.counts.guard++;log(s,"guard",h,a.target,"50 denars spent; experimental local watch +1 security; no troops moved")}
  else if(a.type==="aid"){s.gold[h]-=100;s.gold[a.target]+=100;s.relation[pair(h,a.target)]=clamp(relation(b,s,h,a.target)+1,-100,100);s.counts.aid++;log(s,"aid",h,a.target,"100 denars transferred; experimental raw relation +1")}
  else {s.gold[h]-=100;s.sinks+=100;s.cooldown[a.war]=s.days+7;s.counts.offer++;
   const accepted=relation(b,s,h,a.target)>=0&&(strengths[c]||0)<2*Math.max(1,strengths[a.other]||0)&&(strengths[a.other]||0)<2*Math.max(1,strengths[c]||0);
   if(accepted){s.truce[a.war]=true;s.counts.accepted++}else s.counts.declined++;
   log(s,"offer",h,a.target,accepted?"Experimental truce overlay accepted; 100 denars spent":"Experimental overture declined; 100 denars spent; 7-day cooldown");
  }
 }
}
function command(b,state,c){
 require(state.commands.length<MAX_COMMANDS,"command limit");const s=clone(state);
 require(c&&typeof c==="object","command");
 if(c.type==="advance"){require(int(c.days)&&c.days>=1&&c.days<=30&&s.ticks+c.days*DAY<=s.start+MAX_DAYS*DAY,"advance 1–30 days; max 90");
  const end=s.ticks+c.days*DAY;while(s.next<=end){s.ticks=s.next;s.next+=DAY;daily(b,s)}s.ticks=end;
 }else if(c.type==="shock"){
  require(Object.hasOwn(s.forts,c.target)&&["security","market"].includes(c.kind),"shock target/type");
  const r=s.forts[c.target];if(c.kind==="security"){r.security=clamp(r.security-10,0,100)}else{require(b.forts[c.target].kind==="town","market only town");const loss=Math.min(1000,r.gold);r.gold-=loss;s.external-=loss}
  s.counts.shock++;log(s,"shock",null,c.target,c.kind==="security"?"User scenario: security −10 (clamped)":"User scenario: up to 1,000 market gold removed");
 }else throw Error("unknown command");
 s.commands.push(clone(c));return validate(b,s)
}
function replay(b,history){require(history&&history.version===1&&history.source_sha===b.source_sha&&history.projection_sha===b.projection_sha&&Array.isArray(history.commands)&&history.commands.length<=MAX_COMMANDS,"history identity/limit");let s=create(b);for(const c of history.commands)s=command(b,s,c);return s}
function exportHistory(b,s){return {version:1,source_sha:b.source_sha,projection_sha:b.projection_sha,commands:clone(s.commands)}}
function metrics(s){return {date:date(s.ticks),days:s.days,counts:s.counts,external:s.external,sinks:s.sinks,total:total(s),initial:s.initial,meanSecurity:Object.values(s.forts).reduce((a,b)=>a+b.security,0)/Object.keys(s.forts).length,truce:Object.keys(s.truce).length}}
return {create,command,replay,exportHistory,metrics,date,DAY,HOUR,market,security,housing};
})();
if(typeof module!=="undefined")module.exports=Lab;

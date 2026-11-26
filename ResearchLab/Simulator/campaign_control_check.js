// Inject in a private test copy only; DOM event checks.
try{
const baseline=JSON.stringify(S);
document.querySelector('[data-days="7"]').click();
if(S.days!==7||S.counts.economy!==931||!document.getElementById("clock").textContent.includes("Summer 8"))throw Error("advance");
const before=JSON.stringify(S),h=Lab.exportHistory(B,S);restoreHistory(h);
if(JSON.stringify(S)!==before)throw Error("history restore");
const k=Object.keys(B.forts).find(k=>B.forts[k].kind==="town");
document.getElementById("fort").value=k;document.getElementById("fort").dispatchEvent(new Event("change"));
const sec=S.forts[k].security;document.getElementById("securityShock").click();if(S.forts[k].security!==Math.max(0,sec-10))throw Error("security shock");
const gold=S.forts[k].gold;document.getElementById("marketShock").click();if(S.forts[k].gold!==Math.max(0,gold-1000))throw Error("market shock");
const state=JSON.stringify(S);try{restoreHistory({source_sha:"wrong"})}catch(e){}
if(JSON.stringify(S)!==state)throw Error("invalid history changed state");
window.confirm=()=>true;document.getElementById("reset").click();if(JSON.stringify(S)!==baseline)throw Error("reset");
document.documentElement.dataset.interaction="pass";
}catch(e){document.documentElement.dataset.interaction="fail:"+e.message}

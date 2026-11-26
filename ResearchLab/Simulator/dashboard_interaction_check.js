// Inject only into a separate private local atlas test copy, then check data-interaction.
try {
document.getElementById("stage").value="2";document.getElementById("stage").dispatchEvent(new Event("change"));
if(W!==DATA.worlds[2]||document.getElementById("map").children.length!==2268)throw Error("stage/map");
document.getElementById("category").value="heroes";document.getElementById("query").value="main_hero";document.getElementById("query").dispatchEvent(new Event("input"));
if(document.getElementById("list").children.length!==1)throw Error("search");
document.getElementById("list").firstElementChild.click();
if(all[selected].id!=="main_hero"||!document.getElementById("detail").textContent.includes("Stored personal relations"))throw Error("hero");
const before=selected;document.getElementById("player").click();
if(all[selected].id!=="player_party"||selected===before||!document.getElementById("detail").textContent.includes("Troop roster"))throw Error("party");
document.getElementById("ports").checked=true;document.getElementById("ports").dispatchEvent(new Event("change"));
if(document.getElementById("map").children.length!==2385)throw Error("ports");
document.getElementById("parties").checked=false;document.getElementById("parties").dispatchEvent(new Event("change"));
if(document.getElementById("map").children.length!==657)throw Error("toggle");
document.getElementById("map").firstElementChild.dispatchEvent(new Event("click"));
if(all[selected].type!==330095)throw Error("point click");
document.documentElement.dataset.interaction="pass";
}catch(e){document.documentElement.dataset.interaction="fail:"+e.message}

import sqlite3,pathlib,os,hashlib
p=pathlib.Path(os.environ["LOCALAPPDATA"])/"BannerlordResearchLab/Simulator/AssemblyIndex/c4ffb3eaf13372a879ca858e115aee7941965b58/assemblies.sqlite"
with p.open("rb") as f: assert hashlib.file_digest(f,"sha256").hexdigest()=="94462c1fc7e1526aa8cbdaa60e445a142a9a45f0ce81260d08decd3b94771aca"
db=sqlite3.connect(p.as_uri()+"?mode=ro",uri=True)
for name in ("PartyFoodConsumptionModel","VolunteerModel","PartyWageModel","SettlementTaxModel","SettlementFoodModel","PartyHealingModel","PartySizeLimitModel","PartySpeedCalculatingModel"):
    print("MODEL",name)
    for t,s in db.execute("select type,signature from members where kind='method' and type like ? and name not glob 'get_*' and name not glob 'set_*' and name not like '.%' order by name",("%ComponentInterfaces."+name,)):
        print(s)
print("EVENTS")
for n,s in db.execute("select name,signature from members where kind='property' and type='TaleWorlds.CampaignSystem.CampaignEvents' order by name"):
    if any(x in n.lower() for x in ("recruit","heal","food","trade","settlemententered","settlementleft","hourlytick","dailytick","gold","tax")): print(n,s)
print("FOOD AND SPEED MODELS")
for t,s in db.execute("select type,signature from members where kind='method' and (type like '%FoodConsumptionModel' or type like '%PartySpeedCalculatingModel') and name not glob 'get_*' and name not glob 'set_*' and name not like '.%' order by type,name"):
    if 'ComponentInterfaces' in t or 'GameComponents' in t: print(t,s)
db.close()

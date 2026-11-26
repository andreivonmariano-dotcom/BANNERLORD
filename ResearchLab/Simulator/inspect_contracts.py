import sqlite3, pathlib, os, hashlib
p=pathlib.Path(os.environ["LOCALAPPDATA"])/"BannerlordResearchLab/Simulator/AssemblyIndex/c4ffb3eaf13372a879ca858e115aee7941965b58/assemblies.sqlite"
assert hashlib.file_digest(p.open("rb"),"sha256").hexdigest()=="94462c1fc7e1526aa8cbdaa60e445a142a9a45f0ce81260d08decd3b94771aca"
db=sqlite3.connect(p.as_uri()+"?mode=ro",uri=True)
terms=["PartyHourlyAiTick","GetDistance","GetPartyFoodConsumption","GetDailyFoodConsumption","GetDailyHealingHpForRegulars","GetDailyHealingForRegulars","GetDailyHealingHpForHeroes","GetMaxVolunteerTier","GetRecruitmentCost","GetDailyRecruitmentCost","CalculateClanGoldChange","CalculateClanIncome","CalculateClanExpenses","CalculateDailyTax","CalculateDailyProductionAmount","GetTownGoldChange","GetDailyFoodChange","GetDailyWage"]
for t in terms:
    rows=db.execute("select type,signature from members where kind='method' and name=? order by type",(t,)).fetchall()
    print(t,len(rows))
    for a,b in rows[:15]: print(a, b)
db.close()

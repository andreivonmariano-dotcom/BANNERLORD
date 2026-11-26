import sqlite3,pathlib,os,hashlib
p=pathlib.Path(os.environ['LOCALAPPDATA'])/'BannerlordResearchLab/Simulator/AssemblyIndex/c4ffb3eaf13372a879ca858e115aee7941965b58/assemblies.sqlite'
with p.open('rb') as f: assert hashlib.file_digest(f,'sha256').hexdigest()=='94462c1fc7e1526aa8cbdaa60e445a142a9a45f0ce81260d08decd3b94771aca'
db=sqlite3.connect(p.as_uri()+'?mode=ro',uri=True)
names=('NavalDLCPartyWageModel','NavalDLCMobilePartyFoodConsumptionModel','NavalDLCPartyHealingModel','NavalDLCClanFinanceModel','NavalDLCMapDistanceModel','NavalDLCPartySpeedCalculatingModel')
for n in names:
 print('TYPE',n)
 for t,s in db.execute("select type,signature from members where kind='method' and type like ? order by name",('%.'+n,)): print(s)
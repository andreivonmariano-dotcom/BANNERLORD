import sqlite3,pathlib,os,hashlib
p=pathlib.Path(os.environ['LOCALAPPDATA'])/'BannerlordResearchLab/Simulator/AssemblyIndex/c4ffb3eaf13372a879ca858e115aee7941965b58/assemblies.sqlite'
with p.open('rb') as f: assert hashlib.file_digest(f,'sha256').hexdigest()=='94462c1fc7e1526aa8cbdaa60e445a142a9a45f0ce81260d08decd3b94771aca'
db=sqlite3.connect(p.as_uri()+'?mode=ro',uri=True)
for t,n,s in db.execute("select type,name,signature from members where kind='method' and ((type glob 'NavalDLC.GameComponents.*' and (type like '%Ship%' or type like '%Crew%' or type like '%Skill%' or type like '%Speed%')) or (type like '%DefaultMarriageModel' or type like '%DefaultDiplomacyModel' or type like '%DefaultClanPoliticsModel' or type like '%DefaultKingdomDecisionPermissionModel')) and name not glob '.*' and name not glob 'set_*' order by type,name"):
 print(t,s)
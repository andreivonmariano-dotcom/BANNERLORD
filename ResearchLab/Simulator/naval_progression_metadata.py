import sqlite3,pathlib,os
p=pathlib.Path(os.environ['LOCALAPPDATA'])/'BannerlordResearchLab/Simulator/AssemblyIndex/c4ffb3eaf13372a879ca858e115aee7941965b58/assemblies.sqlite'
db=sqlite3.connect(p.as_uri()+'?mode=ro',uri=True)
for t,n,s in db.execute("select type,name,signature from members where kind='method' and type glob 'NavalDLC.*' and (name like '%SkillXp%' or name like '%SkillExperience%' or name like '%Boatswain%' or name like '%Shipmaster%' or name like '%ShipCaptain%') and name not glob '.*' order by type,name"):
 print(t,s)
import sqlite3,os
c=sqlite3.connect(os.environ["CU80_DB"])
for row in c.execute("select type,name,record from members where kind='method' and (type='TaleWorlds.SaveSystem.FileDriver' or type='TaleWorlds.Library.PlatformDirectoryPath') and (name like '%Path%' or name like '%SaveGameFileNames%' or name like '%Directory%' or name='.ctor')"):print(*row)
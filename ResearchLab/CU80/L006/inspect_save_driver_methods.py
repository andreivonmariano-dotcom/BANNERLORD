import sqlite3,os
c=sqlite3.connect(os.environ["CU80_DB"])
for row in c.execute("select name,signature from members where type='TaleWorlds.SaveSystem.AsyncFileSaveDriver' and kind='method'"):print(*row)
for row in c.execute("select type,name,signature from members where kind='method' and (name like '%SaveGame%Path%' or name like '%SaveDirectory%' or name like '%SaveGame%Directory%')"):print(*row)
import sqlite3,os
c=sqlite3.connect(os.environ["CU80_DB"])
for row in c.execute("select distinct assembly,type from members where type like '%SaveDriver%' or type like '%SaveGameFileDriver%' or type like '%PlatformFilePath%'"): print(*row)
for row in c.execute("select type,name,signature from members where kind='method' and (type like '%SaveDriver%' or type like '%PlatformFilePath%') and (name in ('.ctor','GetSaveFileNames','GetSaveFiles','Save','Load','GetSaveFilePath','GetFileFullPath') or name like '%Path%' or name like '%Directory%')"):print(*row)
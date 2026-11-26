import sqlite3,os
c=sqlite3.connect(os.environ["CU80_DB"])
for t in ["MBSaveLoad","SaveManager","PlatformFileHelper","FileHelper","SaveHandler"]:
 print("TYPE",t)
 for row in c.execute("select type,name,record from members where type like ? and kind='method' and (name like '%Path%' or name like '%Save%' or name like '%Directory%')",("%."+t,)): print(*row)
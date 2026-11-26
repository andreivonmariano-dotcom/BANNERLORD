import sqlite3,os
c=sqlite3.connect(os.environ["CU80_DB"])
for row in c.execute("select name,record from members where type='TaleWorlds.CampaignSystem.CampaignTime' and name in ('get_CurrentTicks','get_NumTicks','get_ToHours')"): print(*row)
for row in c.execute("select type,name,signature from members where name like '%AutoSave%' and kind='method'"):print(*row)
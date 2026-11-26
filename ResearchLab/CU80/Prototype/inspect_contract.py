import sqlite3,os
c=sqlite3.connect("file:"+os.environ["CU80_DB"].replace("\\","/")+"?mode=ro",uri=True)
for t in ["CampaignTime","TroopRoster","TroopRosterElement","CampaignOptions","BannerlordConfig","Campaign","CampaignSaveSystem"]:
 print("TYPE",t)
 for row in c.execute("select type,name,signature from members where type like ? and (name like '%Tick%' or name like '%Save%' or name like '%Roster%' or name in ('GetTroopRoster','get_Now','get_Count','GetElementCopyAtIndex','get_Number','get_Character','get_Models','get_TotalManCount','ToHours','get_ToHours','Number','Character'))",("%."+t,)):
  print(*row)
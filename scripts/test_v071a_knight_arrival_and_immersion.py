#!/usr/bin/env python3
"""v0.71a source gates for Knight arrival and first audience.

Static contracts only. They do not establish map pathfinding, actual
conversation rendering, save-load safety or full campaign play.
"""
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"Source"/"CalradiaUnified"
PREFABS=ROOT/"Modules"/"CalradiaUnified"/"GUI"/"Prefabs"
read=lambda filename:(SOURCE/filename).read_text(encoding="utf-8")
origin=read("SandboxOriginBehavior.cs")
arrival=read("SandboxOriginBehavior.KnightArrival.cs")
dialogue=read("SandboxOriginBehavior.KnightDialogue.cs")

# The position edit is a one-shot new-character action, never a
# load-game or daily repeating teleport.
assert origin.count("IntroduceKnightHouseholdAndArrive(patron);")==1
assert 'if(oathStatus=="oath_recorded")' in origin
assert 'profile.KnightGuidedStart &&' in origin
assert 'profile.KnightOrigin=="SwornHouse"' in origin
assert 'knightOath.Begin(' in origin
assert 'if (failures.Length != 0)' in origin
assert 'cu_knight_patron_arrival' in arrival
assert 'cu_knight_patron_house_known' in arrival
assert 'knightOath.PatronId!=patron.StringId' in arrival
assert 'knightOath.PlayerId!=player.StringId' in arrival
assert 'host.LeaderHero!=patron' in arrival
assert 'var residence=patron.CurrentSettlement;' in arrival
assert 'residence.GatePosition' in arrival
assert 'player.CurrentSettlement!=null' in arrival
assert 'party.LeaderHero!=player' in arrival
assert 'party.SetPositionAfterMapChange(host.Position)' in arrival
assert 'party.GetPosition2D.Distance(' in arrival
assert 'bool arrived=after <= (residence==null ? 0.5f : 6f);' in arrival
assert 'member.IsKnownToPlayer=true;' in arrival
assert 'h.Clan==patron.Clan && h.IsLord' in arrival
assert 'no_relation_or_clan_transfer=true' in arrival
assert 'ChangeClanLeaderAction' not in arrival
assert 'ChangeKingdomAction' not in arrival
assert 'ChangeRelationAction' not in arrival
assert 'OnGameLoaded' not in arrival
assert 'DailyTick' not in arrival

# The first speech is gated by the actual one-to-one conversation hero
# and consumes the real native muster receipt once, not fake menu credit.
assert 'Hero.OneToOneConversationHero' in dialogue
assert 'speaker==patron' in dialogue
assert 'knightOath.LastMusterDay<0' in dialogue
assert 'cu_knight_patron_first_audience_reply' in dialogue
assert 'RecordNativePatronAudience()' in dialogue
assert 'knightOath.RecordMuster(day,true)' in dialogue
assert 'hero_main_options' in dialogue
assert 'I have heard your oath' in dialogue
assert 'cu_knight_ceremony_pledge' in dialogue
assert 'cu_knight_ceremony_defer' in dialogue
assert 'cu_knight_ceremony_house' in dialogue
assert 'cu_knight_ceremony_duties' in dialogue

for name,backdrop in (("CUNobleDossier","#100D0A99"),
                       ("CUFamilyPortraits","#110D0999")):
    root=ET.parse(PREFABS/(name+".xml")).getroot()
    assert root.tag=="Prefab"
    assert any(widget.get("Color")==backdrop for widget in root.iter())
    assert any(w.get("Command.Click")=="ExecuteClose"
               for w in root.iter())

dossier=ET.parse(PREFABS/"CUNobleDossier.xml").getroot()
for key in ("Family","Officers","Duty","Footer"):
    text=next(x for x in dossier.iter("TextWidget")
              if x.get("Text")=="@"+key)
    assert text.get("Brush.TextHorizontalAlignment")=="Left",key
    assert text.get("Brush.TextVerticalAlignment")=="Top",key
    assert int(text.get("Brush.FontSize"))>=15,key
gallery=ET.parse(PREFABS/"CUFamilyPortraits.xml").getroot()
members=next(x for x in gallery.iter("ListPanel")
             if x.get("DataSource")=="{Members}")
assert members.get("StackLayout.LayoutMethod")=="VerticalTopToBottom"
print("PASS v0.71a STATIC: one-shot guarded Knight arrival, known "
      "native patron household, genuine first meeting and readable "
      "Gauntlet layouts. Native game acceptance remains pending.")

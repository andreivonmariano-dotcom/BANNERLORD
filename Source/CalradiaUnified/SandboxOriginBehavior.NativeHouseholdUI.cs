using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.ObjectSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// Read-only, native Bannerlord inquiry for the actual household.
    /// Existing game screens remain the authority for family,
    /// governors, parties and kingdom membership. No new persistence.
    /// </summary>
    public sealed partial class SandboxOriginBehavior
    {
        private static Hero ResolveCampaignHero(string id)
        {
            if(String.IsNullOrWhiteSpace(id))return null;
            // Generated and native Hero objects can be registered in the
            // live campaign without resolving through GetObject<Hero>.
            // The saved ID stays authoritative, never the display label.
            var living=Hero.AllAliveHeroes.FirstOrDefault(h=>h!=null &&
                h.StringId==id);
            if(living!=null)return living;
            var manager=MBObjectManager.Instance;
            var hero=manager==null?null:manager.GetObject<Hero>(id);
            return hero!=null && hero.StringId==id?hero:null;
        }
        private static string NativeHeroDisplayName(string id)
        {
            if(String.IsNullOrWhiteSpace(id))return "none";
            var hero=ResolveCampaignHero(id);
            return hero!=null && hero.Name!=null?
                hero.Name.ToString():"native Hero not currently resolved";
        }
        private static string NativeHeroDisplayNames(string csv)
        {
            if(String.IsNullOrWhiteSpace(csv))return "none";
            return String.Join(", ",csv.Split(',')
                .Where(id=>!String.IsNullOrWhiteSpace(id))
                .Take(6).Select(NativeHeroDisplayName));
        }
        private static string NativePersonName(Hero hero)
        {
            return hero==null || hero.Name==null?"none":
                hero.Name.ToString();
        }

        private void ShowStartingHouseholdStatus()
        {
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            if(player==null || clan==null)return;
            var children=player.Children==null?new Hero[0]:
                player.Children.Where(h=>h!=null && h.IsAlive)
                    .OrderBy(h=>h.StringId,StringComparer.Ordinal)
                    .Take(6).ToArray();
            var officers=clan.Companions==null?new Hero[0]:
                clan.Companions.Where(h=>h!=null && h.IsAlive &&
                        h.CompanionOf==clan)
                    .OrderBy(h=>h.StringId,StringComparer.Ordinal)
                    .Take(8).ToArray();
            string family="Spouse: "+NativePersonName(player.Spouse)+
                ". Living children ("+children.Length+"): "+
                (children.Length==0?"none":
                    String.Join(", ",children.Select(NativePersonName)))+
                ". Native clan: "+clan.Name+".";
            string assignment="Officers in native companion roster ("+
                officers.Length+"): "+(officers.Length==0?"none":
                    String.Join(", ",officers.Select(NativePersonName)))+".";
            string role="Unverified starting role.";
            string type="none";
            if(knightOath.IsConfigured)
            {
                var patron=KnightOathPatron();
                type="Knight";
                role="Sworn Knight, separate clan. Named patron: "+
                    (patron==null?"not currently resolved":
                        NativePersonName(patron))+
                    ". Current objective: "+
                    (knightOath.LastMusterDay<0?
                        "Find your lord and complete a first audience":
                        "Report to your lord before the next muster")+
                    ". Whereabouts: "+KnightPatronWhereabouts(patron)+
                    ". Personal oath: "+knightOath.Status+
                    ". Native relation: "+(patron==null?"unavailable":
                        player.GetRelation(patron).ToString())+
                    ". Next muster day: "+knightOath.NextMusterDay+
                    ". Field escort: "+knightEscort.Status+
                    " ("+knightEscort.ConsecutiveDays+"/"+ 
                    SandboxKnightEscortState.RequiredDays+" days)."+
                    " No native kingdom vassalage or clan transfer.";
            }
            else if(lordInheritance.IsRecorded)
            {
                type="Lord";
                role="Native vassal house. Clan leader: "+
                    NativePersonName(clan.Leader)+
                    ". Native holdings: "+(clan.Settlements==null?0:
                        clan.Settlements.Count)+
                    ". Preferred living child: "+
                    NativeHeroDisplayName(lordInheritance.PrimaryHeirId)+
                    ". Recorded inheritance custom: "+
                    lordInheritance.Rule+
                    ". This does not transfer clan leadership or land.";
            }
            else if(kingSuccession.IsRecorded)
            {
                type="King";
                var minister=NativeCourtOfficer("Royal Minister");
                var marshal=NativeCourtOfficer("Marshal");
                var guard=NativeCourtOfficer("Kingsguard");
                role="Native crown: "+
                    (clan.Kingdom!=null &&
                     clan.Kingdom.RulingClan==clan?"verified ruling clan":
                     "rulership not verified")+
                    ". Marshal: "+NativePersonName(marshal)+
                    ". ONE Kingsguard Hero: "+NativePersonName(guard)+
                    ". Royal Minister: "+NativePersonName(minister)+
                    ". Governor: "+(minister!=null &&
                        SandboxKingNativeMinistry.VerifiedGovernor(minister)?
                        minister.GovernorOf.Settlement.Name.ToString():
                        "not verified")+
                    ". Court: "+(royalCouncil.IsRecorded?
                        "recorded":"not commissioned")+
                    ". Native kingdom decisions remain authoritative.";
            }
            DiagnosticJournal.Event("cu_native_household_display_opened",
                "result=OPENED|role="+type+
                "|spouse_present="+(player.Spouse!=null)+
                "|live_children="+children.Length+
                "|native_companions="+officers.Length+
                "|native_read_only=true");
            ShowNativeDetail("Starting household | "+type,
                family+"\n\n"+assignment+"\n\n"+role+
                "\n\nNative Clan, Party and Kingdom screens show "+
                "the current game-owned state.");
        }
    }
}

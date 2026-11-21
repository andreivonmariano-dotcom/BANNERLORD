using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;

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

        // Entry shared by the required UIExtenderEx mixin buttons in the
        // native Clan, Party and Kingdom views. The actual campaign
        // behavior owns all role facts and save-backed identities.
        public static bool OpenFromNativeBUTRScreen()
        {
            var campaign=Campaign.Current;
            var b=campaign==null?null:
                campaign.GetCampaignBehavior<SandboxOriginBehavior>();
            if(b==null || Hero.MainHero==null ||
                Clan.PlayerClan==null)return false;
            b.ShowStartingHouseholdStatus();
            return true;
        }
        public static string NativeBUTRStatus(string context)
        {
            if(Campaign.Current==null || Hero.MainHero==null ||
                Clan.PlayerClan==null)return "Campaign records unavailable";
            // Short native-tab labels are deliberate. Names, family and
            // duties belong in the full dossier, not a clipped button.
            if(context=="Party")return "Company and officers";
            if(context=="Kingdom")return "Royal and kingdom affairs";
            return "Household and career dossier";
        }
        private static string NativeDossierTitle(string type)
        {
            switch(type)
            {
                case "Wanderer":return "Wanderer | Life and company";
                case "Outlaw":return "Outlaw | Band and allegiance";
                case "Mercenary":return "Mercenary | Company record";
                case "Soldier":return "Soldier | Enlistment record";
                case "Knight":return "Knight | Oath and household";
                case "Lord":return "Lord | House and estates";
                case "King":return "King | Crown and court";
                default:return "Calradia Unified | Campaign record";
            }
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
                "\nChildren ("+children.Length+"): "+
                (children.Length==0?"none":
                    String.Join(", ",children.Select(NativePersonName)))+
                "\nHouse: "+clan.Name+".";
            string assignment="Officers and companions ("+
                officers.Length+"):\n"+(officers.Length==0?
                    "None appointed":
                    String.Join("\n",officers.Select(NativePersonName)))+".";
            string role="Unverified starting role.";
            string type="none";
            if(knightOath.IsConfigured)
            {
                var patron=KnightOathPatron();
                type="Knight";
                role="Sworn to "+NativePersonName(patron)+". "+
                    (knightOath.LastMusterDay<0?
                        "First duty: meet your lord in person.":
                        "Next audience: day "+knightOath.NextMusterDay+".")+
                    "\nStanding with lord: "+(patron==null?
                        "unknown":player.GetRelation(patron).ToString())+
                    " | Escort: "+knightEscort.Status+
                    "\nYour household remains independent.";
            }
            else if(lordInheritance.IsRecorded)
            {
                type="Lord";
                role="Lord of "+clan.Name+". "+
                    "Household head: "+NativePersonName(clan.Leader)+
                    "\nEstates held: "+(clan.Settlements==null?0:
                        clan.Settlements.Count)+
                    " | Named heir: "+
                    NativeHeroDisplayName(lordInheritance.PrimaryHeirId)+
                    "\nInheritance custom: "+lordInheritance.Rule+
                    ". Claims do not automatically grant land.";
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
            else if(roleEconomy.Started)
            {
                // These four non-noble paths retain their own identities
                // and verified campaign state, without fake kingdom
                // standing, salaries, commands or noble households.
                type=roleEconomy.Role=="EnlistedSoldier"?
                    "Soldier":roleEconomy.Role;
                if(type!="Wanderer" && type!="Outlaw" &&
                   type!="Mercenary" && type!="Soldier")
                    type="Campaign";
                string affiliation=clan.Kingdom==null?
                    "No native kingdom affiliation recorded":
                    "Current native kingdom: "+clan.Kingdom.Name;
                role="Career: "+type+
                    ". Actual recorded state: "+
                    roleEconomy.CareerStatus+
                    ". Native denars observed: "+
                    roleEconomy.LastGold+
                    ". "+affiliation+
                    ". Native payroll evidence: "+
                    (roleEconomy.VerifiedExternalDailyWage<0?
                        "not confirmed":
                        roleEconomy.VerifiedExternalDailyWage+
                        " daily from the external provider")+
                    ". This screen does not grant rank, equipment, "+
                    "land, kingdom membership or free troops.";
            }
            DiagnosticJournal.Event("cu_native_household_display_opened",
                "result=OPENED|role="+type+
                "|spouse_present="+(player.Spouse!=null)+
                "|live_children="+children.Length+
                "|native_companions="+officers.Length+
                "|native_read_only=true");
            // Use a native Gauntlet screen for clear reading rather than
            // sending several paragraphs through a one-button inquiry.
            // The prior native inquiry remains as an exception fallback.
            try
            {
                ScreenManager.PushScreen(new CUNobleDossierScreen(
                    type,
                    NativeDossierTitle(type),
                    "Clan: "+clan.Name,
                    family,assignment,role,
                    "Native Clan, Party and Kingdom screens remain "+
                    "authoritative for characters and political powers."));
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_native_household_dossier_fallback",error);
                ShowNativeDetail("Starting household | "+type,
                    family+"\n\n"+assignment+"\n\n"+role+
                    "\n\nNative Clan, Party and Kingdom screens show "+
                    "the current game-owned state.");
            }
        }
    }
}

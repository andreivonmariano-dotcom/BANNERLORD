using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxRoyalCouncilState royalCouncil=
            new SandboxRoyalCouncilState();

        // A single named Kingsguard Hero is part of the three-person
        // starting court, never a recruitable or assigned troop unit.
        private static Hero NativeCourtOfficer(string office)
        {
            var clan=Clan.PlayerClan;
            return clan==null?null:clan.Companions
                .FirstOrDefault(hero=>hero!=null &&
                    hero.IsActive && hero.IsAlive &&
                    hero.Name!=null &&
                    hero.Name.ToString().EndsWith(
                        "("+office+")",StringComparison.Ordinal));
        }

        private Settlement RecordedRoyalSeat()
        {
            if(!royalCouncil.IsRecorded)return null;
            var seat=Settlement.Find(royalCouncil.SeatId);
            return seat!=null && seat.Town!=null &&
                seat.OwnerClan==Clan.PlayerClan &&
                seat.IsFortification && !seat.IsUnderSiege ?
                seat:null;
        }

        private bool NativeRoyalCouncilPresent(bool atSeat)
        {
            var clan=Clan.PlayerClan;
            var party=MobileParty.MainParty;
            var seat=RecordedRoyalSeat();
            var marshal=NativeCourtOfficer("Marshal");
            var minister=NativeCourtOfficer("Royal Minister");
            var kingsguard=NativeCourtOfficer("Kingsguard");
            if(clan==null || party==null || seat==null ||
               clan.Kingdom==null ||
               clan.Kingdom.RulingClan!=clan ||
               clan.Leader!=Hero.MainHero ||
               Hero.MainHero==null ||
               clan.Kingdom.StringId!=royalCouncil.KingdomId ||
               Hero.MainHero.StringId!=royalCouncil.KingId ||
               marshal==null || minister==null || kingsguard==null ||
               marshal.StringId!=royalCouncil.MarshalId ||
               minister.StringId!=royalCouncil.MinisterId ||
               kingsguard.StringId!=royalCouncil.KingsguardHeroId ||
               !SandboxKingNativeMinistry.VerifiedGovernor(minister) ||
               minister.GovernorOf.Settlement!=seat ||
               marshal.Clan!=clan || kingsguard.Clan!=clan ||
               party.MemberRoster.GetTroopCount(
                   marshal.CharacterObject)!=1 ||
               party.MemberRoster.GetTroopCount(
                   kingsguard.CharacterObject)!=1)
                return false;
            return !atSeat || Settlement.CurrentSettlement==seat;
        }

        private string BeginNativeRoyalCouncil(Hero ruler)
        {
            if(ruler==null || ruler.Clan!=Clan.PlayerClan ||
               Clan.PlayerClan.Kingdom==null ||
               Clan.PlayerClan.Kingdom.RulingClan!=Clan.PlayerClan)
                return "native_king_missing";
            var marshal=NativeCourtOfficer("Marshal");
            var minister=NativeCourtOfficer("Royal Minister");
            var guard=NativeCourtOfficer("Kingsguard");
            if(marshal==null || minister==null || guard==null ||
               !SandboxKingNativeMinistry.VerifiedGovernor(minister))
                return "native_hero_council_members_unverified";
            var seat=minister.GovernorOf.Settlement;
            if(seat==null || seat.OwnerClan!=Clan.PlayerClan ||
               seat.Town==null || seat.Town.Governor!=minister ||
               !seat.IsFortification)
                return "native_royal_seat_governorship_missing";
            var result=royalCouncil.Begin(
                Clan.PlayerClan.Kingdom.StringId,
                seat.StringId,ruler.StringId,marshal.StringId,
                minister.StringId,guard.StringId);
            DiagnosticJournal.Event("cu_royal_court_native_commission",
                "result="+result+
                "|native_court_seat="+seat.StringId+
                "|actual_ruler="+ruler.StringId+
                "|actual_marshal="+marshal.StringId+
                "|actual_minister_governor="+minister.StringId+
                "|actual_kingsguard_hero="+guard.StringId+
                "|kingsguard_hero_only=true"+
                "|native_capital_property_unchanged=true"+
                "|native_kingdom_votes_unchanged=true");
            return result;
        }

        private void RegisterNativeRoyalCouncilMenus(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddGameMenu("cu_native_royal_council",
                "Royal court at an actual player-owned seat. The Royal "+
                "Minister governs this holding. Marshal and one real "+
                "Kingsguard Hero accompany the King. Native kingdom "+
                "votes, appointments and estates remain authoritative.",
                args=>{});
            foreach(var menu in new[]{"town","castle"})
            {
                starter.AddGameMenuOption(menu,
                    "cu_native_royal_council_entry_"+menu,
                    "CU: Convene the royal council",
                    args=>NativeRoyalCouncilPresent(true),
                    args=>
                    {
                        int day=CurrentLordInheritanceDay();
                        bool present=NativeRoyalCouncilPresent(true);
                        string result=royalCouncil.Convene(day,
                            present,present,present,present,present);
                        DiagnosticJournal.Event(
                            "cu_king_native_council_meeting",
                            "result="+result+
                            "|seat="+royalCouncil.SeatId+
                            "|day="+day+
                            "|native_minister_governs="+present+
                            "|marshal_in_party="+present+
                            "|kingsguard_single_hero_in_party="+present+
                            "|native_capital_property_unchanged=true"+
                            "|native_kingdom_politics_unchanged=true");
                        if(result=="real_royal_hero_council_convened" ||
                           result=="already_convened_today")
                            GameMenu.SwitchToMenu(
                                "cu_native_royal_council");
                        else
                            Notify("Royal council unavailable: "+result);
                    },false);
            }
            starter.AddGameMenuOption("cu_native_royal_council",
                "cu_native_royal_council_view",
                "Review the native royal council",
                args=>NativeRoyalCouncilPresent(true),
                args=>
                {
                    var seat=RecordedRoyalSeat();
                    Notify("Verified royal court seat: "+seat.Name+
                        ". Native Minister governor: "+
                        royalCouncil.MinisterId+
                        ". Marshal: "+royalCouncil.MarshalId+
                        ". Kingsguard Hero: "+
                        royalCouncil.KingsguardHeroId+
                        ". Verified in-person council meetings: "+
                        royalCouncil.Meetings+
                        ". No Kingsguard troops, recruitment or special "+
                        "unit. This council does not replace the game's "+
                        "native kingdom decisions.");
                },false);
            starter.AddGameMenuOption("cu_native_royal_council",
                "cu_native_royal_council_return","Return",
                args=>true,
                args=>GameMenu.SwitchToMenu(
                    RecordedRoyalSeat()!=null &&
                    RecordedRoyalSeat().IsTown?"town":"castle"),
                false);
        }
    }
}

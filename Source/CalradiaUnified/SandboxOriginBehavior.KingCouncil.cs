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
            string key=SandboxRetinueOfficePolicy.Key(office);
            return clan==null || key.Length==0?null:clan.Companions
                .FirstOrDefault(hero=>hero!=null &&
                    hero.IsActive && hero.IsAlive && !hero.IsPrisoner &&
                    hero.CharacterObject!=null &&
                    hero.Clan==clan && hero.CompanionOf==clan &&
                    RecordedRetinueOffice(hero)==key);
        }

        private static bool CurrentNobleAuthority(bool royal)
        {
            var clan=Clan.PlayerClan;var player=Hero.MainHero;
            var kingdom=clan==null?null:clan.Kingdom;
            return clan!=null && player!=null && player.IsAlive && player.IsActive &&
                !player.IsPrisoner && player.Clan==clan &&
                SandboxNativeNobleHandoffPolicy.HasCurrentAuthority(royal,
                    kingdom!=null,clan.Leader==player,
                    kingdom!=null && kingdom.RulingClan==clan,
                    clan.IsUnderMercenaryService,clan.IsBanditFaction,clan.IsRebelClan,
                    clan.IsEliminated,kingdom!=null && kingdom.IsEliminated);
        }

        private Settlement RecordedRoyalSeat()
        {
            if(!royalCouncil.IsRecorded)return null;
            var seat=Settlement.Find(royalCouncil.SeatId);
            return seat!=null && seat.Town!=null &&
                seat.OwnerClan==Clan.PlayerClan &&
                seat.IsFortification && !seat.IsUnderSiege &&
                (seat.Party==null || seat.Party.MapEvent==null) ?
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
            if(!CurrentNobleAuthority(true) || clan==null || party==null || seat==null ||
               party.LeaderHero!=Hero.MainHero || party.MemberRoster==null || party.MapEvent!=null ||
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
               marshal.PartyBelongedTo!=party || kingsguard.PartyBelongedTo!=party ||
               minister.PartyBelongedTo!=null || minister.CurrentSettlement!=seat ||
               party.MemberRoster.GetTroopCount(
                   marshal.CharacterObject)!=1 ||
               party.MemberRoster.GetTroopCount(
                   kingsguard.CharacterObject)!=1)
                return false;
            return !atSeat || (Settlement.CurrentSettlement==seat && Hero.MainHero.CurrentSettlement==seat);
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
            {
                // Royal state is a personal/kingdom matter, not a
                // permanent tab in every foreign castle.
                starter.AddGameMenuOption("cu_people_lineage",
                    "cu_native_royal_council_entry",
                    "My royal court and council",
                    args=>CurrentNobleAuthority(true) && royalCouncil.IsRecorded &&
                        Hero.MainHero!=null &&
                        Hero.MainHero.StringId==royalCouncil.KingId &&
                        Clan.PlayerClan!=null &&
                        Clan.PlayerClan.Kingdom!=null &&
                        Clan.PlayerClan.Kingdom.RulingClan==
                            Clan.PlayerClan,
                    args=>
                    {
                        bool present=NativeRoyalCouncilPresent(true);
                        if(!present)
                        {
                            var seat=RecordedRoyalSeat();
                            DiagnosticJournal.Event(
                                "cu_royal_council_ui_location",
                                "result=NOT_AT_VERIFIED_COUNCIL"+
                                "|court_seat="+royalCouncil.SeatId+
                                "|seat_still_owned="+(seat!=null)+
                                "|requires_marshal_and_one_kingsguard=true"+
                                "|requires_native_minister_governor=true");
                            ShowNativeDetail(
                                "Royal council | Where to convene",
                                seat==null?
                                    "The recorded royal court seat is no "+
                                    "longer a valid player-owned holding. "+
                                    "The native council cannot meet here.":
                                    "The royal court meets at "+
                                    seat.Name+". Enter this holding with "+
                                    "your Marshal and ONE Kingsguard Hero "+
                                    "in the main party. Your Royal "+
                                    "Minister must still govern this "+
                                    "holding. The kingdom screen remains "+
                                    "the authority for political votes.");
                            return;
                        }
                        int day=CurrentLordInheritanceDay();
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
                    if(!NativeRoyalCouncilPresent(true))return;
                    var seat=RecordedRoyalSeat();
                    ShowNativeDetail("Royal council | Current court",
                        "Verified royal court seat: "+seat.Name+
                        ". Native Minister governor: "+
                        NativeHeroDisplayName(royalCouncil.MinisterId)+
                        ". Marshal: "+
                        NativeHeroDisplayName(royalCouncil.MarshalId)+
                        ". Kingsguard Hero: "+
                        NativeHeroDisplayName(
                            royalCouncil.KingsguardHeroId)+
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
                    "cu_people_lineage"),
                false);
        }
    }
}

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxKingsguardState kingsguard=
            new SandboxKingsguardState();
        private int lastNativeRoyalCouncilDay=-1;
        private int nativeRoyalCouncilMeetings;

        /// <summary>
        /// A real gathering of the ruler, two living field Heroes and
        /// the actual native minister-governor at the original royal
        /// court seat. Native Kingdom decides politics independently.
        /// </summary>
        private static Hero NativeCourtOfficer(string name)
        {
            var clan=Clan.PlayerClan;
            return clan==null?null:clan.Companions
                .FirstOrDefault(o=>o!=null && o.IsActive &&
                    o.IsAlive && o.Name!=null &&
                    o.Name.ToString().EndsWith(
                        "("+name+")",StringComparison.Ordinal));
        }

        private Settlement RecordedRoyalSeat()
        {
            if(!kingsguard.IsRecorded)return null;
            var seat=Settlement.Find(kingsguard.RoyalSeatId);
            return seat!=null && seat.Town!=null &&
                seat.OwnerClan==Clan.PlayerClan &&
                seat.IsFortification &&
                !seat.IsUnderSiege ? seat:null;
        }

        private bool NativeRoyalCouncilPresent(bool mustBeInSeat)
        {
            var clan=Clan.PlayerClan;
            var party=MobileParty.MainParty;
            var seat=RecordedRoyalSeat();
            var marshal=NativeCourtOfficer("Marshal");
            var guard=NativeCourtOfficer("Kingsguard");
            var minister=NativeCourtOfficer("Royal Minister");
            if(clan==null || party==null || seat==null ||
                clan.Kingdom==null ||
                clan.Kingdom.RulingClan!=clan ||
                clan.Leader!=Hero.MainHero ||
                !String.Equals(clan.Kingdom.StringId,
                    kingsguard.KingdomId,StringComparison.Ordinal) ||
                !String.Equals(Hero.MainHero.StringId,
                    kingsguard.KingId,StringComparison.Ordinal) ||
                marshal==null || guard==null || minister==null ||
                marshal.StringId!=kingsguard.MarshalId ||
                guard.StringId!=kingsguard.CommanderId ||
                minister.StringId!=kingsguard.MinisterId ||
                !SandboxKingNativeMinistry.VerifiedGovernor(minister) ||
                minister.GovernorOf.Settlement!=seat ||
                marshal.Clan!=clan || guard.Clan!=clan ||
                party.MemberRoster.GetTroopCount(
                    marshal.CharacterObject)!=1 ||
                party.MemberRoster.GetTroopCount(
                    guard.CharacterObject)!=1)
                return false;
            return !mustBeInSeat ||
                Settlement.CurrentSettlement==seat;
        }

        private string BeginNativeRoyalCouncil(Hero king)
        {
            if(king==null || king.Clan!=Clan.PlayerClan ||
               Clan.PlayerClan.Kingdom==null ||
               Clan.PlayerClan.Kingdom.RulingClan!=Clan.PlayerClan)
                return "native_king_missing";
            var marshal=NativeCourtOfficer("Marshal");
            var guard=NativeCourtOfficer("Kingsguard");
            var minister=NativeCourtOfficer("Royal Minister");
            if(marshal==null || guard==null || minister==null ||
               !SandboxKingNativeMinistry.VerifiedGovernor(minister))
                return "native_council_officers_unverified";
            var seat=minister.GovernorOf.Settlement;
            if(seat==null || seat.OwnerClan!=Clan.PlayerClan ||
               seat.Town==null || seat.Town.Governor!=minister ||
               !seat.IsFortification)
                return "royal_seat_governorship_missing";
            int day=CurrentLordInheritanceDay();
            string result=kingsguard.Begin(
                Clan.PlayerClan.Kingdom.StringId,seat.StringId,
                king.StringId,guard.StringId,marshal.StringId,
                minister.StringId,day);
            DiagnosticJournal.Event("cu_royal_court_native_commission",
                "result="+result+
                "|court_seat="+seat.StringId+
                "|native_capital_not_modified=true"+
                "|king="+king.StringId+
                "|marshal="+marshal.StringId+
                "|minister_native_governor="+minister.StringId+
                "|kingsguard_commander="+guard.StringId+
                "|guard_squad_troops=0"+
                "|recruitment_from_existing_native_garrison_only=true");
            return result;
        }

        private static bool HasNativeGuardGear(CharacterObject troop)
        {
            if(troop==null || troop.IsHero ||
                troop.Tier<SandboxKingsguardState.MinNativeTier ||
                troop.BattleEquipments==null)return false;
            foreach(var kit in troop.BattleEquipments)
            {
                if(kit==null)continue;
                bool armor=false,shield=false,weapon=false;
                for(int slot=0;slot<
                    (int)EquipmentIndex.NumEquipmentSetSlots;slot++)
                {
                    var item=kit[slot].Item;
                    if(item==null)continue;
                    string type=item.ItemType.ToString();
                    if(type.IndexOf("Armor",
                        StringComparison.OrdinalIgnoreCase)>=0 ||
                       type.IndexOf("Cape",
                        StringComparison.OrdinalIgnoreCase)>=0)
                        armor=true;
                    if(type.IndexOf("Shield",
                        StringComparison.OrdinalIgnoreCase)>=0)
                        shield=true;
                    if(type.IndexOf("OneHanded",
                        StringComparison.OrdinalIgnoreCase)>=0 ||
                       type.IndexOf("Polearm",
                        StringComparison.OrdinalIgnoreCase)>=0 ||
                       type.IndexOf("TwoHanded",
                        StringComparison.OrdinalIgnoreCase)>=0)
                        weapon=true;
                }
                if(armor && shield && weapon)return true;
            }
            return false;
        }

        private CharacterObject EligibleNativeGuardTroop(Settlement seat)
        {
            var king=Hero.MainHero;
            var garrison=seat==null || seat.Town==null?
                null:seat.Town.GarrisonParty;
            if(king==null || king.Culture==null ||
               garrison==null || garrison.MemberRoster==null)
                return null;
            // Never fabricate an elite troop or replace its equipment.
            // The actual garrison contains the available native kit.
            return CharacterObject.All.Where(c=>c!=null &&
                c.Culture==king.Culture && !c.IsHero &&
                (kingsguard.TroopId.Length==0 ||
                    kingsguard.TroopId==c.StringId) &&
                HasNativeGuardGear(c) &&
                garrison.MemberRoster.GetTroopCount(c)>0)
                .OrderByDescending(c=>c.Tier)
                .ThenBy(c=>c.StringId,StringComparer.Ordinal)
                .FirstOrDefault();
        }

        private string RecruitNativeKingsguard()
        {
            if(!NativeRoyalCouncilPresent(true))
                return "royal_council_not_physically_present";
            var seat=RecordedRoyalSeat();
            var party=MobileParty.MainParty;
            var garrison=seat.Town.GarrisonParty;
            var troop=EligibleNativeGuardTroop(seat);
            if(troop==null || garrison==null ||
                garrison.MemberRoster==null || party.Party==null ||
                party.MemberRoster==null)
                return "real_native_guard_recruits_unavailable";
            int oldGarrison=garrison.MemberRoster.GetTroopCount(troop);
            int oldParty=party.MemberRoster.GetTroopCount(troop);
            int openSlots=Math.Max(0,party.Party.PartySizeLimit-
                party.MemberRoster.TotalManCount);
            int count=SandboxKingsguardState.CanRecruit(
                oldGarrison,oldParty,kingsguard.DesignatedCount,
                openSlots,HasNativeGuardGear(troop),troop.Tier);
            if(count<=0)return "native_guard_full_or_no_party_space";
            // King's council orders a REAL transfer of existing soldiers.
            // Neither roster receives free troops. Wages and equipment
            // remain the native troop's own game systems.
            bool removed=false;
            try
            {
                garrison.MemberRoster.RemoveTroop(troop,count);
                removed=true;
                if(garrison.MemberRoster.GetTroopCount(troop)!=
                    oldGarrison-count)
                    return "native_garrison_deduction_unconfirmed";
                party.MemberRoster.AddToCounts(troop,count);
                int afterParty=party.MemberRoster.GetTroopCount(troop);
                string result=kingsguard.RecordNativeTransfer(
                    troop.StringId,count,oldGarrison,
                    garrison.MemberRoster.GetTroopCount(troop),
                    oldParty,afterParty,CurrentLordInheritanceDay());
                DiagnosticJournal.Event("cu_kingsguard_native_recruitment",
                    "result="+result+
                    "|court_seat="+seat.StringId+
                    "|native_troop="+troop.StringId+
                    "|native_tier="+troop.Tier+
                    "|native_kit_armor_shield_melee=true"+
                    "|existing_garrison_before="+oldGarrison+
                    "|existing_garrison_after="+
                        garrison.MemberRoster.GetTroopCount(troop)+
                    "|party_before="+oldParty+
                    "|party_after="+afterParty+
                    "|recruited="+count+
                    "|designated_guard="+kingsguard.DesignatedCount+
                    "|no_spawn_or_equipment_fabrication=true");
                return result;
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_kingsguard_native_transfer_exception",error);
                // Reconcile only a genuinely removed, not yet credited
                // contingent. Never duplicate existing native soldiers.
                if(removed &&
                    party.MemberRoster.GetTroopCount(troop)==oldParty &&
                    garrison.MemberRoster.GetTroopCount(troop)==
                        oldGarrison-count)
                {
                    try{garrison.MemberRoster.AddToCounts(troop,count);}
                    catch(Exception rollback)
                    {
                        DiagnosticJournal.Exception(
                            "cu_kingsguard_native_transfer_rollback",rollback);
                    }
                }
                return "native_guard_transfer_partial_review_required";
            }
        }

        private void ObserveKingsguardDaily()
        {
            if(!applied || !IsStandaloneSandbox() ||
                !kingsguard.IsRecorded ||
                MobileParty.MainParty==null)
                return;
            var troop=kingsguard.TroopId.Length==0?null:
                MBObjectManager.Instance.GetObject<CharacterObject>(
                    kingsguard.TroopId);
            int current=troop==null?0:
                MobileParty.MainParty.MemberRoster.GetTroopCount(troop);
            string result=kingsguard.Reconcile(
                current,CurrentLordInheritanceDay());
            if(result=="native_guard_casualties_observed")
                DiagnosticJournal.Event("cu_kingsguard_casualties",
                    "result="+result+
                    "|troop="+kingsguard.TroopId+
                    "|surviving_designation="+kingsguard.DesignatedCount+
                    "|native_party_count="+current+
                    "|no_resurrection_or_new_troop_grant=true");
        }

        private void RegisterNativeRoyalCouncilMenus(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddGameMenu("cu_native_royal_council",
                "Royal court in session. The King's real Minister governs "+
                "this owned holding. Marshal and Kingsguard travel with "+
                "the King. Kingdom votes and diplomacy remain native.",
                args=>{});
            foreach(var menu in new[]{"town","castle"})
            {
                string back=menu;
                starter.AddGameMenuOption(menu,
                    "cu_native_royal_council_entry_"+menu,
                    "CU: Convene the royal council",
                    args=>NativeRoyalCouncilPresent(true),
                    args=>
                    {
                        int day=CurrentLordInheritanceDay();
                        if(day>=0 && lastNativeRoyalCouncilDay!=day)
                        {
                            lastNativeRoyalCouncilDay=day;
                            nativeRoyalCouncilMeetings++;
                        }
                        var seat=RecordedRoyalSeat();
                        DiagnosticJournal.Event(
                            "cu_king_native_council_meeting",
                            "result=PASS"+
                            "|seat="+seat.StringId+
                            "|day="+day+
                            "|native_minister_governs=true"+
                            "|actual_marshal_in_party=true"+
                            "|actual_kingsguard_hero_in_party=true"+
                            "|native_ruling_clan_confirmed=true"+
                            "|native_capital_property_unchanged=true"+
                            "|native_kingdom_politics_unchanged=true");
                        GameMenu.SwitchToMenu(
                            "cu_native_royal_council");
                    },false);
            }
            starter.AddGameMenuOption("cu_native_royal_council",
                "cu_native_royal_council_view",
                "Review native court and royal guards",
                args=>NativeRoyalCouncilPresent(true),
                args=>
                {
                    var seat=RecordedRoyalSeat();
                    Notify("Royal court seat: "+seat.Name+
                        ". Actual native Minister: "+
                        kingsguard.MinisterId+
                        ". Marshal: "+kingsguard.MarshalId+
                        ". Kingsguard commander: "+
                        kingsguard.CommanderId+
                        ". Native elite troop: "+
                        (kingsguard.TroopId.Length==0?
                            "not yet recruited":kingsguard.TroopId)+
                        ". Actual Kingsguard detachment: "+
                        kingsguard.DesignatedCount+
                        " / "+SandboxKingsguardState.MaxGuard+
                        ". Sessions: "+nativeRoyalCouncilMeetings+
                        ". Recruitment transfers already-equipped native "+
                        "soldiers from this fief's garrison. This court "+
                        "does not replace the native kingdom council.");
                },false);
            starter.AddGameMenuOption("cu_native_royal_council",
                "cu_native_royal_council_recruit",
                "Reassign two equipped garrison troops to Kingsguard",
                args=>NativeRoyalCouncilPresent(true) &&
                    EligibleNativeGuardTroop(RecordedRoyalSeat())!=null &&
                    kingsguard.DesignatedCount<
                        SandboxKingsguardState.MaxGuard,
                args=>Notify("Kingsguard: "+
                    RecruitNativeKingsguard()),false);
            starter.AddGameMenuOption("cu_native_royal_council",
                "cu_native_royal_council_return",
                "Return to the settlement",
                args=>true,
                args=>GameMenu.SwitchToMenu(
                    RecordedRoyalSeat()!=null &&
                    RecordedRoyalSeat().IsTown?"town":"castle"),
                false);
        }
    }
}

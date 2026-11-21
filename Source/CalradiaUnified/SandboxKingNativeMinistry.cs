using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace CalradiaUnified
{
    /// <summary>
    /// Real royal governor of a REAL player-owned vacant fortification.
    /// Never dispossesses an incumbent or confers a new settlement.
    /// RoyalMinister is a clan companion in the settlement, not a
    /// fictional officer traveling in the main party.
    /// </summary>
    internal static class SandboxKingNativeMinistry
    {
        internal static Settlement VacantOwnedFief()
        {
            var clan=Clan.PlayerClan;
            if(clan==null || clan.Settlements==null)
                return null;
            return clan.Settlements.Where(s=>s!=null &&
                s.OwnerClan==clan &&
                s.IsFortification && s.Town!=null &&
                !s.IsUnderSiege && s.Town.Governor==null)
                .OrderBy(s=>s.IsTown?0:1)
                .ThenBy(s=>s.StringId,StringComparer.Ordinal)
                .FirstOrDefault();
        }

        internal static string TryAppoint(Hero minister,
            out string settlementId)
        {
            settlementId="";
            if(minister==null || minister.CharacterObject==null ||
               !minister.IsActive || !minister.IsAlive ||
               minister.Clan!=Clan.PlayerClan ||
               minister.CompanionOf!=Clan.PlayerClan)
                return "native_minister_hero_not_registered";
            var seat=VacantOwnedFief();
            if(seat==null)return "native_royal_governor_seat_missing";
            if(minister.GovernorOf!=null)
                return "native_minister_already_governs";
            if(seat.OwnerClan!=Clan.PlayerClan ||
               seat.Town==null || seat.Town.Governor!=null)
                return "native_governorship_no_longer_vacant";
            // This is the engine action, never a Town.Governor setter.
            // The minister is not a MemberRoster troop.
            ChangeGovernorAction.Apply(seat.Town,minister);
            bool accepted=seat.Town.Governor==minister &&
                minister.GovernorOf==seat.Town &&
                minister.Clan==Clan.PlayerClan &&
                minister.CompanionOf==Clan.PlayerClan;
            DiagnosticJournal.Event(
                "cu_king_royal_minister_native_governor",
                "result="+(accepted?"PASS":"FAIL")+
                "|minister="+minister.StringId+
                "|settlement="+seat.StringId+
                "|town_governor="+(seat.Town.Governor==null?
                    "none":seat.Town.Governor.StringId)+
                "|hero_governor_of="+(minister.GovernorOf==null?
                    "none":minister.GovernorOf.StringId)+
                "|native_change_governor_action=true"+
                "|previous_governor=none"+
                "|no_estate_owner_change=true");
            if(!accepted)return "native_governor_action_unconfirmed";
            settlementId=seat.StringId;
            return "native_royal_minister_governs:"+settlementId;
        }

        internal static bool VerifiedGovernor(Hero minister)
        {
            if(minister==null || minister.GovernorOf==null ||
               minister.Clan!=Clan.PlayerClan ||
               minister.CompanionOf!=Clan.PlayerClan)
                return false;
            var town=minister.GovernorOf;
            return town.Governor==minister &&
                town.Settlement!=null &&
                town.Settlement.OwnerClan==Clan.PlayerClan &&
                town.Settlement.IsFortification;
        }
    }
}

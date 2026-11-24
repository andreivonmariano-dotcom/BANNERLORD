using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxNativeMilitaryWitnessState militaryWitness=
            new SandboxNativeMilitaryWitnessState();

        private void ObserveNativeMilitaryAppointmentsDaily()
        {
            if(!applied || !IsStandaloneSandbox() ||
                knightServiceOrders.RoyalHearingDay<0 ||
                Hero.MainHero==null || Clan.PlayerClan==null)
                return;
            var patron=ResolveCampaignHero(
                knightServiceOrders.PatronId);
            var clan=Clan.PlayerClan;
            if(patron==null || patron.Clan==null ||
                patron.Clan.Kingdom==null ||
                clan.Leader!=Hero.MainHero ||
                clan.Kingdom!=patron.Clan.Kingdom ||
                clan.IsUnderMercenaryService)return;
            var party=MobileParty.MainParty;
            bool commandsNativeArmy=party!=null &&
                party.LeaderHero==Hero.MainHero &&
                party.Army!=null &&
                party.Army.LeaderParty==party;
            int day=(int)Math.Floor(CampaignTime.Now.ToDays);
            bool changed=militaryWitness.Observe(day,
                clan.Kingdom.StringId,true,commandsNativeArmy);
            if(changed)
            {
                DiagnosticJournal.Event("cu_knight_native_political_witness",
                    "native_kingdom="+clan.Kingdom.StringId+
                    "|vassal_day="+militaryWitness.VassalDay+
                    "|actual_army_command_day="+
                        militaryWitness.ArmyCommandDay+
                    "|engine_join_action_not_initiated=true"+
                    "|engine_army_action_not_initiated=true");
                Notify(commandsNativeArmy?
                    "Your native kingdom service and command "+
                    "of a real army have been entered in your record.":
                    "Your lawful entry into a real kingdom "+
                    "has been witnessed in your household record.");
            }
        }

        private string NativeMilitaryServiceText()
        {
            if(militaryWitness.VassalDay<0)
                return "Formal vassalage: not verified in native "+
                    "kingdom records. A personal oath or royal "+
                    "audience does not confer this status.";
            var clan=Clan.PlayerClan;
            bool currentlyNative=clan!=null &&
                clan.Kingdom!=null &&
                clan.Kingdom.StringId==militaryWitness.KingdomId &&
                !clan.IsUnderMercenaryService;
            return "First witnessed native vassalage: day "+
                militaryWitness.VassalDay+
                "\nPresent native vassal status: "+
                (currentlyNative?"confirmed":"not currently confirmed")+
                "\nNative army command: "+
                (militaryWitness.ArmyCommandDay>=0?
                    "witnessed on day "+militaryWitness.ArmyCommandDay:
                    "not witnessed")+
                "\nOnly the game's own kingdom and Army "+
                "systems can grant these positions.";
        }
    }
}

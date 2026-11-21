using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        // Runs at most once for a newly finalized, verified sworn-Knight
        // start. Existing saves are not teleported on load.
        private void IntroduceKnightHouseholdAndArrive(Hero patron)
        {
            var player=Hero.MainHero;
            if(player==null || patron==null || !patron.IsAlive ||
                !patron.IsActive || patron.IsPrisoner ||
                patron.Clan==null || !knightOath.HasCurrentOath ||
                knightOath.PatronId!=patron.StringId ||
                knightOath.PlayerId!=player.StringId)
                return;

            // Being acquainted means the native Encyclopedia recognizes
            // these actual Heroes. It is not fabricated friendship, kinship,
            // a clan transfer, or a change in political allegiance.
            int known=0;
            try
            {
                foreach(var member in Hero.AllAliveHeroes.Where(h=>
                    h!=null && h.IsActive && h.IsAlive &&
                    (h==patron ||
                     (h.Clan==patron.Clan && h.IsLord) ||
                     h.Spouse==patron || h.Father==patron ||
                     h.Mother==patron)))
                {
                    if(!member.IsKnownToPlayer)
                    {
                        member.IsKnownToPlayer=true;
                        if(member.IsKnownToPlayer)known++;
                    }
                }
                DiagnosticJournal.Event("cu_knight_patron_house_known",
                    "patron="+patron.StringId+
                    "|native_heroes_newly_known="+known+
                    "|family_links_native_only=true"+
                    "|no_relation_or_clan_transfer=true");
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_knight_patron_house_familiarity_failed",error);
            }

            // The patron is selected preferentially from real field
            // commanders. Do not guess a location, relocate a party in an
            // encounter, or move the lord himself to fit a scripted scene.
            var host=patron.PartyBelongedTo;
            var residence=patron.CurrentSettlement;
            var party=MobileParty.MainParty;
            string refused="";
            if(party==null || party.LeaderHero!=player)
                refused="player_party_not_ready";
            else if(player.CurrentSettlement!=null)
                refused="player_in_settlement";
            else if(residence==null &&
                (host==null || host.LeaderHero!=patron))
                refused="patron_has_no_self_led_field_party";

            if(refused.Length!=0)
            {
                DiagnosticJournal.Event("cu_knight_patron_arrival",
                    "result=SKIPPED|reason="+refused+
                    "|patron="+patron.StringId+
                    "|native_location_not_fabricated=true");
                return;
            }
            try
            {
                // For a resident lord, begin outside the real settlement
                // gate. A field commander is encountered at the real
                // party position. No synthetic garrison or party is made.
                float before=party.GetPosition2D.Distance(
                    residence==null ? host.GetPosition2D :
                        residence.GetPosition2D);
                if(residence==null)
                    party.SetPositionAfterMapChange(host.Position);
                else
                    party.SetPositionAfterMapChange(
                        residence.GatePosition);
                float after=party.GetPosition2D.Distance(
                    residence==null ? host.GetPosition2D :
                        residence.GetPosition2D);
                bool arrived=after <= (residence==null ? 0.5f : 6f);
                DiagnosticJournal.Event("cu_knight_patron_arrival",
                    "result="+(arrived?"ARRIVED":"NOT_VERIFIED")+
                    "|patron="+patron.StringId+
                    "|native_host_party="+(host==null?
                        "none":host.StringId)+
                    "|native_settlement="+(residence==null?
                        "none":residence.StringId)+
                    "|distance_before="+before+
                    "|distance_after="+after+
                    "|player_party_only=true"+
                    "|one_shot_new_campaign=true");
                if(arrived)
                    Notify(residence==null?
                        "Your service begins beside "+
                        patron.Name+"'s banner. Speak to "+
                        patron.Name+" on the campaign map for "+
                        "your first audience.":
                        "Your service begins outside "+
                        residence.Name+". "+patron.Name+
                        " is inside. Enter the settlement and "+
                        "seek your first audience.");
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_knight_patron_arrival_failed",error);
            }
        }
    }
}

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
            var party=MobileParty.MainParty;
            string refused="";
            if(host==null || host.LeaderHero!=patron)
                refused="patron_has_no_self_led_field_party";
            else if(patron.CurrentSettlement!=null)
                refused="patron_inside_settlement";
            else if(party==null || party.LeaderHero!=player)
                refused="player_party_not_ready";
            else if(player.CurrentSettlement!=null)
                refused="player_in_settlement";

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
                float before=party.GetPosition2D.Distance(
                    host.GetPosition2D);
                // Both positions come from the current map's real parties.
                // Never cross maps or send the lord a synthetic AI order.
                party.SetPositionAfterMapChange(host.Position);
                float after=party.GetPosition2D.Distance(
                    host.GetPosition2D);
                DiagnosticJournal.Event("cu_knight_patron_arrival",
                    "result="+(after<=0.5f?"ARRIVED":"NOT_VERIFIED")+
                    "|patron="+patron.StringId+
                    "|native_host_party="+host.StringId+
                    "|distance_before="+before+
                    "|distance_after="+after+
                    "|player_party_only=true"+
                    "|one_shot_new_campaign=true");
                if(after<=0.5f)
                    Notify("Your service begins beside "+
                        patron.Name+"'s banner. Find "+patron.Name+
                        " on the campaign map and speak to your "+
                        "lord for your first audience.");
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_knight_patron_arrival_failed",error);
            }
        }
    }
}

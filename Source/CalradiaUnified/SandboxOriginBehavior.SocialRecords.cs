using System;
using System.Linq;
using TaleWorlds.CampaignSystem;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxSocialBondState socialBonds=
            new SandboxSocialBondState();

        // This names observed history, never changes native AI loyalties.
        private string SocialBondDescription(Hero person)
        {
            if(person==null || Hero.MainHero==null)
                return "No living native social record";
            var agenda=characterAgendas.Get(person.StringId);
            var bond=socialBonds.For(person.StringId);
            string relation=socialBonds.SocialStanding(
                person.StringId,Hero.MainHero.GetRelation(person));
            string goal=agenda==null || String.IsNullOrEmpty(agenda.Goal)?
                "no recorded native-evidence ambition":agenda.Goal;
            return "Native relation: "+
                Hero.MainHero.GetRelation(person)+
                "\nObserved ambition: "+goal+
                "\nRecorded trust: "+relation+
                (bond==null?"":"\nVerified support: "+bond.Supported+
                    " | Refusals: "+bond.Declined+
                    " | Later meetings: "+bond.FollowUps)+
                "\nNo artificial loyalty score or hostility is applied.";
        }

        private string CampChronicleText()
        {
            string result="Witnessed career chapters: "+
                careerChronicle.Chapter+"/3"+
                "\nCurrent calling: "+CurrentCareerRole();
            if(knightOath.IsConfigured)
                result+="\nSworn service: "+
                    knightServiceOrders.CompletedOrders+
                    " reports, "+knightEscort.CompletedEscorts+
                    " field escorts";
            var player=Hero.MainHero;
            if(player!=null)
            {
                int children=player.Children==null?0:
                    player.Children.Count(x=>x!=null && x.IsAlive);
                result+="\nLiving native children: "+children;
            }
            var deeds=petitionStories.Stories.Where(x=>
                x.ResolvedDay>=0)
                .OrderByDescending(x=>x.ResolvedDay)
                .Take(3).ToArray();
            result+="\nNative vassalage witnessed: "+
                (militaryWitness.VassalDay>=0?"yes":"no")+
                " | Army command: "+
                (militaryWitness.ArmyCommandDay>=0?"yes":"no");
            result+="\nRecent personal matters:";
            if(deeds.Length==0)
                result+=" none verified";
            foreach(var deed in deeds)
            {
                var hero=ResolveCampaignHero(deed.HeroId);
                result+="\n"+(hero==null?"Recorded Hero":
                    hero.Name.ToString())+": "+deed.Kind+
                    " ("+deed.Outcome+")";
            }
            return result+
                "\nNative game records remain authoritative.";
        }
    }
}

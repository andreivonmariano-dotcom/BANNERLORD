using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxSocialBondState socialBonds=
            new SandboxSocialBondState();

        private SandboxPeerInitiativeState peerInitiatives=
            new SandboxPeerInitiativeState();
        private SandboxHouseholdAccountsState householdAccounts=
            new SandboxHouseholdAccountsState();

        private static bool EligiblePeer(Hero h)
        {
            return h!=null && h.IsAlive && h.IsActive &&
                !h.IsPrisoner && h.Age>=18f;
        }

        private static bool PeerCoLocated(Hero a,Hero b)
        {
            if(a==null || b==null || a==b)return false;
            if(a.PartyBelongedTo!=null && a.PartyBelongedTo==b.PartyBelongedTo)
                return true;
            var place=a.StayingInSettlement ?? a.CurrentSettlement;
            var other=b.StayingInSettlement ?? b.CurrentSettlement;
            return place!=null && place==other;
        }

        private void ObserveSocietyAccountsAndPeers()
        {
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            if(!applied || !IsStandaloneSandbox() || player==null || clan==null ||
                !player.IsAlive)return;
            int day=(int)Math.Floor(CampaignTime.Now.ToDays);
            householdAccounts.Observe(player.StringId,day,player.Gold);
            var spouse=player.Spouse;
            if(spouse!=null && spouse.IsAlive && spouse.Spouse==player)
                householdAccounts.Observe(spouse.StringId,day,spouse.Gold);
            if(player.Children!=null)
                foreach(var child in player.Children.Where(h=>h!=null && h.IsAlive &&
                    (h.Father==player || h.Mother==player)).Take(32))
                    householdAccounts.Observe(child.StringId,day,child.Gold);
            if(clan.Companions!=null)
                foreach(var companion in clan.Companions.Where(h=>h!=null &&
                    h.IsAlive && h.Clan==clan && h.CompanionOf==clan).Take(32))
                    householdAccounts.Observe(companion.StringId,day,companion.Gold);
            if(day<0 || day%SandboxPeerInitiativeState.IntervalDays!=0)return;
            var previous=peerInitiatives.Entries.LastOrDefault();
            if(previous!=null && (long)day-previous.Day<
                SandboxPeerInitiativeState.IntervalDays)return;
            var ids=characterAgendas.Entries.Select(a=>a.Id).ToArray();
            var eligible=Hero.AllAliveHeroes.Where(h=>EligiblePeer(h) && h!=player &&
                ids.Contains(h.StringId)).GroupBy(h=>h.StringId,StringComparer.Ordinal)
                .ToDictionary(g=>g.Key,g=>g.First(),StringComparer.Ordinal);
            var peers=peerInitiatives.PrioritizePeers(eligible.Keys,16)
                .Select(id=>eligible[id]).ToArray();
            foreach(var actor in peers)
            {
                var agenda=characterAgendas.Get(actor.StringId);
                if(agenda==null || SandboxPeerInitiativeState.Kind(agenda.Goal).Length==0)
                    continue;
                foreach(var target in peers)
                {
                    if(!PeerCoLocated(actor,target))continue;
                    if(!peerInitiatives.Record(actor.StringId,target.StringId,
                        agenda.Goal,day,true,true,actor.GetRelation(target),
                        target.GetTraitLevel(DefaultTraits.Generosity),
                        target.GetTraitLevel(DefaultTraits.Calculating)))continue;
                    var entry=peerInitiatives.Entries.Last();
                    livingPersons.Remember("ModeledPeerInitiative",entry.Actor,entry.Target,
                        "kind="+entry.Kind+"|response="+entry.Outcome+"|day="+day+
                        "|not_native_consent_or_action=true");
                    DiagnosticJournal.Event("cu_modeled_peer_initiative",
                        "actor="+entry.Actor+"|target="+entry.Target+
                        "|day="+day+"|outcome="+entry.Outcome+"|native_effect=false");
                    return;
                }
            }
        }

        private string HouseholdAccountText(Hero person)
        {
            if(person==null)return "No identified person.";
            var account=householdAccounts.For(person.StringId);
            int milestone=familyGrowth.LastMilestone(person.StringId);
            var clan=person.Clan;
            string estates=clan==null || clan.Settlements==null?"unverified":
                clan.Settlements.Count.ToString();
            return "Current native Hero gold: "+person.Gold+
                "\nNative clan: "+(clan==null?"none":clan.Name.ToString())+
                " | Clan settlements: "+estates+
                " (clan property, not individual assets)"+
                "\nNative clan leadership: "+(clan!=null && clan.Leader==person?"yes":"no")+
                (account==null?"\nNo saved account observation.":
                    "\nObserved balance day "+account.Day+": "+account.Gold+
                    " | Change since previous observation: "+
                    (account.LastChange.HasValue?account.LastChange.Value.ToString():"unknown"))+
                "\nTracked CU upbringing milestone: "+(milestone<0?"not tracked":milestone.ToString())+
                "\nNo inferred income, ownership transfer or inherited balance.";
        }

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
                "no recorded modeled ambition":agenda.Goal;
            return "Native relation: "+
                Hero.MainHero.GetRelation(person)+
                "\nModeled ambition (native context): "+goal+
                "\nRecorded trust: "+relation+
                (bond==null?"":"\nVerified support: "+bond.Supported+
                    " | Refusals: "+bond.Declined+
                    " | Later meetings: "+bond.FollowUps)+
                "\nNo artificial loyalty score or hostility is applied.";
        }

        // Display saved record kinds, not invented quotations or dates.
        private string PersonalHistoryText(Hero person)
        {
            if(person==null)return "No identified person.";
            var events=livingPersons.RecentFor(person.StringId,5);
            if(events.Length==0)return "No recent CU events recorded for this person.";
            return "Recent recorded events (newest first):\n"+
                String.Join("\n",events.Select(e=>e.Kind+
                    " | "+e.Detail));
        }

        private string SocietyDigestText()
        {
            var player=Hero.MainHero;
            if(player==null)return "No native character available.";
            var pending=agencyDecisions.HasPending?
                ResolveCampaignHero(agencyDecisions.PendingHeroId):null;
            string reply=pending==null?"none currently resolved":NativePersonName(pending);
            var peers=peerInitiatives.Entries.Reverse().Take(3).ToArray();
            return "Household affairs | Current overview"+
                "\nSelected career: "+SelectedReviewRole()+
                "\n"+RecurringCareerReviewText()+
                "\nPending personal petitioner: "+reply+
                "\n"+CulturalSocietyText(player)+
                "\nRecent modeled NPC interests (not actual consent): "+
                (peers.Length==0?"none recorded":
                    "\n"+String.Join("\n",peers.Select(e=>
                        NativeHeroDisplayName(e.Actor)+" -> "+NativeHeroDisplayName(e.Target)+
                        " | "+e.Kind+" | "+e.Outcome+" | day "+e.Day)));
        }

        private string CampChronicleText()
        {
            string result="Witnessed career chapters: "+
                careerChronicle.Chapter+"/3"+
                "\nRecorded calling: "+CurrentCareerRole();
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
                "\n\n"+SocietyDigestText()+
                "\n\n"+LifetimeCareerText(player)+
                "\n\n"+HouseholdAccountText(player)+
                "\n\n"+PersonalHistoryText(player)+
                "\nNative game records remain authoritative.";
        }
    }
}

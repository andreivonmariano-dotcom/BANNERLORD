using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxPeerSocialState peerSocial=new SandboxPeerSocialState();

        private static bool PeerAvailable(Hero h)
        {
            return h!=null && h!=Hero.MainHero && h.IsAlive && h.IsActive &&
                !h.IsPrisoner && h.Age>=18f;
        }
        private static bool PeerSamePlace(Hero a,Hero b)
        {
            if(!PeerAvailable(a) || !PeerAvailable(b) || a==b)return false;
            if(a.PartyBelongedTo!=null || b.PartyBelongedTo!=null)
            {
                var party=a.PartyBelongedTo;
                return party!=null && party==b.PartyBelongedTo &&
                    party.MapEvent==null && !party.IsCurrentlyAtSea &&
                    party.MemberRoster!=null && a.CharacterObject!=null &&
                    b.CharacterObject!=null &&
                    party.MemberRoster.GetTroopCount(a.CharacterObject)==1 &&
                    party.MemberRoster.GetTroopCount(b.CharacterObject)==1;
            }
            var seat=a.StayingInSettlement;
            return seat!=null && seat==b.StayingInSettlement && !seat.IsUnderSiege;
        }
        private static string PeerVoice(Hero actor,int relation)
        {
            return SandboxPetitionVoicePolicy.Tone(
                actor.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Honor),
                actor.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Generosity),
                actor.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Calculating),
                actor.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Valor),
                actor.GetTraitLevel(TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Mercy),relation);
        }
        private void ObservePeerSocialDaily(Hero player)
        {
            if(!applied || !IsStandaloneSandbox() || player==null ||
                player!=Hero.MainHero)return;
            int day=CurrentHouseholdDay();
            if(day<0 || peerSocial.Pairs.Any(p=>p.LastDay>=day))return;
            // Bounded subset of already tracked adults, not a world-wide
            // pairwise relationship scan. Current native location is required.
            var ids=new System.Collections.Generic.HashSet<string>(
                characterAgendas.Entries.Select(e=>e.Id),StringComparer.Ordinal);
            var people=Hero.AllAliveHeroes.Where(h=>PeerAvailable(h) && ids.Contains(h.StringId))
                .GroupBy(h=>h.StringId,StringComparer.Ordinal).Select(g=>g.First())
                .OrderBy(h=>h.StringId,StringComparer.Ordinal).ToArray();
            // Rotate the bounded window so later IDs are not excluded forever.
            int offset=people.Length==0?0:day%people.Length;
            people=people.Skip(offset).Concat(people.Take(offset)).Take(12).ToArray();
            var choices=(from a in people from b in people
                where StringComparer.Ordinal.Compare(a.StringId,b.StringId)<0 &&
                    PeerSamePlace(a,b) && peerSocial.CanObserve(a.StringId,b.StringId,day)
                let prior=peerSocial.For(a.StringId,b.StringId)
                orderby (prior==null?-1:prior.LastDay),a.StringId,b.StringId
                select new {A=a,B=b}).ToArray();
            foreach(var pair in choices)
            {
                if(!PeerSamePlace(pair.A,pair.B))continue;
                var ga=characterAgendas.Get(pair.A.StringId);
                var gb=characterAgendas.Get(pair.B.StringId);
                int relation=pair.A.GetRelation(pair.B);
                string a=SandboxPetitionVoicePolicy.GroupPreference(EffectiveHouseholdDuty(pair.A),
                    ga==null?"":ga.Goal,PeerVoice(pair.A,relation),relation);
                string b=SandboxPetitionVoicePolicy.GroupPreference(EffectiveHouseholdDuty(pair.B),
                    gb==null?"":gb.Goal,PeerVoice(pair.B,relation),relation);
                if(peerSocial.Observe(pair.A.StringId,pair.B.StringId,day,
                    relation,a==b?"CommonInterest":"DifferentPriorities"))
                {
                    DiagnosticJournal.Event("cu_peer_social_sample",
                        "a="+pair.A.StringId+"|b="+pair.B.StringId+
                        "|actual_co_location=true|modeled_interests=true|native_relation_unchanged=true");
                    break;
                }
            }
        }
        // Diagnostic only: check public API shape on the actual loaded assembly.
        // Never invoke or register a multi-agent mission from this probe.
        private static void ProbeNativeGroupConversationApi()
        {
            try
            {
                var manager=typeof(CampaignGameStarter).Assembly.GetType(
                    "TaleWorlds.CampaignSystem.Conversation.ConversationManager",false);
                var methods=manager==null?new MethodInfo[0]:
                    manager.GetMethods(BindingFlags.Public|BindingFlags.Instance);
                var start=methods.Where(m=>m.Name=="SetupAndStartMissionConversationWithMultipleAgents")
                    .Select(m=>m.ToString()).ToArray();
                var lines=typeof(CampaignGameStarter).GetMethods(BindingFlags.Public|BindingFlags.Instance)
                    .Where(m=>m.Name=="AddDialogLineMultiAgent").Select(m=>m.ToString()).ToArray();
                DiagnosticJournal.Event("cu_native_group_api_probe",
                    "start="+String.Join(" / ",start)+"|register="+String.Join(" / ",lines)+
                    "|invoked=false|scene_verified=false");
            }
            catch(Exception e)
            {
                DiagnosticJournal.Exception("cu_native_group_api_probe_failed",e);
            }
        }
    }
}

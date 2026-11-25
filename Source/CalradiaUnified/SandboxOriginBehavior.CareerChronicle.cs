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
        private SandboxLifetimeCareerState lifetimeCareers=
            new SandboxLifetimeCareerState();

        // The player's voluntary choice records an evidenced calling.
        // It does not enroll, discharge, confer a title or transfer land.
        private bool CanRecordLifetimeRole(string role)
        {
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            if(player==null || clan==null || ReviewCareerEvidence(role).Length==0)
                return false;
            var service=SandboxSaSBridge.Read();
            bool activeOath=knightOath.HasCurrentOath &&
                knightOath.PlayerId==player.StringId;
            if(role=="Soldier")return service.Available && service.Enlisted &&
                service.InsideHostParty && !activeOath;
            if(service.Enlisted || (!service.Available &&
                (serviceCareer.VerifiedExternalEnlistment ||
                 serviceCareer.Status=="ExternalStatusUnverified")))return false;
            if(role=="Knight")return activeOath;
            if(activeOath)return false;
            // Independence does not establish criminality. Only an
            // existing recorded Outlaw calling is eligible for this label.
            if(role=="Outlaw")return CurrentCareerRole()=="Outlaw";
            if(role=="Wanderer")return clan.Settlements!=null &&
                clan.Settlements.Count==0 && clan.Kingdom==null;
            return true;
        }

        // Observe only roles supported by actual authority. Never infer a new criminal or civilian calling.
        private void ObserveLifetimeCareerAutomatically()
        {
            var player=Hero.MainHero;
            if(player==null || !applied || !IsStandaloneSandbox())return;
            try
            {
                string role=new[]{"Soldier","Knight","King","Lord","Mercenary"}.FirstOrDefault(CanRecordLifetimeRole);
                if(role==null && player.StringId==startAuditOwner &&
                    (startAuditRole=="Wanderer" || startAuditRole=="Outlaw") && CanRecordLifetimeRole(startAuditRole))
                {
                    // Do not re-label a former contract-holder as an outlaw or wanderer simply for traveling independently.
                    if(!lifetimeCareers.For(player.StringId).Any(e=>e.Role!=startAuditRole))role=startAuditRole;
                }
                if(role==null)return;
                int day=(int)Math.Floor(CampaignTime.Now.ToDays);
                if(lifetimeCareers.Record(player.StringId,role,day,true,true))
                    DiagnosticJournal.Event("cu_lifetime_native_observation","hero="+player.StringId+"|role="+role+"|day="+day+"|no_new_title=true");
            }
            catch(Exception error){DiagnosticJournal.Exception("cu_lifetime_observation_failed",error);}
        }
        private void RecordLifetimeRole(string role)
        {
            var player=Hero.MainHero;
            if(player==null || !CanRecordLifetimeRole(role))return;
            int day=(int)Math.Floor(CampaignTime.Now.ToDays);
            bool recorded=lifetimeCareers.Record(player.StringId,role,day,true,true);
            ShowNativeDetail("Lifetime career",
                (recorded?"Career recorded.":
                    "No new receipt: same/earlier day or history limit.")+
                "\n\n"+LifetimeCareerText(player));
        }

        private string LifetimeCareerText(Hero person)
        {
            if(person==null)return "No identified person.";
            var records=lifetimeCareers.For(person.StringId);
            return "Lifetime career observations (not continuous service):\n"+
                (records.Length==0?"No personal episodes yet.":
                    String.Join("\n",records.Select(e=>e.Role+
                    " | first seen day "+e.FirstDay+" | last seen day "+e.LastDay)))+
                "\nOriginal career chapter ledger is retained separately.";
        }

        private SandboxCareerReviewState recurringCareerReviews=
            new SandboxCareerReviewState();

        // A voluntary check of present native conditions, not an extra
        // quest, contract, payment, promotion, or political action.
        private string ReviewCareerEvidence(string role)
        {
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            var party=MobileParty.MainParty;
            if(!applied || !IsStandaloneSandbox() || player==null ||
                !player.IsAlive || !player.IsActive || player.IsPrisoner ||
                clan==null || party==null || party.MapEvent!=null ||
                party.IsCurrentlyAtSea)return "";
            var town=Settlement.CurrentSettlement;
            switch(role)
            {
                case "Wanderer":
                    return clan.Kingdom==null && town!=null && town.IsTown?
                        "Current independent town presence confirmed. Review livelihood and household needs.":"";
                case "Outlaw":
                    return clan.Kingdom==null && party.LeaderHero==player &&
                        party.MemberRoster!=null && party.MemberRoster.TotalManCount>=5?
                        "Independent party of at least five confirmed. Review supplies and band security; criminal status not inferred.":"";
                case "Mercenary":
                    return clan.Kingdom!=null && clan.IsUnderMercenaryService?
                        "Mercenary contract confirmed.":"";
                case "Soldier":
                    var service=SandboxSaSBridge.Read();
                    return service.Available && service.Enlisted &&
                        service.InsideHostParty && !String.IsNullOrEmpty(service.LordId)?
                        "External enlistment and host-party presence confirmed. Review provider rank and duty.":"";
                case "Knight":
                    var patron=KnightOathPatron();
                    return knightOath.HasCurrentOath &&
                        knightOath.PlayerId==player.StringId && patron!=null &&
                        patron.IsAlive && !patron.IsPrisoner &&
                        patron.Clan!=null && patron.Clan.StringId==knightOath.PatronClanId &&
                        patron.Clan.Kingdom!=null &&
                        patron.Clan.Kingdom.StringId==knightOath.KingdomId?
                        "Current personal oath and patron affiliation confirmed. Review muster and escort obligations.":"";
                case "Lord":
                    return clan.Leader==player && clan.Kingdom!=null &&
                        clan.Kingdom.RulingClan!=clan && !clan.IsUnderMercenaryService &&
                        clan.Settlements!=null && clan.Settlements.Count>0?
                        "Vassalage confirmed.":"";
                case "King":
                    return clan.Leader==player && clan.Kingdom!=null &&
                        clan.Kingdom.RulingClan==clan && !clan.IsUnderMercenaryService?
                        "Crown confirmed.":"";
                default: return "";
            }
        }

        private string SelectedReviewRole()
        {
            var player=Hero.MainHero;
            var episode=player==null?null:lifetimeCareers.For(player.StringId).LastOrDefault();
            return episode==null?CurrentCareerRole():episode.Role;
        }

        private string RecurringCareerReviewText()
        {
            var player=Hero.MainHero;
            string role=SelectedReviewRole();
            var receipt=player==null?null:
                recurringCareerReviews.For(player.StringId,role);
            string activity=SandboxCareerReviewState.Activity(role);
            if(activity.Length==0)return "No supported career review.";
            return activity+"\nRecorded reviews: "+(receipt==null?0:receipt.Count)+
                (receipt==null?"":" | Last observed day: "+receipt.LastDay)+
                "\nReviews require current evidence and are seven days apart. They grant no rewards.";
        }

        private void ReviewRecurringCareer()
        {
            ShowNativeDetail("Career | Agreed charge",
                DiscussChargeProgress()+"\n\n"+RecurringCareerReviewText());
        }

        private string CurrentCareerRole()
        {
            if(kingSuccession.IsRecorded)return "King";
            if(lordInheritance.IsRecorded)return "Lord";
            if(knightOath.IsConfigured)return "Knight";
            if(!roleEconomy.Started)return "";
            string role=roleEconomy.Role;
            if(role=="EnlistedSoldier")return "Soldier";
            if(role=="HouseholdKnight")return "Knight";
            if(role=="Wanderer" || role=="Outlaw" ||
                role=="Mercenary")return role;
            return "";
        }

        private void ObserveCareerChronicleDaily()
        {
            if(!applied || !IsStandaloneSandbox() ||
                Hero.MainHero==null || Clan.PlayerClan==null ||
                MobileParty.MainParty==null || !roleEconomy.Started)
                return;
            ObserveLifetimeCareerAutomatically();
            // Old starting chapters are founder-specific. Do not award them to successors.
            if(Hero.MainHero.StringId!=startAuditOwner)return;
            string role=CurrentCareerRole();
            if(role.Length==0)return;
            // Never continue the old starting-role chapters on a new calling.
            var selected=lifetimeCareers.For(Hero.MainHero.StringId).LastOrDefault();
            if(selected!=null && selected.Role!=role)return;
            if(!careerChronicle.Started)careerChronicle.Begin(role);
            if(careerChronicle.Role!=role)return;
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            var party=MobileParty.MainParty;
            var location=Settlement.CurrentSettlement ??
                player.CurrentSettlement;
            bool ownLand=clan.Settlements!=null &&
                clan.Settlements.Count>0;
            bool independent=clan.Kingdom==null;
            bool nativeRuler=clan.Kingdom!=null &&
                clan.Kingdom.RulingClan==clan &&
                clan.Leader==player;
            bool first=false,second=false,third=false;
            string townId="";
            switch(role)
            {
                case "Wanderer":
                    bool nativeTown=independent && location!=null &&
                        location.IsTown;
                    townId=nativeTown?location.StringId:"";
                    first=nativeTown;
                    second=nativeTown;
                    third=nativeTown;
                    break;
                case "Outlaw":
                    int partyPeople=party.MemberRoster==null?0:
                        party.MemberRoster.TotalManCount;
                    first=independent && partyPeople>=5;
                    second=first;
                    third=independent && partyPeople>=10;
                    break;
                case "Mercenary":
                    first=clan.IsUnderMercenaryService &&
                        clan.Kingdom!=null;
                    second=first;
                    third=first;
                    break;
                case "Soldier":
                    var liveService=SandboxSaSBridge.Read();
                    first=serviceCareer.VerifiedExternalEnlistment &&
                        liveService.Available && liveService.Enlisted &&
                        liveService.LordId==serviceCareer.ExternalLordId;
                    second=first && liveService.Days>=7 &&
                        serviceCareer.ExternalInsideHostParty && liveService.InsideHostParty;
                    third=first && liveService.Days>=30 &&
                        serviceCareer.ExternalInsideHostParty && liveService.InsideHostParty;
                    break;
                case "Knight":
                    first=knightOath.IsConfigured && knightOath.HasCurrentOath &&
                        knightOath.PlayerId==player.StringId &&
                        knightOath.LastMusterDay>=0;
                    second=first && knightEscort.CompletedEscorts>=1;
                    third=second && knightEscort.CompletedEscorts>=2;
                    break;
                case "Lord":
                    first=lordInheritance.IsRecorded && ownLand &&
                        clan.Leader==player && clan.Kingdom!=null &&
                        clan.Kingdom.RulingClan!=clan && !clan.IsUnderMercenaryService;
                    second=first;
                    third=first && clan.Settlements.Count>=2;
                    break;
                case "King":
                    first=kingSuccession.IsRecorded && nativeRuler;
                    second=first && royalCouncil.IsRecorded &&
                        royalCouncil.Meetings>=1;
                    third=second && royalCouncil.Meetings>=3;
                    break;
            }
            int day=(int)Math.Floor(CampaignTime.Now.ToDays);
            string result=careerChronicle.Observe(
                day,first,second,third,townId);
            if(result.StartsWith("career_chapter_",
                StringComparison.Ordinal))
            {
                DiagnosticJournal.Event("cu_career_native_chapter",
                    "result="+result+
                    "|role="+role+
                    "|chapter="+careerChronicle.Chapter+
                    "|day="+day+
                    "|location="+(townId.Length>0?townId:"none")+
                    "|native_state_verified=true"+
                    "|no_fake_rank_or_salary=true");
                // Historical milestones are recorded silently, never presented as new chores.
            }
        }

        private string CareerChronicleDescription()
        {
            string role=CurrentCareerRole();
            if(role.Length==0)
                return "No verified Calradia Unified starting "+
                    "career has been recorded in this campaign.";
            if(!careerChronicle.Started)
                return "Your first chapter will be entered after "+
                    "the next actual native campaign observation.";
            if(careerChronicle.Role!=role)
                return "The saved career is not the verified "+
                    "current career. No history has been overwritten.";
            var clan=Clan.PlayerClan;
            var player=Hero.MainHero;
            string standing=clan==null || player==null?
                "Unverified political status":
                SandboxStartingLifePolicy.PoliticalStanding(
                    clan.Leader==player,clan.Kingdom!=null,
                    clan.Kingdom!=null && clan.Kingdom.RulingClan==clan,
                    clan.IsUnderMercenaryService);
            return "Recorded career: "+role+
                "\nCareer duties (guidance): "+
                SandboxStartingLifePolicy.CareerContract(role)+
                "\nPolitical standing: "+standing+
                "\nClan settlements: "+
                (clan==null || clan.Settlements==null?"unverified":
                    clan.Settlements.Count.ToString())+
                "\nPersonal oath: "+
                (knightOath.IsConfigured?knightOath.Status:"none")+
                "\nLast service status: "+
                serviceCareer.Status+
                "\nHistorical starting milestones: "+careerChronicle.Chapter+"/3"+
                (IsPersonalChargeActive()?"\nA legacy personal goal remains. You can set it aside.":"")+
                "\nThese chapters record actual Bannerlord "+
                "actions, real parties, native contracts and "+
                "recognized people. They do not grant a "+
                "game rank, salary, troops, land or kingdom office.";
        }

        private string PersonalChronicleText()
        {
            var player=Hero.MainHero;
            if(player==null)return "No current character.";
            return CareerChronicleDescription()+"\n\n"+LifetimeCareerText(player)+
                "\n\n"+(roleEconomy.Started && player.StringId==startAuditOwner?roleEconomy.Describe():"Current livelihood: native economy")+
                "\n\n"+BattleHistoryDescription()+"\n\n"+PersonalHistoryText(player);
        }
        private void RegisterCareerChronicleMenus(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddGameMenu("cu_career_chronicle",
                "Career chronicle. Your deeds and duties are "+
                "recorded from the real campaign. "+
                "No title is granted without the game's own authority.",
                args=>{ObserveCareerChronicleDaily();});
            starter.AddGameMenuOption("cu_people_lineage",
                "cu_career_chronicle_entry",
                "My chronicle",
                args=>roleEconomy.Started &&
                    CurrentCareerRole().Length>0,
                args=>GameMenu.SwitchToMenu(
                    "cu_career_chronicle"),false);
            starter.AddGameMenuOption("cu_career_chronicle",
                "cu_career_chronicle_review",
                "Read my career history",
                args=>true,
                args=>{
                    ObserveCareerChronicleDaily();
                    ShowNativeDetail("Career history",
                        PersonalChronicleText());
                },false);
            starter.AddGameMenuOption("cu_career_chronicle",
                "cu_career_charge_withdraw","Set aside my current charge",
                args=>IsPersonalChargeActive(),
                args=>ShowNativeDetail("Career | Charge",WithdrawCareerCharge()),false);
            starter.AddGameMenuOption("cu_career_chronicle",
                "cu_career_chronicle_return",
                "Back",
                args=>true,
                args=>GameMenu.SwitchToMenu(
                    "cu_people_lineage"),false);
        }
    }
}

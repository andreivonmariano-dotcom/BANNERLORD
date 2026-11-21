using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
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
            string role=CurrentCareerRole();
            if(role.Length==0)return;
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
                    bool nativeTown=location!=null &&
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
                    first=serviceCareer.VerifiedExternalEnlistment;
                    second=first && serviceCareer.DaysInService>=7 &&
                        serviceCareer.ExternalInsideHostParty;
                    third=first && serviceCareer.DaysInService>=30 &&
                        serviceCareer.ExternalInsideHostParty;
                    break;
                case "Knight":
                    first=knightOath.IsConfigured &&
                        knightOath.LastMusterDay>=0;
                    second=first && knightEscort.CompletedEscorts>=1;
                    third=second && knightEscort.CompletedEscorts>=2;
                    break;
                case "Lord":
                    first=lordInheritance.IsRecorded && ownLand;
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
                Notify("Your "+role+" chronicle has recorded "+
                    "chapter "+careerChronicle.Chapter+" of three. "+
                    "Review your next duty from the household ledger.");
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
            return "Your path: "+role+
                "\nWitnessed chapters: "+careerChronicle.Chapter+"/3"+
                "\nPresent charge: "+
                SandboxCareerChronicleState.ChapterObjective(
                    role,careerChronicle.Chapter)+
                (careerChronicle.ConsecutiveDays>0?
                    "\nConfirmed days in this duty: "+
                    careerChronicle.ConsecutiveDays:"")+
                "\nThese chapters record actual Bannerlord "+
                "actions, real parties, native contracts and "+
                "recognized people. They do not grant a "+
                "game rank, salary, troops, land or kingdom office.";
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
                "My career chronicle and next charge",
                args=>roleEconomy.Started &&
                    CurrentCareerRole().Length>0,
                args=>GameMenu.SwitchToMenu(
                    "cu_career_chronicle"),false);
            starter.AddGameMenuOption("cu_career_chronicle",
                "cu_career_chronicle_review",
                "Review my current chapter",
                args=>true,
                args=>{
                    ObserveCareerChronicleDaily();
                    ShowNativeDetail("Career | Your next charge",
                        CareerChronicleDescription());
                },false);
            starter.AddGameMenuOption("cu_career_chronicle",
                "cu_battle_history_review",
                "Review witnessed battle service",
                args=>roleEconomy.Started,
                args=>ShowNativeDetail("Career | Battle history",
                    BattleHistoryDescription()),false);
            starter.AddGameMenuOption("cu_career_chronicle",
                "cu_career_chronicle_return",
                "Return to household affairs",
                args=>true,
                args=>GameMenu.SwitchToMenu(
                    "cu_people_lineage"),false);
        }
    }
}

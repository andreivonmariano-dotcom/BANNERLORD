using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        // Preserve the exact saved fields and original codecs in SyncData.
        private SandboxPeerSocialState peerSocial=new SandboxPeerSocialState();
        private SandboxStoryDirectorState storyDirector=new SandboxStoryDirectorState();

        // Old camp gathering plans cannot open the removed illustrated screen.
        private bool CanSetUpNativeCamp(){return false;}

        private void RegisterHouseholdCampMenus(CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddGameMenu("cu_household_camp",
                "The illustrated camp register is retired. Use My Household, Correspondence and My Chronicle.",
                args=>{});
            starter.AddGameMenuOption("cu_household_camp","cu_household_camp_leave",
                "Return to the campaign",args=>true,args=>GameMenu.ExitToLast(),true);
        }
    }
}

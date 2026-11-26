using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
namespace CalradiaUnified
{
    // Compile seam only: never installed or substituted for actual CU behavior.
    public sealed partial class SandboxOriginBehavior
    {
        private bool applied=true;
        private bool IsStandaloneSandbox(){return false;}
        private bool HasHouseholdCommandServiceAuthority(){return false;}
        private bool CanAssignHouseholdSpecialty(PartyRole role){return false;}
        private void AssignHouseholdSpecialty(PartyRole role,bool recordMemory){throw new System.InvalidOperationException();}
        private bool CanFollowPersonalTopic(){return false;}
        private string personalTopicName="Concerns";
        private History livingPersons=new History();
        private int CurrentHouseholdDay(){return 0;}
        private sealed class History{public bool Remember(string a,string b,string c,string d){return false;}}
    }
}
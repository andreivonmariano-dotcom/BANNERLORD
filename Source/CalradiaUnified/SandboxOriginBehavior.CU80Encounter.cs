using System;
using CalradiaUnified.CU80;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private DurableLedger cu80Ledger=new DurableLedger();
        private SurgeonDialogue cu80Dialogue;
        private void SyncCU80Encounter(IDataStore store)
        {
            // Additive primitive string; all prior behavior IDs and keys are unchanged.
            string packed=store.IsLoading?"":cu80Ledger.Pack();
            bool present=store.SyncData(DurableLedger.SaveKey,ref packed);
            if(store.IsLoading)
            {
                cu80Ledger=DurableLedger.Load(packed,!present);
                cu80Dialogue=null; // no pending token survives a reload
            }
        }
        private bool CU80ManualSurgeonAllowed()
        {
            var player=Hero.MainHero;
            return player!=null&&!cu80Ledger.HasUncertain(player.StringId);
        }
        private void ResetCU80Conversation()
        {
            if(cu80Dialogue!=null)cu80Dialogue.ResetConversation();
        }
        private bool CU80ContextualSurgeonAvailable()
        {
            return cu80Dialogue!=null&&cu80Dialogue.Available();
        }
        private void RegisterCU80Encounter(CampaignGameStarter starter)
        {
            string session=Guid.NewGuid().ToString("N");
            try{cu80Dialogue=new SurgeonDialogue(
                new NativeSurgeonPort(session,()=>applied&&IsStandaloneSandbox(),
                    ()=>HasHouseholdCommandServiceAuthority(),()=>CanAssignHouseholdSpecialty(PartyRole.Surgeon),
                    ()=>AssignHouseholdSpecialty(PartyRole.Surgeon,false)),
                session,cu80Ledger,
                s=>livingPersons.Remember("NativePartyDutyAssigned",s.Player,s.Actor,"role=Surgeon|day="+CurrentHouseholdDay()));}catch{cu80Dialogue=null;}
            SurgeonDialogue.Register(starter,()=>cu80Dialogue,()=>CanFollowPersonalTopic()&&personalTopicName=="Concerns");
        }
    }
}
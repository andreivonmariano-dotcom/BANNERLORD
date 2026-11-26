using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
namespace CU80.Lab
{
    // Native registration, but no separate behavior or role setter.
    public sealed class SurgeonDialogue
    {
        private readonly ISurgeonPort port;
        private readonly Encounter encounter;
        private readonly DurableLedger ledger;
        private readonly Action<Snapshot> recordConfirmed;
        private Offer pending;
        private string reply="";
        public SurgeonDialogue(ISurgeonPort port,string session,DurableLedger ledger,Action<Snapshot> recordConfirmed)
        {
            if(port==null||ledger==null||recordConfirmed==null)throw new ArgumentException();
            this.port=port;this.ledger=ledger;this.recordConfirmed=recordConfirmed;encounter=new Encounter(port,session);
        }
        public bool Available()
        {
            try{var s=port.Read();return s!=null&&s.Eligible&&ledger.CanBegin(DurableLedger.SurgeonKey(s),s.Player);}
            catch{return false;}
        }
        public void ResetConversation()
        {
            pending=null;reply="";
            try{ledger.ReconcileObservedSurgeon(port.Read());}catch{}
        }
        public void Open()
        {
            pending=null;
            try
            {
                var o=encounter.Present();
                if(o!=null&&ledger.CanBegin(DurableLedger.SurgeonKey(o.Before),o.Before.Player))pending=o;
            }catch{}
        }
        public bool Ready()
        {
            try{return pending!=null&&pending.Before.SameOfferState(port.Read())&&ledger.CanBegin(DurableLedger.SurgeonKey(pending.Before),pending.Before.Player);}
            catch{return false;}
        }
        public bool PrepareConcern()
        {
            if(!Ready())return false;
            MBTextManager.SetTextVariable("CU80_WOUNDED",pending.Wounded);
            return true;
        }
        public void Decide(Choice choice)
        {
            reply="The circumstances have changed. We can discuss this another time.";
            var o=pending;bool ready=Ready();pending=null;
            if(o==null||!ready)return;
            string key=DurableLedger.SurgeonKey(o.Before);
            if(!ledger.Begin(key,o.Before.Player,o.Before.Actor,o.Before.Party,"Surgeon"))return;
            Receipt r;
            try{r=encounter.Resolve(o,choice);}
            catch{ledger.Finish(key,AttemptState.Uncertain);reply="I cannot confirm the appointment. We should check the party screen.";return;}
            if(r.Confirmed)
            {
                if(!ledger.Finish(key,AttemptState.Confirmed))return;
                reply="I will tend our wounded.";
                try{recordConfirmed(r.After);}catch{/* no repeated native action or invented memory */}
            }
            else if(r.Status==Outcome.Declined){ledger.Finish(key,AttemptState.Declined);reply="Very well. We will keep our current arrangements.";}
            else if(r.Status==Outcome.Deferred){ledger.Finish(key,AttemptState.Deferred);reply="Of course. We can leave it for now.";}
            else if(r.Status==Outcome.Stale||r.Status==Outcome.InvalidOffer){ledger.Finish(key,AttemptState.NoAction);}
            else{ledger.Finish(key,AttemptState.Uncertain);reply="I cannot confirm the appointment. We should check the party screen.";}
        }
        public bool PrepareReply()
        {
            MBTextManager.SetTextVariable("CU80_SURGEON_REPLY",new TextObject(reply));
            return true;
        }
        public static void Register(CampaignGameStarter starter,Func<SurgeonDialogue> current,Func<bool> concernRoute)
        {
            if(starter==null||current==null||concernRoute==null)throw new ArgumentException();
            // Conditions are read-only; creation/opening is deferred to explicit player consequence.
            starter.AddPlayerLine("cu80_surgeon_open","cu_personal_followup_choices","cu80_surgeon_offer",
                "What should we do about our wounded?",()=>concernRoute()&&current()!=null&&current().Available(),()=>current().Open(),151);
            starter.AddDialogLine("cu80_surgeon_offer_line","cu80_surgeon_offer","cu80_surgeon_choices",
                "{CU80_WOUNDED} of our people are wounded. I have experience tending injuries. Shall I look after them?",
                ()=>current()!=null&&current().PrepareConcern(),null,150);
            starter.AddDialogLine("cu80_surgeon_stale","cu80_surgeon_offer","close_window",
                "The circumstances have changed. We can speak again later.",null,null,1);
            starter.AddPlayerLine("cu80_surgeon_accept","cu80_surgeon_choices","cu80_surgeon_reply",
                "Take responsibility for the wounded.",()=>current()!=null&&current().Ready(),()=>current().Decide(Choice.Assign),150);
            starter.AddPlayerLine("cu80_surgeon_decline","cu80_surgeon_choices","cu80_surgeon_reply",
                "We will keep our current arrangements.",null,()=>{var d=current();if(d!=null)d.Decide(Choice.LeaveAsIs);},149);
            starter.AddPlayerLine("cu80_surgeon_defer","cu80_surgeon_choices","cu80_surgeon_reply",
                "We will discuss this later.",null,()=>{var d=current();if(d!=null)d.Decide(Choice.Defer);},148);
            starter.AddDialogLine("cu80_surgeon_result","cu80_surgeon_reply","close_window",
                "{CU80_SURGEON_REPLY}",()=>current()!=null&&current().PrepareReply(),null,150);
            starter.AddDialogLine("cu80_surgeon_result_exit","cu80_surgeon_reply","close_window",
                "We can speak again later.",null,null,1);
        }
    }
}
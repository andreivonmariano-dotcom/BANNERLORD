using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;

namespace CalradiaUnified
{
    // Retired civic adapter. Same runtime type and primitive save key; no resource mutations.
    public sealed class CivicBehavior : CampaignBehaviorBase
    {
        private CivicState state = new CivicState();
        public CivicBehavior(Settings settings) { }

        public override void RegisterEvents()
        {
            // Save migration only. No daily mutations, advisory scans or active civic UI.
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, RegisterRetiredCivicExits);
        }
        public override void SyncData(IDataStore store)
        {
            string packed = state.Serialize();
            store.SyncData("CU_v030_civic_state", ref packed);
            state = CivicState.Parse(packed);
            DiagnosticJournal.Event("civic_sync_data_invoked", "not_proof_of_save");
            Log.Write("M0 civic state sync reached: depots=" + state.Depots.Count
                + " shipments=" + state.Shipments.Count + ". Review a real save/reload for persistence.");
        }

        private void RegisterRetiredCivicExits(CampaignGameStarter starter)
        {
            if(starter==null)return;
            // Preserve a way out of a menu referenced by an older save.
            // Do not expose these menus from new gameplay or register old transactions.
            foreach(string menu in new[]{"cu_civic_administration","cu_civic_stores","cu_civic_garrison","cu_civic_correspondence"})
            {
                string id=menu;
                starter.AddGameMenu(id,"This old CU administration menu has been retired. Use the settlement's existing management systems.",args=>{});
                starter.AddGameMenuOption(id,"cu_retired_exit_"+id,"Return to settlement",
                    args=>true,args=>GameMenu.ExitToLast(),true);
            }
        }
    }
}

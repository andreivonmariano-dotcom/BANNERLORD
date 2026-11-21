using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Library;

namespace CalradiaUnified
{
    // The UIExtenderEx v2 API remains supported by required v3.0.3.
    // Keep the native Clan, Party and Kingdom tabs intact. Only one
    // small clearly-labelled entry is added per native movie.
    [ViewModelMixin(handleDerived: true)]
    internal sealed class CUClanNativeMixin :
        BaseViewModelMixin<ClanManagementVM>
    {
        public CUClanNativeMixin(ClanManagementVM vm):base(vm){}
        [DataSourceProperty]
        public string CUClanSummary { get {
            return SandboxOriginBehavior.NativeBUTRStatus("Clan");
        }}
        [DataSourceMethod]
        public void ExecuteCUDossier()
        {
            SandboxOriginBehavior.OpenFromNativeBUTRScreen();
        }
    }

    [ViewModelMixin(handleDerived: true)]
    internal sealed class CUPartyNativeMixin :
        BaseViewModelMixin<PartyVM>
    {
        public CUPartyNativeMixin(PartyVM vm):base(vm){}
        [DataSourceProperty]
        public string CUPartySummary { get {
            return SandboxOriginBehavior.NativeBUTRStatus("Party");
        }}
        [DataSourceMethod]
        public void ExecuteCUDossier()
        {
            SandboxOriginBehavior.OpenFromNativeBUTRScreen();
        }
    }

    [ViewModelMixin(handleDerived: true)]
    internal sealed class CUKingdomNativeMixin :
        BaseViewModelMixin<KingdomManagementVM>
    {
        public CUKingdomNativeMixin(KingdomManagementVM vm):base(vm){}
        [DataSourceProperty]
        public string CUKingdomSummary { get {
            return SandboxOriginBehavior.NativeBUTRStatus("Kingdom");
        }}
        [DataSourceMethod]
        public void ExecuteCUDossier()
        {
            SandboxOriginBehavior.OpenFromNativeBUTRScreen();
        }
    }

    // Target native movie root children only. The exact GUI patch
    // placement, collision and responsiveness remain a live Gate C test.
    [PrefabExtension("Clan", "descendant::Window/Children")]
    internal sealed class CUClanMoviePatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;
        [PrefabExtensionText]
        public string Content => CUButton(
            "CUClanButton","@CUClanSummary");
        private static string CUButton(string id,string binding)
        {
            return CUNativeScreenPrefab.Button(id,binding);
        }
    }
    [PrefabExtension("Party", "descendant::Window/Children")]
    internal sealed class CUPartyMoviePatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;
        [PrefabExtensionText]
        public string Content => CUNativeScreenPrefab.Button(
            "CUPartyButton","@CUPartySummary");
    }
    [PrefabExtension("Kingdom", "descendant::Window/Children")]
    internal sealed class CUKingdomMoviePatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;
        [PrefabExtensionText]
        public string Content => CUNativeScreenPrefab.Button(
            "CUKingdomButton","@CUKingdomSummary");
    }

    internal static class CUNativeScreenPrefab
    {
        internal static string Button(string id,string title)
        {
            // Native ButtonBrush, compact slot and one data-bound label.
            return "<ButtonWidget Id=\""+id+
                "\" Brush=\"ButtonBrush\" "+
                "DoNotPassEventsToChildren=\"true\" "+
                "UpdateChildrenStates=\"true\" "+
                "WidthSizePolicy=\"Fixed\" SuggestedWidth=\"330\" "+
                "HeightSizePolicy=\"Fixed\" SuggestedHeight=\"44\" "+
                "HorizontalAlignment=\"Right\" "+
                "VerticalAlignment=\"Top\" "+
                "PositionXOffset=\"-26\" PositionYOffset=\"84\" "+
                "Command.Click=\"ExecuteCUDossier\">"+
                "<Children><TextWidget Brush.FontSize=\"14\" "+
                "Text=\""+title+"\" "+
                "WidthSizePolicy=\"StretchToParent\" "+
                "HeightSizePolicy=\"StretchToParent\" "+
                "HorizontalAlignment=\"Center\" "+
                "VerticalAlignment=\"Center\" />"+
                "</Children></ButtonWidget>";
        }
    }
}

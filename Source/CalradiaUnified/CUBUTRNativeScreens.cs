using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core.ViewModelCollection.Generic;
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
        [DataSourceMethod]
        public void ExecuteCUCamp()
        {
            SandboxOriginBehavior.EnterCampFromPartyDossier();
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

    // Use the native encyclopedia's existing stat-row renderer. No
    // custom portrait, prefab replacement, or change to native filters.
    [ViewModelMixin("RefreshValues")]
    internal sealed class CUEncyclopediaProfessionMixin : BaseViewModelMixin<EncyclopediaHeroPageVM>
    {
        private readonly EncyclopediaHeroPageVM page;
        private StringPairItemVM row;
        public CUEncyclopediaProfessionMixin(EncyclopediaHeroPageVM vm):base(vm){page=vm;}
        public override void OnRefresh()
        {
            try
            {
                if(page==null || page.Stats==null)return;
                if(row!=null && page.Stats.Contains(row))page.Stats.Remove(row);
                row=null;
                var hero=page.Obj as Hero;
                if(hero==null || !hero.IsAlive || page.IsInformationHidden ||
                    Clan.PlayerClan==null || hero.Clan!=Clan.PlayerClan)return;
                string title=SandboxOriginBehavior.ContextualHouseholdTitle(hero);
                if(String.IsNullOrEmpty(title) || title=="Companion")return;
                row=new StringPairItemVM("Standing / profession",title);
                page.Stats.Add(row);
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception("cu_encyclopedia_profession_failed",error);
            }
        }
    }

    // Target native movie root children only. The exact GUI patch
    // placement, collision and responsiveness remain a live Gate C test.
    [PrefabExtension("Clan", "descendant::Window/Children")]
    internal sealed class CUClanMoviePatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;
        [PrefabExtensionText]
        public string Content => "<ButtonWidget Id=\"CUClanButton\" Brush=\"ButtonBrush\" DoNotPassEventsToChildren=\"true\" UpdateChildrenStates=\"true\" WidthSizePolicy=\"Fixed\" SuggestedWidth=\"340\" HeightSizePolicy=\"Fixed\" SuggestedHeight=\"46\" HorizontalAlignment=\"Right\" VerticalAlignment=\"Top\" PositionXOffset=\"-24\" PositionYOffset=\"82\" Command.Click=\"ExecuteCUDossier\"><Children><TextWidget Brush.FontSize=\"16\" Text=\"@CUClanSummary\" WidthSizePolicy=\"StretchToParent\" HeightSizePolicy=\"StretchToParent\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"/></Children></ButtonWidget>";
    }
    [PrefabExtension("Party", "descendant::Window/Children")]
    internal sealed class CUPartyMoviePatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;
        [PrefabExtensionText]
        public string Content => "<ButtonWidget Id=\"CUPartyButton\" Brush=\"ButtonBrush\" DoNotPassEventsToChildren=\"true\" UpdateChildrenStates=\"true\" WidthSizePolicy=\"Fixed\" SuggestedWidth=\"340\" HeightSizePolicy=\"Fixed\" SuggestedHeight=\"46\" HorizontalAlignment=\"Right\" VerticalAlignment=\"Top\" PositionXOffset=\"-24\" PositionYOffset=\"82\" Command.Click=\"ExecuteCUDossier\"><Children><TextWidget Brush.FontSize=\"16\" Text=\"@CUPartySummary\" WidthSizePolicy=\"StretchToParent\" HeightSizePolicy=\"StretchToParent\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"/></Children></ButtonWidget>";
    }
    [PrefabExtension("Kingdom", "descendant::Window/Children")]
    internal sealed class CUKingdomMoviePatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;
        [PrefabExtensionText]
        public string Content => "<ButtonWidget Id=\"CUKingdomButton\" Brush=\"ButtonBrush\" DoNotPassEventsToChildren=\"true\" UpdateChildrenStates=\"true\" WidthSizePolicy=\"Fixed\" SuggestedWidth=\"340\" HeightSizePolicy=\"Fixed\" SuggestedHeight=\"46\" HorizontalAlignment=\"Right\" VerticalAlignment=\"Top\" PositionXOffset=\"-24\" PositionYOffset=\"82\" Command.Click=\"ExecuteCUDossier\"><Children><TextWidget Brush.FontSize=\"16\" Text=\"@CUKingdomSummary\" WidthSizePolicy=\"StretchToParent\" HeightSizePolicy=\"StretchToParent\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"/></Children></ButtonWidget>";
    }

    // This is a second Party-only action. The global dossier is not
    // a scene and does not spawn NPCs. This action changes to the
    // guarded on-map household camp menu.
    [PrefabExtension("Party", "descendant::Window/Children")]
    internal sealed class CUPartyCampMoviePatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;
        [PrefabExtensionText]
        public string Content => "<ButtonWidget Id=\"CUPartyCampButton\" Brush=\"ButtonBrush\" DoNotPassEventsToChildren=\"true\" UpdateChildrenStates=\"true\" WidthSizePolicy=\"Fixed\" SuggestedWidth=\"340\" HeightSizePolicy=\"Fixed\" SuggestedHeight=\"46\" HorizontalAlignment=\"Right\" VerticalAlignment=\"Top\" PositionXOffset=\"-24\" PositionYOffset=\"134\" Command.Click=\"ExecuteCUCamp\"><Children><TextWidget Brush.FontSize=\"16\" Text=\"Household camp\" WidthSizePolicy=\"StretchToParent\" HeightSizePolicy=\"StretchToParent\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"/></Children></ButtonWidget>";
    }

}

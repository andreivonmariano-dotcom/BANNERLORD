using System;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Library;

namespace CalradiaUnified
{
    [ViewModelMixin(handleDerived: true)]
    internal sealed class CUMapBarAffairsMixin : BaseViewModelMixin<MapBarVM>
    {
        public CUMapBarAffairsMixin(MapBarVM vm):base(vm){}
        [DataSourceMethod] public void ExecuteCUAffairs()
        {SandboxOriginBehavior.OpenCampaignAffairs();}
    }
    [ViewModelMixin(handleDerived: true)]
    internal sealed class CUMapNotificationAffairsMixin : BaseViewModelMixin<MapNotificationVM>
    {
        // Weak reference avoids keeping a native map VM alive across campaigns.
        private static WeakReference<CUMapNotificationAffairsMixin> current;
        private static float elapsed;
        private int count=-1;
        public CUMapNotificationAffairsMixin(MapNotificationVM vm):base(vm)
        {current=new WeakReference<CUMapNotificationAffairsMixin>(this);}
        [DataSourceProperty] public bool CUHasPendingAffairs{get{return count>0;}}
        [DataSourceProperty] public string CUAffairsNotice{get{return count==1?"1 matter awaits your attention":count+" matters await your attention";}}
        [DataSourceMethod] public void ExecuteCUAffairsNotice()
        {SandboxOriginBehavior.OpenCampaignAffairs();}
        private void UpdateNotice(int pending)
        {
            if(count==pending)return;
            count=pending;
            OnPropertyChangedWithValue(CUHasPendingAffairs,"CUHasPendingAffairs");
            OnPropertyChangedWithValue(CUAffairsNotice,"CUAffairsNotice");
        }
        internal static void Pulse(float dt)
        {
            if(dt<=0f || dt>10f)return;
            elapsed+=dt;
            if(elapsed<1f)return;
            elapsed=0f;
            CUMapNotificationAffairsMixin target;
            if(current==null || !current.TryGetTarget(out target))return;
            try{target.UpdateNotice(SandboxOriginBehavior.PendingAffairsCount());}
            catch(Exception error)
            {
                current=null;
                DiagnosticJournal.Exception("cu_map_affairs_notice_failed",error);
            }
        }
        internal static void Clear(){current=null;elapsed=0f;}
    }
    // The root placement is deliberately scoped to the native map movies.
    // Exact 1.5.7 movie XPath and collision checks remain live acceptance.
    [PrefabExtension("MapBar", "descendant::Window/Widget/Children")]
    internal sealed class CUMapAffairsTabPatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;
        [PrefabExtensionText]
        public string Content => "<ButtonWidget Id=\"CUMapAffairs\" Brush=\"Popup.Done.Button.NineGrid\" DoNotPassEventsToChildren=\"true\" UpdateChildrenStates=\"true\" WidthSizePolicy=\"Fixed\" SuggestedWidth=\"176\" HeightSizePolicy=\"Fixed\" SuggestedHeight=\"40\" HorizontalAlignment=\"Right\" VerticalAlignment=\"Bottom\" PositionXOffset=\"-24\" PositionYOffset=\"-84\" Command.Click=\"ExecuteCUAffairs\"><Children><TextWidget Brush=\"Popup.Button.Text\" Brush.FontSize=\"15\" Text=\"Correspondence\" WidthSizePolicy=\"StretchToParent\" HeightSizePolicy=\"StretchToParent\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"/></Children></ButtonWidget>";
    }
    [PrefabExtension("MapNotificationUI", "descendant::ListPanel[@Id='MapNotificationWidget']/Children")]
    internal sealed class CUMapAffairsNoticePatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;
        [PrefabExtensionText]
        public string Content => "<ButtonWidget Id=\"CUMapAffairsNotice\" IsVisible=\"@CUHasPendingAffairs\" Brush=\"Popup.Done.Button.NineGrid\" DoNotPassEventsToChildren=\"true\" UpdateChildrenStates=\"true\" WidthSizePolicy=\"Fixed\" SuggestedWidth=\"228\" HeightSizePolicy=\"Fixed\" SuggestedHeight=\"48\" HorizontalAlignment=\"Center\" Command.Click=\"ExecuteCUAffairsNotice\"><Children><TextWidget Brush=\"Popup.Button.Text\" Brush.FontSize=\"15\" Text=\"@CUAffairsNotice\" WidthSizePolicy=\"StretchToParent\" HeightSizePolicy=\"StretchToParent\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"/></Children></ButtonWidget>";
    }
}

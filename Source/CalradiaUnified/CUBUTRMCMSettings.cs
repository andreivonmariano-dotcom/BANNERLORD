using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace CalradiaUnified
{
    /// <summary>
    /// The required MCM v5 options surface. Existing INI persistence
    /// remains authoritative, preserving old profiles and save keys.
    /// New campaign behaviors read changes when they next initialize.
    /// </summary>
    public sealed class CUBUTRMCMSettings :
        AttributeGlobalSettings<CUBUTRMCMSettings>
    {
        public override string Id => "CalradiaUnified";
        public override string DisplayName => "Calradia Unified";
        public override string FolderName => "CalradiaUnified";
        public override string FormatType => "none";

        private static bool Read(string key)
        {
            var descriptor=SettingsCatalog.Find(key);
            return descriptor!=null &&
                SettingsCatalog.Read(Settings.Load(),descriptor)=="true";
        }
        private static void Set(string key,bool enabled)
        {
            var profile=Settings.Load();
            string error;
            if(!profile.TrySetMenuValue(
                    key,enabled?"true":"false",out error))
                DiagnosticJournal.Event("cu_mcm_setting_failed",
                    "setting="+key+"|error="+error);
        }

        [SettingPropertyBool("Diagnostics",Order=0,
            RequireRestart=true,
            HintText="Record campaign events and errors. Takes effect after restart.")]
        [SettingPropertyGroup("Diagnostics")]
        public bool EnableDiagnostics
        {
            get=>Read("EnableDiagnostics");
            set=>Set("EnableDiagnostics",value);
        }

        [SettingPropertyBool("Automatic session reports",Order=1,
            RequireRestart=true,
            HintText="Create diagnostic ZIP reports for real gameplay acceptance.")]
        [SettingPropertyGroup("Diagnostics")]
        public bool AutoSessionReports
        {
            get=>Read("AutoSessionReports");
            set=>Set("AutoSessionReports",value);
        }

        [SettingPropertyBool("Supply warnings",Order=2,
            RequireRestart=true,
            HintText="Show supply and food warnings in the current campaign.")]
        [SettingPropertyGroup("Campaign")]
        public bool EnableSupplyWarnings
        {
            get=>Read("EnableSupplyWarnings");
            set=>Set("EnableSupplyWarnings",value);
        }

        [SettingPropertyBool("Army supply warnings",Order=3,
            RequireRestart=true,
            HintText="Report native army food shortages. Takes effect after restart.")]
        [SettingPropertyGroup("Campaign")]
        public bool EnableArmyWarnings
        {
            get=>Read("EnableArmyWarnings");
            set=>Set("EnableArmyWarnings",value);
        }
    }
}

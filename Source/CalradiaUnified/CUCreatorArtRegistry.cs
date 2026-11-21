using System;
using TaleWorlds.Engine.GauntletUI;

namespace CalradiaUnified
{
    /// <summary>
    /// Optional custom sprite atlas. The pinned game needs TaleWorlds'
    /// sprite-sheet generator and Modding Kit import before raw PNGs are
    /// usable in Gauntlet XML. Never load a custom-art movie until the
    /// category is registered. Keep the installed-game fallback intact.
    /// </summary>
    internal static class CUCreatorArtRegistry
    {
        private const string Category="ui_calradiaunified";
        private static bool unavailableLogged;
        private static bool loaded;

        public static bool IsAvailable()
        {
            // A category checked before resource initialization must be
            // discoverable on later frames. Cache only a successful load.
            if(loaded)return true;
            try
            {
                var data=UIResourceManager.SpriteData;
                if(data==null || data.SpriteCategories==null)
                    return false;
                var categories=data.SpriteCategories;
                if(!categories.ContainsKey(Category))
                {
                    if(!unavailableLogged)
                    {
                        unavailableLogged=true;
                        DiagnosticJournal.Event("cu_v059_art_unavailable",
                            "reason=category_not_imported|fallback=native");
                    }
                    return false;
                }
                // The v1.5.7 pinned references do not expose the newer
                // UIResourceManager.UIResourceDepot property used in the
                // published Modding Kit sample. The category must already
                // be loaded through the imported AlwaysLoad sprite config.
                // If the atlas texture is missing, LoadMovie falls back.
                loaded=true;
                DiagnosticJournal.Event("cu_v059_art_ready",
                    "category="+Category+"|imported=true");
            }
            catch(Exception error)
            {
                loaded=false;
                DiagnosticJournal.Exception(
                    "cu_v059_art_category_load_failed",error);
            }
            return loaded;
        }
    }
}

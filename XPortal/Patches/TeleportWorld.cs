using HarmonyLib;

namespace XPortal.Patches
{
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.GetHoverText))]
    static class TeleportWorld_GetHoverText
    {
        /// <summary>
        /// Replace the game's hover text
        /// </summary>
        static bool Prefix(ZNetView ___m_nview, ref string __result)
        {
            if (!___m_nview || !___m_nview.IsValid())
            {
                Jotunn.Logger.LogError("TeleportWorldGetHoverTextPatch: This portal does not exist. Odin strokes his beard in confusion..");
                __result = "This portal doesn't actually appear to exist. Heimdallr sees you...";
            }
            else
            {
                ZDO portalZDO = ___m_nview.GetZDO();
                var portalId = portalZDO.m_uid;
                var location = portalZDO.GetPosition();
                XPortal.OnPrePortalHover(out __result, portalId, location);
            }

            // Don't run the original method at all
            return false;
        }
    }
}

using HarmonyLib;

namespace XPortal.Patches
{
    [HarmonyPatch(typeof(TextInput), nameof(TextInput.RequestText))]
    static class TextInput_RequestText
    {
        /// <summary>
        /// When the game is trying to show the original "configure portal" window, deny it.
        /// </summary>
        static bool Prefix(TextReceiver sign)
        {
            // Get the TeleportWorld reference
            if (sign is TeleportWorld teleportWorld)
            {
                // Request the XPortal UI here instead of the vanilla "set tag" window
                XPortal.OnPortalRequestText(teleportWorld.m_nview.GetZDO().m_uid);

                // Don't run the original method at all
                return false;
            }

            // Continue as normal
            return true;
        }
    }
}

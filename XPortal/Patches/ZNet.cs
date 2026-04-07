using HarmonyLib;
using XPortal.RPC;

namespace XPortal.Patches
{
    /// <summary>
    /// After peer handshake on clients, request portal network admin status from the server.
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
    internal static class ZNet_RPC_PeerInfo_Postfix
    {
        private static void Postfix(ZNet __instance)
        {
            if (__instance == null || __instance.IsServer())
            {
                return;
            }

            XPortalAdminSync.RequestFromServerIfNeeded();
        }
    }
}

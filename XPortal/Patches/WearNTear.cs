using HarmonyLib;

namespace XPortal.Patches
{
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.OnPlaced))]
    static class WearNTear_OnPlaced
    {
        /// <summary>
        /// After placing a piece, check if it's a portal, and if so, call OnPortalPlaced
        /// Known issue: This patch magically stops working if a patch on Player.PlacePiece exists (here or in another mod)
        /// See: SpikeHimself/XPortal#36 and BepInEx/HarmonyX#71
        /// </summary>
        internal static void Postfix(WearNTear __instance)
        {
            var piece = __instance.GetComponent<Piece>();
            var nview = __instance.GetComponent<ZNetView>();
            if (piece.m_name.Contains("$piece_portal") && nview)
            {
                var portalZDO = nview.GetZDO();
                if (portalZDO == null)
                {
                    Log.Error("A portal was placed but the ZDO is not available");
                    return;
                }

                var portalId = portalZDO.m_uid;
                var location = portalZDO.GetPosition();
                XPortal.OnPortalPlaced(portalId, location);
            }
        }
    }

    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Destroy))]
    static class WearNTear_Destroy
    {
        /// <summary>
        /// Before destroying a piece, check if it's a portal, and if so, call OnPortalDestroyed
        /// </summary>
        static void Prefix(WearNTear __instance)
        {
            var piece = __instance.m_piece;
            if (!piece)
            {
                return;
            }

            var nview = piece.m_nview;
            if (!nview)
            {
                return;
            }

            if (piece.m_name.Contains("$piece_portal") && piece.CanBeRemoved())
            {
                var portalZDO = nview.GetZDO();
                if (portalZDO == null)
                {
                    Log.Error("A portal was destroyed but the ZDO is not available");
                    return;
                }

                var portalId = portalZDO.m_uid;
                XPortal.OnPortalDestroyed(portalId);
            }
        }
    }
}

using HarmonyLib;

namespace XPortal.Patches
{
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.OnPlaced))]
    static class WearNTear_OnPlaced
    {
        /// <summary>
        /// After placing a piece, check if it's a portal, and if so, call OnPortalPlaced
        /// </summary>
        static void Postfix(Piece ___m_piece, ZNetView ___m_nview)
        {
            if (___m_piece.m_name.Contains("$piece_portal") && ___m_piece.CanBeRemoved() && ___m_nview)
            {
                var portalZDO = ___m_nview.GetZDO();
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

            Log.Debug($"Piece destroyed: {piece.m_name}");

            if (piece.m_name.Contains("$piece_portal") && piece.CanBeRemoved())
            {
                var portalZDO = nview.GetZDO();
                if (portalZDO == null)
                {
                    return;
                }

                var portalId = portalZDO.m_uid;
                XPortal.OnPortalDestroyed(portalId);
            }
        }
    }
}

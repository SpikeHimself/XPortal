namespace XPortal
{
    /// <summary>
    /// Peer player id resolution and admin checks for portal network changes.
    /// </summary>
    internal static class NetPeerUtility
    {
        /// <summary>
        /// Player id for the peer's character, or 0 if unavailable.
        /// </summary>
        internal static long GetPeerPlayerId(long peerId)
        {
            if (ZNet.instance == null || ZDOMan.instance == null)
            {
                return 0L;
            }

            var peer = ZNet.instance.GetPeer(peerId);
            if (peer == null)
            {
                // Server Host is not in m_peers; routed RPC sender id is ZNet.GetUID().
                if (ZNet.instance.IsServer() && peerId == ZNet.GetUID())
                {
                    if (Player.m_localPlayer != null)
                    {
                        return Player.m_localPlayer.GetPlayerID();
                    }

                    return Game.instance != null ? Game.instance.GetPlayerProfile().GetPlayerID() : 0L;
                }

                return 0L;
            }

            if (peer.m_characterID.IsNone())
            {
                return 0L;
            }

            var characterZdo = ZDOMan.instance.GetZDO(peer.m_characterID);
            if (characterZdo == null)
            {
                return 0L;
            }

            return characterZdo.GetLong(ZDOVars.s_playerID);
        }

        /// <summary>
        /// True if the peer is a server admin and may change portal network assignment.
        /// </summary>
        internal static bool IsPeerPrivilegedForPortalNetwork(long peerId)
        {
            if (ZNet.instance == null)
            {
                return false;
            }

            var peer = ZNet.instance.GetPeer(peerId);
            if (peer == null)
            {
                if (ZNet.instance.IsServer() && peerId == ZNet.GetUID())
                {
                    return ZNet.instance.LocalPlayerIsAdminOrHost();
                }

                return false;
            }

            return ZNet.instance.IsAdmin(peer.m_socket.GetHostName());
        }
    }
}

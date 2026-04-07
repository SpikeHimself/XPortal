namespace XPortal.RPC
{
    /// <summary>
    /// Server-confirmed admin access for portal network UI. Hosts do not use the RPC.
    /// </summary>
    internal static class XPortalAdminSync
    {
        private static bool _requestedThisSession;
        private static bool _serverReplyReceived;
        private static bool _serverSaysAdmin;

        internal static void ResetForNewSession()
        {
            _requestedThisSession = false;
            _serverReplyReceived = false;
            _serverSaysAdmin = false;
        }

        internal static bool IsLocalPortalNetworkAdmin()
        {
            if (ZNet.instance == null)
            {
                return false;
            }

            if (ZNet.instance.IsServer())
            {
                return true;
            }

            if (_serverReplyReceived)
            {
                return _serverSaysAdmin;
            }

            var userId = UserInfo.GetLocalUser().UserId;
            return userId.IsValid && ZNet.instance.PlayerIsAdmin(userId);
        }

        internal static void ApplyServerReply(bool isAdmin)
        {
            _serverSaysAdmin = isAdmin;
            _serverReplyReceived = true;
        }

        internal static void RequestFromServerIfNeeded()
        {
            if (ZNet.instance == null || ZNet.instance.IsServer())
            {
                return;
            }

            if (_requestedThisSession)
            {
                return;
            }

            _requestedThisSession = true;
            var pkg = new ZPackage();
            ZRoutedRpc.instance.InvokeRoutedRPC(Environment.ServerPeerId, RPCManager.RPC_REQUESTADMINSYNC, pkg);
            Log.Debug("Requested portal network admin status from server");
        }
    }
}

namespace XPortal.RPC
{
    internal static class SendToServer
    {
        /// <summary>
        /// Ask the server to distribute the list of KnownPortals
        /// </summary>
        /// <param name="reason">The reason for the Resync Request</param>
        public static void SyncRequest(string reason)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendSyncRequestToServer] Sending sync request to server, because: {reason}");
            ZRoutedRpc.instance.InvokeRoutedRPC(Environment.ServerPeerId, RPCManager.RPC_SYNCREQUEST, reason);
        }

        /// <summary>
        /// Ask the server to add or update this portal
        /// </summary>
        /// <param name="portal">The KnownPortal that should be added or udpated</param>
        public static void AddOrUpdateRequest(KnownPortal portal)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendAddOrUpdateRequestToServer] {portal.Id}");

            var pkg = portal.Pack();
            ZRoutedRpc.instance.InvokeRoutedRPC(Environment.ServerPeerId, RPCManager.RPC_ADDORUPDATEREQUEST, pkg);
        }

        /// <summary>
        /// Ask the server to remove this portal
        /// </summary>
        /// <param name="id">The ZDOID of the portal that should be removed</param>
        public static void RemoveRequest(ZDOID id)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendRemoveRequestToServer] {id}");
            ZRoutedRpc.instance.InvokeRoutedRPC(Environment.ServerPeerId, RPCManager.RPC_REMOVEREQUEST, id);
        }

        /// <summary>
        /// Ask the server for the config settings
        /// </summary>
        public static void ConfigRequest()
        {
            Jotunn.Logger.LogDebug("[RPC.SendConfigRequestToServer]");
            ZRoutedRpc.instance.InvokeRoutedRPC(Environment.ServerPeerId, RPCManager.RPC_CONFIGREQUEST);
        }

    }
}

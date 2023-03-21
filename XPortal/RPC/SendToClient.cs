using UnityEngine;

namespace XPortal.RPC
{
    internal static class SendToClient
    {
        /// <summary>
        /// Send one portal to all clients
        /// </summary>
        /// <param name="portal">The KnownPortal to send to the clients</param>
        public static void SyncPortal(KnownPortal portal)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendSyncPortalToClients] Sending {portal} to everybody");

            var pkg = portal.Pack();
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPCManager.RPC_SYNCPORTAL, pkg);
        }

        /// <summary>
        /// Send a package of all portals to all clients
        /// </summary>
        /// <param name="pkg">A ZPackage containing a count followed by a ZPackage for each KnownPortal</param>
        /// <param name="reason">The reason that was given for the Resync Request</param>
        public static void Resync(ZPackage pkg, string reason)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendResyncToClients] Sending all portals to everybody, because: {reason}");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPCManager.RPC_RESYNC, pkg, reason);
        }

        /// <summary>
        /// Send a package of all config settings to all clients
        /// </summary>
        /// <param name="pkg">A ZPackage containing all config settings</param>
        public static void Config(ZPackage pkg)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendConfigToClients] Sending config to everyone");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPCManager.RPC_CONFIG, pkg);
        }

        /// <summary>
        /// Send a package of all config settings to a client
        /// </summary>
        /// <param name="clientPeerID">The client to send the package to</param>
        /// <param name="pkg">A ZPackage containing all config settings</param>
        public static void Config(long clientPeerID, ZPackage pkg)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendConfigToClient] Sending config to {clientPeerID}");
            ZRoutedRpc.instance.InvokeRoutedRPC(clientPeerID, RPCManager.RPC_CONFIG, pkg);
        }

        /// <summary>
        /// Send a ping to everyone
        /// </summary>
        /// <param name="location">The location in the world that should be pinged</param>
        /// <param name="text">The text that should appear on the ping message</param>
        public static void PingMap(Vector3 location, string text)
        {
            Jotunn.Logger.LogDebug($"[{nameof(PingMap)}] `{text}` at `{location}`");

            // Since Valheim patch 0.214.2 (2023-03-13), the ChatMessage RPC requires a UserInfo object instead of the player name string
            var localUserInfo = UserInfo.GetLocalUser();
            // ..but XPortal much prefers to show the name of the portal, instead of the name of the player
            localUserInfo.Name = text;

            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPCManager.RPC_CHATMESSAGE, location, (int)Talker.Type.Ping, localUserInfo, string.Empty, PrivilegeManager.GetNetworkUserId());
        }
    }
}

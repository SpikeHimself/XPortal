using UnityEngine;

namespace XPortal
{
    internal static class RPC
    {
        private static long GetServerPeerId()
        {
            return ZRoutedRpc.instance.GetServerPeerID();
        }

        #region From Server
        /// <summary>
        /// Send one portal to all clients
        /// </summary>
        /// <param name="knownPortal">The portal to send to the clients</param>
        public static void SendSyncPortal(KnownPortal knownPortal)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendSyncPortal] Sending {knownPortal} to everybody");

            var pkg = knownPortal.Pack();

            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, XPortal.RPC_SYNCPORTAL, pkg);
        }

        /// <summary>
        /// Send a package of all portals
        /// </summary>
        /// <param name="pkg">A ZPackage containing a count followed by all portals</param>
        public static void SendResync(ZPackage pkg)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendResync] Sending all portals to everybody");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, XPortal.RPC_RESYNC, pkg);
        }
        #endregion

        #region To Server
        /// <summary>
        /// Ask the server to distribute the list of portals
        /// </summary>
        public static void SendSyncRequest()
        {
            Jotunn.Logger.LogDebug($"[RPC.SendSyncRequest] Sending sync request to server");
            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerId(), XPortal.RPC_SYNCREQUEST);
        }

        /// <summary>
        /// Ask the server to update the portal's target. This is also used to remove the target (by setting it to ZDOID.None)
        /// </summary>
        /// <param name="id">The ZDOID of the portal being changed</param>
        /// <param name="target">The ZDOID of the portal's new target. Can also be ZDOID.None</param>
        public static void SendTargetChangeRequest(ZDOID id, ZDOID target)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendTargetChangeRequest] {id} -> {target}");
            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerId(), XPortal.RPC_TARGETCHANGEREQUEST, id, target);
        }

        /// <summary>
        /// Ask the server to change the name of this portal
        /// </summary>
        /// <param name="id">The id of the portal that is changing name</param>
        /// <param name="newName">The portal's new name</param>
        public static void SendNameChangeRequest(ZDOID id, string newName)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendNameChangeRequest] {id} -> {newName}");
            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerId(), XPortal.RPC_NAMECHANGEREQUEST, id, newName);
        }

        /// <summary>
        /// Ask the server to check if the ZDO belonging to this ZDOID actually still exists
        /// (and deal with it, if not)
        /// </summary>
        /// <param name="id">The ZDOID of the portal to inspect</param>
        public static void SendCheckZDORequest(ZDOID id)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendCheckZDORequest] {id}");
            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerId(), XPortal.RPC_CHECKZDOREQUEST, id);
        }
        #endregion

        #region From/To Clients
        public static void SendPingMap(Vector3 location, string text)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendPingMap] {location}, {text}");
            // I have no idea what the numbers 3 and 1 mean in this context, but it works..
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, XPortal.RPC_CHATMESSAGE, location, 3, text, string.Empty, 1);
        }
        #endregion

    }
}

using System;
using UnityEngine;

namespace XPortal
{
    internal static class RPC
    {

        #region RPC Names
        // Server RPCs
        private const string RPC_SYNCPORTAL = "XPortal_SyncPortal";
        private const string RPC_RESYNC = "XPortal_Resync";
        private const string RPC_CONFIG = "XPortal_Config";

        // Client RPCs
        private const string RPC_SYNCREQUEST = "XPortal_SyncRequest";
        private const string RPC_ADDORUPDATEREQUEST = "XPortal_AddOrUpdateRequest";
        private const string RPC_REMOVEREQUEST = "XPortal_RemoveRequest";
        private const string RPC_CONFIGREQUEST = "XPortal_ConfigRequest";

        // Client to client
        private const string RPC_CHATMESSAGE = "ChatMessage";
        #endregion

        private static long GetServerPeerId()
        {
            return ZRoutedRpc.instance.GetServerPeerID();
        }

        /// <summary>
        /// Register our RPCs with ZRoutedRpc, so that the game knows which function to call when these messages arrive
        /// </summary>
        public static void RegisterRPCs()
        {
            // Server RPCs
            ZRoutedRpc.instance.Register<ZPackage>(RPC_SYNCPORTAL, new Action<long, ZPackage>(RPC_SyncPortal));
            ZRoutedRpc.instance.Register<ZPackage, string>(RPC_RESYNC, new Action<long, ZPackage, string>(RPC_Resync));
            ZRoutedRpc.instance.Register<ZPackage>(RPC_CONFIG, new Action<long, ZPackage>(RPC_Config));

            // Client RPCs
            ZRoutedRpc.instance.Register<string>(RPC_SYNCREQUEST, new Action<long, string>(RPC_SyncRequest));
            ZRoutedRpc.instance.Register<ZPackage>(RPC_ADDORUPDATEREQUEST, new Action<long, ZPackage>(RPC_AddOrUpdateRequest));
            ZRoutedRpc.instance.Register<ZDOID>(RPC_REMOVEREQUEST, new Action<long, ZDOID>(RPC_RemoveRequest));
            ZRoutedRpc.instance.Register(RPC_CONFIGREQUEST, new Action<long>(RPC_ConfigRequest));
        }

        #region From Server
        /// <summary>
        /// Send one portal to all clients
        /// </summary>
        /// <param name="portal">The KnownPortal to send to the clients</param>
        public static void SendSyncPortalToClients(KnownPortal portal)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendSyncPortalToClients] Sending {portal} to everybody");

            var pkg = portal.Pack();
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_SYNCPORTAL, pkg);
        }

        /// <summary>
        /// Send a package of all portals to all clients
        /// </summary>
        /// <param name="pkg">A ZPackage containing a count followed by a ZPackage for each KnownPortal</param>
        /// <param name="reason">The reason that was given for the Resync Request</param>
        public static void SendResyncToClients(ZPackage pkg, string reason)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendResyncToClients] Sending all portals to everybody, because: {reason}");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_RESYNC, pkg, reason);
        }

        /// <summary>
        /// Send a package of all config settings to a client
        /// </summary>
        /// <param name="clientPeerID">The client to send the package to</param>
        /// <param name="pkg">A ZPackage containing all config settings</param>
        public static void SendConfigToClient(long clientPeerID, ZPackage pkg)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendConfigToClient] Sending config to {clientPeerID}");
            ZRoutedRpc.instance.InvokeRoutedRPC(clientPeerID, RPC_CONFIG, pkg);
        }

        /// <summary>
        /// Send a package of all config settings to all clients
        /// </summary>
        /// <param name="pkg">A ZPackage containing all config settings</param>
        public static void SendConfigToClients(ZPackage pkg)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendConfigToClients] Sending config to everyone");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_CONFIG, pkg);
        }
        #endregion

        #region To Server
        /// <summary>
        /// Ask the server to distribute the list of KnownPortals
        /// </summary>
        /// <param name="reason">The reason for the Resync Request</param>
        public static void SendSyncRequestToServer(string reason)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendSyncRequestToServer] Sending sync request to server, because: {reason}");
            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerId(), RPC_SYNCREQUEST, reason);
        }

        /// <summary>
        /// Ask the server to add or update this portal
        /// </summary>
        /// <param name="portal">The KnownPortal that should be added or udpated</param>
        public static void SendAddOrUpdateRequestToServer(KnownPortal portal)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendAddOrUpdateRequestToServer] {portal.Id}");

            var pkg = portal.Pack();
            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerId(), RPC_ADDORUPDATEREQUEST, pkg);
        }

        /// <summary>
        /// Ask the server to remove this portal
        /// </summary>
        /// <param name="id">The ZDOID of the portal that should be removed</param>
        public static void SendRemoveRequestToServer(ZDOID id)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendRemoveRequestToServer] {id}");
            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerId(), RPC_REMOVEREQUEST, id);
        }

        /// <summary>
        /// Ask the server for the config settings
        /// </summary>
        public static void SendConfigRequestToServer()
        {
            Jotunn.Logger.LogDebug("[RPC.SendConfigRequestToServer]");
            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerId(), RPC_CONFIGREQUEST);
        }
        #endregion

        #region From/To Clients
        /// <summary>
        /// Send a ping to everyone
        /// </summary>
        /// <param name="location">The location in the world that should be pinged</param>
        /// <param name="text">The text that should appear on the ping message</param>
        public static void SendPingMapToEverybody(Vector3 location, string text)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendPingMapToEverybody] `{text}` at `{location}`");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_CHATMESSAGE, location, 3, text, string.Empty, PrivilegeManager.GetNetworkUserId());
        }
        #endregion

        #region RPC Events (Server)
        /// <summary>
        /// A client wishes to receive the portal list
        /// </summary>
        /// <param name="sender">The id of the sender</param>
        /// <param name="reason">The reason for the Resync Request</param>
        private static void RPC_SyncRequest(long sender, string reason)
        {
            Jotunn.Logger.LogInfo($"Received sync request from `{sender}` because: {reason}");
            XPortal.ProcessSyncRequest(reason);
        }

        /// <summary>
        /// A client wishes for a portal to be added or updated
        /// </summary>
        /// <param name="sender">The id of the sender</param>
        /// <param name="pkg">A ZPackage containing the packed KnownPortal</param>
        private static void RPC_AddOrUpdateRequest(long sender, ZPackage pkg)
        {
            if (!XPortal.IsServer())
            {
                Jotunn.Logger.LogDebug($"[RPC_AddOrUpdateRequest] `{sender}` wants a portal to be added or updated, but I am not the server.");
                return;
            }

            var portal = new KnownPortal(pkg);
            Jotunn.Logger.LogDebug($"[RPC_AddOrUpdateRequest] {sender} wants `{portal.Id}` to be added or updated");

            var updatedPortal = KnownPortalsManager.Instance.AddOrUpdate(portal);

            var portalZDO = ZDOMan.instance.GetZDO(updatedPortal.Id);
            if (portalZDO != null)
            {
                Jotunn.Logger.LogInfo($"Setting portal tag `{updatedPortal.Name}` and target `{updatedPortal.Target}` on behalf of `{sender}`");
                portalZDO.Set("tag", updatedPortal.Name);
                portalZDO.Set("target", updatedPortal.Target);
                ZDOMan.instance.ForceSendZDO(updatedPortal.Id);
            }

            SendSyncPortalToClients(updatedPortal);
        }

        /// <summary>
        /// A client wishes for a portal to be removed
        /// </summary>
        /// <param name="sender">The id of the sender</param>
        /// <param name="portalId">The ZDOID of the portal that should be removed</param>
        private static void RPC_RemoveRequest(long sender, ZDOID portalId)
        {
            if (!XPortal.IsServer())
            {
                Jotunn.Logger.LogDebug($"[RPC_RemoveRequest] {sender} wants `{portalId}` to be removed, but I am not the server.");
                return;
            }

            if (!KnownPortalsManager.Instance.ContainsId(portalId))
            {
                Jotunn.Logger.LogDebug($"[RPC_RemoveRequest] {sender} wants `{portalId}` to be removed, but it doesn't exist");
                return;
            }

            Jotunn.Logger.LogDebug($"[RPC_RemoveRequest] {sender} wants `{portalId}` to be removed");

            if (KnownPortalsManager.Instance.Remove(portalId))
            {
                Jotunn.Logger.LogDebug($"[RPC_RemoveRequest] `{portalId}` removed, checking other portals' targets..");

                var portalsWithInvalidTarget = KnownPortalsManager.Instance.GetPortalsWithTarget(portalId);
                foreach (var portalWithInvalidTarget in portalsWithInvalidTarget)
                {
                    Jotunn.Logger.LogDebug($"[RPC_RemoveRequest] Removing target from `{portalWithInvalidTarget.Name}`");

                    portalWithInvalidTarget.Target = ZDOID.None;
                    SendAddOrUpdateRequestToServer(portalWithInvalidTarget);
                }

                SendResyncToClients(KnownPortalsManager.Instance.Pack(), "A portal was removed");
            }
        }

        /// <summary>
        /// A client has asked for the server's config settings
        /// </summary>
        /// <param name="sender">The id of the sender</param>
        private static void RPC_ConfigRequest(long sender)
        {
            if (!XPortal.IsServer())
            {
                Jotunn.Logger.LogDebug($"[RPC_ConfigRequest] {sender} wants to receive the config, but I am not the server.");
                return;
            }

            Jotunn.Logger.LogDebug($"[RPC_ConfigRequest] {sender} wants to receive the config");
            var pkg = XPortalConfig.Instance.PackLocalConfig();
            SendConfigToClient(sender, pkg);
        }
        #endregion

        #region RPC Events (Client)
        /// <summary>
        /// The server sent us all of the portals it knows
        /// </summary>
        /// <param name="sender">The server</param>
        /// <param name="pkg">A ZPackage containing a number followed by a list of packaged portals</param>
        /// <param name="reason">The reason that was given for the Resync Request</param>
        private static void RPC_Resync(long sender, ZPackage pkg, string reason)
        {
            if (XPortal.IsServer())
            {
                return;
            }

            Jotunn.Logger.LogInfo($"Resyncing because: {reason}");
            KnownPortalsManager.Instance.UpdateFromResyncPackage(pkg);
        }

        /// <summary>
        /// The server sent us a KnownPortal that was added or updated
        /// </summary>
        /// <param name="sender">The server</param>
        /// <param name="pkg">A ZPackage containing the KnownPortal that was added or updated</param>
        private static void RPC_SyncPortal(long sender, ZPackage pkg)
        {
            if (XPortal.IsServer())
            {
                return;
            }

            var incomingPortal = new KnownPortal(pkg);
            Jotunn.Logger.LogDebug($"[RPC_SyncPortal] Received update to portal `{incomingPortal.Name}`");
            KnownPortalsManager.Instance.AddOrUpdate(incomingPortal);
        }

        /// <summary>
        /// The server sent us a package containing all config settings
        /// </summary>
        /// <param name="sender">The server</param>
        /// <param name="pkg">A ZPackage containing all config settings</param>
        private static void RPC_Config(long sender, ZPackage pkg)
        {
            Jotunn.Logger.LogInfo($"Received XPortal Config from server");
            XPortalConfig.Instance.ReceiveServerConfig(pkg);
        }
        #endregion

    }
}

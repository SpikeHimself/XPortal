using System;
using UnityEngine;

namespace XPortal
{
    internal static class RPC
    {

        #region RPC Names
        // Client to server
        public const string RPC_SYNCREQUEST = "XPortal_SyncRequest";
        public const string RPC_ADDORUPDATEREQUEST = "XPortal_AddOrUpdateRequest";
        public const string RPC_REMOVEREQUEST = "XPortal_RemoveRequest";

        // Server to client
        public const string RPC_SYNCPORTAL = "XPortal_SyncPortal";
        public const string RPC_RESYNC = "XPortal_Resync";

        // Client to client
        public const string RPC_CHATMESSAGE = "ChatMessage";
        #endregion

        private static long GetServerPeerId()
        {
            return ZRoutedRpc.instance.GetServerPeerID();
        }

        public static void RegisterRPCs()
        {
            ZRoutedRpc.instance.Register<string>(RPC_SYNCREQUEST, new Action<long, string>(RPC_SyncRequest));
            ZRoutedRpc.instance.Register<ZPackage>(RPC_ADDORUPDATEREQUEST, new Action<long, ZPackage>(RPC_AddOrUpdateRequest));
            ZRoutedRpc.instance.Register<ZDOID>(RPC_REMOVEREQUEST, new Action<long, ZDOID>(RPC_RemoveRequest));

            ZRoutedRpc.instance.Register<ZPackage>(RPC_SYNCPORTAL, new Action<long, ZPackage>(RPC_SyncPortal));
            ZRoutedRpc.instance.Register<ZPackage, string>(RPC_RESYNC, new Action<long, ZPackage, string>(RPC_Resync));
        }

        #region From Server
        /// <summary>
        /// Send one portal to all clients
        /// </summary>
        /// <param name="portal">The KnownPortal to send to the clients</param>
        public static void SendSyncPortalToClients(KnownPortal portal)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendSyncPortal] Sending {portal} to everybody");

            var pkg = portal.Pack();
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_SYNCPORTAL, pkg);
        }

        /// <summary>
        /// Send a package of all portals
        /// </summary>
        /// <param name="pkg">A ZPackage containing a count followed by all KnownPortals</param>
        /// <param name="reason">The reason that was given for the Resync Request</param>
        public static void SendResyncToClients(ZPackage pkg, string reason)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendResync] Sending all portals to everybody, because: {reason}");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_RESYNC, pkg, reason);
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
        #endregion

        #region From/To Clients
        /// <summary>
        /// Send a ping to everyone
        /// </summary>
        /// <param name="location">The location in the world that should be pinged</param>
        /// <param name="text">The text that should appear on the ping message</param>
        public static void SendPingMapToEverybody(Vector3 location, string text)
        {
            Jotunn.Logger.LogDebug($"[RPC.SendPingMap] {location}, {text}");
            // I have no idea what the numbers 3 and 1 mean in this context, but it works..
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_CHATMESSAGE, location, 3, text, string.Empty, 1);
        }
        #endregion

        #region RPC Events
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

            var incomingPortal = KnownPortal.Unpack(pkg);

            Jotunn.Logger.LogDebug($"[OnRpcSyncPortal] Received update to portal `{incomingPortal.Name}`");
            KnownPortalsManager.Instance.AddOrUpdate(incomingPortal);
        }

        /// <summary>
        /// A client wishes to receive the portal list
        /// </summary>
        /// <param name="sender">The id of the sender</param>
        /// <param name="reason">The reason for the Resync Request</param>
        private static void RPC_SyncRequest(long sender, string reason)
        {
            Jotunn.Logger.LogInfo($"Received sync request from {sender} because: {reason}");
            XPortal.ProcessSyncRequest(reason);
        }

        /// <summary>
        /// A client wishes for a portal to be added or updated
        /// </summary>
        /// <param name="sender">The id of the sender</param>
        /// <param name="pkg">A ZPackage containing the packed KnownPortal</param>
        private static void RPC_AddOrUpdateRequest(long sender, ZPackage pkg)
        {
            var portal = KnownPortal.Unpack(pkg);

            if (!XPortal.IsServer())
            {
                Jotunn.Logger.LogDebug($"[RPC_AddOrUpdateRequest] {sender} wants `{portal.Id}` to be added or updated, but I am not the server.");
                return;
            }

            Jotunn.Logger.LogDebug($"[RPC_AddOrUpdateRequest] {sender} wants `{portal.Id}` to be added or updated");

            var isNewPortal = !KnownPortalsManager.Instance.ContainsId(portal.Id);
            var updatedPortal = KnownPortalsManager.Instance.AddOrUpdate(portal);

            if (!isNewPortal)
            {
                var portalZDO = Util.TryGetZDO(updatedPortal.Id);

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
        #endregion

    }
}

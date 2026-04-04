using XPortal;
using XPortal.RPC;

namespace XPortal.RPC.Server
{
    internal static class ServerEvents
    {
        private const string ERR_NOTSERVER = "but I am not the server!";

        /// <summary>
        /// A client wishes to receive the portal list
        /// </summary>
        /// <param name="sender">The id of the sender</param>
        /// <param name="reason">The reason for the Resync Request</param>
        internal static void RPC_SyncRequest(long sender, string reason)
        {
            Log.Info($"Received sync request from `{sender}` because: {reason}");
            XPortal.ProcessSyncRequest(reason);
        }

        /// <summary>
        /// A client wishes for a portal to be added or updated
        /// </summary>
        /// <param name="sender">The id of the sender</param>
        /// <param name="pkg">A ZPackage containing the packed KnownPortal</param>
        internal static void RPC_AddOrUpdateRequest(long sender, ZPackage pkg)
        {
            if (!Environment.IsServer)
            {
                Log.Error($"`{sender}` wants a portal to be added or updated, {ERR_NOTSERVER}");
                return;
            }

            var portal = new KnownPortal(pkg);
            Log.Debug($"{sender} wants `{portal.Id}` to be added or updated");

            var requesterPlayerId = NetPeerUtility.GetPeerPlayerId(sender);
            var portalZdo = ZDOMan.instance.GetZDO(portal.Id);
            if (portalZdo == null)
            {
                Log.Error($"Portal ZDO `{portal.Id}` not found for network validation");
                return;
            }

            var pieceCreator = portalZdo.GetLong(ZDOVars.s_creator);
            var requesterIsCreator = requesterPlayerId != 0L && requesterPlayerId == pieceCreator;
            var requesterMayChangeNetwork = requesterIsCreator || NetPeerUtility.IsPeerPrivilegedForPortalNetwork(sender);
            var requesterMayEditPrivatePortal = requesterIsCreator || NetPeerUtility.IsPeerPrivilegedForPortalNetwork(sender);

            KnownPortalsManager.Instance.TryGetValue(portal.Id, out var existing);

            if (!requesterMayChangeNetwork)
            {
                var authoritativeNetwork = existing != null
                    ? existing.NetworkOwnerPlayerId
                    : ZdoTools.GetNetworkOwnerPlayerId(portalZdo);
                portal.NetworkOwnerPlayerId = authoritativeNetwork;
                portal.NetworkOwnerDisplayName = existing != null
                    ? existing.NetworkOwnerDisplayName
                    : ZdoTools.GetNetworkOwnerDisplayName(portalZdo);
            }
            else
            {
                if (portal.NetworkOwnerPlayerId == 0L)
                {
                    portal.NetworkOwnerDisplayName = string.Empty;
                }
                else
                {
                    portal.NetworkOwnerPlayerId = pieceCreator;
                    if (pieceCreator == 0L)
                    {
                        portal.NetworkOwnerDisplayName = string.Empty;
                    }
                    else if (requesterIsCreator)
                    {
                        var fromClient = PortalNetwork.SanitizeNetworkOwnerDisplayName(portal.NetworkOwnerDisplayName);
                        portal.NetworkOwnerDisplayName = !string.IsNullOrEmpty(fromClient)
                            ? fromClient
                            : (PortalNetwork.TryResolveByWorldState(pieceCreator) ?? string.Empty);
                    }
                    else
                    {
                        portal.NetworkOwnerDisplayName = PortalNetwork.TryResolveByWorldState(pieceCreator)
                            ?? existing?.NetworkOwnerDisplayName
                            ?? ZdoTools.GetNetworkOwnerDisplayName(portalZdo)
                            ?? string.Empty;
                    }
                }
            }

            if (existing != null && existing.IsPrivate && !requesterMayEditPrivatePortal)
            {
                portal.Name = existing.Name;
                portal.Target = existing.Target;
                portal.IsPrivate = existing.IsPrivate;
                portal.NetworkOwnerPlayerId = existing.NetworkOwnerPlayerId;
                portal.NetworkOwnerDisplayName = existing.NetworkOwnerDisplayName ?? string.Empty;
            }
            else if (existing != null && !existing.IsPrivate && !requesterMayEditPrivatePortal)
            {
                portal.IsPrivate = false;
                if (portal.HasTarget()
                    && KnownPortalsManager.Instance.TryGetValue(portal.Target, out var blockedTarget)
                    && blockedTarget.IsPrivate
                    && blockedTarget.NetworkOwnerPlayerId == requesterPlayerId)
                {
                    portal.Target = existing.Target;
                }
            }

            if (portal.IsPrivate)
            {
                if (pieceCreator == 0L)
                {
                    portal.IsPrivate = false;
                }
                else
                {
                    portal.NetworkOwnerPlayerId = pieceCreator;
                }
            }

            if (portal.HasTarget() && KnownPortalsManager.Instance.TryGetValue(portal.Target, out var destForValidation) && destForValidation.IsPrivate)
            {
                var targetZdo = ZDOMan.instance.GetZDO(destForValidation.Id);
                var destPieceCreator = targetZdo != null ? targetZdo.GetLong(ZDOVars.s_creator) : 0L;
                var mayTargetPrivatePortal = NetPeerUtility.IsPeerPrivilegedForPortalNetwork(sender)
                    || (destPieceCreator != 0L && destPieceCreator == requesterPlayerId);
                if (!mayTargetPrivatePortal)
                {
                    portal.Target = existing != null ? existing.Target : ZDOID.None;
                }
            }

            var updatedPortal = KnownPortalsManager.Instance.AddOrUpdate(portal);

            Log.Info($"Setting portal tag `{updatedPortal.Name}`, network `{updatedPortal.NetworkOwnerPlayerId}` (`{updatedPortal.NetworkOwnerDisplayName}`), private `{updatedPortal.IsPrivate}`, target `{updatedPortal.Target}` on behalf of {sender}");
            ZdoTools.UpdateFromKnownPortal(state: updatedPortal);

            SendToClient.SyncPortal(updatedPortal);

            if (updatedPortal.HasTarget())
            {
                // Set the target of the other portal to this portal, if that portal does not currently have a target
                var targetPortal = KnownPortalsManager.Instance.GetKnownPortalById(updatedPortal.Target);
                if (!targetPortal.HasTarget())
                {
                    Log.Info("Target portal does not have a target itself, setting target portal's target to this portal");
                    targetPortal.Target = updatedPortal.Id;
                    SendToServer.AddOrUpdateRequest(targetPortal);
                }
            }
        }

        /// <summary>
        /// A client wishes for a portal to be removed
        /// </summary>
        /// <param name="sender">The id of the sender</param>
        /// <param name="portalId">The ZDOID of the portal that should be removed</param>
        internal static void RPC_RemoveRequest(long sender, ZDOID portalId)
        {
            if (!Environment.IsServer)
            {
                Log.Error($"{sender} wants `{portalId}` to be removed, {ERR_NOTSERVER}");
                return;
            }

            if (!KnownPortalsManager.Instance.ContainsId(portalId))
            {
                Log.Debug($"{sender} wants `{portalId}` to be removed, but it doesn't exist");
                return;
            }

            Log.Debug($"{sender} wants `{portalId}` to be removed");

            if (KnownPortalsManager.Instance.Remove(portalId))
            {
                Log.Debug($"`{portalId}` removed, checking other portals' targets..");

                var portalsWithInvalidTarget = KnownPortalsManager.Instance.GetPortalsWithTarget(portalId);
                foreach (var portalWithInvalidTarget in portalsWithInvalidTarget)
                {
                    Log.Debug($"Removing target from `{portalWithInvalidTarget.Name}`");

                    portalWithInvalidTarget.Target = ZDOID.None;
                    SendToServer.AddOrUpdateRequest(portalWithInvalidTarget);
                }

                SendToClient.Resync(KnownPortalsManager.Instance.Pack(), "A portal was removed");
            }
        }

        /// <summary>
        /// A client has asked for the server's config settings
        /// </summary>
        /// <param name="sender">The id of the sender</param>
        internal static void RPC_ConfigRequest(long sender)
        {
            if (!Environment.IsServer)
            {
                Log.Error($"{sender} wants to receive the config, {ERR_NOTSERVER}");
                return;
            }

            Log.Debug($"{sender} wants to receive the config");
            var pkg = XPortalConfig.Instance.PackLocalConfig();
            SendToClient.Config(sender, pkg);
        }

        /// <summary>
        /// Client asks whether this connection is a server admin for portal network UI.
        /// </summary>
        internal static void RPC_RequestAdminSync(long sender, ZPackage _)
        {
            if (!Environment.IsServer)
            {
                return;
            }

            bool isAdmin = false;
            var peer = ZNet.instance.GetPeer(sender);
            if (peer != null)
            {
                isAdmin = ZNet.instance.IsAdmin(peer.m_socket.GetHostName());
            }
            else if (ZNet.instance.IsServer() && sender == ZNet.GetUID())
            {
                isAdmin = ZNet.instance.LocalPlayerIsAdminOrHost();
            }

            var outPkg = new ZPackage();
            outPkg.Write(isAdmin);
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, RPCManager.RPC_ADMINSYNC, outPkg);
        }
    }
}

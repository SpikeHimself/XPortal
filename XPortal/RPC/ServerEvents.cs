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

            var updatedPortal = KnownPortalsManager.Instance.AddOrUpdate(portal);

            delayedPortalId = updatedPortal.Id;
            UpdatePortalZdo();

            SendToClient.SyncPortal(updatedPortal);
        }

        private static ZDOID delayedPortalId;
        private static void UpdatePortalZdo(bool delayed = true)
        {
            if (delayed)
            {
                Log.Debug("Queueing ConnectPortal()..");
                QueuedAction.Queue(UpdatePortalZdo, delay: 1);
            }
            else
            {
                var updatedPortal = KnownPortalsManager.Instance.GetKnownPortalById(delayedPortalId);
                var portalZDO = ZDOMan.instance.GetZDO(updatedPortal.Id);

                if (portalZDO == null)
                {
                    Log.Debug("Portal ZDO still not found, queueing again..");
                    QueuedAction.Queue(UpdatePortalZdo, delay: 3);
                    return;
                }

                Log.Info($"Setting portal tag `{updatedPortal.Name}` and target `{updatedPortal.Target}` after a delay");
                portalZDO.Set("tag", updatedPortal.Name);
                portalZDO.SetOwner(ZDOMan.GetSessionID());

                portalZDO.Set(XPortal.Key_TargetId, updatedPortal.Target);
                portalZDO.Set(XPortal.Key_PreviousId, updatedPortal.Id);
                portalZDO.SetConnection(ZDOExtraData.ConnectionType.Portal, updatedPortal.Target);
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
    }
}

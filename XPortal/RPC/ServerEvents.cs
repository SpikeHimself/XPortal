namespace XPortal.RPC.Server
{
    internal static class ServerEvents
    {
        /// <summary>
        /// A client wishes to receive the portal list
        /// </summary>
        /// <param name="sender">The id of the sender</param>
        /// <param name="reason">The reason for the Resync Request</param>
        internal static void RPC_SyncRequest(long sender, string reason)
        {
            Jotunn.Logger.LogInfo($"Received sync request from `{sender}` because: {reason}");
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

            SendToClient.SyncPortal(updatedPortal);
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
                Jotunn.Logger.LogDebug($"[RPC_ConfigRequest] {sender} wants to receive the config, but I am not the server.");
                return;
            }

            Jotunn.Logger.LogDebug($"[RPC_ConfigRequest] {sender} wants to receive the config");
            var pkg = XPortalConfig.Instance.PackLocalConfig();
            SendToClient.Config(sender, pkg);
        }
    }
}

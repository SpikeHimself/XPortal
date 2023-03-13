namespace XPortal.RPC.Client
{
    internal static class ClientEvents
    {
        /// <summary>
        /// The server sent us all of the portals it knows
        /// </summary>
        /// <param name="sender">The server</param>
        /// <param name="pkg">A ZPackage containing a number followed by a list of packaged portals</param>
        /// <param name="reason">The reason that was given for the Resync Request</param>
        internal static void RPC_Resync(long sender, ZPackage pkg, string reason)
        {
            if (Environment.IsServer)
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
        internal static void RPC_SyncPortal(long sender, ZPackage pkg)
        {
            if (Environment.IsServer)
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
        internal static void RPC_Config(long sender, ZPackage pkg)
        {
            Jotunn.Logger.LogInfo($"Received XPortal Config from server");
            XPortalConfig.Instance.ReceiveServerConfig(pkg);
        }
    }
}

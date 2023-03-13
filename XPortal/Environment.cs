namespace XPortal
{
    internal static class Environment
    {
        /// <summary>
        /// The PeerID of the server
        /// </summary>
        internal static long ServerPeerId
        {
            get
            {
                return ZRoutedRpc.instance.GetServerPeerID();
            }
        }
    }
}

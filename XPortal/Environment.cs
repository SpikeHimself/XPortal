using Jotunn.Managers;

namespace XPortal
{
    internal static class Environment
    {
        /// <summary>
        /// Are we the Server?
        /// </summary>
        /// <returns>True if ZNet says we are a server</returns>
        internal static bool IsServer
        {
            get
            {
                return ZNet.instance != null && ZNet.instance.IsServer();
            }
        }

        /// <summary>
        /// Are we Headless? (dedicated server)
        /// </summary>
        /// <returns>True if SystemInfo.graphicsDeviceType is not set</returns>
        internal static bool IsHeadless
        {
            get
            {
                return GUIManager.IsHeadless();
            }
        }

        /// <summary>
        /// Is the Game shutting down? This happens on logout and on quit.
        /// </summary>
        internal static bool ShuttingDown
        {
            get
            {
                return Game.instance.m_shuttingDown;
            }
        }

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

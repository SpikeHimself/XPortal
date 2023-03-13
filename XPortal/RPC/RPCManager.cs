using System;

namespace XPortal.RPC
{
    internal static class RPCManager
    {
        #region RPC Names
        // Server RPCs
        internal const string RPC_SYNCPORTAL = Mod.Info.Name + "_SyncPortal";
        internal const string RPC_RESYNC = Mod.Info.Name + "_Resync";
        internal const string RPC_CONFIG = Mod.Info.Name + "_Config";

        // Client RPCs
        internal const string RPC_SYNCREQUEST = Mod.Info.Name + "_SyncRequest";
        internal const string RPC_ADDORUPDATEREQUEST = Mod.Info.Name + "_AddOrUpdateRequest";
        internal const string RPC_REMOVEREQUEST = Mod.Info.Name + "_RemoveRequest";
        internal const string RPC_CONFIGREQUEST = Mod.Info.Name + "_ConfigRequest";

        // Client to client
        internal const string RPC_CHATMESSAGE = "ChatMessage";
        #endregion

        /// <summary>
        /// Register our RPCs with ZRoutedRpc, so that the game knows which function to call when these messages arrive
        /// </summary>
        public static void Register()
        {
            // Server RPCs
            ZRoutedRpc.instance.Register(RPC_SYNCPORTAL, new Action<long, ZPackage>(Client.ClientEvents.RPC_SyncPortal));
            ZRoutedRpc.instance.Register(RPC_RESYNC, new Action<long, ZPackage, string>(Client.ClientEvents.RPC_Resync));
            ZRoutedRpc.instance.Register(RPC_CONFIG, new Action<long, ZPackage>(Client.ClientEvents.RPC_Config));

            // Client RPCs
            ZRoutedRpc.instance.Register(RPC_SYNCREQUEST, new Action<long, string>(Server.ServerEvents.RPC_SyncRequest));
            ZRoutedRpc.instance.Register(RPC_ADDORUPDATEREQUEST, new Action<long, ZPackage>(Server.ServerEvents.RPC_AddOrUpdateRequest));
            ZRoutedRpc.instance.Register(RPC_REMOVEREQUEST, new Action<long, ZDOID>(Server.ServerEvents.RPC_RemoveRequest));
            ZRoutedRpc.instance.Register(RPC_CONFIGREQUEST, new Action<long>(Server.ServerEvents.RPC_ConfigRequest));
        }
    }
}

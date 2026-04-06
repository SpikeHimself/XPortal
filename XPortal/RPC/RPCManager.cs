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
        internal const string RPC_CUSTOMNETWORKS = Mod.Info.Name + "_CustomNetworks";
        internal const string RPC_ADMINSYNC = Mod.Info.Name + "_AdminSync";

        // Client RPCs
        internal const string RPC_SYNCREQUEST = Mod.Info.Name + "_SyncRequest";
        internal const string RPC_ADDORUPDATEREQUEST = Mod.Info.Name + "_AddOrUpdateRequest";
        internal const string RPC_REMOVEREQUEST = Mod.Info.Name + "_RemoveRequest";
        internal const string RPC_CONFIGREQUEST = Mod.Info.Name + "_ConfigRequest";
        internal const string RPC_REQUESTCUSTOMNETWORKS = Mod.Info.Name + "_RequestCustomNetworks";
        internal const string RPC_REQUESTADMINSYNC = Mod.Info.Name + "_RequestAdminSync";

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
            ZRoutedRpc.instance.Register(RPC_CUSTOMNETWORKS, new Action<long, ZPackage>(Client.ClientEvents.RPC_CustomNetworks));
            ZRoutedRpc.instance.Register(RPC_ADMINSYNC, new Action<long, ZPackage>(Client.ClientEvents.RPC_AdminSync));

            // Client RPCs
            ZRoutedRpc.instance.Register(RPC_SYNCREQUEST, new Action<long, string>(Server.ServerEvents.RPC_SyncRequest));
            ZRoutedRpc.instance.Register(RPC_ADDORUPDATEREQUEST, new Action<long, ZPackage>(Server.ServerEvents.RPC_AddOrUpdateRequest));
            ZRoutedRpc.instance.Register(RPC_REMOVEREQUEST, new Action<long, ZDOID>(Server.ServerEvents.RPC_RemoveRequest));
            ZRoutedRpc.instance.Register(RPC_CONFIGREQUEST, new Action<long>(Server.ServerEvents.RPC_ConfigRequest));
            ZRoutedRpc.instance.Register(RPC_REQUESTCUSTOMNETWORKS, new Action<long>(Server.ServerEvents.RPC_RequestCustomNetworks));
            ZRoutedRpc.instance.Register(RPC_REQUESTADMINSYNC, new Action<long, ZPackage>(Server.ServerEvents.RPC_RequestAdminSync));
        }
    }
}

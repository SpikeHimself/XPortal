using HarmonyLib;

namespace XPortal.Patches
{
    internal static class Patcher
    {
        private static readonly Harmony patcher = new Harmony(Mod.Info.HarmonyGUID);
        public static void Patch()
        {
            patcher.PatchAll(typeof(Dropdown_OnSubmit));
            patcher.PatchAll(typeof(Dropdown_Show));
            patcher.PatchAll(typeof(Dropdown_Hide));
            patcher.PatchAll(typeof(Game_Awake));
            patcher.PatchAll(typeof(Game_Start));
            patcher.PatchAll(typeof(Game_ConnectPortals));
            patcher.PatchAll(typeof(Game_ConnectPortalsCoroutine));
            patcher.PatchAll(typeof(TeleportWorld_GetHoverText));
            patcher.PatchAll(typeof(TextInput_RequestText));
            patcher.PatchAll(typeof(WearNTear_Destroy));
            patcher.PatchAll(typeof(ZDOMan_ConnectPortals));
            patcher.PatchAll(typeof(ZNet_RPC_PeerInfo_Postfix));
            patcher.PatchAll(typeof(WearNTear_OnPlaced));
        }

        public static void Unpatch() => patcher?.UnpatchSelf();
    }
}

using HarmonyLib;

namespace XPortal.Patches
{
    internal static class Patcher
    {
        private static readonly Harmony patcher = new Harmony(Mod.Info.HarmonyGUID);
        public static void Patch() => patcher.PatchAll();
        public static void Unpatch() => patcher?.UnpatchSelf();
    }
}

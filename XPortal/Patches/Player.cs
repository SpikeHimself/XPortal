using HarmonyLib;

namespace XPortal.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    static class Player_PlacePiece
    {
        internal static void Prefix()
        {
            // Do nothing
        }
    }
}
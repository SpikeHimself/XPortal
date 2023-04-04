using HarmonyLib;
using System.Linq;

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
            //patcher.PatchAll(typeof(Piece_SetCreator));
            patcher.PatchAll(typeof(TeleportWorld_GetHoverText));
            patcher.PatchAll(typeof(TextInput_RequestText));
            //patcher.PatchAll(typeof(WearNTear_OnPlaced));
            patcher.PatchAll(typeof(WearNTear_Destroy));

            var playerPlacePiecePatched =
                Harmony.GetAllPatchedMethods().Where(m => m.DeclaringType.Name.Equals("Player") && m.Name.Equals("PlacePiece")).Any();

            if (!playerPlacePiecePatched)
            {
                Log.Debug("Patching WearNTear.OnPlaced, yay!");
                patcher.PatchAll(typeof(WearNTear_OnPlaced));
            }
            else
            {
                Log.Debug("Patching Piece.SetCreator, boo!");
                patcher.PatchAll(typeof(Piece_SetCreator));
            }
        }

        public static void Unpatch() => patcher?.UnpatchSelf();
    }
}

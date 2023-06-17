using HarmonyLib;

namespace XPortal.Patches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Awake))]
    static class Game_Awake
    {
        /// <summary>
        /// Set Game.isModded as per the request in Game.messageForModders
        /// </summary>
        static void Prefix(ref bool ___isModded)
        {
            ___isModded = true;
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    static class Game_Start
    {
        /// <summary>
        /// The game has started!
        /// </summary>
        static void Postfix()
        {
            Environment.GameStarted = true;
            XPortal.GameStarted();
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.ConnectPortals))]
    static class Game_ConnectPortals
    {
        static bool Prefix()
        {
            // Do not run this method.
            return false;
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.ConnectPortalsCoroutine))]
    static class Game_ConnectPortalsCoroutine
    {
        static bool Prefix()
        {
            // Do not run this method.
            return false;
        }
    }
}

using HarmonyLib;

namespace XPortal.Patches
{
    [HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
    static class Piece_SetCreator
    {
        private static WearNTear m_WearNTear;

        static void Postfix(Piece __instance)
        {
            // To those brave enough to dive into this to see how it works: Good luck. This will cost you your sanity.
            //
            // Piece.SetCreator is called by Player.PlacePiece, but before WearNTear.OnPlaced is called.
            // For reasons unknown to mankind (some say "inlining"), a patch on WearNTear.OnPlaced does not work if a patch on Player.PlacePiece was applied first.
            // However, we can't just assume that Piece.SetCreator is only called when a piece is being placed. It might be used for other things too (I honestly don't know).
            //
            // So here's a crazy work-around:
            // With a tiny frame delay, we can check *afterwards* if WearNTear.OnPlaced has run, and "pretend-patch" it that way.
            // If you've read this without what-the-fucking out loud at least once, you too are insane. Welcome to the club.
            m_WearNTear = __instance.GetComponent<WearNTear>();
            CheckWearNTearCreationTime();
        }

        private static void CheckWearNTearCreationTime(bool delayed = true)
        {
            if (delayed)
            {
                // Call myself, but later
                QueuedAction.Queue(CheckWearNTearCreationTime, delay: 1);
                return;
            }

            // The only thing that WearNTear.OnPlaced does, is set m_createTime to -1.
            // So by checking if that's indeed the current value, we can determine that WearNTear.OnPlaced has indeed been called (presumably by Player.PlacePiece).
            if (m_WearNTear.m_createTime == -1f)
            {
                Log.Debug("Portal detection work-around: manually invoking WearNTear.OnPlace postfix");
                WearNTear_OnPlaced.Postfix(m_WearNTear);
                m_WearNTear = null;
            }
        }
    }
}

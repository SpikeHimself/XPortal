using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace XPortal
{
    internal static class Patches
    {

        #region Events
        public static event Action OnGameStart;

        public delegate void OnPrePortalHoverAction(out string result, ZDO portalZDO, ZDOID portalId);
        public static event OnPrePortalHoverAction OnPrePortalHover;

        //public delegate void OnPostPortalInteractAction(ZDOID portalId);
        //public static event OnPostPortalInteractAction OnPostPortalInteract;

        public delegate void OnPortalRequestTextAction(ZDOID portalId);
        public static event OnPortalRequestTextAction OnPortalRequestText;

        public delegate void OnPortalPlacedAction(ZDOID portalId, Vector3 location);
        public static event OnPortalPlacedAction OnPortalPlaced;

        public delegate void OnPortalDestroyedAction(ZDOID portalId);
        public static event OnPortalDestroyedAction OnPortalDestroyed;
        #endregion

        private static readonly Harmony patcher;

        static Patches()
        {
            patcher = new Harmony(XPortal.PluginGUID  + ".harmony");
        }

        public static void Patch()
        {
            patcher.PatchAll();
        }

        public static void Unpatch()
        {
            patcher?.UnpatchSelf();
        }


        [HarmonyPatch(typeof(Game), nameof(Game.Awake))]
        static class GameAwakePatch
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
        static class GameStartPatch
        {
            /// <summary>
            /// The game has started!
            /// </summary>
            static void Postfix()
            {
                OnGameStart();
            }
        }


        [HarmonyPatch(typeof(Game), nameof(Game.ConnectPortals))]
        static class GameConnectPortalsPatch
        {
            static bool Prefix()
            {
                // Do not run this method.
                return false;
            }
        }


        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.GetHoverText))]
        static class TeleportWorldGetHoverTextPatch
        {
            /// <summary>
            /// Replace the game's hover text
            /// </summary>
            static bool Prefix(ZNetView ___m_nview, ref string __result)
            {
                if (___m_nview == null || !___m_nview.IsValid())
                {
                    Jotunn.Logger.LogError("TeleportWorldGetHoverTextPatch: This portal does not exist. Odin strokes his beard in confusion..");
                    __result = "This portal doesn't actually appear to exist. Heimdallr sees you...";
                }
                else
                {
                    ZDO portalZDO = ___m_nview.GetZDO();
                    var portalId = portalZDO.m_uid;
                    OnPrePortalHover(out __result, portalZDO, portalId);
                }

                // Don't run the original method at all
                return false;
            }
        }


        [HarmonyPatch(typeof(TextInput), nameof(TextInput.RequestText))]
        static class TextInputRequestTextPatch
        {
            /// <summary>
            /// When the game is trying to show the original "configure portal" window, deny it.
            /// </summary>
            static bool Prefix(TextReceiver sign)
            {
                // Get the TeleportWorld reference
                if (sign is TeleportWorld teleportWorld)
                {
                    // Request the XPortal UI here instead of the vanilla "set tag" window
                    OnPortalRequestText(teleportWorld.m_nview.GetZDO().m_uid);

                    // Don't run the original method at all
                    return false;
                }

                // Continue as normal
                return true;
            }
        }


        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.OnPlaced))]
        static class WearNTearOnPlacedPatch
        {
            /// <summary>
            /// After placing a piece, check if it's a portal, and if so, raise event OnPortalPlaced
            /// </summary>
            static void Postfix(Piece ___m_piece, ZNetView ___m_nview)
            {
                if (___m_piece.m_name.Equals("$piece_portal") && ___m_piece.CanBeRemoved() && ___m_nview != null)
                {
                    var portalZDO = ___m_nview.GetZDO();
                    if (portalZDO == null)
                    {
                        Jotunn.Logger.LogError("A portal was placed but the ZDO is not available");
                        return;
                    }

                    var portalId = portalZDO.m_uid;
                    var location = portalZDO.GetPosition();
                    OnPortalPlaced(portalId, location);
                }

            }

        }


        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Destroy))]
        static class WearNTearDestroyPatch
        {
            /// <summary>
            /// Before destroying a piece, check if it's a portal, and if so, raise event OnPortalDestroyed
            /// </summary>
            static void Prefix(WearNTear __instance)
            {
                var piece = __instance.m_piece;
                if (piece == null)
                {
                    return;
                }

                var nview = piece.m_nview;
                if (nview == null)
                {
                    return;
                }

                if (piece.m_name.Equals("$piece_portal") && piece.CanBeRemoved())
                {
                    var portalZDO = nview.GetZDO();
                    if (portalZDO == null)
                    {
                        return;
                    }

                    var portalId = portalZDO.m_uid;
                    OnPortalDestroyed(portalId);
                }
            }
        }

    }
}

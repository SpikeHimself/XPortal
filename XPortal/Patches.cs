using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace XPortal
{
    internal static class Patches
    {
        public static event Action OnGameStart;

        public delegate void OnPrePortalHoverAction(out string result, ZDO portalZDO, ZDOID portalId);
        public static event OnPrePortalHoverAction OnPrePortalHover;

        public delegate void OnPostPortalInteractAction(ZDOID portalId);
        public static event OnPostPortalInteractAction OnPostPortalInteract;

        public delegate void OnPortalPlacedAction(ZDOID portalId, Vector3 location);
        public static event OnPortalPlacedAction OnPortalPlaced;

        public delegate void OnPortalDestroyedAction(ZDOID portalId);
        public static event OnPortalDestroyedAction OnPortalDestroyed;


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

            /// <summary>
            /// The Game.ConnectPortals() coroutine updates portal connections every half second.
            /// Since we want to customise how portals are linked, this needs to be disabled completely
            /// </summary>
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool shouldNop = false;
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ldstr && ((System.String)instruction.operand) == "ConnectPortals")
                    {
                        shouldNop = true;
                        yield return new CodeInstruction(OpCodes.Nop);
                    }
                    else if (shouldNop)
                    {
                        yield return new CodeInstruction(OpCodes.Nop);
                        shouldNop = false;

                    }
                    else
                        yield return instruction;
                }
                yield break;
            }
        }


        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Interact))]
        static class TeleportWorldInteractPatch
        {
            static void Postfix(TeleportWorld __instance, ZNetView ___m_nview, bool __result, ref Humanoid human)
            {
                if (!__result)
                {
                    return;
                }

                // TODO: Deal with ward protection properly. The orignal method returns true either way, the only
                // difference is that it does so before TextInput.RequestText is called.
                // Perhaps TextInput.RequestText is a better hook for initiating the XPortal UI..?

                // For now, just duplicate the original code:
                if (!PrivateArea.CheckAccess(__instance.transform.position))
                {
                    human.Message(MessageHud.MessageType.Center, "$piece_noaccess");
                    return;
                }

                OnPostPortalInteract(___m_nview.GetZDO().m_uid);
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


        [HarmonyPatch(typeof(TextInput), nameof(TextInput.Show))]
        static class TextInputShowPatch
        {
            /// <summary>
            /// When the game is trying to show the original "configure portal" window, deny it.
            /// </summary>
            static bool Prefix(string topic)
            {
                if (topic.Equals("$piece_portal_tag"))
                {
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

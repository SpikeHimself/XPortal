using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace XPortal
{
    internal static class HarmonyPatches
    {
        public delegate void OnPostCreateSyncListAction(ref List<ZDO> syncList);
        public delegate void OnPostPortalInteractAction(ZDO portalZDO);
        public delegate void OnPrePortalHoverAction(out string result, ref ZDO portalZDO);

        public static event Action OnGameStart;
        public static event OnPostCreateSyncListAction OnPostCreateSyncList;
        public static event OnPrePortalHoverAction OnPrePortalHover;
        //public static event Action OnPrePortalInteract;
        public static event OnPostPortalInteractAction OnPostPortalInteract;

        private static readonly Harmony patcher;

        static HarmonyPatches()
        {
            patcher = new Harmony("yay.spikehimself.xportal.harmony");
        }

        public static void Patch()
        {
            patcher.PatchAll();
        }

        public static void UnPatch()
        {
            patcher?.UnpatchSelf();
        }


        /// <summary>
        /// Set Game.isModded as per the request in Game.messageForModders
        /// </summary>
        [HarmonyPatch(typeof(Game), nameof(Game.Awake))]
        static class GameAwakePatch
        {
            static void Prefix(ref bool ___isModded)
            {
                ___isModded = true;
            }
        }


        /// <summary>
        /// The Game.ConnectPortals() coroutine updates portal connections every half second.
        /// Since we want to customise how portals are linked, this needs to be disabled completely
        /// </summary>
        [HarmonyPatch(typeof(Game), nameof(Game.Start))]
        static class GameStartPatch
        {
            static void Postfix()
            {
                OnGameStart();
            }

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

        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.CreateSyncList))]
        static class ZDOManCreateSyncListPatch
        {
            static void Postfix(ref List<ZDO> toSync)
            {
                if (ZNet.instance.IsServer())
                {
                    OnPostCreateSyncList(ref toSync);
                }
            }
        }


        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Interact))]
        static class TeleportWorldInteractPatch
        {
            //static void Prefix()
            //{
            //    OnPrePortalInteract();
            //}

            static void Postfix(ref ZNetView ___m_nview, ref bool __result)
            {
                if (!__result)
                {
                    return;
                }

                OnPostPortalInteract(___m_nview.GetZDO());
            }
        }


        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.GetHoverText))]
        static class TeleportWorldGetHoverTextPatch
        {
            static bool Prefix(ref ZNetView ___m_nview, ref string __result)
            {
                if (___m_nview == null || !___m_nview.IsValid())
                {
                    Jotunn.Logger.LogError("TeleportWorldGetHoverTextPatch: This portal does not exist. Odin strokes his beard in confusion..");
                    __result = "This portal doesn't actually appear to exist. Heimdallr sees you.";
                    return false;
                }

                ZDO portalZDO = ___m_nview.GetZDO();

                OnPrePortalHover(out __result, ref portalZDO);
                return false;
            }
        }


        /// <summary>
        /// When the game is trying to show the original "configure portal" window, deny it.
        /// </summary>
        [HarmonyPatch(typeof(TextInput), nameof(TextInput.Show))]
        static class TextInputShowPatch
        {
            static bool Prefix(ref string topic)
            {
                if (topic.Equals("$piece_portal_tag"))
                {
                    return false;
                }
                return true;
            }
        }


        /// <summary>
        /// InventoryGui checks whether any UI elements are currently visible.
        /// When XPortalUI is showing, we must let it know.
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.IsVisible))]
        static class InventoryGuiIsVisiblePatch
        {
            static void Postfix(ref bool __result)
            {
                if (!GUIManager.IsHeadless() && XPortalUI.Instance != null && XPortalUI.Instance.IsActive())
                {
                    __result = true;
                }
            }
        }

    }
}

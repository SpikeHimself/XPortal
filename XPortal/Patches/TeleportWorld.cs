using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace XPortal.Patches
{
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.GetHoverText))]
    static class TeleportWorld_GetHoverText
    {
        /// <summary>
        /// Replace the game's hover text
        /// </summary>
        static bool Prefix(ZNetView ___m_nview, ref string __result)
        {
            if (Environment.ShuttingDown)
            {
                Log.Debug("Shutting down, ignoring hover");
                __result = string.Empty;

                // Don't run the original method
                return false;
            }

            if (!___m_nview || ___m_nview.GetZDO() == null)
            {
                Log.Error("TeleportWorldGetHoverTextPatch: This portal does not exist. Odin strokes his beard in confusion..");
                __result = "This portal doesn't actually appear to exist. Heimdallr sees you...";

                // Don't run the original method
                return false;
            }

            var portalZDO = ___m_nview.GetZDO();
            var portalId = portalZDO.m_uid;
            var location = portalZDO.GetPosition();
            XPortal.OnPrePortalHover(out __result, portalId, location);

            // Don't run the original method
            return false;
        }
    }

    /// <summary>
    /// Blocks using a portal when its connection target is another player's private portal (client-side).
    /// Vanilla <see cref="TeleportWorld.Teleport"/> then checks <see cref="Humanoid.IsTeleportable"/> for inventory
    /// (ores, etc.); that call has no portal context, so destination privacy is handled here instead.
    /// </summary>
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
    static class TeleportWorld_Teleport_PrivateDestination
    {
        static bool Prefix(Player player, ZNetView ___m_nview)
        {
            if (Environment.ShuttingDown)
            {
                return true;
            }

            if (player == null || Player.m_localPlayer == null || player != Player.m_localPlayer)
            {
                return true;
            }

            if (___m_nview == null || !___m_nview.IsValid())
            {
                return true;
            }

            var portalZdo = ___m_nview.GetZDO();
            if (portalZdo == null)
            {
                return true;
            }

            if (!XPortal.LocalPrivateUseBlocked(portalZdo.m_uid))
            {
                return true;
            }

            // Skip original Teleport (no extra message — see design notes).
            return false;
        }
    }

    /// <summary>
    /// Injects into flag2 after vanilla computes closestPlayer.IsTeleportable() || m_allowAllItems, so
    /// private-destination gating runs in the same pass as inventory gating (no duplicate proximity / target work).
    /// Using a transpiler here to reduce the amount of logic per frame used.
    /// </summary>
    [HarmonyPatch(typeof(TeleportWorld), "UpdatePortal")]
    static class TeleportWorld_UpdatePortal_Transpiler
    {

        [UsedImplicitly]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instrs = instructions.ToList();
            var counter = 0;

            CodeInstruction LogMessage(CodeInstruction instruction)
            {
                Log.Debug($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
                return instruction;
            }
            
            // The function we want to run
            // Passing flag2 into the function so that we can escape out of it, if the flag is already false.
            // flag2 = XPortal.PrivateUseBlockedForPortal(this, closestPlayer, flag2)
            
            var mTargetFound = AccessTools.DeclaredField(typeof(TeleportWorld), nameof(TeleportWorld.m_target_found));

            for (int i = 0; i < instrs.Count; ++i)
            {
                // In IL code, we are looking for the store loc.2 opcode that is right before the m_target_found.SetActive() virtual call.
                // So we are matching on the 3 opcodes of stloc_2, ldarg_0, and lfld (where the field = mTargetFound)
                // If matched, we are loading the variables needed and then calling Xportal.IsUseablePortal() and setting that output to flag2/loc.2.
                if (i > 5 && instrs[i].opcode == OpCodes.Stloc_2 && instrs[i+1].opcode == OpCodes.Ldarg_0 && instrs[i+2].opcode == OpCodes.Ldfld && instrs[i+2].operand.Equals(mTargetFound))
                {
                    // Return original stloc.2 - flag2
                    yield return LogMessage(instrs[i]);
                    
                    // Load Ldarg_0 (this)
                    yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
                    
                    // load loc.0 (closest player)
                    yield return LogMessage(new CodeInstruction(OpCodes.Ldloc_0));
                    
                    // Load loc.2 (flag2)
                    yield return LogMessage(new CodeInstruction(OpCodes.Ldloc_2));
                    
                    // Call Method
                    yield return LogMessage(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(XPortal), nameof(XPortal.IsUsablePortal))));
                    
                    // Set loc.2
                    yield return LogMessage(new CodeInstruction(OpCodes.Stloc_2));
                }
                else
                {
                    yield return LogMessage(instrs[i]);
                    counter++;
                }
            }
        }
    }
}

using HarmonyLib;

namespace XPortal.Patches
{
    [HarmonyPatch(typeof(UIInputHint), nameof(UIInputHint.UpdateInputHints))]
    internal static class UIInputHint_UpdateInputHints
    {
        //static void Postfix(UIInputHint __instance)
        //{
        //    if (__instance.m_group.name.Contains(Mod.Info.Name))
        //    {
        //        bool flag =
        //            (
        //                (
        //                    __instance.m_button == null || __instance.m_button.IsInteractable()
        //                )
        //                &&
        //                (
        //                    __instance.m_group == null || __instance.m_group.IsActive
        //                )
        //            )
        //            ||
        //            (
        //                __instance.alternativeGroupHandler != null && __instance.alternativeGroupHandler.IsActive
        //            );
        //    }
        //}
    }
}

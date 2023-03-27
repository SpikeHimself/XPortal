using HarmonyLib;
using UnityEngine.UI;

namespace XPortal.Patches
{
    [HarmonyPatch(typeof(Dropdown), nameof(Dropdown.OnSubmit))]
    static class Dropdown_OnSubmit
    {
        static bool m_DropdownExpanded;
        static bool Prefix(Dropdown __instance)
        {
            if (__instance.name.Equals(XPortalUI.GO_DESTINATIONDROPDOWN))
            {
                if (m_DropdownExpanded)
                {
                    __instance.Hide();
                }
                else
                {
                    __instance.Show();
                }
                m_DropdownExpanded = !m_DropdownExpanded;
                return false;
            }

            return true;
        }
    }
}

using HarmonyLib;
using UnityEngine.UI;
using XPortal.UI;

namespace XPortal.Patches
{
    [HarmonyPatch(typeof(Dropdown), nameof(Dropdown.OnSubmit))]
    static class Dropdown_OnSubmit
    {
        public static bool m_DropdownExpanded;
        static bool Prefix(Dropdown __instance)
        {
            if (__instance.name.Equals(PortalConfigurationPanel.GO_DESTINATIONDROPDOWN))
            {
                if (m_DropdownExpanded)
                {
                    __instance.Hide();
                }
                else
                {
                    __instance.Show();
                }
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Dropdown), nameof(Dropdown.Show))]
    static class Dropdown_Show
    {
        static void Postfix(Dropdown __instance)
        {
            if (__instance.name.Equals(PortalConfigurationPanel.GO_DESTINATIONDROPDOWN))
            {
                Dropdown_OnSubmit.m_DropdownExpanded = true;
            }
        }
    }

    [HarmonyPatch(typeof(Dropdown), nameof(Dropdown.Hide))]
    static class Dropdown_Hide
    {
        static void Postfix(Dropdown __instance)
        {
            if (__instance.name.Equals(PortalConfigurationPanel.GO_DESTINATIONDROPDOWN))
            {
                Dropdown_OnSubmit.m_DropdownExpanded = false;
            }
        }
    }
}

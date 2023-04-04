using HarmonyLib;
using UnityEngine.UI;
using XPortal.UI;

namespace XPortal.Patches
{
    [HarmonyPatch(typeof(Dropdown), nameof(Dropdown.OnSubmit))]
    static class Dropdown_OnSubmit
    {
        static bool Prefix(Dropdown __instance)
        {
            if (PortalConfigurationPanel.Instance != null && __instance.name.Equals(PortalConfigurationPanel.GO_DESTINATIONDROPDOWN))
            {
                if (PortalConfigurationPanel.Instance.DropdownExpanded)
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
                PortalConfigurationPanel.Instance.DropdownExpanded = true;
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
                PortalConfigurationPanel.Instance.DropdownExpanded = false;
            }
        }
    }
}

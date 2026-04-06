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
                PortalConfigurationPanel.ClearListScroll();
                PortalConfigurationPanel.Instance.DropdownExpanded = true;
                PortalConfigurationPanel.ApplyListScroll(__instance);
                PortalConfigurationPanel.QueueListScroll(__instance);
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
                PortalConfigurationPanel.ClearListScroll();
            }
        }
    }

    [HarmonyPatch(typeof(Dropdown), "set_value")]
    static class Dropdown_SetValue
    {
        static void Postfix(Dropdown __instance)
        {
            if (!__instance.name.Equals(PortalConfigurationPanel.GO_DESTINATIONDROPDOWN))
            {
                return;
            }

            if (PortalConfigurationPanel.Instance == null || !PortalConfigurationPanel.Instance.DropdownExpanded)
            {
                return;
            }

            PortalConfigurationPanel.ApplyListScroll(__instance);
        }
    }
}

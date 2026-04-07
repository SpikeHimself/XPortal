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
            if (PortalConfigurationPanel.Instance == null || !PortalConfigurationPanel.IsManagedDropdown(__instance))
            {
                return true;
            }

            PortalConfigurationPanel panel = PortalConfigurationPanel.Instance;

            if (panel.ExpandedDropdown == __instance)
            {
                __instance.Hide();
                return false;
            }

            if (panel.ExpandedDropdown != null && panel.ExpandedDropdown != __instance)
            {
                panel.ExpandedDropdown.Hide();
            }

            __instance.Show();
            return false;
        }
    }

    [HarmonyPatch(typeof(Dropdown), nameof(Dropdown.Show))]
    static class Dropdown_Show
    {
        static void Postfix(Dropdown __instance)
        {
            if (!PortalConfigurationPanel.IsManagedDropdownName(__instance.name))
            {
                return;
            }

            PortalConfigurationPanel.ClearListScroll();
            PortalConfigurationPanel.Instance.SetExpandedDropdown(__instance);
            PortalConfigurationPanel.ApplyListScroll(__instance);
            PortalConfigurationPanel.QueueListScroll(__instance);
        }
    }

    [HarmonyPatch(typeof(Dropdown), nameof(Dropdown.Hide))]
    static class Dropdown_Hide
    {
        static void Postfix(Dropdown __instance)
        {
            if (!PortalConfigurationPanel.IsManagedDropdownName(__instance.name))
            {
                return;
            }

            PortalConfigurationPanel.Instance.ClearExpandedDropdownIf(__instance);
            PortalConfigurationPanel.ClearListScroll();
        }
    }

    [HarmonyPatch(typeof(Dropdown), "set_value")]
    static class Dropdown_SetValue
    {
        static void Postfix(Dropdown __instance)
        {
            if (!PortalConfigurationPanel.IsManagedDropdownName(__instance.name))
            {
                return;
            }

            PortalConfigurationPanel panel = PortalConfigurationPanel.Instance;
            if (panel == null || panel.ExpandedDropdown != __instance)
            {
                return;
            }

            PortalConfigurationPanel.ApplyListScroll(__instance);
        }
    }
}

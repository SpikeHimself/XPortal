using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace XPortal.Patches
{
    [HarmonyPatch(typeof(Dropdown), nameof(Dropdown.OnSubmit))]
    static class Dropdown_OnSubmit
    {
        static bool m_DropdownExpanded;
        static bool Prefix(Dropdown __instance)
        {
            if (__instance.name.Equals(Mod.Info.Name + "_PortalDestinationDropdown"))
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

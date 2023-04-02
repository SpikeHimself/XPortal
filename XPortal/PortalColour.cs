using UnityEngine;

namespace XPortal
{
    internal static class PortalColour
    {
        private const string DefaultColour = "#FF6400";
        private const string DefaultStoneColour = "#33C7FF";

        public static string GetPortalColour(ZDOID portalId)
        {
            if (portalId == ZDOID.None)
            {
                return DefaultColour;
            }

            var zdo = ZDOMan.instance.GetZDO(portalId);
            var prefab = ZNetScene.instance.GetPrefab(zdo.m_prefab);
            if (!prefab)
            {
                Log.Debug($"Could not find prefab `{zdo.m_prefab}`");
                return DefaultColour;
            }

            if (string.Equals(prefab.name, XPortal.StonePortalPrefabName, System.StringComparison.OrdinalIgnoreCase))
            {
                return DefaultStoneColour;
            }

            var pointLight = prefab.transform.Find("_target_found_red/Point light");
            if (!pointLight)
            {
                Log.Debug($"Portal prefab `{prefab.name}` does not have a Point light");
                return DefaultColour;
            }

            var light = pointLight.GetComponent<Light>();
            if (!light)
            {
                Log.Debug($"Portal prefab `{prefab.name}` does not have a Light component");
                return DefaultColour;
            }

            var colour = light.color;
            return "#" + ColorUtility.ToHtmlStringRGB(colour);
        }
    }
}

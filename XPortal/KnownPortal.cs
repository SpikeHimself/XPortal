using UnityEngine;

namespace XPortal
{
    public class KnownPortal
    {
        public ZDOID Id { get; set; }
        public string Name { get; set; }
        public ZDOID Target { get; set; }
        public Vector3 Location { get; set; }
        public string Colour { get; set; }

        public KnownPortal(ZDOID id)
        {
            Id = id;
            Name = string.Empty;
            Location = Vector3.zero;
            Target = ZDOID.None;
            Colour = GetPortalColour(id);
        }

        public KnownPortal(ZDOID id, Vector3 location) : this(id)
        {
            Location = location;
        }

        public KnownPortal(ZPackage pkg)
        {
            Id = pkg.ReadZDOID();
            Name = pkg.ReadString();
            Location = pkg.ReadVector3();
            Target = pkg.ReadZDOID();
            Colour = pkg.ReadString();
            //Colour = GetPortalColour(Id);
        }

        public string GetFriendlyName()
        {
            var portalName = Name;
            if (string.IsNullOrEmpty(portalName))
            {
                return Localization.instance.Localize("$piece_portal_tag_none");  // "(No Name)"
            }
            else
            {
                return portalName;
            }
        }

        public string GetFriendlyTargetName()
        {
            if (!HasTarget())
            {
                return Localization.instance.Localize("$piece_portal_target_none");   // "(None)"
            }

            if (!KnownPortalsManager.Instance.ContainsId(Target))
            {
                return $"{Target} (invalid)";
            }

            return KnownPortalsManager.Instance.GetKnownPortalById(Target).GetFriendlyName();
        }

        public bool HasTarget()
        {
            return Target != null && Target != ZDOID.None && !Target.IsNone();
        }

        public ZPackage Pack()
        {
            var pkg = new ZPackage();
            pkg.Write(Id);
            pkg.Write(Name);
            pkg.Write(Location);
            pkg.Write(Target);
            pkg.Write(Colour);
            return pkg;
        }

        public bool Targets(ZDOID target)
        {
            return Target == target;
        }

        public override string ToString()
        {
            return $"{{ Id: `{Id}`, Name; `{GetFriendlyName()}`, Location: `{Location}`, Target: `{GetFriendlyTargetName()}`, Colour: `{Colour}` }}";
        }

        #region Colour
        private const string DefaultColour = "#FF6400";
        private static string GetPortalColour(ZDOID portalId)
        {
            if (portalId == ZDOID.None) // || !XPortalConfig.Instance.Local.DisplayPortalColour)
            {
                return DefaultColour;
            }

            var zdo = ZDOMan.instance.GetZDO(portalId);
            var prefab = ZNetScene.instance.GetPrefab(zdo.m_prefab);
            if (!prefab)
            {
                Jotunn.Logger.LogDebug($"Could not find prefab `{zdo.m_prefab}`");
                return DefaultColour;
            }

            var pointLight = prefab.transform.Find("_target_found_red/Point light");
            if (!pointLight)
            {
                Jotunn.Logger.LogDebug($"Portal prefab `{prefab.name}` does not have a Point light");
                return DefaultColour;
            }

            var light = pointLight.GetComponent<Light>();
            if (!light)
            {
                Jotunn.Logger.LogDebug($"Portal prefab `{prefab.name}` does not have a Light component");
                return DefaultColour;
            }

            var colour = light.color;
            return "#" + ColorUtility.ToHtmlStringRGB(colour);
        }
        #endregion
    }
}

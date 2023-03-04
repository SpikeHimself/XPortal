using UnityEngine;

namespace XPortal
{
    public class KnownPortal
    {

        public static readonly string DefaultColour = GetColour(Game.instance.m_portalPrefab);

        public static string GetPortalColour(ZDOID portalId)
        {
            if (portalId == ZDOID.None)
            {
                return DefaultColour;
            }

            var zdo = ZDOMan.instance.GetZDO(portalId);
            var prefab = ZNetScene.instance.GetPrefab(zdo.m_prefab);
            var colour = GetColour(prefab);
            Jotunn.Logger.LogDebug($"Portal {portalId} with prefab {prefab.name} has colour {colour}");
            return colour;
        }

        private static string GetColour(GameObject prefab)
        {
            var colour = prefab.transform.Find("_target_found_red/Point light").GetComponent<Light>().color;
            return "#" + ColorUtility.ToHtmlStringRGB(colour);
        }

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
    }
}

using UnityEngine;

namespace XPortal
{
    public class KnownPortal
    {
        public ZDOID Id { get; set; }
        public string Name { get; set; }
        public ZDOID PreviousId { get; set; }
        public ZDOID Target { get; set; }
        public Vector3 Location { get; set; }
        public string Colour { get; set; }

        public KnownPortal(ZDOID id)
        {
            Id = id;
            Name = string.Empty;
            Location = Vector3.zero;
            PreviousId = ZDOID.None;
            Target = ZDOID.None;
            Colour = PortalColour.GetPortalColour(id);
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
            PreviousId = pkg.ReadZDOID();
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
            pkg.Write(PreviousId);
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

using UnityEngine;

namespace XPortal
{
    public class KnownPortal
    {
        public ZDOID Id { get; set; }
        public string Name { get; set; }
        public ZDOID Target { get; set; }
        public Vector3 Location { get; set; }

        public KnownPortal(ZDOID id, string name, Vector3 location, ZDOID target)
        {
            Id = id;
            Name = name;
            Location = location;
            Target = target == null ? ZDOID.None : target;
        }

        public KnownPortal() : this(
            id: ZDOID.None,
            name: string.Empty,
            location: new Vector3(),
            target: ZDOID.None)
        { }

        public bool HasTarget()
        {
            return Target != null && Target != ZDOID.None && !Target.IsNone();
        }

        public bool Targets(ZDOID target)
        {
            return Target == target;
        }

        public ZPackage Pack()
        {
            var pkg = new ZPackage();
            pkg.Write(Id);
            pkg.Write(Name);
            pkg.Write(Location);
            pkg.Write(Target);
            return pkg;
        }

        public static KnownPortal Unpack(ZPackage pkg)
        {
            var x = new KnownPortal()
            {
                Id = pkg.ReadZDOID(),
                Name = pkg.ReadString(),
                Location = pkg.ReadVector3(),
                Target = pkg.ReadZDOID()
            };
            return x;
        }

        public override string ToString()
        {
            var portalName = Name;
            if (string.IsNullOrEmpty(portalName))
            {
                portalName = Localization.instance.Localize("$piece_portal_tag_none");  // "(No Name)"
            }
            else
            {
                portalName = $"`{portalName}`";
            }

            return $"{{ ID: `{Id}`, Name; {portalName}, Location: `{Location}`, {TargetToString()} }}";
        }

        private string TargetToString()
        {
            if (!HasTarget())
            {
                var targetNone = Localization.instance.Localize("$piece_portal_target_none");   // "(None)"
                return $"Target: {targetNone}";
            }

            var targetExists = KnownPortalsManager.Instance.ContainsId(Target);
            if (!targetExists)
            {
                return $"Target (invalid): `{Target}`";
            }

            var targetName = KnownPortalsManager.Instance.GetPortalName(Target);
            if (string.IsNullOrEmpty(targetName))
            {
                targetName = Localization.instance.Localize("$piece_portal_tag_none");  // "(No Name)"
            }
            return $"Target: `{targetName}`";
        }

    }
}

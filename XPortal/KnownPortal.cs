﻿using UnityEngine;

namespace XPortal
{
    public class KnownPortal
    {
        public ZDOID Id { get; set; }
        public string Name { get; set; }
        public ZDOID Target { get; set; }
        public Vector3 Location { get; set; }

        private KnownPortal() { }

        public KnownPortal(ZDOID id, string portalName, Vector3 location, ZDOID targetId)
        {
            Id = id;
            Name = portalName;
            Location = location;
            Target = targetId == null ? ZDOID.None : targetId;
        }

        public bool HasTarget()
        {
            return Target != ZDOID.None && !Target.IsNone();
        }

        public bool Targets(ZDOID targetId)
        {
            return Target == targetId;
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
            return $"{{ ID: `{Id}`, Name; `{Name}`, Location: {Location}, {TargetToString()} }}";
        }

        private string TargetToString()
        {
            if (!HasTarget())
            {
                return "No Target";
            }

            var targetExists = KnownPortalsManager.Instance.ContainsId(Target);
            if (!targetExists)
            {
                return "Invalid Target";
            }

            var targetPortal = KnownPortalsManager.Instance.GetKnownPortalById(Target);
            return $"Target: `{targetPortal.Name}`";
        }

    }
}

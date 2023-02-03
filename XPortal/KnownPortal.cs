using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace XPortal
{
    public class KnownPortal
    {
        public ZDOID Id { get; set; }
        public string Name { get; set; }
        public ZDOID Target { get; set; }
        public Vector3 Location { get; set; }

        private KnownPortal() { }

        public KnownPortal(ZDOID portalZDOID, string portalName, Vector3 location, ZDOID targetZDOID)
        {
            Id = portalZDOID;
            Name = portalName;
            Location = location;
            Target = targetZDOID == null ? ZDOID.None : targetZDOID;
        }

        public bool HasTarget()
        {
            return Target != ZDOID.None && !Target.IsNone();
        }

        public bool Targets(ZDOID targetZDOID)
        {
            return Target == targetZDOID;
        }

        //public bool IsZDOValid()
        //{
        //    var ZDO = Util.TryGetZDO(ZDOID);
        //    return ZDO != null && ZDO.IsValid();
        //}

        //public bool IsTargetZDOValid()
        //{
        //    var ZDO = Util.TryGetZDO(Target);
        //    return ZDO != null && ZDO.IsValid();
        //}

        //private static string GetTag(ZDOID portalZDOID)
        //{
        //    if (portalZDOID == ZDOID.None)
        //    {
        //        return string.Empty;
        //    }

        //    var portalZDO = Util.TryGetZDO(portalZDOID);
        //    return portalZDO.GetString("tag");
        //}

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
            if (HasTarget())
            {
                return $"{{ ID: `{Id}`, Name; `{Name}`, Target: `{Target}` }}";
            }
            return $"{{ ID: `{Id}`, Name; `{Name}`, No Target }}";
        }

    }
}

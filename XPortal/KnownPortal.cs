namespace XPortal
{
    public class KnownPortal
    {
        public ZDOID ZDOID { get; }
        public string Name { get; set; }
        public ZDOID Target { get; set; }

        public KnownPortal(ZDOID portalZDOID, string portalName, ZDOID targetZDOID)
        {
            ZDOID = portalZDOID;
            Name = portalName;
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

        public bool IsZDOValid()
        {
            var ZDO = Util.TryGetZDO(ZDOID);
            return ZDO != null && ZDO.IsValid();
        }

        public bool IsTargetZDOValid()
        {
            var ZDO = Util.TryGetZDO(Target);
            return ZDO != null && ZDO.IsValid();
        }

        private static string GetTag(ZDOID portalZDOID)
        {
            if (portalZDOID == ZDOID.None)
            {
                return string.Empty;
            }

            var portalZDO = Util.TryGetZDO(portalZDOID);
            return portalZDO.GetString("tag");
        }

        public override string ToString()
        {
            if (HasTarget() && IsTargetZDOValid())
            {
                return $"{{ ID: `{ZDOID}`, Name; `{Name}`, Target: `{GetTag(Target)}` }}";
            }
            return $"{{ ID: `{ZDOID}`, Name; `{Name}`, No Target }}";
        }

    }
}

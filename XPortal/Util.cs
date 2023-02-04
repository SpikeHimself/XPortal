namespace XPortal
{
    internal static class Util
    {
        public static ZDO TryGetZDO(ZDOID portalZDOID)
        {
            return ZDOMan.instance.GetZDO(portalZDOID);
        }

        public static Vector3 GetPosition(ZDOID portalZDOID)
        {
            return TryGetZDO(portalZDOID).GetPosition();
        }
    }
}

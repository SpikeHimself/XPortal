namespace XPortal
{
    internal static class Util
    {
        public static ZDO TryGetZDO(ZDOID portalZDOID)
        {
            return ZDOMan.instance.GetZDO(portalZDOID);
        }

    }
}

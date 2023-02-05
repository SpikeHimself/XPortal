namespace XPortal
{
    internal static class Util
    {
        public static ZDO TryGetZDO(ZDOID portalId)
        {
            return ZDOMan.instance.GetZDO(portalId);
        }

    }
}

namespace XPortal
{
    internal static class ZdoTools
    {
        public static string GetName(ZDO portalZdo)
        {
            return portalZdo.GetString("tag");
        }

        public static void SetName (ZDO portalZdo, string name)
        {
            portalZdo.Set("tag", name);
        }

        public static void SetOwner(ZDO portalZdo)
        {
            portalZdo.SetOwner(ZDOMan.GetSessionID());
        }

        public static void SetPreviousId(ZDO portalZdo)
        {
            portalZdo.Set(XPortal.Key_PreviousId, portalZdo.m_uid);
        }

        public static void SetTarget(ZDO portalZdo, ZDOID targetId)
        {
            portalZdo.Set(XPortal.Key_TargetId, targetId);
            portalZdo.SetConnection(ZDOExtraData.ConnectionType.Portal, targetId);
        }

        public static long GetNetworkOwnerPlayerId(ZDO portalZdo)
        {
            return portalZdo.GetLong(XPortal.Key_NetworkOwnerPlayerId);
        }

        public static void SetNetworkOwnerPlayerId(ZDO portalZdo, long ownerPlayerId)
        {
            portalZdo.Set(XPortal.Key_NetworkOwnerPlayerId, ownerPlayerId);
        }

        public static string GetNetworkOwnerDisplayName(ZDO portalZdo)
        {
            return portalZdo.GetString(XPortal.Key_NetworkOwnerDisplayName);
        }

        public static void SetNetworkOwnerDisplayName(ZDO portalZdo, string displayName)
        {
            portalZdo.Set(XPortal.Key_NetworkOwnerDisplayName, displayName ?? string.Empty);
        }

        public static bool GetIsPrivate(ZDO portalZdo)
        {
            return portalZdo.GetBool(XPortal.Key_IsPrivate, false);
        }

        public static void SetIsPrivate(ZDO portalZdo, bool isPrivate)
        {
            portalZdo.Set(XPortal.Key_IsPrivate, isPrivate);
        }

        public static void UpdateFromKnownPortal(bool delayed = false, object state = null)
        {
            if (delayed)
            {
                QueuedAction.Queue(UpdateFromKnownPortal, delay: 1);
                return;
            }

            var portal = (KnownPortal)state;
            var portalZdo = ZDOMan.instance.GetZDO(portal.Id);

            if (portalZdo == null)
            {
                Log.Debug("Portal ZDO not found, trying again with delay..");
                QueuedAction.Queue(UpdateFromKnownPortal, delay: 3, state: portal);
                return;
            }

            SetOwner(portalZdo);
            SetName(portalZdo, portal.Name);
            SetPreviousId(portalZdo);
            SetNetworkOwnerPlayerId(portalZdo, portal.NetworkOwnerPlayerId);
            SetNetworkOwnerDisplayName(portalZdo, portal.NetworkOwnerDisplayName ?? string.Empty);
            SetIsPrivate(portalZdo, portal.IsPrivate);
            SetTarget(portalZdo, portal.Target);
        }
    }
}

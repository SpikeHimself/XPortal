using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static UnityEngine.GraphicsBuffer;

namespace XPortal.Patches
{
    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ConnectPortals))]
    static class ZDOMan_ConnectPortals
    {

        static ZDOID FindNewId(List<ZDOID> allPortals, ZDOID thisId, ZDOID targetId)
        {
            foreach (var newId in allPortals.Where(p => p != thisId))
            {
                ZDO newZdo = ZDOMan.instance.GetZDO(newId);
                if (newZdo != null)
                {
                    var previousId = newZdo.GetZDOID(XPortal.Key_PreviousId);
                    if (targetId == previousId)
                    {
                        Log.Debug($"  New ZDOID for old Target `{targetId}` is `{newId}`");
                        return newId;
                    }
                }
            }

            return targetId;
        }

        static void Postfix()
        {
            List<ZDOID> connectionIds1 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Portal );
            List<ZDOID> connectionIds2 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Portal | ZDOExtraData.ConnectionType.Target);
            
            List<ZDOID> allPortalIds = new List<ZDOID>();
            allPortalIds.AddRange(connectionIds1);
            allPortalIds.AddRange(connectionIds2);

            Log.Debug($"Found {allPortalIds.Count} portals");

            foreach (ZDOID portalId in allPortalIds)
            {
                ZDO portalZdo = ZDOMan.instance.GetZDO(portalId);
                if (portalZdo != null)
                {
                    Log.Debug($"Checking connections for `{portalId}`");

                    var targetId = portalZdo.GetZDOID(XPortal.Key_TargetId);
                    var targetZdo = ZDOMan.instance.GetZDO(targetId);

                    if (targetZdo == null)
                    {
                        Log.Debug($" Target `{targetId}` does not exist, finding new ZDOID..");
                        targetId = FindNewId(allPortalIds, portalId, targetId);
                    }

                    Log.Debug($"Connecting `{portalId}` to `{targetId}`");

                    portalZdo.SetOwner(ZDOMan.GetSessionID());
                    portalZdo.SetConnection(ZDOExtraData.ConnectionType.Portal, targetId);
                    portalZdo.Set(XPortal.Key_TargetId, targetId);
                    continue;
                }
            }

            // Finish by setting the previousid so that the next session can use that to find the new zdoids
            foreach (ZDOID portalId in allPortalIds)
            {
                Log.Debug($"Updating PreviousId for `{portalId}`");
                ZDO portalZdo = ZDOMan.instance.GetZDO(portalId);
                portalZdo?.Set(XPortal.Key_PreviousId, portalId);
            }
        }

    }
}

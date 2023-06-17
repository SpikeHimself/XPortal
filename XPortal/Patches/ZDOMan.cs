using HarmonyLib;
using System.Collections.Generic;

namespace XPortal.Patches
{
    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ConnectPortals))]
    static class ZDOMan_ConnectPortals
    {
        static bool Prefix()
        {
            List<ZDOID> connectionZdoiDs1 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Portal);
            List<ZDOID> connectionZdoiDs2 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Portal | ZDOExtraData.ConnectionType.Target);
            int num = 0;
            foreach (ZDOID zdoid1 in connectionZdoiDs1)
            {
                ZDO zdo1 = ZDOMan.instance.GetZDO(zdoid1);
                if (zdo1 != null)
                {
                    ZDOConnectionHashData connectionHashData1 = zdo1.GetConnectionHashData(ZDOExtraData.ConnectionType.Portal);
                    if (connectionHashData1 != null)
                    {
                        foreach (ZDOID zdoid2 in connectionZdoiDs2)
                        {
                            if (!(zdoid2 == zdoid1) && ZDOExtraData.GetConnectionType(zdoid2) == ZDOExtraData.ConnectionType.None)
                            {
                                ZDO zdo2 = ZDOMan.instance.GetZDO(zdoid2);
                                if (zdo2 != null)
                                {
                                    ZDOConnectionHashData connectionHashData2 = ZDOExtraData.GetConnectionHashData(zdoid2, ZDOExtraData.ConnectionType.Portal | ZDOExtraData.ConnectionType.Target);
                                    if (connectionHashData2 != null && connectionHashData1.m_hash == connectionHashData2.m_hash)
                                    {
                                        ++num;
                                        zdo1.SetOwner(ZDOMan.GetSessionID());
                                        //zdo2.SetOwner(ZDOMan.GetSessionID());
                                        zdo1.SetConnection(ZDOExtraData.ConnectionType.Portal, zdoid2);
                                        //zdo2.SetConnection(ZDOExtraData.ConnectionType.Portal, zdoid1);


                                        // TODO: Remove this and fix target detection in KnownPortalsManager
                                        zdo1.Set("target", zdoid2); // backwards compatibility (pre 0.216.9)
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            UnityEngine.Debug.Log((object)("ConnectPortals => Connected " + num.ToString() + " portals."));

            return false;
        }
    }
}

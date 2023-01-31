using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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

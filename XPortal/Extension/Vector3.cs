using UnityEngine;

namespace XPortal.Extension
{
    internal static class Vector3Extensions
    {
        /// <summary>
        /// Cast this vector's values to integers
        /// </summary>
        public static Vector3 Round(this Vector3 v)
        {
            return new Vector3((int)v.x, (int)v.y, (int)v.z);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.Core
{
    public static partial class BoundsExtensions
    {
        public static Vector3 GetPositionFromNormalized(this Bounds bounds, Vector3 normalized)
        {
            return bounds.center + Vector3.Scale(normalized, bounds.extents);
        }

        public static Vector3[] GetCorners(this Bounds bounds)
        {
            Vector3[] result = new Vector3[8];
            result[0] = bounds.min;
            result[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            result[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
            result[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            result[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            result[5] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            result[6] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            result[7] = bounds.max;
            return result;
        }

        public static void GetCorners(this Bounds bounds, List<Vector3> list)
        {
            list.Add(bounds.min);
            list.Add(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z));
            list.Add(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z));
            list.Add(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z));
            list.Add(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z));
            list.Add(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z));
            list.Add(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z));
            list.Add(bounds.max);
        }
    }
}
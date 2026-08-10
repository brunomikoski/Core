using System;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools
{
    public static partial class RaycastHitExtensions
    {
        private static readonly RaycastHitDistanceFromPointComparer RAYCAST_HIT_DISTANCE_FROM_POINT_COMPARER = new();
        
        public static void SortByClosestTo(this RaycastHit[] hits, Vector3 worldPosition)
        {
            RAYCAST_HIT_DISTANCE_FROM_POINT_COMPARER.SetOrigin(worldPosition);
            Array.Sort(hits, 0, hits.Length, RAYCAST_HIT_DISTANCE_FROM_POINT_COMPARER);
        }
        
        public static void SortByFurthestFrom(this RaycastHit[] hits, Vector3 worldPosition)
        {
            RAYCAST_HIT_DISTANCE_FROM_POINT_COMPARER.SetOrigin(worldPosition, false);
            Array.Sort(hits, 0, hits.Length, RAYCAST_HIT_DISTANCE_FROM_POINT_COMPARER);
        }
    }
}
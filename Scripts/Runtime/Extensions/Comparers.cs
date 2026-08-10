using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools
{
    public sealed class ColliderDistanceFromPointComparer : IComparer<Collider>
    {            
        private Vector3 targetPosition;
        private bool inversed;

        public int Compare(Collider colliderA, Collider colliderB)
        {
            float sqrMagnitudeA = (targetPosition - colliderA.transform.position).sqrMagnitude;

            float sqrMagnitudeB = (targetPosition - colliderB.transform.position).sqrMagnitude;

            return inversed ? sqrMagnitudeB.CompareTo(sqrMagnitudeA) : sqrMagnitudeA.CompareTo(sqrMagnitudeB);
        }
            
        public void SetOrigin(Vector3 worldPosition, bool closest = true)
        {
            targetPosition = worldPosition;
            inversed = !closest;
        }
    }
    
    public sealed class RaycastHitDistanceFromPointComparer : IComparer<RaycastHit>
    {
        private Vector3 targetPosition;
        private bool inversed;

        public int Compare(RaycastHit raycastHitA, RaycastHit raycastHitB)
        {
            float sqrMagnitudeA = (targetPosition - raycastHitA.point).sqrMagnitude;

            float sqrMagnitudeB = (targetPosition - raycastHitB.point).sqrMagnitude;

            return inversed ? sqrMagnitudeB.CompareTo(sqrMagnitudeA) : sqrMagnitudeA.CompareTo(sqrMagnitudeB);
        }

        public void SetOrigin(Vector3 worldPosition, bool closest = true)
        {
            targetPosition = worldPosition;
            inversed = !closest;
        }
    }
}
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools
{
    public static class CollisionExtension
    {
        public static bool TryGetAverageCollisionPoint(this Collision collision, out Vector3 averagePoint)
        {
            averagePoint = Vector3.zero;

            int contactCount = collision.contactCount;
            if (contactCount == 0)
                return false;

            for (int j = 0; j < contactCount; j++)
                averagePoint += collision.GetContact(j).point;

            averagePoint /= contactCount;
            return true;
        }

        public static bool TryGetAverageCollisionNormal(this Collision collision, Vector3 orientTowards, out Vector3 averageNormal)
        {
            averageNormal = Vector3.zero;

            int contactCount = collision.contactCount;
            if (contactCount == 0)
                return false;

            Vector3 pointSum = Vector3.zero;
            for (int j = 0; j < contactCount; j++)
            {
                ContactPoint contact = collision.GetContact(j);
                Vector3 normal = contact.normal;
                if (Vector3.Dot(normal, orientTowards - contact.point) < 0f)
                    normal = -normal;
                averageNormal += normal;
                pointSum += contact.point;
            }

            if (averageNormal.sqrMagnitude <= Mathf.Epsilon)
                averageNormal = orientTowards - pointSum / contactCount;

            averageNormal = averageNormal.normalized;
            return averageNormal.sqrMagnitude > Mathf.Epsilon;
        }
    }
}

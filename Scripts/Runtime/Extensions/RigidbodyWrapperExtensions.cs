using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.Core.PhysX
{
    public static class RigidbodyWrapperExtensions
    {
        private static BoxCollider safelyRemovePoolCollider;


        public static Bounds GetLocalColliderBounds(this RigidbodyWrapper rigidbodyWrapper,
            LayerMask layersToIgnore = default)
        {
            IList<Collider> colliders = rigidbodyWrapper.Colliders;
            Bounds result = default(Bounds);
            bool hasFirstBounds = false;
            Matrix4x4 worldToLocal = rigidbodyWrapper.transform.worldToLocalMatrix;

            for (int i = colliders.Count - 1; i >= 0; i--)
            {
                Collider collider = colliders[i];

                if (collider == null)
                {
                    colliders.RemoveAt(i);
                    continue;
                }

                if (!collider.enabled)
                    continue;

                if (layersToIgnore.Contains(collider.gameObject.layer))
                    continue;

                Bounds colliderBounds = collider.GetLocalBounds();
                Matrix4x4 colliderMatrix = collider.transform.localToWorldMatrix;
                Matrix4x4 finalMatrix = worldToLocal * colliderMatrix;

                colliderBounds = finalMatrix.MultiplyBounds(colliderBounds);

                if (!hasFirstBounds)
                {
                    result = colliderBounds;
                    hasFirstBounds = true;
                }
                else
                {
                    result.Encapsulate(colliderBounds);
                }
            }

            return result;
        }


        public static LocalBounds GetLocalBounds(this RigidbodyWrapper rigidbodyWrapper,
            LayerMask layersToIgnore = default)
        {
            return new LocalBounds(rigidbodyWrapper.transform, rigidbodyWrapper.GetLocalColliderBounds(layersToIgnore));
        }


        public static bool IsEncapsulatedInBounds(this RigidbodyWrapper rigidbodyWrapper,
            LocalBounds bounds, LayerMask layersToIgnore = default)
        {
            for (int j = 0; j < rigidbodyWrapper.Colliders.Count; j++)
            {
                Collider collider = rigidbodyWrapper.Colliders[j];

                if (!collider.enabled || layersToIgnore.Contains(collider.gameObject.layer))
                    continue;

                if (bounds.Encapsulates(collider))
                    continue;

                return false;
            }

            return true;
        }


        public static bool IsEncapsulatedInWorldBounds(this RigidbodyWrapper rigidbodyWrapper,
            Bounds bounds, LayerMask layersToIgnore = default)
        {
            for (int j = 0; j < rigidbodyWrapper.Colliders.Count; j++)
            {
                Collider collider = rigidbodyWrapper.Colliders[j];

                if (!collider.enabled || layersToIgnore.Contains(collider.gameObject.layer))
                    continue;

                if (bounds.Contains(collider.bounds.min) && bounds.Contains(collider.bounds.max))
                    continue;

                return false;
            }

            return true;
        }


        public static bool IncludesAll(this RigidbodyWrapper rigidbodyWrapper, IList<Collider> colliders)
        {
            IList<Collider> myColliders = rigidbodyWrapper.Colliders;
            int collidersCount = colliders.Count;
            for (int i = 0; i < collidersCount; i++)
            {
                Collider collider = colliders[i];

                // Break if collider is reference null, it means we are at the end of an array
                // filled using a Physics.*NonAlloc function
                if (collider.IsNull())
                    break;
                if (!myColliders.Contains(collider))
                    return false;
            }

            return true;
        }


        public static bool IncludesAll(this RigidbodyWrapper rigidbodyWrapper, IList<RaycastHit> hits)
        {
            IList<Collider> myColliders = rigidbodyWrapper.Colliders;
            int collidersCount = hits.Count;
            for (int i = 0; i < collidersCount; i++)
            {
                RaycastHit hit = hits[i];

                // Break if collider is reference null, it means we are at the end of an array
                // filled using a Physics.*NonAlloc function
                if (hit.collider.IsNull())
                    break;

                if (!myColliders.Contains(hit.collider))
                    return false;
            }

            return true;
        }


        public static Vector3 GetClosestPointOnColliders(this RigidbodyWrapper rigidbodyWrapper, Vector3 to)
        {
            Vector3 closestPoint = Vector3.zero, pointOnCollider;
            float distance, distanceMin = float.PositiveInfinity;
            for (int i = 0; i < rigidbodyWrapper.Colliders.Count; i++)
            {
                pointOnCollider = rigidbodyWrapper.Colliders[i].ClosestPoint(to);
                distance = Vector3.Distance(pointOnCollider, to);
                if (distance < distanceMin)
                {
                    closestPoint = pointOnCollider;
                    distanceMin = distance;
                }
            }

            return closestPoint;
        }


        public static float GetDistanceToColliders(this RigidbodyWrapper rigidbodyWrapper, Vector3 position)
        {
            return Vector3.Distance(rigidbodyWrapper.GetClosestPointOnColliders(position), position);
        }

        /// <summary>
        /// This is an approximation. You may get strange results for complex shapes such as concave objects.
        /// </summary>
        public static float GetDistanceBetweenColliders(this RigidbodyWrapper rigidbodyWrapper, RigidbodyWrapper other)
        {
            Vector3 closestMine = rigidbodyWrapper.GetClosestPointOnColliders(other.CenterOfColliders);
            Vector3 closestOther = other.GetClosestPointOnColliders(closestMine);
            return Vector3.Distance(closestMine, closestOther);
        }
    }
}
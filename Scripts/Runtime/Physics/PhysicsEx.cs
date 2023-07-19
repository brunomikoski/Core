using System;
using System.Collections.Generic;
using UnityEngine;
using Vertx.Debugging;
#if UNITY_EDITOR
using Physics = Vertx.Debugging.DrawPhysics;
#endif

namespace BrunoMikoski.Core.PhysX
{
    /// <summary>
    /// Mirror of Physics Cast and Overlap methods. Use the results only for one frame, since
    /// arrays are recycled every frame.
    /// </summary>
    public class PhysicsEx
    {
        public enum SortType
        {
            None = 0,
            Closest = 1,
            Furthest = 2
        }

        private const int ARRAY_SIZE = 256;
        private const int DEFAULT_RAYCAST_LAYERS = -5;
        private const int PHYSICS_POOL_SIZE_WARNING_THRESHOLD = 100;
        private const int PHYSICS_POOL_SIZE_ERROR_THRESHOLD = 200;

        private class ArrayPool<T>
        {
            private List<PoolArray<T>> arrays = new(100);
            private int lastFrameNumber = int.MinValue;
            private int nextArrayIndex = 0;

            public PoolArray<T> ObtainArray()
            {
                if (lastFrameNumber < Time.frameCount || !Application.isPlaying)
                {
                    for (int i = 0; i < nextArrayIndex; i++)
                    {
                        PoolArray<T> array = arrays[i];
                        Array.Clear(array.Data, 0, array.Data.Length);
                        array.SetUsedCount(0);
                    }
                    nextArrayIndex = 0;
                    lastFrameNumber = Time.frameCount;
                }

                if (nextArrayIndex >= arrays.Count)
                {
                    arrays.Add(new PoolArray<T>(new T[ARRAY_SIZE]));
#if DEBUG
                    if (arrays.Count >= PHYSICS_POOL_SIZE_ERROR_THRESHOLD)
                    {
                        Debug.LogErrorFormat("Physics pool size of {0} is now size {1}",
                            typeof(T).Name, arrays.Count);
                    }
                    else if (arrays.Count >= PHYSICS_POOL_SIZE_WARNING_THRESHOLD)
                    {
                        Debug.LogWarningFormat("Physics pool size of {0} is now size {1}",
                            typeof(T).Name, arrays.Count);
                    }
#endif
                }
                return arrays[nextArrayIndex++];
            }

            public void Release(PoolArray<T> array)
            {
                Array.Clear(array.Data, 0, array.Data.Length);
                array.SetUsedCount(0);

                if (arrays[nextArrayIndex - 1] != array)
                {
                    Debug.LogError("Releasing array out of order");
                    return;
                }

                nextArrayIndex--;
            }
        }

        private sealed class ClosestRaycastHitComparer : IComparer<RaycastHit>
        {
            public int Compare(RaycastHit raycastHitA, RaycastHit raycastHitB)
            {
                float sqrMagnitudeA = (raycastOrigin - raycastHitA.point).sqrMagnitude;

                float sqrMagnitudeB = (raycastOrigin - raycastHitB.point).sqrMagnitude;

                return sqrMagnitudeA.CompareTo(sqrMagnitudeB);
            }
        }

        internal sealed class FurthestRaycastHitComparer : IComparer<RaycastHit>
        {
            public int Compare(RaycastHit raycastHitA, RaycastHit raycastHitB)
            {
                float sqrMagnitudeA = (raycastOrigin - raycastHitA.point).sqrMagnitude;

                float sqrMagnitudeB = (raycastOrigin - raycastHitB.point).sqrMagnitude;

                return sqrMagnitudeB.CompareTo(sqrMagnitudeA);
            }
        }

        internal sealed class ClosestColliderComparer : IComparer<Collider>
        {
            public int Compare(Collider colliderA, Collider colliderB)
            {
                float sqrMagnitudeA = (raycastOrigin - colliderA.transform.position).sqrMagnitude;

                float sqrMagnitudeB = (raycastOrigin - colliderB.transform.position).sqrMagnitude;

                return sqrMagnitudeA.CompareTo(sqrMagnitudeB);
            }
        }

        private sealed class FurthestColliderComparer : IComparer<Collider>
        {
            public int Compare(Collider colliderA, Collider colliderB)
            {
                float sqrMagnitudeA = (raycastOrigin - colliderA.transform.position).sqrMagnitude;

                float sqrMagnitudeB = (raycastOrigin - colliderB.transform.position).sqrMagnitude;

                return sqrMagnitudeB.CompareTo(sqrMagnitudeA);
            }
        }

        private static ArrayPool<RaycastHit> raycastHitPool = new();
        private static ArrayPool<Collider> colliderPool = new();

        private static Vector3 raycastOrigin;

        private static ClosestRaycastHitComparer closestRaycastHitComparison = new();

        private static FurthestRaycastHitComparer furthestRaycastHitComparison = new();

        private static ClosestColliderComparer closestColliderComparison = new();

        private static FurthestColliderComparer furthestColliderComparison = new();

        public static PoolArray<RaycastHit> Raycast(
            Ray ray,
            float maxDistance = Mathf.Infinity,
            int layerMask = DEFAULT_RAYCAST_LAYERS,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
            SortType sortMethod = SortType.None,
            bool drawGizmos = false)
        {
            PoolArray<RaycastHit> raycastHits = raycastHitPool.ObtainArray();
            Raycast(ray, raycastHits, maxDistance, layerMask, queryTriggerInteraction, sortMethod,
                drawGizmos);
            return raycastHits;
        }

        public static void Raycast(
            Ray ray,
            PoolArray<RaycastHit> array,
            float maxDistance = Mathf.Infinity,
            int layerMask = DEFAULT_RAYCAST_LAYERS,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
            SortType sortMethod = SortType.None,
            bool drawGizmos = false)
        {
            Raycast(ray.origin, ray.direction, array, maxDistance, layerMask,
                queryTriggerInteraction, sortMethod, drawGizmos);
        }


        public static PoolArray<RaycastHit> Raycast(
            Vector3 origin,
            Vector3 direction,
            float maxDistance = Mathf.Infinity,
            int layerMask = DEFAULT_RAYCAST_LAYERS,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
            SortType sortMethod = SortType.None,
            bool drawGizmos = false)
        {
            PoolArray<RaycastHit> array = raycastHitPool.ObtainArray();
            Raycast(origin, direction, array, maxDistance, layerMask, queryTriggerInteraction,
                sortMethod, drawGizmos);
            return array;
        }

        public static void Raycast(
            Vector3 origin,
            Vector3 direction,
            PoolArray<RaycastHit> array,
            float maxDistance = Mathf.Infinity,
            int layerMask = DEFAULT_RAYCAST_LAYERS,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
            SortType sortMethod = SortType.None,
            bool drawGizmos = false)
        {
            int count;

            if (drawGizmos)
            {
                count = DrawPhysics.RaycastNonAlloc(origin, direction, array.Data, maxDistance,
                    layerMask, queryTriggerInteraction);
            }
            else
            {
                count = Physics.RaycastNonAlloc(origin, direction, array.Data, maxDistance,
                    layerMask, queryTriggerInteraction);
            }
            
            array.SetUsedCount(count);
            Sort(array, origin, sortMethod);
        }

        public static PoolArray<RaycastHit> BoxCast(
            Vector3 center,
            Vector3 halfExtents,
            Vector3 direction,
            Quaternion orientation,
            float maxDistance = Mathf.Infinity,
            int layerMask = DEFAULT_RAYCAST_LAYERS,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
            SortType sortMethod = SortType.None,
            bool drawGizmo = false)
        {
            PoolArray<RaycastHit> array = raycastHitPool.ObtainArray();
            BoxCast(center, halfExtents, direction, orientation,array, maxDistance, layerMask,
                queryTriggerInteraction, sortMethod, drawGizmo);
            return array;
        }

        public static void BoxCast(
            Vector3 center,
            Vector3 halfExtents,
            Vector3 direction,
            Quaternion orientation,
            PoolArray<RaycastHit> array,
            float maxDistance = Mathf.Infinity,
            int layerMask = DEFAULT_RAYCAST_LAYERS,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
            SortType sortMethod = SortType.None,
            bool drawGizmo = false)
        {
            int count;
            if (drawGizmo)
            {
                count = DrawPhysics.BoxCastNonAlloc(center, halfExtents, direction, array.Data, orientation,
                    maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                count = Physics.BoxCastNonAlloc(center, halfExtents, direction, array.Data, orientation,
                    maxDistance, layerMask, queryTriggerInteraction);
            }

            array.SetUsedCount(count);
            Sort(array, center, sortMethod);
        }

        public static PoolArray<RaycastHit> SphereCast(
            Vector3 origin,
            float radius,
            Vector3 direction,
            float maxDistance = Mathf.Infinity,
            int layerMask = DEFAULT_RAYCAST_LAYERS,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
            SortType sortMethod = SortType.None,
            bool drawGizmo = false)
        {
            PoolArray<RaycastHit> array = raycastHitPool.ObtainArray();
            SphereCast(origin, radius, direction, array, maxDistance, layerMask,
                queryTriggerInteraction, sortMethod, drawGizmo);
            return array;
        }

        public static void SphereCast(
            Vector3 origin,
            float radius,
            Vector3 direction,
            PoolArray<RaycastHit> array,
            float maxDistance = Mathf.Infinity,
            int layerMask = DEFAULT_RAYCAST_LAYERS,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
            SortType sortMethod = SortType.None,
            bool drawGizmo = false)
        {
            int count;

            if (drawGizmo)
            {
                count = DrawPhysics.SphereCastNonAlloc(origin, radius, direction, array.Data,
                    maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                count = Physics.SphereCastNonAlloc(origin, radius, direction, array.Data,
                    maxDistance, layerMask, queryTriggerInteraction);
            }

            array.SetUsedCount(count);
            Sort(array, origin, sortMethod);
        }

        public static PoolArray<RaycastHit> CapsuleCast(
            Vector3 point1,
            Vector3 point2,
            float radius,
            Vector3 direction,
            float maxDistance = Mathf.Infinity,
            int layerMask = DEFAULT_RAYCAST_LAYERS,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
            SortType sortMethod = SortType.None,
            bool drawGizmo = false)
        {
            PoolArray<RaycastHit> array = raycastHitPool.ObtainArray();
            CapsuleCast(point1, point2, radius, direction, array, maxDistance, layerMask,
                queryTriggerInteraction, sortMethod, drawGizmo);
            return array;
        }

        public static void CapsuleCast(
            Vector3 point1,
            Vector3 point2,
            float radius,
            Vector3 direction,
            PoolArray<RaycastHit> array,
            float maxDistance = Mathf.Infinity,
            int layerMask = DEFAULT_RAYCAST_LAYERS,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
            SortType sortMethod = SortType.None,
            bool drawGizmo = false)
        {
            int count;
            if (drawGizmo)
            {
                count = DrawPhysics.CapsuleCastNonAlloc(point1, point2, radius, direction, array.Data,
                    maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                count = Physics.CapsuleCastNonAlloc(point1, point2, radius, direction, array.Data,
                    maxDistance, layerMask, queryTriggerInteraction);
            }
            
            array.SetUsedCount(count);
            Sort(array, point1, sortMethod);
        }

        public static PoolArray<Collider> OverlapBox(
            Vector3 center,
            Vector3 halfExtents,
            Quaternion orientation,
            int layerMask = DEFAULT_RAYCAST_LAYERS,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
            SortType sortMethod = SortType.None,
            bool drawGizmo = false)
        {
            PoolArray<Collider> array = colliderPool.ObtainArray();
            OverlapBox(center, halfExtents, orientation, array, layerMask,
                queryTriggerInteraction, sortMethod, drawGizmo);
            return array;
        }

        public static void OverlapBox(
            Vector3 center,
            Vector3 halfExtents,
            Quaternion orientation,
            PoolArray<Collider> array,
            int layerMask = DEFAULT_RAYCAST_LAYERS,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
            SortType sortMethod = SortType.None,
            bool drawGizmo = false)
        {
            int count;
            if (drawGizmo)
            {
                count = DrawPhysics.OverlapBoxNonAlloc(center, halfExtents, array.Data, orientation, layerMask,
                    queryTriggerInteraction);
            }
            else
            {
                count = Physics.OverlapBoxNonAlloc(center, halfExtents, array.Data, orientation, layerMask,
                    queryTriggerInteraction);
            }
            
            array.SetUsedCount(count);
            Sort(array, center, sortMethod);
        }

        public static PoolArray<Collider> OverlapSphere(
            Vector3 origin,
            float radius,
            int layerMask = DEFAULT_RAYCAST_LAYERS,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
            SortType sortMethod = SortType.None,
            bool drawGizmo = false)
        {
            PoolArray<Collider> array = colliderPool.ObtainArray();
            OverlapSphere(origin, radius, array, layerMask, queryTriggerInteraction,
                sortMethod, drawGizmo);
            return array;
        }

        public static void OverlapSphere(
            Vector3 origin,
            float radius,
            PoolArray<Collider> array,
            int layerMask = DEFAULT_RAYCAST_LAYERS,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
            SortType sortMethod = SortType.None,
            bool drawGizmo = false)
        {
            int count;

            if (drawGizmo)
            {
                count = DrawPhysics.OverlapSphereNonAlloc(origin, radius, array.Data, layerMask, queryTriggerInteraction);
            }
            else
            {
                count = Physics.OverlapSphereNonAlloc(origin, radius, array.Data, layerMask, queryTriggerInteraction);
            }

            array.SetUsedCount(count);
            Sort(array, origin, sortMethod);
        }


        public static PoolArray<Collider> OverlapCapsule(
            Vector3 point0,
            Vector3 point1,
            float radius,
            int layerMask = DEFAULT_RAYCAST_LAYERS,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
            SortType sortMethod = SortType.None,
            bool drawGizmo = false)
        {
            PoolArray<Collider> array = colliderPool.ObtainArray();
            OverlapCapsule(point0, point1, radius, array, layerMask, queryTriggerInteraction,
                sortMethod, drawGizmo);
            return array;
        }


        public static void OverlapCapsule(
            Vector3 point0,
            Vector3 point1,
            float radius,
            PoolArray<Collider> array,
            int layerMask = DEFAULT_RAYCAST_LAYERS,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
            SortType sortMethod = SortType.None,
            bool drawGizmo = false)
        {
            int count;
            if (drawGizmo)
            {
                count = DrawPhysics.OverlapCapsuleNonAlloc(point0, point1, radius, array.Data, layerMask, queryTriggerInteraction);
            }
            else
            {
                count = Physics.OverlapCapsuleNonAlloc(point0, point1, radius, array.Data, layerMask, queryTriggerInteraction);
            }
            array.SetUsedCount(count);
            Sort(array, point0, sortMethod);
        }

        private static void Sort(PoolArray<RaycastHit> array, Vector3 origin, SortType sortMethod)
        {
            raycastOrigin = origin;
            switch (sortMethod)
            {
                case SortType.None:
                    return;
                case SortType.Closest:
                    Array.Sort(array.Data, 0, array.Count, closestRaycastHitComparison);
                    break;
                case SortType.Furthest:
                    Array.Sort(array.Data, 0, array.Count, furthestRaycastHitComparison);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sortMethod), sortMethod, null);
            }
        }

        private static void Sort(PoolArray<Collider> array, Vector3 origin, SortType sortMethod)
        {
            raycastOrigin = origin;
            switch (sortMethod)
            {
                case SortType.None:
                    return;
                case SortType.Closest:
                    Array.Sort(array.Data, 0, array.Count, closestColliderComparison);
                    break;
                case SortType.Furthest:
                    Array.Sort(array.Data, 0, array.Count, furthestColliderComparison);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sortMethod), sortMethod, null);
            }
        }

        public static PoolArray<RaycastHit> ObtainRaycastHitArray()
        {
            return raycastHitPool.ObtainArray();
        }

        public static PoolArray<Collider> ObtainColliderArray()
        {
            return colliderPool.ObtainArray();
        }

        public static void Release(PoolArray<RaycastHit> array)
        {
            raycastHitPool.Release(array);
        }

        public static void Release(PoolArray<Collider> array)
        {
            colliderPool.Release(array);
        }
    }
}

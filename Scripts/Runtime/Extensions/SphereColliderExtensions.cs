using UnityEngine;
#if UNITY_EDITOR && VERTX_DEBUG_ENABLED
using Physics = Vertx.Debugging.DrawPhysics;
#endif

namespace BrunoMikoski.Core.DevelopmentTools
{
    public static partial class SphereColliderExtensions
    {
        private const float SPHERECAST_DEFAULT_INSET_FACTOR = 0.2285714285714286f;

        private static float GetWorldSpaceRadius(this SphereCollider sphereCollider)
        {
            Vector3 colliderTransformLossyScale = sphereCollider.transform.lossyScale;
            float scale = Mathf.Max(
                colliderTransformLossyScale.x, colliderTransformLossyScale.y,
                colliderTransformLossyScale.z);
            return sphereCollider.radius * scale;
        }

        private static Vector3 GetWorldSpaceCenter(this SphereCollider sphereCollider)
        {
            return sphereCollider.transform.TransformPoint(sphereCollider.center);
        }

        public static RaycastHit SphereCast(
            this SphereCollider sphereCollider, Vector3 direction, float maxDistance,
            int layerMask = ~0,
            float insetFactor = SPHERECAST_DEFAULT_INSET_FACTOR,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float radius = sphereCollider.GetWorldSpaceRadius();
            Vector3 center = sphereCollider.GetWorldSpaceCenter();

            Physics.SphereCast(
                center, radius * (1.0f - insetFactor), direction, out RaycastHit hitInfo,
                maxDistance + radius * insetFactor,
                layerMask,
                queryTriggerInteraction);

            return hitInfo;
        }

        public static int SphereCastAllNonAlloc(
            this SphereCollider sphereCollider, Vector3 direction, RaycastHit[] hits, float maxDistance,
            int layerMask = ~0,
            float insetFactor = SPHERECAST_DEFAULT_INSET_FACTOR,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float radius = sphereCollider.GetWorldSpaceRadius();
            Vector3 center = sphereCollider.GetWorldSpaceCenter();
            return Physics.SphereCastNonAlloc(center, radius, direction, hits, maxDistance + radius * insetFactor,
                layerMask,
                queryTriggerInteraction);
        }
    }
}
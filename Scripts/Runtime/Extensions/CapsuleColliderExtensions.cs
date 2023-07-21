using UnityEngine;
#if UNITY_EDITOR && !FORCE_REGULAR_PHYSICS
using Physics = Vertx.Debugging.DrawPhysics;
#endif

namespace BrunoMikoski.Core
{
    public static partial class CapsuleColliderExtensions
    {
        private const float CAPSULECAST_DEFAULT_INSET_FACTOR = 0.2285714285714286f;

        public static RaycastHit CapsuleCast(
            this CapsuleCollider capsuleCollider, Vector3 direction, float maxDistance,
            int layerMask = ~0, float insetFactor = CAPSULECAST_DEFAULT_INSET_FACTOR,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            Transform capsuleColliderTransform = capsuleCollider.transform;
            Vector3 capsuleColliderTransformLossyScale = capsuleColliderTransform.lossyScale;
            float scale = Mathf.Max(
                capsuleColliderTransformLossyScale.x, capsuleColliderTransformLossyScale.y,
                capsuleColliderTransformLossyScale.z);

            float radius = capsuleCollider.radius * scale;
            float height = capsuleCollider.height * scale;
            Vector3 origin = capsuleColliderTransform.TransformPoint(capsuleCollider.center);

            return CapsuleCast(
                origin, capsuleColliderTransform.rotation, height, radius, direction, maxDistance, layerMask,
                insetFactor, queryTriggerInteraction);
        }

        public static RaycastHit CapsuleCast(Vector3 position, Quaternion rotation, float height, float radius,
            Vector3 direction, float maxDistance,
            int layerMask = ~0, float insetFactor = CAPSULECAST_DEFAULT_INSET_FACTOR,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            Vector3 up = rotation * Vector3.up;

            Vector3 sphereOriginTop = position + up * (height * 0.5f - radius);
            Vector3 sphereOriginBottom = position - up * (height * 0.5f - radius);

            Physics.CapsuleCast(
                sphereOriginTop, sphereOriginBottom, radius * (1.0f - insetFactor), direction, out RaycastHit hitInfo,
                maxDistance + radius * insetFactor, layerMask, queryTriggerInteraction);
            return hitInfo;
        }

        public static int CapsuleCastAllNonAlloc(Vector3 position, Quaternion rotation, float height, float radius,
            Vector3 direction, RaycastHit[] hits, float maxDistance,
            int layerMask = ~0, float insetFactor = CAPSULECAST_DEFAULT_INSET_FACTOR,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            Vector3 up = rotation * Vector3.up;

            Vector3 sphereOriginTop = position + up * (height * 0.5f - radius);
            Vector3 sphereOriginBottom = position - up * (height * 0.5f - radius);

            return Physics.CapsuleCastNonAlloc(
                sphereOriginTop, sphereOriginBottom, radius * (1.0f - insetFactor), direction, hits,
                maxDistance + radius * insetFactor, layerMask, queryTriggerInteraction);
        }
    }
}

using UnityEngine;
#if UNITY_EDITOR && VERTX_DEBUG_ENABLED
using Physics = Vertx.Debugging.DrawPhysics;
#endif

namespace BrunoMikoski.Core
{
    public static partial class BoxColliderExtensions
    {
        private const float BOXCAST_DEFAULT_INSET_FACTOR = 0.1f;

        public static RaycastHit BoxCast(
            this BoxCollider boxCollider, Vector3 direction, float maxDistance,
            int layerMask = ~0, float insetFactor = BOXCAST_DEFAULT_INSET_FACTOR,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            Transform boxColliderTransform = boxCollider.transform;
            Vector3 center = boxColliderTransform.TransformPoint(boxCollider.center);
            Vector3 boxColliderSize = boxCollider.size;
            Vector3 halfExtents =
                Matrix4x4.TRS(Vector3.zero, Quaternion.identity, boxColliderTransform.lossyScale) *
                boxColliderSize * 0.5f * (1.0f - insetFactor);

            float size = Mathf.Max(boxColliderSize.x, boxColliderSize.y, boxColliderSize.z);

            Physics.BoxCast(
                center, halfExtents, direction, out RaycastHit hitInfo, boxColliderTransform.rotation,
                maxDistance + size * 0.5f * insetFactor, layerMask, queryTriggerInteraction);
            return hitInfo;
        }

        public static int BoxCastAllNonAlloc(
            this BoxCollider boxCollider, Vector3 direction, RaycastHit[] hits, float maxDistance,
            int layerMask = ~0, float insetFactor = BOXCAST_DEFAULT_INSET_FACTOR,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            Transform boxColliderTransform = boxCollider.transform;
            Vector3 center = boxColliderTransform.TransformPoint(boxCollider.center);
            Vector3 boxColliderSize = boxCollider.size;
            Vector3 halfExtents =
                Matrix4x4.TRS(Vector3.zero, Quaternion.identity, boxColliderTransform.lossyScale) *
                boxColliderSize * 0.5f * (1.0f - insetFactor);

            float size = Mathf.Max(boxColliderSize.x, boxColliderSize.y, boxColliderSize.z);

            return Physics.BoxCastNonAlloc(
                center, halfExtents, direction, hits, boxColliderTransform.rotation,
                maxDistance + size * 0.5f * insetFactor, layerMask, queryTriggerInteraction);
        }
    }
}
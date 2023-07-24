using UnityEngine;

namespace BrunoMikoski.Core.PhysX
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MultiCollidersTriggerVolume : TriggerVolumeBase
    {
        [Header("Colliders References")]
        [SerializeField]
        private Collider[] colliders = { };

        private Vector3 lastPosition;
        private Quaternion lastRotation;

        private Bounds triggerBounds;


        public override Vector3 GetTriggerOrigin()
        {
            return GetTriggerBounds().center;
        }

        public override Bounds GetTriggerBounds()
        {
            if (!Application.isPlaying || HasTransformChanged() || triggerBounds == default)
                UpdateBounds();
            return triggerBounds;
        }

        private void UpdateBounds()
        {
            triggerBounds = default;
            bool hasFirstBounds = false;
            for (int i = colliders.Length - 1; i >= 0; i--)
            {
                Collider targetCollider = colliders[i];

                if (targetCollider == null)
                    continue;

                if (!targetCollider.enabled)
                    continue;

                if (!hasFirstBounds)
                {
                    triggerBounds = targetCollider.bounds;
                    hasFirstBounds = true;
                }
                else
                {
                    triggerBounds.Encapsulate(targetCollider.bounds);
                }
            }

            Transform boundsTranform = transform;
            lastPosition = boundsTranform.position;
            lastRotation = boundsTranform.rotation;
        }

        protected override void CreateTriggerCollider()
        {
            Rigidbody targetRigidbody = gameObject.GetOrAddComponent<Rigidbody>();
            targetRigidbody.useGravity = false;
            targetRigidbody.isKinematic = true;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                collider.gameObject.layer = gameObject.layer;
                collider.isTrigger = true;
                collider.enabled = true;
            }

            SetTriggerCollider(colliders[0]);
        }

        private bool HasTransformChanged()
        {
            if (lastPosition != transform.position)
                return true;
            if (lastRotation != transform.rotation)
                return true;
            return false;
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider targetCollider = colliders[i];
                Gizmos.matrix = targetCollider.transform.localToWorldMatrix;
                GizmoUtility.DrawCollider(targetCollider);
            }
        }
    }
}

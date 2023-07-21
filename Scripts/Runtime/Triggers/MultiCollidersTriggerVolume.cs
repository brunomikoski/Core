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
                Collider collider = colliders[i];

                if (collider == null)
                    continue;

                if (!collider.enabled)
                    continue;

                if (!hasFirstBounds)
                {
                    triggerBounds = collider.bounds;
                    hasFirstBounds = true;
                }
                else
                {
                    triggerBounds.Encapsulate(collider.bounds);
                }
            }

            lastPosition = transform.position;
            lastRotation = transform.rotation;
        }

        protected override void CreateTriggerCollider()
        {
            Rigidbody rigidbody = gameObject.GetOrAddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
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
    }
}

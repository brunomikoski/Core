using UnityEngine;

namespace BrunoMikoski.Core.PhysX
{

    [AddComponentMenu("Triggers/Sphere Trigger Volume")]
    public sealed class SphereTriggerVolume : TriggerVolumeBase
    {
        [Header("Sphere")]
        [Range(0f, 50)]
        [SerializeField]
        private float size = 1f;

        public float Size => size;

        public SphereCollider SphereCollider => (SphereCollider) TriggerCollider;

        protected override void CreateTriggerCollider()
        {
            CreateCollider<SphereCollider>();

            UpdateSphereDimensions();
        }

        private void UpdateSphereDimensions()
        {
            float newRadius = size;
            if (!Mathf.Approximately(newRadius, SphereCollider.radius))
                SphereCollider.radius = newRadius;
        }

        public override Vector3 GetTriggerOrigin()
        {
            return transform.position;
        }

        public override Bounds GetTriggerBounds()
        {
            SphereCollider sphereCollider = (SphereCollider) TriggerCollider;
            return new Bounds(sphereCollider.center, Vector3.one * sphereCollider.radius);
        }

        public void SetSize(float targetSize)
        {
            this.size = targetSize;
            UpdateSphereDimensions();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(Vector3.zero, size);
        }
    }
}
using UnityEngine;

namespace BrunoMikoski.Core.PhysX
{
    [AddComponentMenu("Triggers/Box Trigger Volume")]
    public sealed class BoxTriggerVolume : TriggerVolumeBase
    {
        [Header("Box")]
        [SerializeField]
        private Vector3 center = Vector3.zero;

        [SerializeField]
        private Vector3 size = Vector3.one;

        public Vector3 LocalCenter => center;
        public Vector3 LocalSize => size;

        public Bounds WorldBounds
        {
            get
            {
                Bounds bounds = new Bounds(center, size);
                return transform.TransformBounds(bounds);
            }
        }

        protected override void CreateTriggerCollider()
        {
            CreateCollider<BoxCollider>();
            UpdateBoxCollider();
        }

        private void UpdateBoxCollider()
        {
            BoxCollider boxCollider = (BoxCollider) TriggerCollider;

            boxCollider.center = center;
            boxCollider.size = size;
        }

        public override Vector3 GetTriggerOrigin()
        {
            return transform.TransformPoint(center);
        }

        public override Bounds GetTriggerBounds()
        {
            BoxCollider boxCollider = (BoxCollider) TriggerCollider;
            return new Bounds(boxCollider.center, boxCollider.size);
        }

        public void SetSize(Vector3 targetSize)
        {
            this.size = targetSize;
            UpdateBoxCollider();
        }

        public void SetCenter(Vector3 targetCenter)
        {
            this.center = targetCenter;
            UpdateBoxCollider();
        }
    }
}

using UnityEngine;

namespace BrunoMikoski.Core.PhysX
{
    [AddComponentMenu("Triggers/2d/2D Box Trigger Volume")]
    public sealed class Box2DTriggerVolume : TriggerVolume2DBase
    {
        [Header("Box")]
        [SerializeField]
        private Vector3 offSet = Vector3.zero;

        [SerializeField]
        private Vector3 size = Vector3.one;

        public Vector3 LocalOffSet => offSet;
        public Vector3 LocalSize => size;

        public Bounds WorldBounds
        {
            get
            {
                Bounds bounds = new Bounds(offSet, size);
                return transform.TransformBounds(bounds);
            }
        }

        protected override void CreateTriggerCollider()
        {
            CreateCollider<BoxCollider2D>();
            UpdateBoxCollider();
        }

        private void UpdateBoxCollider()
        {
            BoxCollider2D boxCollider = (BoxCollider2D) TriggerCollider;

            boxCollider.offset = offSet;
            boxCollider.size = size;
        }

        public override Vector3 GetTriggerOrigin()
        {
            return transform.TransformPoint(offSet);
        }

        public override Bounds GetTriggerBounds()
        {
            BoxCollider2D boxCollider = (BoxCollider2D) TriggerCollider;
            return new Bounds(boxCollider.offset, boxCollider.size);
        }

        public void SetSize(Vector3 targetSize)
        {
            this.size = targetSize;
            UpdateBoxCollider();
        }

        public void SetCenter(Vector3 targetCenter)
        {
            this.offSet = targetCenter;
            UpdateBoxCollider();
        }

        private void OnDrawGizmos()
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(offSet, size);


            Gizmos.matrix = oldMatrix;
            for (int i = 0; i < ObjectsInVolume.Count; i++)
            {
                Rigidbody2D colliding = ObjectsInVolume[i];
                if (colliding == null)
                    continue;
                
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, colliding.transform.position);
            }
        }
    }
}

using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Triggers
{
    [AddComponentMenu("Triggers/Mesh Trigger Volume")]
    public sealed class MeshTriggerVolume : TriggerVolumeBase
    {
        [Header("Mesh")]
        [SerializeField]
        private Mesh mesh;

        [SerializeField]
        private Vector3 center = Vector3.zero;

        [SerializeField]
        [Tooltip("Multiplier applied to the mesh, not an absolute size. One keeps the mesh at its authored dimensions.")]
        private Vector3 size = Vector3.one;

        public MeshCollider MeshCollider => (MeshCollider) TriggerCollider;

        private Mesh transformedMesh;

        protected override void CreateTriggerCollider()
        {
            CreateCollider<MeshCollider>();
            UpdateMeshCollider();
        }

        private void UpdateMeshCollider()
        {
            Mesh previousMesh = transformedMesh;
            transformedMesh = null;

            MeshCollider meshCollider = (MeshCollider) TriggerCollider;
            meshCollider.sharedMesh = BuildColliderMesh();

            if (previousMesh != null)
                Destroy(previousMesh);
        }

        private Mesh BuildColliderMesh()
        {
            if (mesh == null)
                return null;

            if (center == Vector3.zero && size == Vector3.one)
                return mesh;

            if (!mesh.isReadable)
            {
                Debug.LogError(
                    $"[{nameof(MeshTriggerVolume)}] {name} uses the mesh '{mesh.name}', which is not readable, so Center "
                    + "and Size cannot be applied. Enable Read/Write on its import settings, or leave Center at zero and "
                    + "Size at one and place the volume through the transform instead.", this);
                return mesh;
            }

            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = Vector3.Scale(vertices[i], size) + center;

            transformedMesh = new Mesh
            {
                name = $"{mesh.name} (Trigger Volume)",
                hideFlags = HideFlags.HideAndDontSave
            };
            transformedMesh.SetVertices(vertices);
            transformedMesh.SetTriangles(mesh.triangles, 0);
            return transformedMesh;
        }

        public override Vector3 GetTriggerOrigin()
        {
            return transform.TransformPoint(GetTriggerBounds().center);
        }

        public override Bounds GetTriggerBounds()
        {
            if (mesh == null)
                return new Bounds(center, size);

            Bounds meshBounds = mesh.bounds;
            return new Bounds(Vector3.Scale(meshBounds.center, size) + center, Vector3.Scale(meshBounds.size, size));
        }

        public void SetMesh(Mesh targetMesh)
        {
            mesh = targetMesh;
            UpdateMeshCollider();
        }

        public void SetSize(Vector3 targetSize)
        {
            size = targetSize;
            UpdateMeshCollider();
        }

        public void SetCenter(Vector3 targetCenter)
        {
            center = targetCenter;
            UpdateMeshCollider();
        }

        private void OnDrawGizmos()
        {
            if (mesh == null)
                return;

            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.TRS(center, Quaternion.identity, size);
            Gizmos.DrawMesh(mesh);
        }
    }
}

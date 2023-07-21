using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrunoMikoski.Core.PhysX
{
    /// <summary>
    /// A compount mesh trigger volume is a solution for having non-convex meshes
    /// as triggers.
    /// </summary>
    [AddComponentMenu("Triggers/Compound Mesh Trigger Volume")]
    public sealed class CompoundMeshTriggerVolume : TriggerVolumeBase
    {
        [Header("Mesh")]
        [SerializeField]
        private Mesh mesh;

        [SerializeField]
        private Vector3 center = Vector3.zero;

        [SerializeField]
        private Vector3 size = Vector3.one;

        private List<MeshCollider> triggerColliders;

        [SerializeField]
        private bool setupOnStart = true;

        private void Start()
        {
            if (setupOnStart)
                Setup();
        }

        public void Setup(Mesh targetMesh)
        {
            mesh = targetMesh;
            Setup();
        }

        private void Setup()
        {
            List<int> meshTriangles = mesh.triangles.ToList();
            List<Vector3> meshVertices = mesh.vertices.ToList();

            List<int> triangles = new List<int>();
            List<Vector3> vertices = new List<Vector3>();

            List<Mesh> subMeshes = new List<Mesh>();
            for (int i = 0; i < meshTriangles.Count; i += 3)
            {
                vertices.Clear();
                for (int j = 0; j < 3; j++)
                    vertices.Add(meshVertices[meshTriangles[i + j]] + Vector3.up * 0.5f);

                for (int j = 0; j < 3; j++)
                    vertices.Add(vertices[j] + Vector3.down);

                triangles.Clear();
                triangles.AddRange(new[] {0, 1, 2});
                triangles.AddRange(new[] {3, 5, 4});

                triangles.AddRange(new[] {0, 2, 3});
                triangles.AddRange(new[] {3, 2, 5});
                triangles.AddRange(new[] {2, 1, 5});
                triangles.AddRange(new[] {5, 1, 4});
                triangles.AddRange(new[] {1, 0, 4});
                triangles.AddRange(new[] {4, 0, 3});

                Mesh subMesh = new Mesh();
                subMesh.SetVertices(vertices);
                subMesh.SetTriangles(triangles, 0);
                subMeshes.Add(subMesh);
            }

            triggerColliders = new List<MeshCollider>();
            for (int i = 0; i < subMeshes.Count; i++)
            {
                MeshCollider meshCollider = (MeshCollider) CreateCollider<MeshCollider>();
                meshCollider.sharedMesh = subMeshes[i];
                triggerColliders.Add(meshCollider);
            }

            UpdateMeshDimensions();
        }

        protected override void OnEnable()
        {
            if (triggerColliders != null)
            {
                foreach (MeshCollider collider in triggerColliders)
                    collider.enabled = true;
            }
        }

        protected override void OnDisable()
        {
            if (triggerColliders != null)
            {
                foreach (MeshCollider collider in triggerColliders)
                    collider.enabled = false;
            }

            ClearAllObjectsInVolume();
        }


        private void UpdateMeshDimensions()
        {
            if (triggerChild != null)
            {
                triggerChild.transform.localPosition = center;
                triggerChild.transform.localScale = new Vector3(1, size.y, 1);
            }
        }

        public override Vector3 GetTriggerOrigin()
        {
            return transform.TransformPoint(center);
        }

        public override Bounds GetTriggerBounds()
        {
            return new Bounds(center, size);
        }

        public void SetMesh(Mesh mesh)
        {
            this.mesh = mesh;
        }

        public void SetSize(Vector3 size)
        {
            this.size = size;
            UpdateMeshDimensions();
        }

        public void SetCenter(Vector3 center)
        {
            this.center = center;
            UpdateMeshDimensions();
        }
    }
}

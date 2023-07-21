using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BrunoMikoski.Core
{
    public static partial class ColliderExtensions
    {
        private static readonly ColliderDistanceFromPointComparer COLLIDER_DISTANCE_FROM_POINT_COMPARER = new();

        public static void SortByClosest(this Collider[] colliders, Vector3 worldPosition)
        {
            COLLIDER_DISTANCE_FROM_POINT_COMPARER.SetOrigin(worldPosition);
            Array.Sort(colliders, 0, colliders.Length, COLLIDER_DISTANCE_FROM_POINT_COMPARER);
        }
        
        public static void SortByFurthest(this Collider[] colliders, Vector3 worldPosition)
        {
            COLLIDER_DISTANCE_FROM_POINT_COMPARER.SetOrigin(worldPosition, false);
            Array.Sort(colliders, 0, colliders.Length, COLLIDER_DISTANCE_FROM_POINT_COMPARER);
        }
        
        public static Vector3 GetRandomPositionInVolume(this Collider collider)
        {
            if (collider is BoxCollider box)
            {
                Vector3 size = box.size;
                Vector3 randomOffset = new Vector3();
                randomOffset.x += Random.Range(-size.x, size.x) * .5f;
                randomOffset.y += Random.Range(-size.y, size.y) * .5f;
                randomOffset.z += Random.Range(-size.z, size.z) * .5f;
                Vector3 position = box.transform.TransformPoint(
                    randomOffset + box.center);

                return position;
            }
            if (collider is SphereCollider sphereCollider)
            {
                return sphereCollider.transform.TransformPoint(sphereCollider.center +
                                                       Random.insideUnitSphere * sphereCollider.radius);
            }
            Debug.LogError("Collider does not have valid type or is not implemented "
                           + collider.GetType().Name, collider.gameObject);
            return collider.transform.TransformPoint(Random.insideUnitSphere);
        }

        public static float GetLocalVolume(this Collider collider)
        {
            if (collider is BoxCollider boxCollider)
            {
                Vector3 boxColliderSize = boxCollider.size;
                return boxColliderSize.x * boxColliderSize.y * boxColliderSize.z;
            }
            if (collider is SphereCollider sphereCollider)
            {
                return Mathf.Pow(sphereCollider.radius, 3) * Mathf.PI * 4f / 3f;
            }
            if (collider is CapsuleCollider capsuleCollider)
            {
                float diameter = capsuleCollider.radius * 2;
                return Mathf.PI * diameter * diameter * (3 * capsuleCollider.height - diameter) / 12;
            }
            if (collider is MeshCollider meshCollider)
            {
                float volume = 0f;
                Mesh mesh = meshCollider.sharedMesh;
                int[] triangles = mesh.triangles;
                Vector3[] vertices = mesh.vertices;
                for(int i = 0; i < triangles.Length; i += 3) {
                    Vector3 v1 = vertices[triangles[i]];
                    Vector3 v2 = vertices[triangles[i + 1]];
                    Vector3 v3 = vertices[triangles[i + 2]];

                    Vector3 cross = Vector3.Cross(v1, v2);

                    volume += Vector3.Dot(cross, v3);
                }

                return Mathf.Abs(volume * .5f);
            }
            Debug.LogError("Collider does not have valid type " + collider.GetType().Name,
                collider.gameObject);
            return 0f;
        }

        public static Bounds GetLocalBounds(this Collider collider)
        {
            if (collider is BoxCollider boxCollider)
            {
                return new Bounds(boxCollider.center, boxCollider.size);
            }
            if (collider is SphereCollider sphereCollider)
            {
                return new Bounds(sphereCollider.center, sphereCollider.radius * 2 * Vector3.one);
            }
            if (collider is CapsuleCollider capsuleCollider)
            {
                return new Bounds(capsuleCollider.center,
                    new Vector3(capsuleCollider.radius * 2, capsuleCollider.height, capsuleCollider.radius * 2));
            }
            if (collider is MeshCollider meshCollider)
            {
                Mesh sharedMesh = meshCollider.sharedMesh;
                return new Bounds(sharedMesh.bounds.center, sharedMesh.bounds.size);
            }
            Debug.LogError("Collider does not have valid type " + collider.GetType().Name,
                collider.gameObject);
            return new Bounds(Vector3.zero, Vector3.zero);
        }
    }
}

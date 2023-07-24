using UnityEngine;
using UnityEngine.AI;

namespace BrunoMikoski.Core
{
    public static class GizmoUtility
    {
        public static void DrawArrow(Vector3 from, Vector3 to, Vector3 up, Color color, float headSize = 0.1f)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(from, to);

            Vector3 fromTo = (from - to).normalized * headSize;
            Gizmos.DrawLine(to, to + Quaternion.AngleAxis(45, up) * fromTo);
            Gizmos.DrawLine(to, to + Quaternion.AngleAxis(-45, up) * fromTo);
            Gizmos.color = Color.white;
        }

        public static void DrawCollider(Collider collider)
        {
            if (collider is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
            }
            if (collider is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
            if (collider is CapsuleCollider)
            {
            }
            if (collider is MeshCollider meshCollider)
            {
                Gizmos.matrix = meshCollider.transform.localToWorldMatrix;
                Gizmos.DrawMesh(meshCollider.sharedMesh);
            }
        }

        public static void DrawNavMeshPath(NavMeshPath path)
        {
            Color targetColor;

            switch (path.status)
            {
                case NavMeshPathStatus.PathPartial:
                    targetColor = Color.yellow;
                    break;
                case NavMeshPathStatus.PathInvalid:
                    targetColor = Color.red;
                    break;
                default:
                    targetColor = Color.green;
                    break;
            }
            for (int i = 0; i < path.corners.Length - 1; i++)
                DrawArrow(path.corners[i], path.corners[i + 1], Vector3.up, targetColor);
        }
    }
}
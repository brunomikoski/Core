using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools
{
    public static partial class BoundsExtensions
    {
        private static Vector3[] pts = new Vector3[8];
        private static readonly Vector3[] ptScales = new Vector3[8]
        {
            new Vector3( 1.0f,  1.0f,  1.0f),
            new Vector3(-1.0f,  1.0f,  1.0f),
            new Vector3( 1.0f, -1.0f,  1.0f),
            new Vector3(-1.0f, -1.0f,  1.0f),
            new Vector3( 1.0f,  1.0f, -1.0f),
            new Vector3(-1.0f,  1.0f, -1.0f),
            new Vector3( 1.0f, -1.0f, -1.0f),
            new Vector3(-1.0f, -1.0f, -1.0f)
        };

        public static Vector3 GetPositionFromNormalized(this Bounds bounds, Vector3 normalized)
        {
            return bounds.center + Vector3.Scale(normalized, bounds.extents);
        }

        public static Vector3[] GetCorners(this Bounds bounds)
        {
            Vector3[] result = new Vector3[8];
            result[0] = bounds.min;
            result[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            result[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
            result[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            result[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            result[5] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            result[6] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            result[7] = bounds.max;
            return result;
        }

        public static void GetCorners(this Bounds bounds, List<Vector3> list)
        {
            list.Add(bounds.min);
            list.Add(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z));
            list.Add(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z));
            list.Add(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z));
            list.Add(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z));
            list.Add(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z));
            list.Add(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z));
            list.Add(bounds.max);
        }
        
        public static Bounds AddCollider(this Bounds bounds, Transform parentTransform, Collider collider, bool boundsInitialised)
        {
            Vector3 center = Vector3.zero;
            Vector3 halfSize = Vector3.zero;
            
            if (collider is BoxCollider boxCollider)
            {
                center = boxCollider.center;
                halfSize = boxCollider.size * 0.5f;
            }
            else if (collider is SphereCollider sphereCollider)
            {
                center = sphereCollider.center;
                halfSize = new Vector3(sphereCollider.radius, sphereCollider.radius, sphereCollider.radius);
            }
            else if (collider is CapsuleCollider capsuleCollider)
            {
                center = capsuleCollider.center;
                
                switch (capsuleCollider.direction)
                {
                    default:
                    case 0: halfSize = new Vector3(capsuleCollider.height * 0.5f, capsuleCollider.radius, capsuleCollider.radius); break;
                    case 1: halfSize = new Vector3(capsuleCollider.radius, capsuleCollider.height * 0.5f, capsuleCollider.radius); break;
                    case 2: halfSize = new Vector3(capsuleCollider.radius, capsuleCollider.radius, capsuleCollider.height * 0.5f); break;
                }
            }
            
            // Transform collider points into world space and then into the local parent space
            for (int i = 0; i < 8; i++)
            {
                pts[i] = center + Vector3.Scale(halfSize, ptScales[i]);
                pts[i] = collider.transform.TransformPoint(pts[i]);
                pts[i] = parentTransform.InverseTransformPoint(pts[i]);

                if (boundsInitialised == false)
                {
                    boundsInitialised = true;
                    bounds = new Bounds(pts[i], Vector3.zero);
                }
                else
                {
                    bounds.Encapsulate(pts[i]);
                }
            }
            
            return bounds;
        }
    }
}
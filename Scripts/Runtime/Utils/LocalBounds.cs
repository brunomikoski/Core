using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace BrunoMikoski.Core.DevelopmentTools
{
    public struct LocalBounds
    {
        private Transform transform;
        private Bounds bounds;

        public LocalBounds(Transform transform, Bounds bounds)
        {
            this.transform = transform;
            this.bounds = bounds;
        }

        public LocalBounds(Collider collider)
        {
            transform = collider.transform;
            bounds = collider.GetLocalBounds();
        }

        public void GetCorners(List<Vector3> corners)
        {
            bounds.GetCorners(corners);

            for (int i = 0; i < corners.Count; i++)
                corners[i] = transform.TransformPoint(corners[i]);
        }

        public bool Encapsulates(LocalBounds other)
        {
            using (ListPool<Vector3>.Get(out List<Vector3> corners))
            {
                other.GetCorners(corners);

                for (int i = 0; i < corners.Count; i++)
                {
                    Vector3 pointInThisSpace =
                        transform.InverseTransformPoint(corners[i]);

                    if (!bounds.Contains(pointInThisSpace))
                        return false;
                }

                return true;
            }
        }

        public bool Encapsulates(Collider collider)
        {
            return Encapsulates(new LocalBounds(collider));
        }
    }
}
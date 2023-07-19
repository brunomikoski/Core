using UnityEngine;

namespace BrunoMikoski.Core
{
    public static partial class TransformExtensions
    {
        /// <summary>
        /// Slightly altered version of Bunny83's solution:
        /// https://answers.unity.com/questions/799429/transformfindstring-no-longer-finds-grandchild.html
        ///
        /// Cheers mate!
        /// </summary>
        public static Transform FindRecursive(this Transform transform, string name)
        {
            Transform result = transform.Find(name);
            if (result != null)
                return result;

            foreach (Transform child in transform)
            {
                result = child.FindRecursive(name);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static void DestroyAllChildren(this Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; --i)
                Object.Destroy(t.GetChild(i).gameObject);
        }

        public static void DestroyAllChildrenImmediate(this Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; --i)
                Object.DestroyImmediate(t.GetChild(i).gameObject);
        }

        public static void SetAllChildrenActive(this Transform t, bool active)
        {
            for (int i = 0; i < t.childCount; ++i)
                t.GetChild(i).gameObject.SetActive(active);
        }

        public static Transform[] GetChildren(this Transform t)
        {
            Transform[] result = new Transform[t.childCount];

            for (int i = 0; i < t.childCount; ++i)
                result[i] = t.GetChild(i);

            return result;
        }

        public static Bounds TransformBounds(this Transform transform, Bounds localBounds)
        {
            Vector3 center = transform.TransformPoint(localBounds.center);

            // transform the local extents' axes
            Vector3 extents = localBounds.extents;
            Vector3 axisX = transform.TransformVector(extents.x, 0, 0);
            Vector3 axisY = transform.TransformVector(0, extents.y, 0);
            Vector3 axisZ = transform.TransformVector(0, 0, extents.z);

            // sum their absolute value to get the world extents
            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds { center = center, extents = extents };
        }

        public static Bounds InverseTransformBounds(this Transform transform, Bounds worldBounds)
        {
            Vector3 center = transform.InverseTransformPoint(worldBounds.center);

            // transform the world extents' axes
            Vector3 extents = worldBounds.extents;
            Vector3 axisX = transform.InverseTransformVector(extents.x, 0, 0);
            Vector3 axisY = transform.InverseTransformVector(0, extents.y, 0);
            Vector3 axisZ = transform.InverseTransformVector(0, 0, extents.z);

            // sum their absolute value to get the local extents
            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds { center = center, extents = extents };
        }

        public static void ResetLocal(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public static string GetPath(this Transform transform, Transform upToParent = null,
            string separator = "/")
        {
            if (transform == upToParent)
                return "";

            if (transform.parent == upToParent)
                return transform.name;

            return GetPath(transform.parent, upToParent, separator) + separator + transform.name;
        }
    }
}
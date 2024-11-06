using System.Collections.Generic;
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

        public static void SetScale(
            this Transform transform, float fraction)
        {
            SetScale(transform, 0, 1, fraction);
        }

        public static void SetScale(
            this Transform transform, float minimum, float maximum, float fraction)
        {
            if (transform == null)
                return;

            transform.localScale = Vector3.one * (
                Mathf.LerpUnclamped(
                    Mathf.Max(float.Epsilon, minimum), Mathf.Max(float.Epsilon, maximum),
                    fraction));
        }

        public static void SetScaleX(
            this Transform transform, float fraction)
        {
            SetScaleX(transform, 0, 1, fraction);
        }

        public static void SetScaleX(
            this Transform transform, float minimum, float maximum, float fraction)
        {
            if (transform == null)
                return;

            Vector3 localScale = transform.localScale;
            localScale.x = Mathf.LerpUnclamped(
                Mathf.Max(float.Epsilon, minimum), Mathf.Max(float.Epsilon, maximum), fraction);
            transform.localScale = localScale;
        }

        public static void SetScaleY(
            this Transform transform, float fraction)
        {
            SetScaleY(transform, 0, 1, fraction);
        }

        public static void SetScaleY(
            this Transform transform, float minimum, float maximum, float fraction)
        {
            if (transform == null)
                return;

            Vector3 localScale = transform.localScale;
            localScale.y = Mathf.LerpUnclamped(
                Mathf.Max(float.Epsilon, minimum), Mathf.Max(float.Epsilon, maximum), fraction);
            transform.localScale = localScale;
        }

        public static void SetScaleZ(
            this Transform transform, float fraction)
        {
            SetScaleZ(transform, 0, 1, fraction);
        }

        public static void SetScaleZ(
            this Transform transform, float minimum, float maximum, float fraction)
        {
            if (transform == null)
                return;

            Vector3 localScale = transform.localScale;
            localScale.z = Mathf.LerpUnclamped(
                Mathf.Max(float.Epsilon, minimum), Mathf.Max(float.Epsilon, maximum), fraction);
            transform.localScale = localScale;
        }

        public static RectTransform AsRectTransform(this Transform transform)
        {
            return (RectTransform)transform;
        }

        public static Vector3 InverseTransformPointInParent(this Transform transform, Vector3 point)
        {
            if (transform.parent.IsNull())
            {
                return point;
            }

            return transform.parent.InverseTransformPoint(point);
        }

        public static Vector3 TransformPointInParent(this Transform transform, Vector3 point)
        {
            if (transform.parent.IsNull())
            {
                return point;
            }

            return transform.parent.TransformPoint(point);
        }

        public static Vector3 InverseTransformDirectionInParent(this Transform transform, Vector3 point)
        {
            if (transform.parent.IsNull())
            {
                return point;
            }

            return transform.parent.InverseTransformDirection(point);
        }

        public static Vector3 TransformDirectionInParent(this Transform transform, Vector3 point)
        {
            if (transform.parent.IsNull())
            {
                return point;
            }

            return transform.parent.TransformDirection(point);
        }


        public static Vector3 InverseTransformVectorInParent(this Transform transform, Vector3 point)
        {
            if (transform.parent.IsNull())
            {
                return point;
            }

            return transform.parent.InverseTransformVector(point);
        }

        public static Vector3 TransformVectorInParent(this Transform transform, Vector3 point)
        {
            if (transform.parent.IsNull())
            {
                return point;
            }

            return transform.parent.TransformVector(point);
        }

        public static List<Transform> FindChildren(this Transform transform, string name)
        {
            List<Transform> result = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.Equals(name))
                    result.Add(child);
            }

            return result;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("CONTEXT/Transform/Reset without children")]
        private static void ResetWithoutChildren()
        {
            GameObject[] gameObjects = UnityEditor.Selection.gameObjects;
            foreach (GameObject go in gameObjects)
            {
                Transform selectedTransform = go.transform;
                ResetWithoutChildren(selectedTransform);
            }
        }

        private static void ResetWithoutChildren(Transform selectedTransform,
            bool recursive = false)
        {
            if (selectedTransform == null)
                return;

            UnityEditor.Undo.RecordObject(selectedTransform, "Reset Without Children");

            List<Transform> children = new List<Transform>();
            foreach (Transform child in selectedTransform)
            {
                UnityEditor.Undo.RecordObject(child, "Reset Without Children");
                children.Add(child);
            }

            foreach (Transform child in children)
            {
                child.SetParent(null, true);
            }

            selectedTransform.ResetLocal();

            foreach (Transform child in children)
            {
                child.SetParent(selectedTransform, true);
            }

            if (recursive)
            {
                foreach (Transform child in children)
                {
                    if (child.GetComponents<Component>().Length != 1)
                        continue;

                    if (UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) != null)
                        continue;

                    ResetWithoutChildren(child, true);
                }
            }
        }

        [UnityEditor.MenuItem("CONTEXT/Transform/Reset without children (recursive)")]
        private static void ResetWithoutChildrenRecursive()
        {
            GameObject[] gameObjects = UnityEditor.Selection.gameObjects;
            foreach (GameObject go in gameObjects)
            {
                Transform selectedTransform = go.transform;
                if (selectedTransform == null)
                    continue;

                ResetWithoutChildren(selectedTransform, true);
            }
        }
#endif
    }
}
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace BrunoMikoski.Core.DevelopmentTools
{
    public static class GameObjectExtensions
    {
        private static int INITIAL_SIZE = 100;

        private static readonly List<MeshFilter> meshFilterCache = new();
        private static readonly List<SkinnedMeshRenderer> skinnedMeshRendererCache = new(INITIAL_SIZE);
        private static readonly List<BoxCollider> boxCollidersCache = new(INITIAL_SIZE);


        [Flags]
        public enum SearchType
        {
            Self = 1,
            InParent = 2,
            InChildren = 4,
            All = Self | InParent | InChildren,
            SelfAndParent = Self | InParent
        }

        public static T GetOrAddComponent<T>(this GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null)
                component = target.AddComponent<T>();

            return component;
        }

        public static bool TryGetComponentInContext<T>(this GameObject targetGameObject, out T result, SearchType searchType = SearchType.All) where T : Component
        {
            if (searchType.HasFlag(SearchType.Self))
            {
                if (targetGameObject.TryGetComponent(out result))
                    return true;
            }

            if (searchType.HasFlag(SearchType.InChildren))
            {
                result = targetGameObject.GetComponentInChildren<T>();
                if (result != null)
                {
                    return true;
                }
            }

            if (searchType.HasFlag(SearchType.InParent))
            {
                result = targetGameObject.GetComponentInParent<T>();
                if (result != null)
                {
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static bool TryGetComponentInContext<T>(this Component targetComponent, out T result, SearchType searchType = SearchType.All) where T : Component
        {
            return TryGetComponentInContext<T>(targetComponent.gameObject, out result, searchType);
        }

        public static Bounds GetCompleteLocalBounds(this GameObject gameObject)
        {
            Transform transform = gameObject.transform;
            gameObject.GetComponentsInChildren(meshFilterCache);
            Bounds result = new Bounds(Vector3.zero, Vector3.zero);
            bool firstBoundsSet = false;

            for (int i = 0; i < meshFilterCache.Count; i++)
            {
                Mesh sharedMesh = meshFilterCache[i].sharedMesh;

                if (sharedMesh == null)
                    continue;

                Bounds bounds = GetLocalBounds(sharedMesh.bounds, meshFilterCache[i].transform, transform);

                if (firstBoundsSet)
                {
                    result.Encapsulate(bounds);
                }
                else
                {
                    result = bounds;
                    firstBoundsSet = true;
                }
            }

            gameObject.GetComponentsInChildren(skinnedMeshRendererCache);
            for (int i = 0; i < skinnedMeshRendererCache.Count; i++)
            {
                Bounds bounds = GetLocalBounds(skinnedMeshRendererCache[i].sharedMesh.bounds,
                    skinnedMeshRendererCache[i].transform, transform);

                if (firstBoundsSet)
                {
                    result.Encapsulate(bounds);
                }
                else
                {
                    result = bounds;
                    firstBoundsSet = true;
                }
            }

            return result;
        }


        private static Bounds GetLocalBounds(Bounds bounds, Transform from, Transform to)
        {
            Vector3 position = to.InverseTransformPoint(from.TransformPoint(bounds.center));

            Vector3 size = to.InverseTransformVector(from.TransformVector(bounds.size));

            return new Bounds(position, size);
        }

        public static bool IsDontDestroyOnLoad(this GameObject gameObject)
        {
            return string.Equals(gameObject.scene.name, "DontDestroyOnLoad", StringComparison.InvariantCulture);
        }

        public static Component GetOrAddComponent(this GameObject gameObject, Type componentType)
        {
            Component component = gameObject.GetComponent(componentType);
            if (component == null)
                component = gameObject.AddComponent(componentType);
            return component;
        }


        public static T[] GetComponentsInChildren<T>(this IList<GameObject> gameObjects, bool includeInactive = false)
        {
            List<T> results = new List<T>();
            for (int i = 0; i < gameObjects.Count; i++)
            {
                GameObject gameObject = gameObjects[i];
                results.AddRange(gameObject.GetComponentsInChildren<T>(includeInactive));
            }

            return results.ToArray();
        }

        /// <summary>
        /// Finds the component of type T either on the same GameObject, otherwise in the children,
        /// and if that does not yield results in the parent.
        /// </summary>
        /// <param name="go">gameObject to start the search</param>
        /// <param name="includeInactive">When searching children, include inactive gameObjects.
        ///  (inactive parents will not be searched anyway)</param>
        /// <typeparam name="T">The type of Component to retrieve.</typeparam>
        /// <returns>Returns the component of Type T if found, null if it doesn't.</returns>
        public static T GetComponentInContext<T>(this GameObject go, bool includeInactive = true)
            where T : Component
        {
            T result = go.GetComponent<T>();
            if (result != null)
                return result;

            result = go.GetComponentInChildren<T>(includeInactive);
            if (result != null)
                return result;

            result = go.GetComponentInParent<T>();
            return result;
        }


        public static Bounds GetBoundsBasedOnBoxColliders(this GameObject gameObject)
        {
            gameObject.GetComponentsInChildren(boxCollidersCache);

            int resultCount = boxCollidersCache.Count;
            if (resultCount == 0)
                return new Bounds();

            bool didSetInitialBounds = false;
            Bounds bounds = new Bounds();
            for (int i = 0; i < resultCount; ++i)
            {
                if (!didSetInitialBounds)
                {
                    bounds = boxCollidersCache[i].GetLocalBounds();
                    didSetInitialBounds = true;
                }

                bounds.Encapsulate(boxCollidersCache[i].GetLocalBounds());
            }

            if (!didSetInitialBounds)
                return new Bounds();

            return bounds;
        }

        public static bool IsSelectedInEditor(this GameObject gameObject)
        {
#if UNITY_EDITOR
            return UnityEditor.Selection.activeGameObject == gameObject;
#else
            return false;
#endif
        }

        public static bool IsActiveInPrefabHierarchy(this GameObject prefab)
        {
            while (true)
            {
                Assert.IsTrue(prefab.IsPrefab(), "Is not prefab");

                if (prefab.activeInHierarchy)
                    return true;

                if (prefab.transform.parent == null)
                    return prefab.activeSelf;

                prefab = prefab.transform.parent.gameObject;
            }
        }

        public static bool IsInScene(this GameObject gameObject)
        {
            if (gameObject.IsPrefab())
                return false;
#if UNITY_EDITOR && UNITY_2018_3_OR_NEWER

            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
                return false;

            if (UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) != null)
                return false;

#endif
            return true;
        }

        public static Bounds GetBoundsBasedOnRenderers(this GameObject gameObject,
            bool includeParticleRenderers = true)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();

            if (renderers == null)
                return new Bounds();

            bool didSetInitialBounds = false;
            Bounds bounds = new Bounds();
            for (int i = 0; i < renderers.Length; ++i)
            {
                if (!includeParticleRenderers && renderers[i] is ParticleSystemRenderer)
                    continue;

                if (!didSetInitialBounds)
                {
                    bounds = renderers[i].bounds;
                    didSetInitialBounds = true;
                }

                bounds.Encapsulate(renderers[i].bounds);
            }

            if (!didSetInitialBounds)
                return new Bounds();

            Bounds localBounds = gameObject.transform.InverseTransformBounds(bounds);
            return localBounds;
        }

        public static BoxCollider AddBoxColliderBasedOnRenderBounds(this GameObject gameObject)
        {
            Bounds bounds = gameObject.GetBoundsBasedOnRenderers();

            if (bounds == (default(Bounds)))
            {
                Debug.LogWarningFormat(
                    "Cannot add box collider to gameObject '{0}', it has no render bounds", gameObject.name);
                return null;
            }

            BoxCollider collider = gameObject.AddComponent<BoxCollider>();

            collider.center = bounds.center;
            collider.size = bounds.size;
            collider.isTrigger = true;

            return collider;
        }

        public static void NormalizeByFactor(this GameObject targetGameObject, float targetFactor)
        {
            targetGameObject.transform.localScale = new Vector3(1, 1, 1);
            targetGameObject.SetActive(true);
            Bounds bounds = targetGameObject.GetBoundsBasedOnRenderers();

            if (bounds == default(Bounds))
            {
                Debug.LogWarningFormat("Can't normalize an object without renderers {0} ", targetGameObject);
                return;
            }

            float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float finalScale = Mathf.Clamp(targetGameObject.transform.localScale.x * targetFactor / maxDimension, 0,
                float.MaxValue);

            targetGameObject.transform.localScale = new Vector3(finalScale, finalScale, finalScale);
        }

        public static void SetHideFlagsRecursively(this GameObject root, HideFlags hideFlags,
            bool replaceHideFlags = false)
        {
            Stack<GameObject> objectsToLock = new Stack<GameObject>();

            objectsToLock.Push(root);

            while (objectsToLock.Count > 0)
            {
                GameObject gameObject = objectsToLock.Pop();

                if (replaceHideFlags)
                {
                    gameObject.hideFlags = hideFlags;
                }
                else
                {
                    gameObject.hideFlags |= hideFlags;
                }

                Transform transform = gameObject.transform;
                int childCount = transform.childCount;

                for (int i = 0; i < childCount; ++i)
                {
                    objectsToLock.Push(transform.GetChild(i).gameObject);
                }
            }
        }

        public static void ChangeLayerRecursively(this GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            Transform[] children = gameObject.transform.GetChildren();

            for (int i = 0; i < children.Length; i++)
            {
                GameObject child = children[i].gameObject;
                child.ChangeLayerRecursively(layer);
            }
        }

        public static void DestroyAllChildren(this GameObject gameObject)
        {
            gameObject.transform.DestroyAllChildren();
        }

        /// <summary>
        /// Check if an object is a prefab, WORKS ALL THE TIME!
        /// PrefabUtility can be used in the editor for additional prefab functionality
        /// </summary>
        public static bool IsPrefab(this GameObject gameObject)
        {
            return !gameObject.scene.IsValid();
        }
        
        public static bool IsObjectOrPartOfObject(this GameObject sourceGameObject, GameObject targetGameObject)
        {
            return sourceGameObject == targetGameObject || targetGameObject.transform.IsChildOf(sourceGameObject.transform);
        }
    }
}

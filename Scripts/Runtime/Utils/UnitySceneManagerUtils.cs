using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Utils
{
    public static class UnitySceneManagerUtils
    {
        /// <summary>
        /// Finds all components of type <typeparam name="T"/> in the scene.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="includeInactive"></param>
        /// <returns></returns>
        public static T[] GetComponents<T>(this Scene scene, bool includeInactive) where T : Component
        {
            return GetComponents<T>(scene, includeInactive, out _);
        }

        /// <summary>
        /// Finds all components of type <typeparam name="T"/> in the scene.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="includeInactive"></param>
        /// <param name="rootObjects"></param>
        /// <returns></returns>
        public static T[] GetComponents<T>(this Scene scene, bool includeInactive, out GameObject[] rootObjects) where T : Component
        {
            rootObjects = scene.GetRootGameObjects();

            var partialResult = new List<T>();
            var result = new List<T>();

            foreach (var go in rootObjects)
            {
                // depth-first, according to docs and verified by our tests
                go.GetComponentsInChildren(includeInactive: includeInactive, partialResult);
                // AddRange accepts IEnumerable, so there would be an alloc
                foreach (var comp in partialResult)
                {
                    result.Add(comp);
                }
            }

            return result.ToArray();
        }

        private static readonly List<GameObject> _reusableGameObjectList = new List<GameObject>();

        /// <summary>
        /// Finds all components of type <typeparam name="T"/> in the scene.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="results"></param>
        /// <param name="includeInactive"></param>
        /// <returns></returns>
        public static void GetComponents<T>(this Scene scene, List<T> results, bool includeInactive) where T : Component
        {
            var rootObjects = _reusableGameObjectList;
            scene.GetRootGameObjects(rootObjects);
            results.Clear();

            var partialResult = new List<T>();

            foreach (var go in rootObjects)
            {
                // depth-first, according to docs and verified by our tests
                go.GetComponentsInChildren(includeInactive: includeInactive, partialResult);
                // AddRange accepts IEnumerable, so there would be an alloc
                foreach (var comp in partialResult)
                {
                    results.Add(comp);
                }
            }
        }

        /// <summary>
        /// Finds the first instance of type <typeparam name="T"/> in the scene. Returns null if no instance found.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="includeInactive"></param>
        /// <returns></returns>
        public static T FindComponent<T>(this Scene scene, bool includeInactive = false) where T : Component
        {
            var rootObjects = _reusableGameObjectList;
            scene.GetRootGameObjects(rootObjects);

            foreach (var go in rootObjects)
            {
                // depth-first, according to docs and verified by our tests
                var found = go.GetComponentInChildren<T>(includeInactive);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public static int GetSceneIndex(IList<string> scenePathsOrNames, string nameOrPath)
        {
            if (nameOrPath.IndexOf('/') >= 0)
            {
                return scenePathsOrNames.IndexOf(nameOrPath);
            }
            else
            {
                for (int i = 0; i < scenePathsOrNames.Count; ++i)
                {
                    var scenePath = scenePathsOrNames[i];
                    GetFileNameWithoutExtensionPosition(scenePath, out var nameIndex, out var nameLength);
                    if (nameLength == nameOrPath.Length && string.Compare(scenePath, nameIndex, nameOrPath, 0, nameLength, true) == 0)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        public static void GetFileNameWithoutExtensionPosition(string nameOrPath, out int index, out int length)
        {
            var lastSlash = nameOrPath.LastIndexOf('/');
            if (lastSlash >= 0)
            {
                index = lastSlash + 1;
            }
            else
            {
                index = 0;
            }

            var lastDot = nameOrPath.LastIndexOf('.');
            if (lastDot > index)
            {
                length = lastDot - index;
            }
            else
            {
                length = nameOrPath.Length - index;
            }
        }

        /// <summary>
        /// A comparer that can be used when scenes need to be comparable, like when being as keys in a dictionary.
        /// </summary>
        public class SceneEqualityComparer : IEqualityComparer<Scene>
        {
            /// <summary>
            /// Compares scenes by their <see cref="Scene.handle"/>
            /// </summary>
            public bool Equals(Scene x, Scene y)
            {
                return x.handle == y.handle;
            }


            /// <summary>
            /// Returns <see cref="Scene.handle"/>
            /// </summary>
            public int GetHashCode(Scene obj)
            {
                return obj.handle.GetHashCode();
            }
        }

        /// <summary>
        /// Returns true if <paramref name="scene"/> is in the build settings. Note that just checking <see cref="Scene.buildIndex"/> is not enough.
        /// </summary>
        public static bool IsAddedToBuildSettings(this Scene scene)
        {
            if (scene.buildIndex < 0)
            {
                return false;
            }

            // yep that's a thing: https://docs.unity3d.com/ScriptReference/SceneManagement.Scene-buildIndex.html
            if (scene.buildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only. Adds scene to the build settings.
        /// </summary>
        /// <returns>False if the scene is already added</returns>
        public static bool AddToBuildSettings(Scene scene)
        {
            if (IsAddedToBuildSettings(scene))
            {
                return false;
            }

            EditorBuildSettings.scenes =
                new[] { new EditorBuildSettingsScene(scene.path, true) }
                    .Concat(EditorBuildSettings.scenes)
                    .ToArray();

            Debug.Log($"Added '{scene.path}' as first entry in Build Settings.");
            return true;
        }
#endif

        /// <summary>
        /// Returns true if <paramref name="scene"/> can be unloaded.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public static bool CanBeUnloaded(this Scene scene)
        {
            if (!scene.isLoaded)
            {
                return false;
            }

            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s != scene && s.isLoaded)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the raw handle of a <paramref name="scene"/>
        /// </summary>
        public static ulong GetRawHandle(this Scene scene)
        {
#if UNITY_6000_4_OR_NEWER
            return scene.handle.GetRawData();
#else
      return (ulong)scene.handle;
#endif
        }

        /// <summary>
        /// Compares the object id to the internal handle of <paramref name="scene"/>
        /// </summary>
        public static int CompareRawHandle(this Scene scene, EntityId id)
        {
#if UNITY_6000_4_OR_NEWER
            var idRaw = EntityId.ToULong(id);
#elif UNITY_6000_3_OR_NEWER
      var idRaw = (ulong)(int)id;
#else
      var idRaw = (ulong)id;
#endif
            return scene.GetRawHandle().CompareTo(idRaw);
        }

        /// <summary>
        /// Converts <paramref name="scene"/> into string, dumping all its fields and properties.
        /// </summary>
        public static string Dump(this Scene scene)
        {
            StringBuilder result = new StringBuilder();

            result.Append("[UnityScene:");

            if (scene.IsValid())
            {
                result.Append(scene.name);
                result.Append(", isLoaded:").Append(scene.isLoaded);
                result.Append(", buildIndex:").Append(scene.buildIndex);
                result.Append(", isDirty:").Append(scene.isDirty);
                result.Append(", path:").Append(scene.path);
                result.Append(", rootCount:").Append(scene.rootCount);
                result.Append(", isSubScene:").Append(scene.isSubScene);
            }
            else
            {
                result.Append("<Invalid>");
            }

            result.Append(", handle:").Append(scene.handle.ToString());
            result.Append("]");
            return result.ToString();
        }

        /// <summary>
        /// Converts <paramref name="loadSceneParameters"/> to string.
        /// </summary>
        public static string Dump(this LoadSceneParameters loadSceneParameters)
        {
            return $"[LoadSceneParameters: {loadSceneParameters.loadSceneMode}, localPhysicsMode:{loadSceneParameters.localPhysicsMode}]";
        }

        /// <summary>
        /// Gets scene's build index based on its name or path.
        /// </summary>
        public static int GetSceneBuildIndex(string nameOrPath)
        {
            if (nameOrPath.IndexOf('/', StringComparison.Ordinal) >= 0)
            {
                return SceneUtility.GetBuildIndexByScenePath(nameOrPath);
            }
            else
            {
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; ++i)
                {
                    var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    var sceneName = GetFileNameWithoutExtension(scenePath);
                    if (sceneName.Equals(nameOrPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        internal static ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> nameOrPath)
        {
            var lastSlash = nameOrPath.LastIndexOf('/');
            int index = 0;
            if (lastSlash >= 0)
            {
                index = lastSlash + 1;
            }
            else
            {
                index = 0;
            }

            var lastDot = nameOrPath.LastIndexOf('.');
            if (lastDot >= index)
            {
                return nameOrPath.Slice(index, lastDot - index);
            }
            else
            {
                return nameOrPath.Slice(index);
            }
        }

        /// <summary>
        /// Gets all the root GameObjects, sorted by their simbling index.
        /// </summary>
        public static void GetRootGameObjectsInHierarchyOrder(this Scene scene, List<GameObject> rootGameObjects)
        {
            scene.GetRootGameObjects(rootGameObjects ?? throw new ArgumentNullException(nameof(rootGameObjects)));
            // roots are not served in any order, it seems; sort them by sibling index
            rootGameObjects.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        }

        /// <summary>
        /// Gets a component on a scene. If there are none or more than one, throws an exception.
        /// </summary>
        public static T GetSingleComponentOrThrow<T>(this Scene scene, bool includeInactive = false) where T : Component
        {
            using (ListPool<GameObject>.Get(out var roots))
            {
                // order does not matter
                scene.GetRootGameObjects(roots);

                T result = null;

                foreach (var root in roots)
                {
                    if (!includeInactive && !root.activeInHierarchy)
                    {
                        continue;
                    }

                    var component = root.GetComponentInChildren<T>(includeInactive: includeInactive);
                    if (component)
                    {
                        if (result)
                        {
                            throw new InvalidOperationException($"Multiple components of type {typeof(T).FullName} found");
                        }
                        else
                        {
                            result = component;
                        }
                    }
                }

                if (result == null)
                {
                    throw new InvalidOperationException($"Components of type {typeof(T).FullName} not found");
                }

                return result;
            }
        }

        /// <summary>
        /// Gets all the component present on a scene, depth first.
        /// </summary>
        public static T GetComponentInHierarchyOrder<T>(this Scene scene, bool includeInactive = false) where T : class
        {
            using (ListPool<GameObject>.Get(out var roots))
            {
                scene.GetRootGameObjectsInHierarchyOrder(roots);
                foreach (var root in roots)
                {
                    if (!includeInactive && !root.activeInHierarchy)
                    {
                        continue;
                    }

                    var result = root.GetComponentInChildren<T>(includeInactive: includeInactive);
                    if (result != null)
                    {
                        return result;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets all the component present on a scene, depth first.
        /// </summary>
        public static T[] GetComponentsInHierarchyOrder<T>(this Scene scene, bool includeInactive = false) where T : class
        {
            using (ListPool<GameObject>.Get(out var roots))
            {
                scene.GetRootGameObjectsInHierarchyOrder(roots);
                return GetComponentsInHierarchyOrder<T>(roots, includeInactive);
            }
        }

        /// <summary>
        /// Gets all the component present on a scene, depth first.
        /// </summary>
        public static T[] GetComponentsInHierarchyOrder<T>(IList<GameObject> roots, bool includeInactive = false) where T : class
        {
            using (ListPool<T>.Get(out var partialResults))
            using (ListPool<T>.Get(out var fullResults))
            {
                foreach (var root in roots)
                {
                    if (!includeInactive && !root.activeInHierarchy)
                    {
                        continue;
                    }

                    partialResults.Clear();
                    root.GetComponentsInChildren<T>(includeInactive: includeInactive, partialResults);
                    fullResults.AddRange(partialResults);
                }

                return fullResults.ToArray();
            }
        }
    }
}

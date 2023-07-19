using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrunoMikoski.Core
{
    public static class PoolListExtensions
    {
        public static PoolList<T> GetComponentsPooled<T>(this Component target)
        {
            PoolList<T> list = PoolList<T>.Obtain();
            target.GetComponents<T>(list);
            return list;
        }

        public static PoolList<T> GetComponentsInChildrenPooled<T>(this Component target, bool
            includeInactive = false)
        {
            PoolList<T> list = PoolList<T>.Obtain();
            target.GetComponentsInChildren<T>(includeInactive, list);
            return list;
        }

        public static PoolList<T> GetComponentsInParentPooled<T>(this Component target, bool
            includeInactive = false)
        {
            PoolList<T> list = PoolList<T>.Obtain();
            target.GetComponentsInParent<T>(includeInactive, list);
            return list;
        }

        public static PoolList<T> GetComponentsPooled<T>(this GameObject target)
        {
            PoolList<T> list = PoolList<T>.Obtain();
            target.GetComponents<T>(list);
            return list;
        }

        public static PoolList<T> GetComponentsInChildrenPooled<T>(this GameObject target, bool
            includeInactive = false)
        {
            PoolList<T> list = PoolList<T>.Obtain();
            target.GetComponentsInChildren<T>(includeInactive, list);
            return list;
        }

        public static PoolList<T> GetComponentsInParentPooled<T>(this GameObject target, bool
            includeInactive = false)
        {
            PoolList<T> list = PoolList<T>.Obtain();
            target.GetComponentsInParent<T>(includeInactive, list);
            return list;
        }

        public static int GetComponentsCount<T>(this Component target)
        {
            using (PoolList<T> list = target.GetComponentsPooled<T>())
            {
                return list.Count;
            }
        }

        public static int GetComponentsInChildrenCount<T>(this Component target, bool
            includeInactive = false)
        {
            using (PoolList<T> list = target.GetComponentsInChildrenPooled<T>(includeInactive))
            {
                return list.Count;
            }
        }

        public static int GetComponentsInParentCount<T>(this Component target, bool
            includeInactive = false)
        {
            using (PoolList<T> list = target.GetComponentsInParentPooled<T>(includeInactive))
            {
                return list.Count;
            }
        }

        public static int GetComponentsCount<T>(this GameObject target)
        {
            using (PoolList<T> list = target.GetComponentsPooled<T>())
            {
                return list.Count;
            }
        }

        public static int GetComponentsInChildrenCount<T>(this GameObject target, bool
            includeInactive = false)
        {
            using (PoolList<T> list = target.GetComponentsInChildrenPooled<T>(includeInactive))
            {
                return list.Count;
            }
        }

        public static int GetComponentsInParentCount<T>(this GameObject target, bool
            includeInactive = false)
        {
            using (PoolList<T> list = target.GetComponentsInParentPooled<T>(includeInactive))
            {
                return list.Count;
            }
        }

        public static PoolList<T> GetComponentsPooled<T>(this Scene scene,
            bool includeInactive = false)
        {
            PoolList<T> result = PoolList<T>.Obtain();
            using (PoolList<GameObject> roots = PoolList<GameObject>.Obtain())
            {
                scene.GetRootGameObjects(roots);
                for (int i = 0; i < roots.Count; i++)
                {
                    GameObject root = roots[i];
                    using (PoolList<T> children = root.GetComponentsInChildrenPooled<T>(includeInactive))
                    {
                        result.AddRange(children);
                    }
                }
            }
            return result;
        }

        public static PoolList<GameObject> GetRootGameObjectsPooled(this Scene scene)
        {
            PoolList<GameObject> result = PoolList<GameObject>.Obtain();
            scene.GetRootGameObjects(result);
            return result;
        }
    }
}
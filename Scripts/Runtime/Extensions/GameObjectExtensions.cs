using UnityEngine;

namespace BrunoMikoski.Core
{
    public static class GameObjectExtensions
    {
        public static T GetOrAddComponent<T>(this GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null)
                component = target.AddComponent<T>();

            return component;
        }
    }
}
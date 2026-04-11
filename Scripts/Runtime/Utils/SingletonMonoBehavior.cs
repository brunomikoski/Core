using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.Core
{
    /// <summary>
    /// Helper class for singleton reset on domain reload. Must be non-generic because
    /// [RuntimeInitializeOnLoadMethod] cannot be used on methods in generic classes.
    /// </summary>
    internal static class SingletonMonoBehaviourRegistry
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            foreach (var reset in _resetActions)
            {
                reset?.Invoke();
            }
            _resetActions.Clear();
        }

        private static readonly List<Action> _resetActions = new();

        public static void RegisterReset(Action reset)
        {
            _resetActions.Add(reset);
        }
    }

    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : Component
    {
        private static bool hasInstance;
        private static T instance;

        static SingletonMonoBehaviour()
        {
            SingletonMonoBehaviourRegistry.RegisterReset(() =>
            {
                instance = null;
                hasInstance = false;
            });
        }
        public static T Instance
        {
            get
            {
                if (!hasInstance)
                {
#if UNITY_6000_0_OR_NEWER
                    instance = FindAnyObjectByType<T>();
#else
                    instance = FindObjectOfType<T>();
#endif
                    if (instance == null)
                    {
                        GameObject obj = new GameObject
                        {
                            name = $"{typeof(T).Name} (Singleton)"
                        };
                        instance = obj.AddComponent<T>();
                    }

                    hasInstance = instance != null;
                }

                return instance;
            }
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                hasInstance = true;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}

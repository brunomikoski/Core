using System;
using System.Collections.Generic;
using BrunoMikoski.ServicesLocation;
using UnityEngine;

namespace BrunoMikoski.Core
{
    public sealed class PoolList<T> : List<T>, IDisposable
    {
#if DEBUG
        private static readonly List<PoolList<T>> shortTermLists = new List<PoolList<T>>();
#endif
        private static readonly Stack<PoolList<T>> lists = new Stack<PoolList<T>>();

#if DEBUG
        private static bool isInitialized;
        private static ServiceReference<MonoBehaviourService> MonoBehaviourService = new();
        private static void Initialize()
        {
            if (isInitialized)
                return;

            MonoBehaviourService.Reference.OnUpdate += OnUpdate;

            isInitialized = true;
        }
#endif

        private PoolList() : base()
        {
        }

        private PoolList(int capacity) : base(capacity)
        {
        }

        public static PoolList<T> Obtain(int capacity = 20)
        {
            return Obtain(true, capacity);
        }

        public static PoolList<T> ObtainLongTerm(int capacity = 20)
        {
            return Obtain(false, capacity);
        }

        private static PoolList<T> Obtain(bool shortTerm, int capacity)
        {
#if DEBUG
            Initialize();
#endif

            PoolList<T> result;
            if (lists.Count > 0)
            {
                result = lists.Pop();
            }
            else
            {
                result = new PoolList<T>(capacity);
            }

#if DEBUG
            if (shortTerm)
                shortTermLists.Add(result);
#endif
            return result;
        }


        private static void Release(PoolList<T> poolList)
        {
            poolList.Clear();
            lists.Push(poolList);
#if DEBUG
            shortTermLists.Remove(poolList);
#endif
        }

        public void Dispose()
        {
            Release(this);
        }

#if DEBUG
        public static void OnUpdate()
        {
            if (shortTermLists.Count == 0)
                return;

            for (int i = 0; i < shortTermLists.Count; i++)
            {
                PoolList<T> list = shortTermLists[i];
                Debug.LogErrorFormat("Short term list of type {0} was not disposed: {1}", typeof(T), list);
            }
        }
#endif
    }
}
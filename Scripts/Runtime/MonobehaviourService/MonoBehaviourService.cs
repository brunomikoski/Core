using System;
using System.Collections.Generic;
using System.Reflection;
using BrunoMikoski.ServicesLocation;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;


namespace BrunoMikoski.Core.DevelopmentTools
{
    [ServiceImplementation]
    public sealed class MonoBehaviourService
    {
        public enum UpdateType {
            Update,
            LateUpdate,
            FixedUpdate
        }

        public class UpdateSubscription : IEquatable<UpdateSubscription> {
            public Action<float> Callback;
            public UpdateType UpdateType;
            public int FrameInterval;
            public bool SparseCalls;
            public int FrameCounter;
            public int InstanceID;
            public float CumulativeDeltaTime;
            public int UpdateGroup;
            public bool PendingRemoval;
            public ProfilerMarker Marker;

            public bool Equals(UpdateSubscription other) {
                return other != null && InstanceID == other.InstanceID;
            }

            public override bool Equals(object obj) {
                return obj is UpdateSubscription other && Equals(other);
            }

            public override int GetHashCode() {
                return InstanceID;
            }

            public void Reset()
            {
                Callback = null;
                UpdateType = UpdateType.Update;
                FrameInterval = 1;
                SparseCalls = true;
                FrameCounter = 0;
                InstanceID = 0;
                CumulativeDeltaTime = 0f;
                UpdateGroup = 0;
                PendingRemoval = false;
                Marker = default;
            }
        }


        private static bool isQuitting;
        public static bool IsQuitting
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    return true;
#endif
                return isQuitting;
            }
        }

        private readonly List<UpdateSubscription> _updateListeners = new(256);
        private readonly List<UpdateSubscription> _lateUpdateListeners = new(128);
        private readonly List<UpdateSubscription> _fixedUpdateListeners = new(512);

        private readonly ObjectPool<UpdateSubscription> subscriptionPool;
        private int nextSubscriptionID = 1;
        private int updateFrameIdx;
        private int lateUpdateFrameIdx;
        private int fixedUpdateFrameIdx;
        private MonoBehaviourServiceRunner runner;

        private readonly Dictionary<long, int[]> sparseGroupCounts = new();
        private List<UpdateSubscription> currentlyIterating;
        private readonly List<UpdateSubscription> pendingRemovals = new(16);


        public MonoBehaviourService()
        {
            isQuitting = false;
            GameObject go = new(nameof(MonoBehaviourService)) {
                hideFlags = HideFlags.DontSave
            };
            // DontDestroyOnLoad throws outside play mode (edit-mode tests construct the service directly).
            if (Application.isPlaying)
                Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<MonoBehaviourServiceRunner>();
            runner.Initialize(this);

            subscriptionPool = new ObjectPool<UpdateSubscription>(
                () => new UpdateSubscription(),
                _ => { },
                sub => sub.Reset(),
                _ => { },
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                true,
#else
                false,
#endif
                256,
                4096);
        }


        private void ReleaseAllSubscriptions(List<UpdateSubscription> list) {
            for (int i = list.Count - 1; i >= 0; --i) {
                subscriptionPool.Release(list[i]);
            }
            list.Clear();
        }

        private List<UpdateSubscription> GetListForType(UpdateType updateType)
        {
            switch (updateType)
            {
                case UpdateType.Update:
                    return _updateListeners;
                case UpdateType.LateUpdate:
                    return _lateUpdateListeners;
                case UpdateType.FixedUpdate:
                    return _fixedUpdateListeners;
                default:
                    throw new ArgumentOutOfRangeException(nameof(updateType), updateType, null);
            }
        }

        private static long GetSparseGroupKey(UpdateType updateType, int frameInterval)
        {
            return ((long)updateType << 32) | (uint)frameInterval;
        }

        private int AcquireSparseGroup(UpdateType updateType, int frameInterval)
        {
            long key = GetSparseGroupKey(updateType, frameInterval);
            if (!sparseGroupCounts.TryGetValue(key, out int[] counts))
            {
                counts = new int[frameInterval];
                sparseGroupCounts.Add(key, counts);
            }

            int group = 0;
            for (int i = 1; i < counts.Length; ++i)
            {
                if (counts[i] < counts[group])
                    group = i;
            }

            counts[group]++;
            return group;
        }

        private void ReleaseSparseGroup(UpdateSubscription subscription)
        {
            if (subscription.FrameInterval <= 1 || !subscription.SparseCalls)
                return;

            long key = GetSparseGroupKey(subscription.UpdateType, subscription.FrameInterval);
            if (sparseGroupCounts.TryGetValue(key, out int[] counts) && counts[subscription.UpdateGroup] > 0)
                counts[subscription.UpdateGroup]--;
        }

        public UpdateSubscription SubscribeToUpdate(Action<float> targetCallback, UpdateType targetUpdateType,
            int frameInterval = 1, bool sparseCalls = true)
        {
            if (frameInterval < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(frameInterval), "Frame interval must be greater than 0.");
            }

            List<UpdateSubscription> list = GetListForType(targetUpdateType);

#if DEBUG
            for (int i = 0; i < list.Count; ++i)
            {
                UpdateSubscription existing = list[i];
                if (!existing.PendingRemoval && existing.Callback == targetCallback)
                {
                    throw new InvalidOperationException(
                        $"SubscribeToUpdate called twice for {targetCallback.Method.DeclaringType?.Name}.{targetCallback.Method.Name} on {targetUpdateType}.");
                }
            }
#endif

            int instanceID = nextSubscriptionID++;

            UpdateSubscription sub = subscriptionPool.Get();
            sub.Callback = targetCallback;
            sub.UpdateType = targetUpdateType;
            sub.FrameInterval = frameInterval;
            sub.SparseCalls = sparseCalls;
            sub.FrameCounter = 0;
            sub.InstanceID = instanceID;
            sub.CumulativeDeltaTime = 0f;
            sub.UpdateGroup = sparseCalls && frameInterval > 1 ? AcquireSparseGroup(targetUpdateType, frameInterval) : 0;
#if ENABLE_PROFILER
            sub.Marker = GetMarkerForCallback(targetCallback);
#endif

            list.Add(sub);
#if UNITY_EDITOR
            MonoBehaviourServiceTracker.TrackSubscription(sub);
#endif
            return sub;
        }

        public void UnsubscribeFromUpdate(UpdateSubscription subscription)
        {
            if (subscription == null || subscription.PendingRemoval)
                return;

            List<UpdateSubscription> list = GetListForType(subscription.UpdateType);
            if (list == currentlyIterating)
            {
                if (list.Contains(subscription))
                {
                    subscription.PendingRemoval = true;
                    pendingRemovals.Add(subscription);
                }
            }
            else if (list.Remove(subscription))
            {
                ReleaseSubscription(subscription);
            }
        }

        public void UnsubscribeFromUpdate(Action<float> targetCallback) {
            UnsubscribeFromUpdateInList(_updateListeners, targetCallback);
            UnsubscribeFromUpdateInList(_lateUpdateListeners, targetCallback);
            UnsubscribeFromUpdateInList(_fixedUpdateListeners, targetCallback);
        }

        private void UnsubscribeFromUpdateInList(List<UpdateSubscription> list, Action<float> targetCallback)
        {
            bool iterating = list == currentlyIterating;
            for (int i = list.Count - 1; i >= 0; --i)
            {
                UpdateSubscription sub = list[i];
                if (sub.PendingRemoval || sub.Callback != targetCallback)
                    continue;

                if (iterating)
                {
                    sub.PendingRemoval = true;
                    pendingRemovals.Add(sub);
                }
                else
                {
                    list.RemoveAt(i);
                    ReleaseSubscription(sub);
                }
            }
        }

        private void ReleaseSubscription(UpdateSubscription subscription)
        {
            ReleaseSparseGroup(subscription);
#if UNITY_EDITOR
            MonoBehaviourServiceTracker.UntrackSubscription(subscription);
#endif
            subscriptionPool.Release(subscription);
        }

#if ENABLE_PROFILER
        private static readonly Dictionary<MethodInfo, ProfilerMarker> callbackMarkers = new(128);

        private static ProfilerMarker GetMarkerForCallback(Action<float> targetCallback)
        {
            MethodInfo method = targetCallback.Method;
            if (callbackMarkers.TryGetValue(method, out ProfilerMarker marker))
                return marker;

            string markerName = (method.DeclaringType?.Name ?? "?") + "." + method.Name;
            marker = new ProfilerMarker(ProfilerCategory.Scripts, markerName);
            callbackMarkers.Add(method, marker);
            return marker;
        }
#endif

        internal void Update()
        {
            HandleUpdate(_updateListeners, updateFrameIdx, Time.deltaTime);
            updateFrameIdx++;
        }

        internal void FixedUpdate()
        {
            HandleUpdate(_fixedUpdateListeners, fixedUpdateFrameIdx, Time.fixedDeltaTime);
            fixedUpdateFrameIdx++;
        }

        internal void LateUpdate()
        {
            HandleUpdate(_lateUpdateListeners, lateUpdateFrameIdx, Time.deltaTime);
            lateUpdateFrameIdx++;
        }

        private void HandleUpdate(List<UpdateSubscription> listeners, int globalFrameIndex, float deltaTime)
        {
            Debug.Assert(currentlyIterating == null, "MonoBehaviourService update passes must not nest.");
            currentlyIterating = listeners;

            int count = listeners.Count;
            for (int i = 0; i < count; ++i)
            {
                UpdateSubscription sub = listeners[i];
                if (sub.PendingRemoval)
                    continue;

                sub.CumulativeDeltaTime += deltaTime;

                bool shouldCall;

                if (sub.FrameInterval == 1)
                {
                    shouldCall = true;
                }
                else if (sub.SparseCalls)
                {
                    shouldCall = (globalFrameIndex % sub.FrameInterval) == sub.UpdateGroup;
                }
                else
                {
                    sub.FrameCounter++;
                    shouldCall = sub.FrameCounter >= sub.FrameInterval;
                }

                if (!shouldCall)
                    continue;

                Action<float> callback = sub.Callback;
                ProfilerMarker marker = sub.Marker;
                float callbackDeltaTime = sub.CumulativeDeltaTime;
                sub.CumulativeDeltaTime = 0f;
                sub.FrameCounter = 0;

                marker.Begin();
                try
                {
                    callback.Invoke(callbackDeltaTime);
                }
                catch (Exception exception)
                {
                    HandleCallbackException(sub, callback, exception);
                }
                finally
                {
                    marker.End();
                }
            }

            currentlyIterating = null;
            FlushPendingRemovals(listeners);
        }

        private void HandleCallbackException(UpdateSubscription subscription, Action<float> callback, Exception exception)
        {
            Debug.LogException(exception);

            if (callback.Target is Object unityTarget && unityTarget == null)
            {
                Debug.LogError(
                    $"MonoBehaviourService: {callback.Method.DeclaringType?.Name}.{callback.Method.Name} is subscribed but its target is destroyed; removing leaked subscription.");
                if (!subscription.PendingRemoval)
                {
                    subscription.PendingRemoval = true;
                    pendingRemovals.Add(subscription);
                }
            }
        }

        private void FlushPendingRemovals(List<UpdateSubscription> listeners)
        {
            if (pendingRemovals.Count == 0)
                return;

            for (int i = 0; i < pendingRemovals.Count; ++i)
            {
                UpdateSubscription sub = pendingRemovals[i];
                listeners.Remove(sub);
                ReleaseSubscription(sub);
            }

            pendingRemovals.Clear();
        }

        internal void OnApplicationQuit()
        {
            isQuitting = true;

            pendingRemovals.Clear();
            ReleaseAllSubscriptions(_updateListeners);
            ReleaseAllSubscriptions(_lateUpdateListeners);
            ReleaseAllSubscriptions(_fixedUpdateListeners);
            sparseGroupCounts.Clear();

            if (runner)
            {
                Object.Destroy(runner.gameObject);
                runner = null;
            }

            subscriptionPool.Clear();
        }

#if UNITY_EDITOR
        public IReadOnlyList<UpdateSubscription> GetUpdateListenersForTracking(UpdateType updateType) => GetListForType(updateType);

        public void GetAllUpdateListenersForTracking(List<UpdateSubscription> result)
        {
            result.Clear();
            result.AddRange(_updateListeners);
            result.AddRange(_lateUpdateListeners);
            result.AddRange(_fixedUpdateListeners);
        }
#endif
    }
}

using System;
using System.Collections.Generic;
using BrunoMikoski.ServicesLocation;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;


namespace BrunoMikoski.Core
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
            public IUpdateListener Listener;
            public UpdateType UpdateType;
            public int FrameInterval;
            public bool SparseCalls;
            public int FrameCounter;
            public int InstanceID;
            public float CumulativeDeltaTime;
            public int UpdateGroup;

            public bool Equals(UpdateSubscription other) {
                return InstanceID == other.InstanceID;
            }

            public override bool Equals(object obj) {
                return obj is UpdateSubscription other && Equals(other);
            }

            public override int GetHashCode() {
                return InstanceID;
            }

            public void Reset() {
                Listener = null;
                UpdateType = UpdateType.Update;
                FrameInterval = 1;
                SparseCalls = true;
                FrameCounter = 0;
                InstanceID = 0;
                CumulativeDeltaTime = 0f;
                UpdateGroup = 0;
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
        
        private readonly List<UpdateSubscription> updateListeners = new(256);
        private readonly ObjectPool<UpdateSubscription> subscriptionPool;
        private int nextSubscriptionID;
        private int updateFrameIdx;
        private int lateUpdateFrameIdx;
        private int fixedUpdateFrameIdx;
        private MonoBehaviourServiceRunner runner;
        
        
        public MonoBehaviourService()
        {
            isQuitting = false;
            GameObject go = new(nameof(MonoBehaviourService)) {
                hideFlags = HideFlags.DontSave
            };
            Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<MonoBehaviourServiceRunner>();
            runner.Initialize(this);
            isQuitting = false;

            subscriptionPool = new ObjectPool<UpdateSubscription>(
                () => new UpdateSubscription(),
                _ => { },
                sub => sub.Reset(),
                _ => { },
                true,
                256,
                4096);
        }

        ~MonoBehaviourService()
        {
            Deconstruct();
        }

        private void Deconstruct()
        {
            isQuitting = true;
            Object.Destroy(runner.gameObject);

            for (int i = updateListeners.Count - 1; i >= 0; --i) {
                subscriptionPool.Release(updateListeners[i]);
            }

            updateListeners.Clear();

            if (runner) {
                Object.Destroy(runner.gameObject);
                runner = null;
            }
            subscriptionPool.Clear();
        }

        public UpdateSubscription SubscribeToUpdate(IUpdateListener listener, UpdateType targetUpdateType,
            int frameInterval = 1, bool sparseCalls = true) {
            if (frameInterval < 1) {
                throw new ArgumentOutOfRangeException(nameof(frameInterval), "Frame interval must be greater than 0.");
            }

            int instanceID = nextSubscriptionID++;
            int updateGroup = instanceID % frameInterval;

            UpdateSubscription sub = subscriptionPool.Get();
            sub.Listener = listener;
            sub.UpdateType = targetUpdateType;
            sub.FrameInterval = frameInterval;
            sub.SparseCalls = sparseCalls;
            sub.FrameCounter = 0;
            sub.InstanceID = instanceID;
            sub.CumulativeDeltaTime = 0f;
            sub.UpdateGroup = updateGroup;

            updateListeners.Add(sub);
            return sub;
        }

        public void UnsubscribeFromUpdate(UpdateSubscription subscription) {
            if (updateListeners.Remove(subscription)) {
                subscriptionPool.Release(subscription);
            }
        }

        public void UnsubscribeFromUpdate(IUpdateListener listener) {
            for (int i = updateListeners.Count - 1; i >= 0; --i) {
                UpdateSubscription sub = updateListeners[i];
                if (sub.Listener == listener) {
                    updateListeners.RemoveAt(i);
                    subscriptionPool.Release(sub);
                    break;
                }
            }
        }

        internal void Update() {
            HandleUpdate(UpdateType.Update, updateFrameIdx, Time.deltaTime);
            updateFrameIdx++;
        }

        internal void FixedUpdate() {
            HandleUpdate(UpdateType.FixedUpdate, fixedUpdateFrameIdx, Time.fixedDeltaTime);
            fixedUpdateFrameIdx++;
        }

        internal void LateUpdate() {
            HandleUpdate(UpdateType.LateUpdate, lateUpdateFrameIdx, Time.deltaTime);
            lateUpdateFrameIdx++;
        }

        private void HandleUpdate(UpdateType updateType, int globalFrameIndex, float deltaTime) {
            for (int i = 0; i < updateListeners.Count; ++i) {
                UpdateSubscription sub = updateListeners[i];
                if (sub.UpdateType != updateType) {
                    continue;
                }

                sub.CumulativeDeltaTime += deltaTime;

                bool shouldCall;

                if (sub.FrameInterval == 1) {
                    shouldCall = true;
                } else if (sub.SparseCalls) {
                    shouldCall = (globalFrameIndex % sub.FrameInterval) == sub.UpdateGroup;
                } else {
                    sub.FrameCounter++;
                    shouldCall = sub.FrameCounter >= sub.FrameInterval;
                }

                if (shouldCall) {
                    sub.Listener.OnUpdate(sub.CumulativeDeltaTime);
                    sub.CumulativeDeltaTime = 0f;
                    sub.FrameCounter = 0;
                }
            }
        }

        internal void OnApplicationQuit() {
            isQuitting = true;
        }
    }
}
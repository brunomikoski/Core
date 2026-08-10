using System;
using System.Collections.Generic;
using System.Diagnostics;
using BrunoMikoski.Core.DevelopmentTools.Routines;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools
{
    public static class MonoBehaviourServiceTracker
    {
        public struct SubscriptionTrackingData
        {
            public readonly int InstanceID;
            public readonly string CallbackName;
            public readonly string CallbackTargetType;
            public readonly MonoBehaviourService.UpdateType UpdateType;
            public readonly bool SparseCalls;
            public readonly int FrameInterval;
            public readonly int UpdateGroup;
            public readonly string StackTrace;
            public readonly DateTime Created;

            public SubscriptionTrackingData(
                int instanceID,
                string callbackName,
                string callbackTargetType,
                MonoBehaviourService.UpdateType updateType,
                bool sparseCalls,
                int frameInterval,
                int updateGroup,
                string stackTrace,
                DateTime created)
            {
                InstanceID = instanceID;
                CallbackName = callbackName;
                CallbackTargetType = callbackTargetType;
                UpdateType = updateType;
                SparseCalls = sparseCalls;
                FrameInterval = frameInterval;
                UpdateGroup = updateGroup;
                StackTrace = stackTrace;
                Created = created;
            }
        }

        public static readonly List<SubscriptionTrackingData> TRACKED_SUBSCRIPTIONS = new(256);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void SupportDomainReload()
        {
            TRACKED_SUBSCRIPTIONS.Clear();
        }

        public static void TrackSubscription(MonoBehaviourService.UpdateSubscription sub)
        {
#if UNITY_EDITOR
            string stackTrace = new StackTrace(2, true).CleanupAsyncStackTrace();

            TRACKED_SUBSCRIPTIONS.Add(new SubscriptionTrackingData(
                sub.InstanceID,
                GetCallbackName(sub.Callback),
                GetCallbackTargetType(sub.Callback),
                sub.UpdateType,
                sub.SparseCalls,
                sub.FrameInterval,
                sub.UpdateGroup,
                stackTrace,
                DateTime.UtcNow
            ));
#endif
        }

        public static void TrackExistingSubscription(MonoBehaviourService.UpdateSubscription sub)
        {
#if UNITY_EDITOR
            for (int i = 0; i < TRACKED_SUBSCRIPTIONS.Count; i++)
            {
                if (TRACKED_SUBSCRIPTIONS[i].InstanceID == sub.InstanceID)
                    return;
            }

            TRACKED_SUBSCRIPTIONS.Add(new SubscriptionTrackingData(
                sub.InstanceID,
                GetCallbackName(sub.Callback),
                GetCallbackTargetType(sub.Callback),
                sub.UpdateType,
                sub.SparseCalls,
                sub.FrameInterval,
                sub.UpdateGroup,
                null,
                DateTime.UtcNow
            ));
#endif
        }

        public static void UntrackSubscription(MonoBehaviourService.UpdateSubscription sub)
        {
#if UNITY_EDITOR
            for (int i = TRACKED_SUBSCRIPTIONS.Count - 1; i >= 0; i--)
            {
                if (TRACKED_SUBSCRIPTIONS[i].InstanceID == sub.InstanceID)
                {
                    TRACKED_SUBSCRIPTIONS.RemoveAt(i);
                    break;
                }
            }
#endif
        }

        static string GetCallbackName(Action<float> callback)
        {
            return (callback.Method.DeclaringType?.Name ?? "?") + "." + callback.Method.Name;
        }

        static string GetCallbackTargetType(Action<float> callback)
        {
            return callback.Target?.GetType().Name ?? "static";
        }
    }
}

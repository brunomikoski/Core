using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BrunoMikoski.Core.DevelopmentTools.Tests
{
    internal sealed class MonoBehaviourServicePruneTarget : ScriptableObject
    {
        public void Tick(float _)
        {
            throw new InvalidOperationException("MBS prune test");
        }
    }

    /// <summary>
    /// Edit-mode tests for the MonoBehaviourService update pump. The pump is driven
    /// deterministically by invoking the private HandleUpdate with an explicit frame
    /// index and deltaTime, so every assertion is frame-exact and independent of
    /// editor timing.
    /// </summary>
    public class MonoBehaviourServiceTests
    {
        private const float DT = 0.02f;
        private const MonoBehaviourService.UpdateType U = MonoBehaviourService.UpdateType.Update;
        private const MonoBehaviourService.UpdateType FU = MonoBehaviourService.UpdateType.FixedUpdate;

        private static readonly MethodInfo HandleUpdateMethod = typeof(MonoBehaviourService)
            .GetMethod("HandleUpdate", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo RunnerField = typeof(MonoBehaviourService)
            .GetField("runner", BindingFlags.NonPublic | BindingFlags.Instance);

        private MonoBehaviourService service;
        private int updateFrame;

        [SetUp]
        public void SetUp()
        {
            Assert.IsNotNull(HandleUpdateMethod, "MonoBehaviourService.HandleUpdate(List, int, float) not found — update the test pump.");
            Assert.IsNotNull(RunnerField, "MonoBehaviourService.runner field not found — update the test teardown.");

            service = new MonoBehaviourService();
            updateFrame = 0;
        }

        [TearDown]
        public void TearDown()
        {
            if (service != null)
            {
                MonoBehaviourServiceRunner runner = (MonoBehaviourServiceRunner)RunnerField.GetValue(service);
                if (runner != null)
                    Object.DestroyImmediate(runner.gameObject);
                service = null;
            }

            MonoBehaviourServiceTracker.TRACKED_SUBSCRIPTIONS.Clear();
        }

        private List<MonoBehaviourService.UpdateSubscription> GetList(MonoBehaviourService.UpdateType type)
        {
            return (List<MonoBehaviourService.UpdateSubscription>)service.GetUpdateListenersForTracking(type);
        }

        private List<MonoBehaviourService.UpdateSubscription> UpdateList => GetList(U);

        private void Pump(int frames, float dt = DT)
        {
            List<MonoBehaviourService.UpdateSubscription> list = UpdateList;
            object[] args = new object[3];
            for (int i = 0; i < frames; i++)
            {
                args[0] = list;
                args[1] = updateFrame;
                args[2] = dt;
                HandleUpdateMethod.Invoke(service, args);
                updateFrame++;
            }
        }

        private int[] GroupHistogram(int interval)
        {
            int[] histogram = new int[interval];
            List<MonoBehaviourService.UpdateSubscription> list = UpdateList;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].FrameInterval == interval && list[i].SparseCalls)
                    histogram[list[i].UpdateGroup]++;
            }

            return histogram;
        }

        // ---------------------------------------------------------------------------
        // Basic interval semantics
        // ---------------------------------------------------------------------------

        [Test]
        public void Interval1_IsCalledEveryFrame_WithFrameDeltaTime()
        {
            int calls = 0;
            float dtSum = 0f;
            service.SubscribeToUpdate(dt => { calls++; dtSum += dt; }, U);

            Pump(10);

            Assert.AreEqual(10, calls);
            Assert.AreEqual(10 * DT, dtSum, 1e-5f);
        }

        [Test]
        public void SparseSubscriber_ReceivesCumulativeDeltaTime_NoTimeLostOrDoubleCounted()
        {
            List<float> perCall = new List<float>();
            service.SubscribeToUpdate(dt => perCall.Add(dt), U, 5);

            // A single interval-5 subscriber lands in group 0, so it fires on frames 0, 5, 10, 15.
            Pump(16);

            Assert.AreEqual(4, perCall.Count);
            Assert.AreEqual(DT, perCall[0], 1e-5f, "first call accumulates only the frames since subscribing");
            for (int i = 1; i < perCall.Count; i++)
                Assert.AreEqual(5 * DT, perCall[i], 1e-5f, "steady-state calls receive exactly one interval of time");

            float total = 0f;
            foreach (float dt in perCall)
                total += dt;
            Assert.AreEqual(16 * DT, total, 1e-5f, "the sum of received deltaTime must equal elapsed time");
        }

        [Test]
        public void NonSparseInterval_FiresEveryNthFrame_WithAccumulatedDeltaTime()
        {
            List<float> perCall = new List<float>();
            service.SubscribeToUpdate(dt => perCall.Add(dt), U, 3, sparseCalls: false);

            Pump(9);

            Assert.AreEqual(3, perCall.Count);
            foreach (float dt in perCall)
                Assert.AreEqual(3 * DT, dt, 1e-5f);
        }

        [Test]
        public void FrameIntervalBelowOne_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => service.SubscribeToUpdate(_ => { }, U, 0));
        }

        // ---------------------------------------------------------------------------
        // Sparse distribution — the load-spreading guarantees
        // ---------------------------------------------------------------------------

        [Test]
        public void HundredSubscribers_Interval5_SpreadExactlyTwentyPerGroup_AndTwentyFirePerFrame()
        {
            int[] callsPerSubscriber = new int[100];
            int totalCalls = 0;
            for (int i = 0; i < 100; i++)
            {
                int index = i;
                service.SubscribeToUpdate(_ => { callsPerSubscriber[index]++; totalCalls++; }, U, 5);
            }

            CollectionAssert.AreEqual(new[] { 20, 20, 20, 20, 20 }, GroupHistogram(5),
                "100 interval-5 subscribers must spread exactly 20 per group");

            for (int frame = 1; frame <= 5; frame++)
            {
                Pump(1);
                Assert.AreEqual(20 * frame, totalCalls, $"exactly 20 subscribers must fire on frame {frame}");
            }

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(1, callsPerSubscriber[i], $"subscriber {i} must fire exactly once per full interval");
        }

        [Test]
        public void BurstSubscribes_WithMixedIntervalStride_StayUniform()
        {
            // Emulates the junk-spawn shape: every object subscribes one interval-10
            // callback and one interval-1 callback. The old global-ID modulo scheme
            // collapsed this stride-2 pattern into half the groups.
            int[] sink = new int[40];
            for (int i = 0; i < 40; i++)
            {
                int index = i;
                service.SubscribeToUpdate(_ => sink[index]++, U, 10);
                service.SubscribeToUpdate(_ => sink[index]--, U, 1);
            }

            CollectionAssert.AreEqual(new[] { 4, 4, 4, 4, 4, 4, 4, 4, 4, 4 }, GroupHistogram(10));
        }

        [Test]
        public void EverySparseSubscriber_GetsItsFirstCallWithinOneInterval()
        {
            int[] calls = new int[5];
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                service.SubscribeToUpdate(_ => calls[index]++, U, 5);
            }

            Pump(5);

            for (int i = 0; i < 5; i++)
                Assert.AreEqual(1, calls[i], $"subscriber {i} must have fired exactly once within its first interval");
        }

        [Test]
        public void UnsubscribedGroupSlot_IsReusedByTheNextSubscriber()
        {
            int[] sink = new int[7];
            var handles = new List<MonoBehaviourService.UpdateSubscription>();
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                handles.Add(service.SubscribeToUpdate(_ => sink[index]++, U, 5));
            }

            Assert.AreEqual(0, handles[0].UpdateGroup, "first subscriber lands in group 0");
            service.UnsubscribeFromUpdate(handles[0]);

            MonoBehaviourService.UpdateSubscription fresh = service.SubscribeToUpdate(_ => sink[5]++, U, 5);
            Assert.AreEqual(0, fresh.UpdateGroup, "the freed group must be refilled first (least-loaded assignment)");
        }

        [Test]
        public void ChurnedGroups_RebalanceToUniform()
        {
            int[] sink = new int[75];
            var handles = new List<MonoBehaviourService.UpdateSubscription>();
            for (int i = 0; i < 50; i++)
            {
                int index = i;
                handles.Add(service.SubscribeToUpdate(_ => sink[index]++, U, 5));
            }

            CollectionAssert.AreEqual(new[] { 10, 10, 10, 10, 10 }, GroupHistogram(5));

            // Drain groups 0 and 1 entirely and group 2 halfway, then add 25 replacements.
            int[] groupsBefore = new int[handles.Count];
            for (int i = 0; i < handles.Count; i++)
                groupsBefore[i] = handles[i].UpdateGroup;

            int removedFromGroup2 = 0;
            for (int i = 0; i < handles.Count; i++)
            {
                int group = groupsBefore[i];
                if (group == 0 || group == 1 || (group == 2 && removedFromGroup2 < 5))
                {
                    service.UnsubscribeFromUpdate(handles[i]);
                    if (group == 2)
                        removedFromGroup2++;
                }
            }

            CollectionAssert.AreEqual(new[] { 0, 0, 5, 10, 10 }, GroupHistogram(5));

            for (int i = 0; i < 25; i++)
            {
                int index = 50 + i;
                service.SubscribeToUpdate(_ => sink[index]++, U, 5);
            }

            CollectionAssert.AreEqual(new[] { 10, 10, 10, 10, 10 }, GroupHistogram(5),
                "least-loaded assignment must level the drained groups back to uniform");
        }

        // ---------------------------------------------------------------------------
        // Subscription hygiene
        // ---------------------------------------------------------------------------

        [Test]
        public void SubscribingTheSameCallbackTwice_ToTheSameUpdateType_Throws()
        {
            int calls = 0;
            Action<float> callback = _ => calls++;
            service.SubscribeToUpdate(callback, U);

            Assert.Throws<InvalidOperationException>(() => service.SubscribeToUpdate(callback, U));
            Assert.AreEqual(1, UpdateList.Count, "the failed subscribe must not mutate the list");

            Pump(1);
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void SameCallback_OnDifferentUpdateTypes_IsAllowed_AndUnsubscribeByCallbackRemovesBoth()
        {
            int calls = 0;
            Action<float> callback = _ => calls++;
            service.SubscribeToUpdate(callback, U);
            service.SubscribeToUpdate(callback, FU);

            Assert.AreEqual(1, UpdateList.Count);
            Assert.AreEqual(1, GetList(FU).Count);

            service.UnsubscribeFromUpdate(callback);

            Assert.AreEqual(0, UpdateList.Count);
            Assert.AreEqual(0, GetList(FU).Count);
        }

        [Test]
        public void UnsubscribingAHandleTwice_IsANoOp()
        {
            int calls = 0;
            MonoBehaviourService.UpdateSubscription handle = service.SubscribeToUpdate(_ => calls++, U);

            service.UnsubscribeFromUpdate(handle);
            Assert.AreEqual(0, UpdateList.Count);

            Assert.DoesNotThrow(() => service.UnsubscribeFromUpdate(handle));
            Assert.AreEqual(0, UpdateList.Count);
        }

        // ---------------------------------------------------------------------------
        // Re-entrancy — subscribing/unsubscribing from inside pumped callbacks
        // ---------------------------------------------------------------------------

        [Test]
        public void CallbackUnsubscribingItself_StopsExactlyAfterThatCall()
        {
            int calls = 0;
            Action<float> callback = null;
            callback = _ =>
            {
                calls++;
                if (calls == 3)
                    service.UnsubscribeFromUpdate(callback);
            };
            service.SubscribeToUpdate(callback, U);

            Pump(10);

            Assert.AreEqual(3, calls);
            Assert.AreEqual(0, UpdateList.Count);
        }

        [Test]
        public void UnsubscribingALaterSubscriber_MidPass_PreventsItsCallThatSameFrame()
        {
            int bCalls = 0;
            int cCalls = 0;
            Action<float> c = _ => cCalls++;
            Action<float> b = _ =>
            {
                bCalls++;
                if (bCalls == 1)
                    service.UnsubscribeFromUpdate(c);
            };
            service.SubscribeToUpdate(b, U);
            service.SubscribeToUpdate(c, U);

            Pump(5);

            Assert.AreEqual(5, bCalls);
            Assert.AreEqual(0, cCalls, "a subscriber unsubscribed earlier in the same pass must not be called");
        }

        [Test]
        public void RemovalMidPass_DoesNotSkipOrShortchangeNeighbors()
        {
            int xCalls = 0, aCalls = 0, yCalls = 0, zCalls = 0;
            float yDt = 0f;
            Action<float> x = _ => xCalls++;
            Action<float> a = _ =>
            {
                aCalls++;
                if (aCalls == 1)
                    service.UnsubscribeFromUpdate(x);
            };
            Action<float> y = dt => { yCalls++; yDt += dt; };
            Action<float> z = _ => zCalls++;
            service.SubscribeToUpdate(x, U);
            service.SubscribeToUpdate(a, U);
            service.SubscribeToUpdate(y, U);
            service.SubscribeToUpdate(z, U);

            Pump(3);

            Assert.AreEqual(1, xCalls, "x ran on frame 0 before being unsubscribed");
            Assert.AreEqual(3, yCalls, "the subscriber after the remover must not be skipped");
            Assert.AreEqual(3, zCalls);
            Assert.AreEqual(3 * DT, yDt, 1e-5f, "no deltaTime may be lost to a mid-pass removal");
        }

        [Test]
        public void SubscriptionAddedFromACallback_StartsTheNextFrame()
        {
            int aCalls = 0;
            int dCalls = 0;
            Action<float> d = _ => dCalls++;
            Action<float> a = _ =>
            {
                aCalls++;
                if (aCalls == 1)
                    service.SubscribeToUpdate(d, U);
            };
            service.SubscribeToUpdate(a, U);

            Pump(1);
            Assert.AreEqual(0, dCalls, "a subscription added during a pass must not run in that same pass");

            Pump(1);
            Assert.AreEqual(1, dCalls);
        }

        // ---------------------------------------------------------------------------
        // Failure containment
        // ---------------------------------------------------------------------------

        [Test]
        public void ThrowingCallback_IsIsolated_NeighborsRunNormally()
        {
            LogAssert.Expect(LogType.Exception, new Regex("MBS test exception"));
            LogAssert.Expect(LogType.Exception, new Regex("MBS test exception"));

            int throwerCalls = 0;
            int healthyCalls = 0;
            float healthyDt = 0f;
            service.SubscribeToUpdate(_ =>
            {
                throwerCalls++;
                if (throwerCalls <= 2)
                    throw new InvalidOperationException("MBS test exception");
            }, U);
            service.SubscribeToUpdate(dt => { healthyCalls++; healthyDt += dt; }, U);

            Pump(4);

            Assert.AreEqual(4, throwerCalls, "a throwing subscriber stays subscribed");
            Assert.AreEqual(4, healthyCalls, "subscribers after a thrower must still run every frame");
            Assert.AreEqual(4 * DT, healthyDt, 1e-5f, "subscribers after a thrower must not lose deltaTime");
        }

        [Test]
        public void ThrowingCallbackWithDestroyedUnityTarget_IsAutoPruned()
        {
            MonoBehaviourServicePruneTarget target = ScriptableObject.CreateInstance<MonoBehaviourServicePruneTarget>();
            service.SubscribeToUpdate(target.Tick, U);
            Object.DestroyImmediate(target);

            LogAssert.Expect(LogType.Exception, new Regex("MBS prune test"));
            LogAssert.Expect(LogType.Error, new Regex("removing leaked subscription"));

            Pump(1);
            Assert.AreEqual(0, UpdateList.Count, "a subscription whose Unity target is destroyed must be removed after it throws");

            // Further frames must produce no more exceptions (LogAssert fails the test on unexpected errors).
            Pump(3);
        }
    }
}

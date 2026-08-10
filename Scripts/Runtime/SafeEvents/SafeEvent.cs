using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BrunoMikoski.Core.DevelopmentTools;
using Object = UnityEngine.Object;

namespace BrunoMikoski.Core.SafeEvents
{
    public abstract class SafeEventBase
    {
        private static readonly Dictionary<object, List<SafeEventBase>> eventContextToEvents = new();

        public static Dictionary<object, List<SafeEventBase>> GetEventContextToEvents()
        {
            return eventContextToEvents;
        }

        private static int totalEventsCreated;
        public static int TotalEventsCreated => totalEventsCreated;

        protected SafeEventBase(object context = null)
        {
            totalEventsCreated++;
            if (context != null)
            {
                SetEventContext(context);
            }
        }

        private void SetEventContext(object context)
        {
#if UNITY_EDITOR
            if (!eventContextToEvents.TryGetValue(context, out List<SafeEventBase> eventList))
            {
                eventList = new List<SafeEventBase>();
                eventContextToEvents[context] = eventList;
            }

            eventList.Add(this);
#endif
        }

        public struct CallbackData
        {
            public object target;
            public MethodInfo methodInfo;

            public CallbackData(object target, MethodInfo methodInfo)
            {
                this.target = target;
                this.methodInfo = methodInfo;
            }

            public bool IsTargetDestroyedOrDespawned
            {
                get
                {
                    Object asObject = target as Object;

                    if (asObject.IsNotNull() && asObject == null)
                        return true;

                    return false;
                }
            }
        }

        private int dispatchCount;
        public int DispatchCount => dispatchCount;
        private DateTime lastDispatch;
        public DateTime LastDispatch => lastDispatch;

        protected void OnDispatch()
        {
            dispatchCount++;
            lastDispatch = DateTime.UtcNow;
        }

        public abstract IList<CallbackData> Listeners { get; }
        public abstract bool HasListeners { get; }

        public bool HasDestroyedOrDespawnedListeners
        {
            get
            {
                IList<CallbackData> listeners = Listeners;
                for (int i = 0; i < listeners.Count; i++)
                {
                    if (listeners[i].IsTargetDestroyedOrDespawned)
                        return true;
                }

                return false;
            }
        }

        public abstract void Clear();
    }

    public class SafeEvent : SafeEventBase
    {
        private readonly LinkedList<Action> listeners = new LinkedList<Action>();

        // We need to take care to not add listeners to the same list while dispatching.
        // So we have one 'active' list where we add new listeners, so we can use the other
        // list while dispatching.
        private bool activeOnceListenersB = false;
        private readonly List<Action> onceListenersA = new List<Action>();
        private readonly List<Action> onceListenersB = new List<Action>();

        private Dictionary<object, SafeEventBase> addedConstantParameters;
        private bool hasListeners;

        public SafeEvent<T> WithConstant<T>(T value)
        {
            if (addedConstantParameters == null)
                addedConstantParameters = new Dictionary<object, SafeEventBase>();

            object valueAsObject = value;
            SafeEventBase safeEventBase;

            if (addedConstantParameters.TryGetValue(valueAsObject, out safeEventBase))
                return (SafeEvent<T>)safeEventBase;

            SafeEvent<T> result = new SafeEvent<T>();
            addedConstantParameters.Add(value, result);

            AddListener(() => { result.Invoke(value); });

            return result;
        }

        private List<Action> ActiveOnceListeners
        {
            get
            {
                if (activeOnceListenersB)
                    return onceListenersB;
                return onceListenersA;
            }
        }

        public SafeEvent(object context = null) : base(context) { }

        public virtual void AddListener(Action callback)
        {
#if DEBUG
            if (ContainsCallback(callback))
            {
                throw new Exception("AddListener to event twice");
            }
#endif
            listeners.AddLast(callback);
        }

        public void RemoveListener(Action callback)
        {
            listeners.Remove(callback);
            onceListenersA?.Remove(callback);
            onceListenersB?.Remove(callback);
        }

        public void SubscribeOnce(Action callback)
        {
#if DEBUG
            if (ContainsCallback(callback))
            {
                throw new Exception("AddListener to event twice");
            }
#endif
            ActiveOnceListeners.Add(callback);
        }

        private bool ContainsCallback(Action callback)
        {
            using (LinkedList<Action>.Enumerator enumerator = listeners.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Action listener = enumerator.Current;
                    if (listener.Method == callback.Method && listener.Target == callback.Target)
                    {
                        return true;
                    }
                }
            }

            for (int i = 0; i < ActiveOnceListeners.Count; i++)
            {
                Action listener = ActiveOnceListeners[i];
                if (listener.Method == callback.Method && listener.Target == callback.Target)
                {
                    return true;
                }
            }

            return false;
        }

        public virtual void Invoke()
        {
            LinkedListNode<Action> linkedListNode = listeners.First;
            while (linkedListNode != null)
            {
                LinkedListNode<Action> next = linkedListNode.Next;
                linkedListNode.Value();
                linkedListNode = next;
            }

            List<Action> dispatchingListeners = ActiveOnceListeners;
            activeOnceListenersB = !activeOnceListenersB;
            for (int i = 0; i < dispatchingListeners.Count; i++)
            {
                Action listener = dispatchingListeners[i];
                listener();
            }

            dispatchingListeners.Clear();

            OnDispatch();
        }

        public static SafeEvent operator +(SafeEvent e, Action a)
        {
            e.AddListener(a);
            return e;
        }

        public static SafeEvent operator -(SafeEvent e, Action a)
        {
            e.RemoveListener(a);
            return e;
        }

        public override IList<CallbackData> Listeners
        {
            get
            {
                List<CallbackData> result = new List<CallbackData>(listeners.Count);
                result.AddRange(listeners.Select(l => new CallbackData(l.Target, l.Method)));
                result.AddRange(onceListenersA.Select(l => new CallbackData(l.Target, l.Method)));
                result.AddRange(onceListenersB.Select(l => new CallbackData(l.Target, l.Method)));
                if (addedConstantParameters != null)
                {
                    foreach (SafeEventBase eventBase in addedConstantParameters.Values)
                    {
                        result.AddRange(eventBase.Listeners);
                    }
                }

                return result;
            }
        }

        public override bool HasListeners
        {
            get
            {
                if (listeners.Count > 0)
                    return true;
                if (onceListenersA.Count > 0)
                    return true;
                if (onceListenersB.Count > 0)
                    return true;
                if (addedConstantParameters != null)
                {
                    foreach (SafeEventBase eventBase in addedConstantParameters.Values)
                        if (eventBase.HasListeners)
                            return true;
                }

                return false;
            }
        }

        public override void Clear()
        {
            listeners.Clear();
            if (onceListenersA.Count > 0)
                onceListenersA.Clear();
            if (onceListenersB.Count > 0)
                onceListenersB.Clear();
            if (addedConstantParameters != null)
            {
                foreach (SafeEventBase eventBase in addedConstantParameters.Values)
                    eventBase.Clear();
            }
        }
    }

    /// <summary>
    /// A single dispatch event will only fire once, and after firing, will automatically call
    /// each new subscriber
    /// </summary>
    public class SingleDispatchSafeEvent : SafeEvent
    {
        private bool hasBeenDispatched = false;

        public override void Invoke()
        {
            if (hasBeenDispatched)
                throw new InvalidOperationException("Dispatch has already been called");

            hasBeenDispatched = true;
            base.Invoke();
        }

        public override void AddListener(Action callback)
        {
            if (hasBeenDispatched)
            {
                callback();
                return;
            }

            base.AddListener(callback);
        }
    }

    public class SafeEvent<T> : SafeEventBase
    {
        private readonly LinkedList<Action<T>> listeners = new LinkedList<Action<T>>();

        private SafeEvent simplifiedSafeEvent;
        private Dictionary<T, SafeEvent> filteredEvents;
        private Dictionary<object, SafeEventBase> addedConstantParameters;

        public SafeEvent(object context = null) : base(context) { }

        public SafeEvent<T, T2> WithConstant<T2>(T2 value)
        {
            if (addedConstantParameters == null)
                addedConstantParameters = new Dictionary<object, SafeEventBase>();

            object valueAsObject = value;
            SafeEventBase safeEventBase;

            if (addedConstantParameters.TryGetValue(valueAsObject, out safeEventBase))
                return (SafeEvent<T, T2>)safeEventBase;

            SafeEvent<T, T2> result = new SafeEvent<T, T2>();
            addedConstantParameters.Add(value, result);

            AddListener(t => { result.Invoke(t, value); });

            return result;
        }

        public SafeEvent Simplified()
        {
            if (simplifiedSafeEvent != null)
                return simplifiedSafeEvent;

            simplifiedSafeEvent = new SafeEvent();
            return simplifiedSafeEvent;
        }

        public SafeEvent Filtered(T firstParam)
        {
            if (filteredEvents == null)
                filteredEvents = new Dictionary<T, SafeEvent>();

            SafeEvent result;

            if (filteredEvents.TryGetValue(firstParam, out result))
                return result;

            result = new SafeEvent();

            filteredEvents.Add(firstParam, result);
            return result;
        }

        public void AddListener(Action<T> callback)
        {
#if DEBUG
            if (ContainsCallback(callback))
            {
                throw new Exception("AddListener to event twice");
            }
#endif
            listeners.AddLast(callback);
        }

        public void RemoveListener(Action<T> callback)
        {
            listeners.Remove(callback);
        }

        private bool ContainsCallback(Action<T> callback)
        {
            using (LinkedList<Action<T>>.Enumerator enumerator = listeners.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Action<T> listener = enumerator.Current;
                    if (listener.Method == callback.Method && listener.Target == callback.Target)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
        public void Invoke(T t)
        {
            LinkedListNode<Action<T>> linkedListNode = listeners.First;
            while (linkedListNode != null)
            {
                LinkedListNode<Action<T>> next = linkedListNode.Next;
                linkedListNode.Value(t);
                linkedListNode = next;
            }

            if (filteredEvents != null)
            {
                SafeEvent filtered;
                if (filteredEvents.TryGetValue(t, out filtered))
                {
                    filtered.Invoke();
                }
            }

            if (simplifiedSafeEvent != null)
                simplifiedSafeEvent.Invoke();

            OnDispatch();
        }

        public static SafeEvent<T> operator +(SafeEvent<T> e, Action<T> a)
        {
            e.AddListener(a);
            return e;
        }

        public static SafeEvent<T> operator -(SafeEvent<T> e, Action<T> a)
        {
            e.RemoveListener(a);
            return e;
        }

        public override IList<CallbackData> Listeners
        {
            get
            {
                List<CallbackData> result = new List<CallbackData>(listeners.Count);
                result.AddRange(listeners.Select(l => new CallbackData(l.Target, l.Method)));
                if (addedConstantParameters != null)
                {
                    foreach (SafeEventBase eventBase in addedConstantParameters.Values)
                        result.AddRange(eventBase.Listeners);
                }

                if (filteredEvents != null)
                {
                    foreach (SafeEvent eventsValue in filteredEvents.Values)
                        result.AddRange(eventsValue.Listeners);
                }

                if (simplifiedSafeEvent != null)
                    result.AddRange(simplifiedSafeEvent.Listeners);

                return result;
            }
        }

        public override bool HasListeners
        {
            get
            {
                if (listeners.Count > 0)
                    return true;
                if (simplifiedSafeEvent != null && simplifiedSafeEvent.HasListeners)
                    return true;
                if (addedConstantParameters != null)
                {
                    foreach (SafeEventBase eventBase in addedConstantParameters.Values)
                        if (eventBase.HasListeners)
                            return true;
                }

                if (filteredEvents != null)
                {
                    foreach (SafeEvent eventBase in filteredEvents.Values)
                        if (eventBase.HasListeners)
                            return true;
                }

                return false;
            }
        }

        public override void Clear()
        {
            listeners.Clear();
            if (simplifiedSafeEvent != null)
                simplifiedSafeEvent.Clear();
            if (addedConstantParameters != null)
            {
                foreach (SafeEventBase eventBase in addedConstantParameters.Values)
                    eventBase.Clear();
            }

            if (filteredEvents != null)
            {
                foreach (SafeEvent eventBase in filteredEvents.Values)
                    eventBase.Clear();
            }
        }
    }

    public class SafeEvent<T1, T2> : SafeEventBase
    {
        private SafeEvent<T1> simplifiedSafeEventT1;
        private SafeEvent<T2> simplifiedSafeEventT2;
        private readonly LinkedList<Action<T1, T2>> listeners = new LinkedList<Action<T1, T2>>();

        private Dictionary<T1, SafeEvent<T2>> filteredEvents;

        public SafeEvent(object context = null) : base(context) { }

        public SafeEvent<T2> SimplifiedRemoveParam1()
        {
            if (simplifiedSafeEventT2 != null)
                return simplifiedSafeEventT2;

            simplifiedSafeEventT2 = new SafeEvent<T2>();
            return simplifiedSafeEventT2;
        }

        public SafeEvent<T1> SimplifiedRemoveParam2()
        {
            if (simplifiedSafeEventT1 != null)
                return simplifiedSafeEventT1;

            simplifiedSafeEventT1 = new SafeEvent<T1>();
            return simplifiedSafeEventT1;
        }

        public SafeEvent<T2> Filtered(T1 firstParam)
        {
            if (filteredEvents == null)
                filteredEvents = new Dictionary<T1, SafeEvent<T2>>();

            SafeEvent<T2> result;

            if (filteredEvents.TryGetValue(firstParam, out result))
                return result;

            result = new SafeEvent<T2>();

            filteredEvents.Add(firstParam, result);
            return result;
        }

        public void AddListener(Action<T1, T2> callback)
        {
#if DEBUG
            if (ContainsCallback(callback))
            {
                throw new Exception("AddListener to event twice");
            }
#endif
            listeners.AddLast(callback);
        }

        public void RemoveListener(Action<T1, T2> callback)
        {
            listeners.Remove(callback);
        }

        private bool ContainsCallback(Action<T1, T2> callback)
        {
            using (LinkedList<Action<T1, T2>>.Enumerator enumerator = listeners.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Action<T1, T2> listener = enumerator.Current;
                    if (listener.Method == callback.Method && listener.Target == callback.Target)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Invoke(T1 t1, T2 t2)
        {
            LinkedListNode<Action<T1, T2>> linkedListNode = listeners.First;
            while (linkedListNode != null)
            {
                LinkedListNode<Action<T1, T2>> next = linkedListNode.Next;
                linkedListNode.Value(t1, t2);
                linkedListNode = next;
            }

            if (filteredEvents != null)
            {
                SafeEvent<T2> filtered;
                if (filteredEvents.TryGetValue(t1, out filtered))
                {
                    filtered.Invoke(t2);
                }
            }

            if (simplifiedSafeEventT1 != null)
                simplifiedSafeEventT1.Invoke(t1);
            if (simplifiedSafeEventT2 != null)
                simplifiedSafeEventT2.Invoke(t2);

            OnDispatch();
        }

        public static SafeEvent<T1, T2> operator +(SafeEvent<T1, T2> e, Action<T1, T2> a)
        {
            e.AddListener(a);
            return e;
        }

        public static SafeEvent<T1, T2> operator -(SafeEvent<T1, T2> e, Action<T1, T2> a)
        {
            e.RemoveListener(a);
            return e;
        }

        public override IList<CallbackData> Listeners
        {
            get
            {
                List<CallbackData> result = new List<CallbackData>(listeners.Count);
                result.AddRange(listeners.Select(l => new CallbackData(l.Target, l.Method)));

                if (filteredEvents != null)
                {
                    foreach (SafeEvent<T2> eventsValue in filteredEvents.Values)
                        result.AddRange(eventsValue.Listeners);
                }

                if (simplifiedSafeEventT1 != null)
                    result.AddRange(simplifiedSafeEventT1.Listeners);

                if (simplifiedSafeEventT2 != null)
                    result.AddRange(simplifiedSafeEventT2.Listeners);

                return result;
            }
        }

        public override bool HasListeners
        {
            get
            {
                if (listeners.Count > 0)
                    return true;
                if (simplifiedSafeEventT1 != null && simplifiedSafeEventT1.HasListeners)
                    return true;
                if (simplifiedSafeEventT2 != null && simplifiedSafeEventT2.HasListeners)
                    return true;
                if (filteredEvents != null)
                {
                    foreach (SafeEvent<T2> eventBase in filteredEvents.Values)
                        if (eventBase.HasListeners)
                            return true;
                }

                return false;
            }
        }

        public override void Clear()
        {
            listeners.Clear();
            if (simplifiedSafeEventT1 != null)
                simplifiedSafeEventT1.Clear();
            if (simplifiedSafeEventT2 != null)
                simplifiedSafeEventT2.Clear();
            if (filteredEvents != null)
            {
                foreach (SafeEvent<T2> eventBase in filteredEvents.Values)
                    eventBase.Clear();
            }
        }
    }

    public class SafeEvent<T1, T2, T3> : SafeEventBase
    {
        private SafeEvent simplifiedSafeEvent;
        private SafeEvent<T1, T2> simplifiedSafeEventT1T2;
        private SafeEvent<T1, T3> simplifiedSafeEventT1T3;
        private SafeEvent<T2, T3> simplifiedSafeEventT2T3;
        private readonly LinkedList<Action<T1, T2, T3>> listeners = new LinkedList<Action<T1, T2, T3>>();

        private Dictionary<T1, SafeEvent<T2, T3>> filteredEvents;

        public SafeEvent(object context = null) : base(context) { }

        public SafeEvent<T2, T3> SimplifiedRemoveParam1()
        {
            if (simplifiedSafeEventT2T3 != null)
                return simplifiedSafeEventT2T3;

            simplifiedSafeEventT2T3 = new SafeEvent<T2, T3>();
            return simplifiedSafeEventT2T3;
        }

        public SafeEvent<T1, T3> SimplifiedRemoveParam2()
        {
            if (simplifiedSafeEventT1T3 != null)
                return simplifiedSafeEventT1T3;

            simplifiedSafeEventT1T3 = new SafeEvent<T1, T3>();
            return simplifiedSafeEventT1T3;
        }


        public SafeEvent<T1, T2> SimplifiedRemoveParam3()
        {
            if (simplifiedSafeEventT1T2 != null)
                return simplifiedSafeEventT1T2;

            simplifiedSafeEventT1T2 = new SafeEvent<T1, T2>();
            return simplifiedSafeEventT1T2;
        }

        public SafeEvent<T2, T3> Filtered(T1 firstParam)
        {
            if (filteredEvents == null)
                filteredEvents = new Dictionary<T1, SafeEvent<T2, T3>>();

            SafeEvent<T2, T3> result;

            if (filteredEvents.TryGetValue(firstParam, out result))
                return result;

            result = new SafeEvent<T2, T3>();

            filteredEvents.Add(firstParam, result);
            return result;
        }

        public void AddListener(Action<T1, T2, T3> callback)
        {
#if DEBUG
            if (ContainsCallback(callback))
            {
                throw new Exception("AddListener to event twice");
            }
#endif
            listeners.AddLast(callback);
        }

        public void RemoveListener(Action<T1, T2, T3> callback)
        {
            listeners.Remove(callback);
        }

        private bool ContainsCallback(Action<T1, T2, T3> callback)
        {
            using (LinkedList<Action<T1, T2, T3>>.Enumerator enumerator = listeners.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Action<T1, T2, T3> listener = enumerator.Current;
                    if (listener.Method == callback.Method && listener.Target == callback.Target)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Invoke(T1 t1, T2 t2, T3 t3)
        {
            LinkedListNode<Action<T1, T2, T3>> linkedListNode = listeners.First;
            while (linkedListNode != null)
            {
                LinkedListNode<Action<T1, T2, T3>> next = linkedListNode.Next;
                linkedListNode.Value(t1, t2, t3);
                linkedListNode = next;
            }

            if (filteredEvents != null)
            {
                if (filteredEvents.TryGetValue(t1, out SafeEvent<T2, T3> filtered))
                {
                    filtered.Invoke(t2, t3);
                }
            }

            if (simplifiedSafeEventT1T2 != null)
                simplifiedSafeEventT1T2.Invoke(t1, t2);
            if (simplifiedSafeEventT1T3 != null)
                simplifiedSafeEventT1T3.Invoke(t1, t3);
            if (simplifiedSafeEventT2T3 != null)
                simplifiedSafeEventT2T3.Invoke(t2, t3);

            OnDispatch();
        }

        public static SafeEvent<T1, T2, T3> operator +(SafeEvent<T1, T2, T3> e, Action<T1, T2, T3> a)
        {
            e.AddListener(a);
            return e;
        }

        public static SafeEvent<T1, T2, T3> operator -(SafeEvent<T1, T2, T3> e, Action<T1, T2, T3> a)
        {
            e.RemoveListener(a);
            return e;
        }

        public override IList<CallbackData> Listeners
        {
            get
            {
                List<CallbackData> result = new List<CallbackData>(listeners.Count);
                result.AddRange(listeners.Select(l => new CallbackData(l.Target, l.Method)));

                if (filteredEvents != null)
                {
                    foreach (SafeEvent<T2, T3> eventsValue in filteredEvents.Values)
                        result.AddRange(eventsValue.Listeners);
                }

                if (simplifiedSafeEventT1T2 != null)
                    result.AddRange(simplifiedSafeEventT1T2.Listeners);

                if (simplifiedSafeEventT2T3 != null)
                    result.AddRange(simplifiedSafeEventT2T3.Listeners);

                if (simplifiedSafeEventT1T3 != null)
                    result.AddRange(simplifiedSafeEventT1T3.Listeners);

                return result;
            }
        }

        public override bool HasListeners
        {
            get
            {
                if (listeners.Count > 0)
                    return true;
                if (simplifiedSafeEventT1T2 != null && simplifiedSafeEventT1T2.HasListeners)
                    return true;
                if (simplifiedSafeEventT1T3 != null && simplifiedSafeEventT1T3.HasListeners)
                    return true;
                if (simplifiedSafeEventT2T3 != null && simplifiedSafeEventT2T3.HasListeners)
                    return true;
                if (filteredEvents != null)
                {
                    foreach (SafeEvent<T2, T3> eventBase in filteredEvents.Values)
                        if (eventBase.HasListeners)
                            return true;
                }

                return false;
            }
        }

        public override void Clear()
        {
            listeners.Clear();
            if (simplifiedSafeEventT1T2 != null)
                simplifiedSafeEventT1T2.Clear();
            if (simplifiedSafeEventT1T3 != null)
                simplifiedSafeEventT1T3.Clear();
            if (simplifiedSafeEventT2T3 != null)
                simplifiedSafeEventT2T3.Clear();
            if (filteredEvents != null)
            {
                foreach (SafeEvent<T2, T3> eventBase in filteredEvents.Values)
                    eventBase.Clear();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace BrunoMikoski.Core.DevelopmentTools.StateMachines
{
    public static class StateMachineRegistry
    {
        private struct QueuedEvents
        {
            public GlobalStateID GlobalStateID;
            public StateMachineEvent StateMachineEvent;
            public Action Callback;
        }
        
        private static List<StateMachineBase> RUNNING_STATE_MACHINES = new(10);
        private static List<QueuedEvents> QUEUED_EVENTS_LIST = new(20);
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void SupportDomainReload()
        {
            RUNNING_STATE_MACHINES.Clear();
            QUEUED_EVENTS_LIST.Clear();
        }

        public static void Register(StateMachineBase stateMachineBase)
        {
            if(RUNNING_STATE_MACHINES.Contains(stateMachineBase))
                return;

            RegisterNewStateMachineInstance(stateMachineBase);
        }

        public static void Unregister(StateMachineBase stateMachineBase)
        {
            if(!RUNNING_STATE_MACHINES.Contains(stateMachineBase))
                return;
            
            UnregisterStateMachineInstance(stateMachineBase);
        }

        private static void RegisterNewStateMachineInstance(StateMachineBase stateMachineBase)
        {
            EnsureSingleGlobalStateMachine(stateMachineBase);
            
            RUNNING_STATE_MACHINES.Add(stateMachineBase);
            ResolveStateMachineEventsQueue(stateMachineBase);
        }

        private static void EnsureSingleGlobalStateMachine(StateMachineBase stateMachineBase)
        {
            if (stateMachineBase.StateIDType.IsTypeOrSubclassOfType(typeof(GlobalStateID)))
            {
                for (int i = 0; i < RUNNING_STATE_MACHINES.Count; i++)
                {
                    StateMachineBase machineBase = RUNNING_STATE_MACHINES[i];
                    if (machineBase.StateIDType == stateMachineBase.StateIDType)
                    {
                        throw new Exception(
                            $"You can't have more than one state machine of the type {stateMachineBase.StateIDType} running at the same time");
                    }
                }
            }
        }

        private static void UnregisterStateMachineInstance(StateMachineBase stateMachineBase)
        {
            RUNNING_STATE_MACHINES.Remove(stateMachineBase);
            CleanStateMachineEventQueue(stateMachineBase);
        }

        public static void SubscribeToGlobalEvent<TGlobalStateType>(StateMachineEvent targetStateEvent, TGlobalStateType targetGlobalState, Action callback) where TGlobalStateType : GlobalStateID
        {
            if (TryGetStateMachineOfType(targetGlobalState, out StateMachineBase stateMachineBase))
            {
                stateMachineBase.SubscribeToStateEvent(targetStateEvent, targetGlobalState, callback);
            }
            else
            {
                QUEUED_EVENTS_LIST.Add(new QueuedEvents()
                {
                    GlobalStateID = targetGlobalState,
                    StateMachineEvent = targetStateEvent,
                    Callback = callback
                });
            }
        }
        
        public static void UnsubscribeFromGlobalEvent<TGlobalStateType>(StateMachineEvent targetStateEvent, TGlobalStateType targetGlobalState, Action callback)  where TGlobalStateType : GlobalStateID
        {
            if (TryGetStateMachineOfType(targetGlobalState, out StateMachineBase stateMachineBase))
            {
                stateMachineBase.UnsubscribeFromStateEvent(targetStateEvent, targetGlobalState, callback);
            }
            else
            {
                TryRemoveFromQueue(targetStateEvent, targetGlobalState, callback);
            }
        }
        
        private static void ResolveStateMachineEventsQueue(StateMachineBase stateMachineBase)
        {
            for (int i = QUEUED_EVENTS_LIST.Count - 1; i >= 0; i--)
            {
                QueuedEvents queuedEvents = QUEUED_EVENTS_LIST[i];

                if (stateMachineBase.StateIDType.IsNotTypeOrSubclassOfType(queuedEvents.GlobalStateID.GetType()))
                    continue;
                
                stateMachineBase.SubscribeToStateEvent(queuedEvents.StateMachineEvent, queuedEvents.GlobalStateID, queuedEvents.Callback);
                QUEUED_EVENTS_LIST.RemoveAt(i);
            }
        }
         private static void CleanStateMachineEventQueue(StateMachineBase stateMachineBase)
        {
            for (int i = QUEUED_EVENTS_LIST.Count - 1; i >= 0; i--)
            {
                QueuedEvents queuedEvents = QUEUED_EVENTS_LIST[i];

                if(stateMachineBase.StateIDType.IsNotTypeOrSubclassOfType(queuedEvents.GlobalStateID.GetType()))
                    continue;
                
                QUEUED_EVENTS_LIST.RemoveAt(i);
            }
        }

        private static void TryRemoveFromQueue<TGlobalStateType>(StateMachineEvent targetStateEvent, TGlobalStateType targetGlobalState, Action callback) where TGlobalStateType : GlobalStateID
        {
            for (int i = QUEUED_EVENTS_LIST.Count - 1; i >= 0; i--)
            {
                QueuedEvents queuedEvents = QUEUED_EVENTS_LIST[i];
                if(queuedEvents.StateMachineEvent == targetStateEvent && queuedEvents.GlobalStateID == targetGlobalState && queuedEvents.Callback == callback)
                    QUEUED_EVENTS_LIST.RemoveAt(i);
            }
        }

        private static bool TryGetStateMachineOfType<TGlobalStateType>(TGlobalStateType targetGlobalState, out StateMachineBase result) where TGlobalStateType : GlobalStateID
        {
            for (int i = 0; i < RUNNING_STATE_MACHINES.Count; i++)
            {
                StateMachineBase machineBase = RUNNING_STATE_MACHINES[i];

                if (machineBase.StateIDType.IsTypeOrSubclassOfType(targetGlobalState.GetType()))
                {
                    result = machineBase;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static bool TryGetStateMachineByStateType(Type stateMachineStateType, out StateMachineBase resultStateMachine)
        {
            using (ListPool<StateMachineBase>.Get(out List<StateMachineBase> results))
            {
                for (int i = 0; i < RUNNING_STATE_MACHINES.Count; i++)
                {
                    StateMachineBase stateMachineBase = RUNNING_STATE_MACHINES[i];
                    if(stateMachineBase.StateIDType == stateMachineStateType)
                    {
                        results.Add(stateMachineBase);
                    }
                }

                if (results.Count == 1)
                {
                    resultStateMachine = results[0];
                    return true;
                }

                if (results.Count > 1)
                {
                    Debug.LogError($"There are more than one state machine of type {stateMachineStateType} running at the same time");
                    resultStateMachine = null;
                    return false;
                }
            }
            
            resultStateMachine = null;
            return false;
        }
    }
}
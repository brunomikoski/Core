using System;
using System.Collections.Generic;

namespace BrunoMikoski.Core.StateMachines
{
    public enum StateMachineEvent
    {
        StateEnter,
        StateExit,
        BeginTransitionTo,
        BeginTransitionFrom
    }

    public partial class StateMachineBase
    {
        private Dictionary<StateID, Dictionary<StateMachineEvent, List<Action>>> stateToEventToCallbackList = new();


        public void UnsubscribeFromStateEvent(StateMachineEvent targetEvent, StateID targetState, Action callback)
        {
            if (!stateToEventToCallbackList.ContainsKey(targetState))
                return;

            if (!stateToEventToCallbackList[targetState].ContainsKey(targetEvent))
                return;

            stateToEventToCallbackList[targetState][targetEvent].Remove(callback);
        }
        
        public void SubscribeToStateEvent(StateMachineEvent targetEvent, StateID uiWindow, Action callback)
        {
            if (!stateToEventToCallbackList.ContainsKey(uiWindow))
                stateToEventToCallbackList.Add(uiWindow, new Dictionary<StateMachineEvent, List<Action>>());

            if (!stateToEventToCallbackList[uiWindow].ContainsKey(targetEvent))
                stateToEventToCallbackList[uiWindow].Add(targetEvent, new List<Action>());

            if (stateToEventToCallbackList[uiWindow][targetEvent].Contains(callback))
                return;

            stateToEventToCallbackList[uiWindow][targetEvent].Add(callback);
        }
        
        internal void DispatchEvent(StateMachineEvent stateMachineEvent, StateID targetState)
        {
            if (!stateToEventToCallbackList.ContainsKey(targetState))
                return;

            if (!stateToEventToCallbackList[targetState].ContainsKey(stateMachineEvent))
                return;

            for (int i = 0; i < stateToEventToCallbackList[targetState][stateMachineEvent].Count; i++)
            {
                stateToEventToCallbackList[targetState][stateMachineEvent][i].Invoke();
            }
        }
    }
}
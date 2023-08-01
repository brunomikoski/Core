using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.Core.StateMachines
{
    public class StateMachineBase : MonoBehaviour
    {
        protected List<State> availableStates = new();
        public List<State> AvailableStates => availableStates;
        
        protected List<TransitionBase> availableTransitions = new();
        public List<TransitionBase> AvailableTransitions => availableTransitions;

        protected State previousState;
        public State PreviousState => previousState;

        protected State currentState;
        public State CurrentState => currentState;

        public event Action<StateID, StateID> OnStateChangedEvent;

        
        public virtual void TransitionFinished(StateID toState)
        {
        }
        
        protected void DispatchStateChangedEvent(StateID fromState, StateID toState)
        {
            OnStateChangedEvent?.Invoke(fromState, toState);
        }
    }
}
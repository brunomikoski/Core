using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.Core.StateMachines
{
    public class StateMachineBase : MonoBehaviour
    {
        protected List<StateBase> availableStates = new();
        public List<StateBase> AvailableStates => availableStates;
        
        protected List<TransitionBase> availableTransitions = new();
        public List<TransitionBase> AvailableTransitions => availableTransitions;

        protected StateBase previousState;
        public StateBase PreviousState => previousState;

        protected StateBase currentState;
        public StateBase CurrentState => currentState;

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

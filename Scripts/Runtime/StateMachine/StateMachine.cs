using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Enum = System.Enum;

namespace BrunoMikoski.Core.StateMachines
{
    
    
    
    
    public class StateMachine : MonoBehaviour
    {
        public event Action<State, State> OnStateChangedEvent;

        private Dictionary<Enum, State> states = new();
        private HashSet<Enum> fromAnyTransitions = new();
        public HashSet<Enum> FromAnyTransitions => fromAnyTransitions;
        private Dictionary<Enum, HashSet<Enum>> transitions = new();

        private Enum startStateType;
        private State currentState;
        private State previousState;

        public State CurrentState => currentState;
        public State PreviousState => previousState;

        public HashSet<Enum> AvailableNextStates
        {
            get
            {
                transitions.TryGetValue(currentState.StateType, out HashSet<Enum> availableNext);
                return availableNext;
            }
        }

        public void EnterStartState()
        {
            if (currentState != null)
            {
                Debug.LogError("State machine is already started!");
                return;
            }

            if (startStateType == null)
            {
                Debug.LogError("Tried starting state machine, but no start state defined");
                return;
            }
            EnterState(startStateType);
        }

        public void Stop()
        {
            if (currentState == null)
            {
                Debug.LogError("Tried stopping the state machine, but it has not started");
                return;
            }
            ExitCurrentState();
        }

        public void SetStartState(Enum type)
        {
            startStateType = type;
        }

        public void AddState(Enum type, State state)
        {
            states.Add(type, state);
            state.StateMachine = this;
            state.StateType = type;
        }

        public T AddState<T>(Enum type) where T : State
        {
            T instance = Activator.CreateInstance<T>();
            AddState(type, instance);
            return instance;
        }

        public void CreateTransition(Enum from, Enum to)
        {
            if (!transitions.TryGetValue(from, out var toStates))
            {
                toStates = new HashSet<Enum>();
                transitions.Add(from, toStates);
            }
            toStates.Add(to);
        }

        protected void CreateFromAnyTransition(Enum to)
        {
            fromAnyTransitions.Add(to);
        }

        private bool CanTransitionTo(Enum from, Enum to)
        {
            if (fromAnyTransitions.Contains(to))
            {
                //If exist in the Any, block if is the same state
                return !Equals(from, to);
            }

            if (!transitions.TryGetValue(from, out var toStates))
                return false;
            
            return toStates.Contains(to);
        }

        public void TransitionTo(Enum to)
        {
            if (!CanTransitionTo(currentState.StateType, to))
            {
                Debug.LogErrorFormat("Transition not allowed {0} -> {1}", currentState.StateType, to);
                return;
            }

            previousState = currentState;
            ExitCurrentState();
            EnterState(to);

            if (OnStateChangedEvent != null)
                OnStateChangedEvent(previousState, currentState);
        }

        private void EnterState(Enum type)
        {
            if (!states.TryGetValue(type, out State state))
            {
                Debug.LogErrorFormat("Cannot enter state {0}, no state defined", type);
                return;
            }

            currentState = state;
            currentState.Enter();
        }

        private void ExitCurrentState()
        {
            currentState.Exit();
            currentState = null;
        }

        protected virtual void Update()
        {
            currentState?.Update();
        }

        protected virtual void LateUpdate()
        {
            currentState?.LateUpdate();
        }

        protected virtual void FixedUpdate()
        {
            currentState?.FixedUpdate();
        }

    }
}

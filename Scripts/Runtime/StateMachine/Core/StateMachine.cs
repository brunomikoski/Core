using UnityEngine;

namespace BrunoMikoski.Core.StateMachines
{
    public abstract class StateMachine<TStateID> : StateMachineBase where TStateID : StateID
    {
        private TStateID overrideInitialState;
        private TStateID defaultInitialState;

        private TStateID InitialState
        {
            get
            {
                if (overrideInitialState != null)
                    return overrideInitialState;
                return defaultInitialState;
            }
        }

        private Transition<TStateID> currentTransition;

        public void StartStateMachine()
        {
            if (InitialState == null)
            {
                Debug.LogError($"Cannot initialize start state machine, no initial state was set");
                return;
            }

            TransitTo(InitialState);
        }

        public void TransitTo(TStateID targetStateID)
        {
            if (!CanTransitTo(targetStateID))
            {
                if (currentState != null)
                    Debug.LogError($"Cannot transit to {targetStateID} from {currentState.StateID}");
                else
                    Debug.LogError($"Cannot transit to {targetStateID}");
                return;
            }

            if (currentState != null && currentState.StateID == targetStateID)
            {
                Debug.LogError($"Cannot transit to the same state {targetStateID}");
                return;
            }

            BeginEnterState(targetStateID);
        }

        private bool CanTransitTo(TStateID targetStateID)
        {
            if (targetStateID.FromAnyState)
                return true;

            if (currentState == null && targetStateID == InitialState)
                return true;
            
            if (currentState != null)
            {
                for (int i = 0; i < availableTransitions.Count; i++)
                {
                    Transition<TStateID> availableTransition = (Transition<TStateID>) availableTransitions[i];
                    if (availableTransition.FromState == currentState.StateID &&
                        availableTransition.ToState == targetStateID)
                        return true;
                }
            }

            return false;
        }

        private void BeginEnterState(TStateID nextStateID)
        {
            if (!TryGetStateWithID(nextStateID, out State<TStateID> _))
            {
                Debug.LogError($"Cannot find state implementation for {nextStateID}");
                return;
            }

            if (currentState != null)
            {
                currentState.Exit();
                currentTransition = GetOrCreateTransition(currentState.StateID, nextStateID);
                currentTransition.Start();
            }
            else
            {
                FinishEnterState(nextStateID);
            }
        }

        private void FinishEnterState(TStateID toState)
        {
            currentTransition = null;
            if (!TryGetStateWithID(toState, out State<TStateID> newState))
            {
                Debug.LogError($"Cannot find state implementation for {toState}");
                return;
            }

            previousState = currentState;
            currentState = newState;
            currentState.Enter();
            
            TStateID previousStateID = null;

            if (previousState != null)
                previousStateID = (TStateID) previousState.StateID;

            DispatchStateChangedEvent(previousStateID, toState);
        }

        public void AddState(State<TStateID> state)
        {
            if (AlreadyContainState(state))
            {
                Debug.LogError(
                    $"State with the same ID ({state.StateID}) already exist on the state machine, ignoring it");
                return;
            }

            state.SetStateMachine(this);
            availableStates.Add(state);
            if (state.StateID.InitialState)
            {
                if (defaultInitialState == null)
                    defaultInitialState = state.StateID;
                else
                    Debug.LogError(
                        $"Initial state already exist ({InitialState}), ignoring the new one ({state.StateID})");
            }
        }

        public void AddTransition(Transition<TStateID> transition)
        {
            if (AlreadyContainTransition(transition))
            {
                Debug.LogError(
                    $"Transition from {transition.FromState} to {transition.ToState} already exist, ignoring it");
                return;
            }

            transition.SetStateMachine(this);
            availableTransitions.Add(transition);
            base.AvailableTransitions.Add(transition);
        }

        private bool AlreadyContainTransition(Transition<TStateID> transition)
        {
            return TryGetTransition(transition.FromState, transition.ToState, out _);
        }

        private Transition<TStateID> GetOrCreateTransition(StateID fromStateID, StateID toStateID)
        {
            if (!TryGetTransition(fromStateID, toStateID, out Transition<TStateID> transition))
            {
                transition = new Transition<TStateID>((TStateID) fromStateID, (TStateID) toStateID);
                AddTransition(transition);
            }

            return transition;
        }

        private bool TryGetTransition(StateID fromStateID, StateID toStateID, out Transition<TStateID> resultTransition)
        {
            for (int i = 0; i < availableTransitions.Count; i++)
            {
                Transition<TStateID> availableTransition = (Transition<TStateID>) availableTransitions[i];
                if (availableTransition.FromState == fromStateID && availableTransition.ToState == toStateID)
                {
                    resultTransition = availableTransition;
                    return true;
                }
            }

            resultTransition = null;
            return false;
        }

        private bool TryGetStateWithID(StateID targetStaID, out State<TStateID> resultState)
        {
            for (int i = 0; i < availableStates.Count; i++)
            {
                StateBase availableState = availableStates[i];
                if (availableState.StateID == targetStaID)
                {
                    resultState = (State<TStateID>) availableState;
                    return true;
                }
            }

            resultState = null;
            return false;
        }

        private void Update()
        {
            currentState?.Update();
        }

        private bool AlreadyContainState(State<TStateID> state)
        {
            return TryGetStateWithID(state.StateID, out _);
        }

        public override void TransitionFinished(StateID toState)
        {
            FinishEnterState((TStateID) toState);
        }

        public void OverrideInitialState(TStateID targetStateID)
        {
            overrideInitialState = targetStateID;
        }
    }
}

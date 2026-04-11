using System;
using System.Collections.Generic;
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

        public override Type StateIDType => typeof(TStateID);
        private Queue<TStateID> queuedTransitions = new();

        protected override string GetLogPrefix()
        {
            return $"[StateMachine<{typeof(TStateID).Name}>:{name}]";
        }

        public void StartStateMachine()
        {
            if (InitialState == null)
            {
                Debug.LogError($"Cannot initialize start state machine, no initial state was set");
                return;
            }

            TransitTo(InitialState);
        }

        protected override void ClearAll()
        {
            base.ClearAll();
            currentState = null;
            queuedTransitions.Clear();
        }

        public void TransitTo(TStateID targetStateID)
        {
            if (currentTransition != null)
            {
                // Make sure we aren't already transitioning to the targetStateID
                if (currentTransition.ToState != targetStateID || targetStateID.AllowSelfTransition)
                {
                    LogWarning("Cannot transit to a new state while a transition is in progress, queuing the transition");
                    queuedTransitions.Enqueue(targetStateID);
                }
                else
                {
                    LogWarning($"Cannot transit to state '{targetStateID}' because the current transition is already transitioning there");
                }
                return;
            }

            if (!CanTransitTo(targetStateID))
            {
                if (currentState != null)
                    LogError($"Cannot transit to {targetStateID} from {currentState.StateID}");
                else
                    LogError($"Cannot transit to {targetStateID}");
                return;
            }

            if (currentState != null && currentState.StateID == targetStateID && !currentState.StateID.AllowSelfTransition)
            {
                LogError($"Cannot transit to the same state {targetStateID}");
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
                LogError($"Cannot find state implementation for {nextStateID}");
                return;
            }

            if (currentState != null)
            {
                currentState.Internal_Exit();
                currentTransition = GetOrCreateTransition(currentState.StateID, nextStateID);
                currentTransition.Internal_Start();

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
                LogError($"Cannot find state implementation for {toState}");
                return;
            }

            previousState = currentState;
            currentState = newState;
            currentState.Internal_Enter();

            TStateID previousStateID = null;

            if (previousState != null)
                previousStateID = (TStateID) previousState.StateID;

            DispatchStateChangedEvent(previousStateID, toState);
            if (queuedTransitions.Count > 0)
            {
                TStateID nextTransition = queuedTransitions.Dequeue();
                Log($"Processing queued transition to {nextTransition}");
                TransitTo(nextTransition);
            }
        }

        public void AddState(State<TStateID> state)
        {
            if (AlreadyContainState(state))
            {
                LogError(
                    $"State with the same ID ({state.StateID}) already exist on the state machine, ignoring it");
                return;
            }

            state.SetStateMachine(this);
            availableStates.Add(state);
            if (state.StateID.InitialState)
            {
                if (defaultInitialState == null)
                {
                    defaultInitialState = state.StateID;
                }
                else
                {
                    LogError(
                        $"Initial state already exist ({InitialState}), ignoring the new one ({state.StateID})");
                }
            }
        }

        public void AddTransition(Transition<TStateID> transition)
        {
            if (AlreadyContainTransition(transition))
            {
                LogError(
                    $"Transition from {transition.FromState} to {transition.ToState} already exist, ignoring it");
                return;
            }

            transition.SetStateMachine(this);
            availableTransitions.Add(transition);
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

        private bool TryGetStateWithID(StateID targetStaID, out State<StateID> resultState)
        {
            for (int i = 0; i < availableStates.Count; i++)
            {
                StateBase availableState = availableStates[i];
                if (availableState.StateID == targetStaID)
                {
                    resultState = (State<StateID>) availableState;
                    return true;
                }
            }

            resultState = null;
            return false;
        }

        public bool TryGetStateWithID<T>(StateID targetStaID, out T resultState) where T : State<TStateID>
        {
            for (int i = 0; i < availableStates.Count; i++)
            {
                StateBase availableState = availableStates[i];
                if (availableState.StateID == targetStaID)
                {
                    resultState = (T) availableState;
                    return resultState != null;
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

        internal override void TransitionFinished(StateID toState)
        {
            FinishEnterState((TStateID) toState);
        }

        public void OverrideInitialState(TStateID targetStateID)
        {
            overrideInitialState = targetStateID;
        }


        public void UnsubscribeFromStateEvent(StateMachineEvent targetEvent, TStateID targetState, Action callback)
        {
            base.UnsubscribeFromStateEvent(targetEvent, targetState, callback);
        }

        public void SubscribeToStateEvent(StateMachineEvent targetEvent, TStateID targetState, Action callback)
        {
            base.SubscribeToStateEvent(targetEvent, targetState, callback);
        }
    }

    public partial class StateMachineBase : MonoBehaviour
    {
        protected List<StateBase> availableStates = new();
        public List<StateBase> AvailableStates => availableStates;

        protected List<TransitionBase> availableTransitions = new();
        public List<TransitionBase> AvailableTransitions => availableTransitions;

        protected StateBase previousState;
        public StateBase PreviousState => previousState;

        protected StateBase currentState;
        public StateBase CurrentState => currentState;

        protected TransitionBase currentTransition;
        public TransitionBase CurrentTransition => currentTransition;


        public virtual Type StateIDType => typeof(StateID);

        public event Action<StateID, StateID> OnStateChangedEvent;

        [field: SerializeField]
        public bool ShowLogs { get; private set; }

        public void Log(string message, LogType logType = LogType.Log)
        {
            if (!ShowLogs)
                return;

            switch (logType)
            {
                case LogType.Error:
                {
                    Debug.LogError($"{GetLogPrefix()} {message}", this);
                    break;
                }
                case LogType.Assert:
                {
                    Debug.LogAssertion($"{GetLogPrefix()} {message}", this);
                    break;
                }
                case LogType.Warning:
                {
                    Debug.LogWarning($"{GetLogPrefix()} {message}", this);

                    break;
                }
                case LogType.Exception:
                {
                    Debug.LogError($"{GetLogPrefix()} {message}", this);

                    break;
                }
                default:
                {
                    Debug.Log($"{GetLogPrefix()} {message}", this);
                    break;
                }
            }
        }

        public void LogWarning(string message)
        {
            Log(message, LogType.Warning);
        }

        public void LogError(string message)
        {
            Log(message, LogType.Error);
        }

        protected virtual string GetLogPrefix()
        {
            return $"[StateMachine:{GetType().Name}]";
        }

        internal virtual void TransitionFinished(StateID toState)
        {
        }

        protected void DispatchStateChangedEvent(StateID fromState, StateID toState)
        {
            OnStateChangedEvent?.Invoke(fromState, toState);
        }

        public bool TryGetTransition<T>(StateID from, StateID to, out T resultTransition) where T : TransitionBase
        {
            for (int i = 0; i < availableTransitions.Count; i++)
            {
                TransitionBase availableTransition = availableTransitions[i];
                if (availableTransition is not T transition)
                    continue;

                if (from != null && to != null)
                {
                    if (availableTransition.FromState == from && availableTransition.ToState == to)
                    {
                        resultTransition = transition;
                        return true;
                    }
                }

                resultTransition = transition;
                return true;
            }

            resultTransition = null;
            return false;
        }

        protected virtual void ClearAll()
        {
            availableStates.Clear();
            availableTransitions.Clear();
        }

        protected virtual void Awake()
        {
            StateMachineRegistry.Register(this);
        }

        protected virtual void OnDestroy()
        {
            StateMachineRegistry.Unregister(this);
#if UNITY_EDITOR
            StateMachineTracker.Forget(this);
#endif
        }

    }
}

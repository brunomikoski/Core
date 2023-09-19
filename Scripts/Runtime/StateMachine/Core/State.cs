#if UNITASK_AVAILABLE
using Cysharp.Threading.Tasks;
#endif

namespace BrunoMikoski.Core.StateMachines
{
    public class State<TStateID> : StateBase where TStateID : StateID
    {
        public new TStateID StateID => (TStateID) stateID;
        public new StateMachine<TStateID> StateMachine => (StateMachine<TStateID>)stateMachine;

        public State(TStateID targetStateID) : base(targetStateID)
        {
        }
    }
    
    public class StateBase
    {
        protected StateMachineBase stateMachine;
        public StateMachineBase StateMachine => stateMachine;

        protected StateID stateID;
        public StateID StateID => stateID;

        protected StateBase(StateID targetStateID)
        {
            stateID = targetStateID;
        }

        internal void Internal_Enter()
        {
#if UNITY_EDITOR
            StateMachineTracker.TrackEnterState(stateMachine, this);
#endif
            Enter();
#if UNITASK_AVAILABLE
            Enter_Async().Forget();
#endif
            stateMachine.DispatchEvent(StateMachineEvent.StateEnter, stateID);
        }
        
        internal void Internal_Exit()
        {
#if UNITY_EDITOR
            StateMachineTracker.TrackExitState(stateMachine, this);
#endif
            Exit();
#if UNITASK_AVAILABLE
            Exit_Async().Forget();
#endif           
            stateMachine.DispatchEvent(StateMachineEvent.StateExit, stateID);
        }


        protected virtual void Enter()
        {
        }
        
#if UNITASK_AVAILABLE
        protected virtual UniTaskVoid Enter_Async()
        {
            return default;
        }
#endif

        protected virtual void Exit()
        {
        }
        
#if UNITASK_AVAILABLE
        protected virtual UniTaskVoid Exit_Async()
        {
            return default;
        }
#endif

        internal virtual void Update()
        {
        }

        internal virtual void SetStateMachine(StateMachineBase targetStateMachine)
        {
            stateMachine = targetStateMachine;
        }
    }
}

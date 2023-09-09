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
            stateMachine.DispatchEvent(StateMachineEvent.StateEnter, stateID);
        }
        
        internal void Internal_Exit()
        {
#if UNITY_EDITOR
            StateMachineTracker.TrackExitState(stateMachine, this);
#endif
            Exit();
            stateMachine.DispatchEvent(StateMachineEvent.StateExit, stateID);
        }
        
        
        public virtual void Enter()
        {
        }

        public virtual void Exit()
        {
        }

        public virtual void Update()
        {
        }

        internal virtual void SetStateMachine(StateMachineBase targetStateMachine)
        {
            stateMachine = targetStateMachine;
        }
    }
}

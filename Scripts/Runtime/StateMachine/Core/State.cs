namespace BrunoMikoski.Core.StateMachines
{
    public class State<TStateID> : StateBase where TStateID : StateID
    {
        public new TStateID StateID => (TStateID) stateID;

        public State(TStateID targetStateID) : base(targetStateID)
        {
        }
    }
    
    public class StateBase
    {
        private StateMachineBase stateMachine;
        
        protected StateID stateID;
        public StateID StateID => stateID;

        protected StateBase(StateID targetStateID)
        {
            stateID = targetStateID;
        }

        internal void Internal_Enter()
        {
            Enter();
            stateMachine.DispatchEvent(StateMachineEvent.StateEnter, stateID);
#if UNITY_EDITOR
            StateMachineTracker.TrackEnterState(stateMachine, this);
#endif
        }
        
        internal void Internal_Exit()
        {
            Exit();
            stateMachine.DispatchEvent(StateMachineEvent.StateExit, stateID);
#if UNITY_EDITOR
            StateMachineTracker.TrackExitState(stateMachine, this);
#endif
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

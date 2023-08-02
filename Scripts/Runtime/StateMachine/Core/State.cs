namespace BrunoMikoski.Core.StateMachines
{
    public class State<TStateID> : StateBase where TStateID : StateID
    {
        protected StateMachine<TStateID> StateMachine;
        public new TStateID StateID => (TStateID) stateID;

        public State(TStateID targetStateID) : base(targetStateID)
        {
        }

        public override void SetStateMachine(StateMachineBase targetStateMachine)
        {
            base.SetStateMachine(targetStateMachine);
            StateMachine = (StateMachine<TStateID>) targetStateMachine;
        }
    }
    
    
    public class StateBase
    {
        private StateMachineBase stateMachineBase;
        
        protected StateID stateID;
        public StateID StateID => stateID;

        protected StateBase(StateID targetStateID)
        {
            stateID = targetStateID;
        }

        internal void Internal_Enter()
        {
            Enter();
#if UNITY_EDITOR
            StateMachineTracker.TrackEnterState(stateMachineBase, this);
#endif
        }
        
        internal void Internal_Exit()
        {
            Exit();
#if UNITY_EDITOR
            StateMachineTracker.TrackExitState(stateMachineBase, this);
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

        public virtual void SetStateMachine(StateMachineBase targetStateMachine)
        {
            stateMachineBase = targetStateMachine;
        }
    }
}

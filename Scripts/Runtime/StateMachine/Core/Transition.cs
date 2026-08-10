namespace BrunoMikoski.Core.DevelopmentTools.StateMachines
{
    public class Transition<TStateID> : TransitionBase where TStateID : StateID
    {
        public new TStateID FromState => (TStateID)fromState;
        public new TStateID ToState => (TStateID)toState;

        public Transition(TStateID fromState, TStateID toState) : base(fromState, toState)
        {
        }

        public void SetStateMachine(StateMachineBase targetStateMachine)
        {
            stateMachine = targetStateMachine;
        }
    }
    
    
    public class TransitionBase
    {
        protected StateMachineBase stateMachine;

        protected readonly StateID fromState;
        public StateID FromState => fromState;

        protected readonly StateID toState;
        public StateID ToState => toState;
        
        protected TransitionBase(StateID fromState, StateID toState)
        {
            this.fromState = fromState;
            this.toState = toState;
        }
        
        internal void Internal_Start()
        {
#if UNITY_EDITOR
            StateMachineTracker.TrackTransitionBegin(stateMachine, this);
#endif
            Start();
            
            stateMachine.DispatchEvent(StateMachineEvent.BeginTransitionFrom, fromState);
            stateMachine.DispatchEvent(StateMachineEvent.BeginTransitionTo, toState);
        }

        public virtual void Start()
        {
            ReportFinished();
        }

        protected void ReportFinished()
        {
#if UNITY_EDITOR
            StateMachineTracker.TrackTransitionEnd(stateMachine, this);
#endif
            stateMachine.TransitionFinished(toState);
        }
    }

}
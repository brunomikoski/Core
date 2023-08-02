namespace BrunoMikoski.Core.StateMachines
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
            StateMachine = targetStateMachine;
        }
    }
    
    
    public class TransitionBase
    {
        protected StateMachineBase StateMachine;

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
            Start();
#if UNITY_EDITOR
            StateMachineTracker.TrackTransitionBegin(StateMachine, this);
#endif
        }

        public virtual void Start()
        {
            ReportFinished();
        }

        protected void ReportFinished()
        {
            StateMachine.TransitionFinished(toState);
            
#if UNITY_EDITOR
            StateMachineTracker.TrackTransitionEnd(StateMachine, this);
#endif
        }

    }

}
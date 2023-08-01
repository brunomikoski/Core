using System;

namespace BrunoMikoski.Core.StateMachines
{
    public class TransitionBase
    {
        protected readonly StateID fromState;
        public StateID FromState => fromState;

        protected readonly StateID toState;
        public StateID ToState => toState;
        
        protected TransitionBase(StateID fromState, StateID toState)
        {
            this.fromState = fromState;
            this.toState = toState;
        }

    }
    [Serializable]
    public class Transition<TStateID> : TransitionBase where TStateID : StateID
    {
        protected StateMachineBase StateMachine;

        public new TStateID FromState => (TStateID)fromState;
        public new TStateID ToState => (TStateID)toState;

        public Transition(TStateID fromState, TStateID toState) : base(fromState, toState)
        {
        }

        public void SetStateMachine(StateMachineBase targetStateMachine)
        {
            StateMachine = targetStateMachine;
        }

        public virtual void Start()
        {
            ReportFinished();
        }

        protected void ReportFinished()
        {
            StateMachine.TransitionFinished(toState);
        }
    }
}
using System;

namespace BrunoMikoski.Core.StateMachines
{
    public class StateBase
    {
        protected StateID stateID;
        public StateID StateID => stateID;

        protected StateBase(StateID targetStateID)
        {
            stateID = targetStateID;
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
    }

    [Serializable]
    public class State<TStateID> : StateBase where TStateID : StateID
    {
        protected StateMachine<TStateID> StateMachine;
        public new TStateID StateID => (TStateID) stateID;

        public State(TStateID targetStateID) : base(targetStateID)
        {
        }

        public void SetStateMachine(StateMachine<TStateID> targetStateMachine)
        {
            StateMachine = targetStateMachine;
        }
    }
}

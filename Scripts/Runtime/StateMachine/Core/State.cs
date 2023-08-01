using System;

namespace BrunoMikoski.Core.StateMachines
{
    [Serializable]
    public class State
    {
        protected StateMachineBase StateMachine;

        private StateID stateID;
        public StateID StateID => stateID;


        public State(StateID targetStateID)
        {
            stateID = targetStateID;
        }

        public void SetStateMachine(StateMachineBase targetStateMachine)
        {
            StateMachine = targetStateMachine;
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
}
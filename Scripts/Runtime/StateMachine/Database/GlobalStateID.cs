using System;
#if UNITASK_AVAILABLE
using Cysharp.Threading.Tasks;
#endif

namespace BrunoMikoski.Core.DevelopmentTools.StateMachines
{
    public abstract class GlobalStateID : StateID
    {
        public event Action OnEnterEvent
        {
            add => StateMachineRegistry.SubscribeToGlobalEvent(StateMachineEvent.StateEnter, this, value);
            remove => StateMachineRegistry.UnsubscribeFromGlobalEvent(StateMachineEvent.StateEnter, this, value);
        }
        
        public event Action OnExitEvent
        {
            add => StateMachineRegistry.SubscribeToGlobalEvent(StateMachineEvent.StateExit, this, value);
            remove => StateMachineRegistry.UnsubscribeFromGlobalEvent(StateMachineEvent.StateExit, this, value);
        }
        
        public event Action OnTransitionOutEvent
        {
            add => StateMachineRegistry.SubscribeToGlobalEvent(StateMachineEvent.BeginTransitionFrom, this, value);
            remove => StateMachineRegistry.UnsubscribeFromGlobalEvent(StateMachineEvent.BeginTransitionFrom, this, value);
        }

        public event Action OnTransitionInEvent
        {
            add => StateMachineRegistry.SubscribeToGlobalEvent(StateMachineEvent.BeginTransitionTo, this, value);
            remove => StateMachineRegistry.UnsubscribeFromGlobalEvent(StateMachineEvent.BeginTransitionTo, this, value);
        }



        [NonSerialized]
        private bool hasCachedStateMachine;
        private StateMachineBase cachedStateMachine;
        private StateMachineBase StateMachine
        {
            get
            {
                if (!hasCachedStateMachine || !StateMachineRegistry.IsRegistered(cachedStateMachine))
                {
                    hasCachedStateMachine = StateMachineRegistry.TryGetStateMachineByStateType(this.GetType(), out cachedStateMachine);
                }
                return cachedStateMachine;
            }
        }
        
        public bool IsOnState
        {
            get
            {
                if (StateMachine != null)
                {
                    if (StateMachine.CurrentState != null && StateMachine.CurrentState.StateID == this)
                        return true;
                }

                return false;
            }   
        }
        
#if UNITASK_AVAILABLE
        public async UniTask WaitStateEnterAsync()
        {
            while (StateMachine == null || StateMachine.CurrentState.StateID != this)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }
#endif

    }
}
